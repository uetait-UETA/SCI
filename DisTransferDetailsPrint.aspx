<%@ Page Language="C#" AutoEventWireup="true" CodeFile="DisTransferDetailsPrint.aspx.cs" Inherits="DisTransferDetailsPrint" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Dispatch/Received Document</title>
    <link href="Styles/StyleSheet.css" rel="stylesheet" type="text/css" />
    <style type="text/css">
        * { box-sizing: border-box; }
        body { font-family: Arial, Helvetica, sans-serif; font-size: 10pt; color: #000; background: #fff; margin: 0; padding: 0; }
        #divContent { margin: 16px auto; width: 98%; max-width: 1060px; }
        #Button1 { margin: 12px 0 0 14px; padding: 5px 18px; background: #fff; color: #000; border: 0.5px solid #555; font-size: 9pt; font-weight: bold; cursor: pointer; border-radius: 3px; }
        #Button1:hover { background: #f0f0f0; }

        /* ── Document page ── */
        .doc-page { border: 0.3px solid #bbb; margin-bottom: 22px; }

        /* Title bar */
        .doc-title-bar { border-bottom: 0.5px solid #000; padding: 10px 18px; display: table; width: 100%; }
        .doc-title-l { display: table-cell; font-size: 14pt; font-weight: bold; letter-spacing: .5px; vertical-align: middle; }
        .doc-title-r { display: table-cell; text-align: right; font-size: 11pt; font-weight: bold; vertical-align: middle; }

        /* Info row */
        .doc-info-row { display: table; width: 100%; border-bottom: 0.3px solid #bbb; }
        .doc-info-left  { display: table-cell; padding: 12px 18px; vertical-align: top; }
        .doc-info-right { display: table-cell; padding: 12px 18px; vertical-align: middle; text-align: right; width: 185px; }
        .field-group { margin-bottom: 6px; }
        .field-label { font-size: 7pt; font-weight: bold; text-transform: uppercase; letter-spacing: .4px; }
        .field-value { font-size: 10pt; margin-top: 1px; }

        /* Signatures */
        .sig-section { padding: 10px 18px 12px; border-bottom: 0.3px solid #bbb; }
        .sig-title { font-size: 7.5pt; font-weight: bold; text-transform: uppercase; letter-spacing: .3px; margin-bottom: 8px; }
        .sig-entry { display: inline-block; margin-right: 24px; margin-bottom: 6px; font-size: 9.5pt; }
        .sig-line { display: inline-block; width: 118px; border-bottom: 0.5px solid #000; margin-left: 5px; vertical-align: bottom; height: 14px; }
        .notes-line { font-size: 9.5pt; }
        .notes-line .sig-line { width: 480px; }

        /* Items table — horizontal lines only, no vertical grid */
        .items-wrap { padding: 4px 18px 16px; }
        table.doc-table { width: 100%; border-collapse: collapse; margin-top: 10px; border-top: 0.5px solid #555; }
        table.doc-table th { background: #fff; color: #000; padding: 6px 8px; font-size: 7.5pt; font-weight: bold; text-transform: uppercase; letter-spacing: .3px; border: none; border-bottom: 0.5px solid #555; text-align: center; }
        table.doc-table td { padding: 5px 8px; border: none; border-bottom: 0.3px solid #ccc; font-size: 8.5pt; vertical-align: middle; }
        .pg-footer { text-align: center; font-size: 8pt; color: #555; padding: 8px 0 2px; }

        @page { size: landscape; margin: 10mm; }

        @media print {
            #divContent { margin: 0; width: 100%; max-width: none; }
            #Button1 { display: none; }
            H1.SaltoDePagina { PAGE-BREAK-AFTER: always; }
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
