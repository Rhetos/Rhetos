(function ($) {

    var opts;

    var methods = {
        init: function (options) {

            opts = $.extend({}, $.fn.rhetosTriStateCheckbox.defaults, options);

            $(this).prepend('<img />');
            setIcon($(this), opts.state);

            $(this).find('img').attr('id', $(this).attr('id'));

            $(this).click(function () {

                var state = getState($(this));

                if (state == 'intermediate') {
                    state = 'checked';
                }
                else if (state == 'checked') {
                    state = 'unchecked';
                }
                else if (state == 'unchecked') {
                    state = 'intermediate';
                }
                else {
                    return;
                }

                setIcon($(this), state);
            });
        },

        state: function (state) {
            if (state != null) {
                setIcon($(this), state);
            }
            else {
                return getState($(this));
            }
        }
    };

    function setIcon(el, state) {

        var icon = '';

        if (state == 'intermediate') {
            icon = opts.indeterminatedIcon;
        }
        else if (state == 'checked') {
            icon = opts.checkedIcon;
        }
        else if (state == 'unchecked') {
            icon = opts.uncheckedIcon;
        }
        else {
            return;
        }
                
        if($(el).is('div')) {
            el.find('img').attr('src', icon);
        }
    }

    function getState(el) {

        var icon = el.find('img').attr('src');
        var state = '';

        if (icon == opts.indeterminatedIcon) {
            state = 'intermediate'
        }
        else if (icon == opts.checkedIcon) {
            state = 'checked'
        }
        else if (icon == opts.uncheckedIcon) {
            state = 'unchecked'
        }
        else {
            return;
        }

        return state;
    }

    $.fn.rhetosTriStateCheckbox = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist on jQuery.triStateCheckbox');
        }
    };
})(jQuery);

$.fn.rhetosTriStateCheckbox.defaults = {
    state: 'intermediate',
    checkedIcon: '',
    uncheckedIcon: '',
    indeterminatedIcon: ''
};
