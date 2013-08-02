<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Inherits="Rhetos.Default" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link type="text/css" rel="Stylesheet" href="Css/rhetos-permissons-ui.css" />
    <script type="text/javascript" src="Js/rhetos-tri-state-checkbox.js"></script>
    <script type="text/javascript" src="Js/rhetos-permissons-ui.js"></script>
    <script type="text/javascript" src="Js/jquery.tablesorter.min.js"></script>
    <script type="text/javascript">
        $(window).keydown(function (event) {
            if (event.keyCode == 13) {
                event.preventDefault();
                return false;
            }
        });

        $(document).ready(function () {
            $('#premissions-ui').rhetosPermissionsUI({ url: 'SecurityRestService.svc' });
        });
    </script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div id="premissions-ui" class="ui-corner-all">
    </div>
</asp:Content>
