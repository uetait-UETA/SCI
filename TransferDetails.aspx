<%@ Page Language="C#" AutoEventWireup="true" CodeFile="TransferDetails.aspx.cs" Inherits="TransferDetails" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Transfer Draft Document </title>
    <link href="BootStrap/css/bootstrap.css" rel="stylesheet" type="text/css" />
    <link href="Font-Awesome/css/font-awesome.css" rel="stylesheet" type="text/css" />
    <link href="Styles/StyleSheet.css" rel="stylesheet" type="text/css" />
    <link href="default.css" rel="stylesheet" type="text/css" />
     <style>
            tr td {font-size: 8pt }
    </style>
</head>
<body>
    <a href="javascript:CloseOnReload();" class="modalClose"></a>
    <form id="form1" runat="server">

    <div class="container">
        <div class="row">&nbsp;</div>
        <asp:Button ID="Button1" runat="server" onclick="Button1_Click"  
            Text="Print" Font-Size="Medium" 
            Height="40px" Width="102px" />
    
        <%--<input type="button" onclick="window.print()"  value="Imprimir" style="height:22px; width:70px; font-size:8pt; font-weight:bold;" />--%>
        <%--<div id="divContent" runat="server" style="width:649px; height: 18px;">copia</div>
        &nbsp;&nbsp;&nbsp;&nbsp; 
        <asp:PlaceHolder ID="plBarCode" runat="server" OnDataBinding="plBarCode_DataBinding" />--%>
           
    
        <table runat="server" align="left">        
            <tr>
                <td>
                     <div id="divContent" runat="server" style="width:649px; height: 18px;">copy</div>
                </td>
                <td >
                     <%--<asp:PlaceHolder ID="plBarCode" runat="server" OnDataBinding="plBarCode_DataBinding" />--%>
                </td>
            </tr>                               
                                                                                
        </table>       
    </div>
    </form>

    <script type="text/javascript">
        function GetRadWindow() {
            var oWindow = null;
            if (window.radWindow) oWindow = window.radWindow; //Will work in Moz in all cases, including clasic dialog
            else if (window.frameElement.radWindow) oWindow = window.frameElement.radWindow; //IE (and Moz az well) 
            return oWindow;
        }

        function CloseOnReload() {
            GetRadWindow().close();
        }

    </script>
</body>
</html>
