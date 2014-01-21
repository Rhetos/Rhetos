(function ($) {

    var opts = null;
    var selected_pricipal_id = '';
    var selected_pricipal_name = '';

    var methods = {
        init: function (options) {
            opts = $.extend({}, $.fn.rhetosPermissionsUI.defaults, options);

            jQuery.support.cors = true;

            buildElementsTree(this);
            bindElementsTreeEvents();
            getPrincipals();
            getClaims();
        }
    };

    $.fn.rhetosPermissionsUI = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist on jQuery.rhetosPermissionsUI');
        }
    };

    function ajaxGet(uri, successCallback, async) {
        $.ajax({
            url: opts.url + uri,
            dataType: 'json',
            data: 'format=json',
            cache: false,
            async: async,
            success: function (data) {
                successCallback(data);
            },
            error: function (jqXHR, textStatus, errorThrown) {
                var msg = jqXHR.responseText
                try { msg = JSON.parse(msg); } catch (e) {}
                alert('ERROR:\n\n' + msg);
            }
        });
    }

    function ajaxPost(uri, data, successCallback, async) {
        $.ajax({
            url: opts.url + uri,
            type: 'POST',
            dataType: 'json',
            async: async,
            contentType: "application/json; charset=utf-8",
            data: data,
            success: successCallback,
            error: function (jqXHR, textStatus, errorThrown) {
                var msg = jqXHR.responseText
                try { msg = JSON.parse(msg); } catch (e) {}
                alert('ERROR:\n\n' + msg);
            }
        });
    }

    function getPrincipals() {
        ajaxGet('/principals', refreshPrincipalsList, true);
    };

    function getClaims() {
        ajaxGet('/claims', refreshClaimsTable, true);
    };

    function getPrincipalPermissions() {
        ajaxGet('/principals/' + selected_pricipal_id + '/permissions',
        function (data) {
            setPrincipalPermissions(data);

            setHeaderTitle();

            $('#apui_widget_start').hide();
            $('#apui_permissions').show();

            $('#filterQuery').width($('#apui_permissions table').width() - 6 - 5);
            $('#filterQuery').focus();
        },
        false);
    };

    function addPrincipal() {

        var name = $('#principal_name').val();

        if (name == null || name == '') return;

        ajaxPost('/principals/create', JSON.stringify({ "name": name }), getPrincipals, false);

        $('#principal_name').val('');
        $('#principal_name').focus();
    };

    function updatePrincipalName() {

        var name = window.prompt('Enter new principal name:', '');

        if (name == null || name == '') return;

        ajaxPost('/principals/' + selected_pricipal_id + '/update', JSON.stringify({ "name": name }),
            function (data) {
                selected_pricipal_name = name;
                setHeaderTitle();
                getPrincipals(data);
            },
            false);
    };

    function deletePrincipal() {

        if (!confirm('Are you sure you want to remove principal \'' + selected_pricipal_name + '\'?')) return;

        ajaxPost('/principals/' + selected_pricipal_id + '/delete', null, function () {
            hidePrincipalsPermissions();
        },
        false);

        getPrincipals();
    };

    function applyPrincipalPermission(claimId, isAuthorized) {

        ajaxPost('/principals/' + selected_pricipal_id + '/permissions', JSON.stringify({ "claimId": claimId, "isAuthorized": isAuthorized }), null, false);
    };

    function refreshPrincipalsList(data) {

        $('#apui_principals ul').remove();
        $('#apui_principals').append('<ul />')

        $.map(data, function (item, index) {
            $('#apui_principals ul').append('<li><button id=principal_id_' + item.ID + '>' + item.Name + '</button></li>');
        });

        $("#apui_principals ul button").button().click(function (event, ui) {
            event.preventDefault();
            managePrincipal($(this).attr('id'), $(this).find('span').html());
        });
    };

    function refreshClaimsTable(data) {

        $('#apui_permissions table tbody').remove();
        $('#apui_permissions table').append('<thead><tr><th>Resource</th><th>Action authorizations</th></tr></thead>');
        $('#apui_permissions table').append('<tbody />');

        var i = 0;
        var el = null;
        var resource = '';

        $.map(data, function (item, index) {

            if (item.ClaimResource != resource) {
                $('#apui_permissions table tbody').append('<tr id="row' + i + '"><td>' + item.ClaimResource + '</td><td></td>' + '</tr>');

                el = $('#apui_permissions table tbody #row' + i + ' td:eq(1)');

                i++;
                resource = item.ClaimResource;
            }

            el.append('<div id="claim_id_' + item.ID + '"> ' + item.ClaimRight + '</div>');
            $('#' + 'claim_id_' + item.ID).rhetosTriStateCheckbox({ checkedIcon: 'Img/checked_highlighted.gif', uncheckedIcon: 'Img/unchecked_highlighted.gif', indeterminatedIcon: 'Img/intermediate_highlighted.gif' });
            $('#' + 'claim_id_' + item.ID).click(function (event) {

                var claim_id = event.target.id.replace('claim_id_', ''); ;
                var state = $(this).rhetosTriStateCheckbox('state');
                var isAuthorized = '';

                if (state == 'checked') {
                    isAuthorized = 'true';
                }
                else if (state == 'unchecked') {
                    isAuthorized = 'false';
                }
                else if (state == 'intermediate') {
                    isAuthorized = 'null';
                }
                else {
                    return;
                }

                applyPrincipalPermission(event.target.id.replace('claim_id_', ''), isAuthorized);
            });
        });

        $("#apui_permissions table").tablesorter({ sortList: [[0, 0]] });
    };

    function managePrincipal(id, name) {

        if (id == null || id == '') return;

        selected_pricipal_id = id.replace('principal_id_', '');
        selected_pricipal_name = name;

        getPrincipalPermissions();
    }

    function setPrincipalPermissions(data) {

        $('#apui_permissions table tbody tr td div').rhetosTriStateCheckbox('state', 'intermediate');

        $.map(data, function (item, index) {
            $('#apui_permissions table tbody tr td #claim_id_' + item.ClaimID).rhetosTriStateCheckbox('state', item.IsAuthorized ? 'checked' : 'unchecked');
        });
    };

    function hidePrincipalsPermissions() {

        selected_pricipal_id = '';
        selected_pricipal_name = '';

        $('#apui_permissions').hide();
        $('#apui_widget_start').show();
        $("#principal_name").focus();
    };

    function setHeaderTitle() {
        $('#apui_permissions_head').html('<h2 class="ui-widget ui-corner-all ui-state-highlight">Manage authorizations for <span id="principal_name">' + selected_pricipal_name + '</span></h2>');
    };

    function filterClaimsTable() {
        var query = $('#filterQuery').val();

        if (query == null || query == '') {
            $('#apui_permissions table tr').show();
        }
        else {
            $('#apui_permissions table tbody tr').each(function (index) {

                var resource = $(this).find('td:first').text();

                if (resource != null && resource != '') {
                    if (resource.toLowerCase().indexOf(query.toLowerCase()) == -1) {
                        $(this).hide();
                    }
                    else {
                        $(this).show();
                    }
                }
            });
        }
    };

    function buildElementsTree(host) {
        $(host).append('<div id="apui_widget" />');
        $('#apui_widget').addClass("ui-widget");

        $('#apui_widget').append('<div id="apui_widget_head" />');
        $('#apui_widget_head').append('<h2 class="inline-block"><img alt="" src="Img/lock.png" class="inline-block" />Authorization administration</h2>')

        $('#apui_widget').append('<div id="apui_principals" />');
        $('#apui_principals').addClass("inline-block");
        $('#apui_principals').append('<h3>Principals:</h3>');

        $('#apui_widget').append('<div id="apui_widget_start" />');
        $('#apui_widget_start').addClass("inline-block");
        $('#apui_widget_start').addClass("ui-widget ui-corner-all ui-state-highlight");
        $('#apui_widget_start').append('<h2>Welcome to Rhetos authorization administration</h2>')
        $('#apui_widget_start').append('<p>To add new principal enter name in textbox below and click <strong>Add principal</strong>. After that you will be able to manage action authorizations for resources.<p>')
        $('#apui_widget_start').append('<input type="text" id="principal_name" />')

        $('#apui_widget_start').append('<button>Add principal</button>')
        $('#apui_widget_start button').button();
        $('#apui_widget_start button').button("option", "disabled", true);

        $('#apui_widget_start').append('<p>To manage action authorization for existing principals click button with principal name on left side.<p>')
        $('#principal_name').height($('#apui_widget_start button').height() - 4).width(400);

        $('#apui_widget').append('<div id="apui_permissions" />');
        $('#apui_permissions').addClass("inline-block");
        $('#apui_permissions').addClass("ui-corner-all");
        $('#apui_permissions').append('<div id="close" class="ui-icon ui-icon-circle-close"></div>')
        $('#apui_permissions').append('<div id="apui_permissions_head" />')
        $('#apui_permissions').append('<div><button id="rename_principal">Rename principal</button> <button id="remove_principal">Remove principal</button></div>');
        $('#rename_principal').button({ icons: { primary: "ui-icon-document-b"} });
        $('#remove_principal').button({ icons: { primary: "ui-icon-cancel"} });
        $('#apui_permissions').append('<input id="filterQuery" type="text" />')
        $('#apui_permissions').append('<table />')

        $('#principal_name').focus();
        $('#apui_permissions').hide();
    };

    function bindElementsTreeEvents() {
        $('#principal_name').keyup(function (event) {
            $('#apui_widget_start button').button("option", "disabled", !($('#principal_name').val().length > 0));
        });

        $('#principal_name').keydown(function (event) {
            if (event.keyCode == 13) {
                event.preventDefault();
                addPrincipal();
                return false;
            }

            return true;
        });

        $('#apui_widget_start button').keydown(function (event) {
            event.preventDefault();
            addPrincipal();
        });

        $('#apui_widget_start button').click(function (event) {
            event.preventDefault();
            addPrincipal();
        });

        $("#close").click(function () {
            hidePrincipalsPermissions();
        });

        $('#rename_principal').click(function (event) {
            event.preventDefault();
            updatePrincipalName();
        });

        $('#remove_principal').click(function (event) {
            event.preventDefault();
            deletePrincipal();
        });

        $('#filterQuery').keyup(function (event) {

            if (event.charCode == 13) return;

            event.preventDefault();
            filterClaimsTable();
        });
    };
})(jQuery);

$.fn.rhetosPermissionsUI.defaults = {
    url: ''
};
