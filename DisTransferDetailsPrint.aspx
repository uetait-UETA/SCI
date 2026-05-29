<%@ Page Language="C#" AutoEventWireup="true" CodeFile="DisTransferDetailsPrint.aspx.cs" Inherits="DisTransferDetailsPrint" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Dispatch/Received Document</title>
    <link href="Styles/StyleSheet.css" rel="stylesheet" type="text/css" />
    <style type="text/css">
        #divContent {
            margin-left: auto;
            margin-right:auto;
            padding-top: 52px;
            width: 95%;
        }
        #Button1 {
            position: relative;
            top: 35px;
            left: 15px;
        }
        .printHeader {
            font-weight:bold;
            text-decoration:underline;
            font-size: 9pt;
            color:white;
            background-color:black;
        }
        .tblHeader {
            width:99%; 
            font-family: Arial, Helvetica, sans-serif;
            font-size:8pt;
            padding-top: 26px;
        }
        .tblHeaderTitle {
            font-weight:bold;
            color:Red;
            font-size:9pt;
            text-align: center;
        }
        .printRow, .printData {
            font-family: Arial, Helvetica, sans-serif;
            font-size: 8pt;
        }
        .printDataBottom {
            border-bottom: 0.2px solid Black;
            padding:0;
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
                padding-top: 26px;
            }
            .tblHeaderTitle {
                font-weight:bold;
                color:Black;
                font-size:7.7pt;
            }
            .printRow, .printData {
                font-family: Arial, Helvetica, sans-serif;
                font-size: 7pt;
            }
            .printDataBottom {
                border-bottom: 0.1px thin black;
                padding:0;
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
            H1.SaltoDePagina {
                PAGE-BREAK-AFTER: always;
            }
        }
    </style>
</head>
<body>
    <a href="javascript:CloseOnReload();" class="modalClose"></a>
    <form id="form1" runat="server">
        <%--<div id="barCodeContainer">
            <asp:PlaceHolder ID="plBarCode" runat="server" />
        </div>--%>
        <div>
            <input type="button" onclick="window.print()" value="Print" style="height:22px; width:70px; font-size:8pt; font-weight:bold;" id="Button1" />
        </div>
        <div style="width:100%; padding:0; margin:0;">
            <div id="divContent" runat="server"></div>
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
