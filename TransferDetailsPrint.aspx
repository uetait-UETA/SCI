<%@ Page Language="C#" AutoEventWireup="true" CodeFile="TransferDetailsPrint.aspx.cs" Inherits="TransferDetailsPrint" %>


<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Transfer Draft Document</title>
    <link href="Styles/StyleSheet.css" rel="stylesheet" type="text/css" />
    <style type="text/css">
        #divContent {
            padding-left: 40px;
            padding-top: 52px;
            width: 65%;
        }
        #Button1 {
            position: relative;
            top: 25px;
            right:33%;  
            float:right;
        }
        .printHeader {
            font-weight:bold;
            text-decoration:underline;
            font-size: 8.25pt;
            color:white;
            background-color:black;
        }
        .tblHeader {
            width:99%; 
            font-family: Arial, Helvetica, sans-serif;
            font-size:8pt;
        }
        .tblHeaderTitle {
            font-weight:bold;
            color:Red;
            font-size:9pt;
        }
        .printRow, .printData {
            font-family: Arial, Helvetica, sans-serif;
            font-size: 8.25pt;
            padding-bottom: 0px;
            margin-bottom: 0px;
            padding-top: 0px;
            margin-top: 0px;
        }
        .printDataBottom {
            border-bottom: 0.2px solid Black;
            padding:0;
            margin:0;
        }
        .barCodeImg {
            position: relative;
            padding:0;
            margin:0;
            right:-47px;
        }
        .barCodeContainer {
            text-align: right;
            vertical-align:top;
            overflow: hidden;
        }

        @media print {
            .modalClose {
                display:none;
            }
            #divContent {
                width:100%;
                padding: 0;
                margin:0;
            }
            #Button1 {
                display:none;
            }
            .printHeader {
                font-weight:bold;
                text-decoration:underline;
                font-size: 8pt;
                color:black;
                background-color:white;
            }
            .tblHeader {
                width:100%; 
                font-family: Arial, Helvetica, sans-serif;
                font-size:7pt;
            }
            .tblHeaderTitle {
                font-weight:bold;
                color:Black;
                font-size:7.7pt;
            }
            .printRow, .printData {
                font-family: Arial, Helvetica, sans-serif;
                font-size: 7pt;
                padding-bottom: 0px;
                margin-bottom: 0px;
                padding-top: 0px;
                margin-top: 0px;
            }
            .printDataBottom {
                border-bottom: 0.1px thin black;
                padding:0;
                margin:0;
            }
            .barCodeImg {
                position: relative;
                padding:0;
                margin:0;
            }
            .barCodeContainer {
                text-align: right;
                vertical-align:top;
                overflow: hidden;
            }
            H1.SaltoDePagina {
                PAGE-BREAK-AFTER: always;
            }
        }
    </style>
</head>
<body>
    <a href="javascript:CloseOnReload();" class="modalClose"></a>
    <%--<div id="barCodeContainer">
        <asp:PlaceHolder ID="plBarCode" runat="server"  />
    </div>--%>
    <form id="form2" runat="server">
        <div style="width:100%; padding:0; margin:0;">
            <asp:Button ID="Button1" ClientIDMode="Static" runat="server" onclick="Button1_Click"  
                Text="Print" Font-Size="Medium" 
                Height="40px" Width="102px" />

            <div id="divContent" runat="server">copia</div>
            
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