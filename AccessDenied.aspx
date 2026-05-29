<%@ Page Language="C#" AutoEventWireup="true" CodeFile="AccessDenied.aspx.cs" Inherits="AccessDenied" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Access Denied</title>
    <meta http-equiv="X-UA-Compatible" content="IE=edge" />
    <link href="BootStrap/css/bootstrap.css" rel="stylesheet" type="text/css" />
    <link href="Font-Awesome/css/font-awesome.css" rel="stylesheet" type="text/css" />
    <link href="Styles/StyleSheet.css" rel="stylesheet" type="text/css" />
    <!--[if gte IE 9]
<style type="text/css">
.gradient {
filter: none;
}
</style>
<![endif]-->
</head>
<body class="AccessDenied" style="text-align:center; vertical-align:middle;">
    <form id="form1" runat="server">
        <div class="row">&nbsp;</div>
        <tel:RadScriptManager ID="RadScriptManager1" runat="server"></tel:RadScriptManager>
        <tel:RadSkinManager ID="QsfSkinManager" runat="server" ShowChooser="false" Skin="Windows7" />

        <tel:RadAjaxLoadingPanel ID="ralpMain" runat="server" Height="75px" Width="75px" Transparency="20"
            IsSticky="false"
            Style="position: absolute; top: 0; left: 0; height: 100%; width: 100%;">
        </tel:RadAjaxLoadingPanel>
        <tel:RadAjaxPanel ID="rapMain" runat="server" LoadingPanelID="ralpMain">
            <div class="row">
                <div class="col-md-12">
                    <h1 style="color:white;">
                        Cannot connect to the SQL Server. Our team is working on it. Please try again later!
                    </h1>
                    <%--<h2 style="color:white;">
                        Please contact MIS Helpdesk to get access!
                    </h2>--%>
                </div>
            </div>
        </tel:RadAjaxPanel>
    </form>
    <script src="http://code.jquery.com/jquery-1.11.0.min.js" type="text/javascript"></script>
    <script src="BootStrap/js/bootstrap.min.js" type="text/javascript"></script>
</body>
</html>
