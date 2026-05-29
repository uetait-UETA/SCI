<%@ Page Language="C#" AutoEventWireup="true" CodeFile="DisTransferDetailsPrint.aspx.cs" Inherits="DisTransferDetailsPrint" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Dispatch/Received Document</title>
    <link href="Styles/StyleSheet.css" rel="stylesheet" type="text/css" />
    <style type="text/css">
        * { box-sizing: border-box; }
        body { font-family: Arial, Helvetica, sans-serif; font-size: 10pt; color: #222; background: #eef1f5; margin: 0; padding: 0; }
        #divContent { margin: 16px auto; width: 98%; max-width: 1060px; }
        #Button1 { margin: 12px 0 0 14px; padding: 5px 18px; background: #1e3a5f; color: white; border: none; font-size: 9pt; font-weight: bold; cursor: pointer; border-radius: 3px; }
        #Button1:hover { background: #2a4f82; }

        /* ── Document page card ── */
        .doc-page { background: #fff; border: 1px solid #c5cad4; box-shadow: 0 2px 8px rgba(0,0,0,.11); margin-bottom: 22px; border-radius: 4px; overflow: hidden; }

        /* Title bar */
        .doc-title-bar { background: #1e3a5f; color: #fff; padding: 11px 18px; display: table; width: 100%; }
        .doc-title-l { display: table-cell; font-size: 14pt; font-weight: bold; letter-spacing: .6px; vertical-align: middle; }
        .doc-title-r { display: table-cell; text-align: right; font-size: 11pt; font-weight: bold; opacity: .88; vertical-align: middle; }

        /* Info row */
        .doc-info-row { display: table; width: 100%; border-bottom: 2px solid #1e3a5f; }
        .doc-info-left  { display: table-cell; padding: 14px 18px; vertical-align: top; }
        .doc-info-right { display: table-cell; padding: 14px 18px; vertical-align: middle; text-align: right; width: 185px; }
        .field-group { margin-bottom: 7px; }
        .field-label { font-size: 7pt; font-weight: bold; color: #1e3a5f; text-transform: uppercase; letter-spacing: .4px; }
        .field-value { font-size: 10pt; color: #222; margin-top: 1px; }

        /* Signatures */
        .sig-section { padding: 10px 18px 13px; border-bottom: 1px solid #e2e6ed; }
        .sig-title { font-size: 7.5pt; font-weight: bold; color: #1e3a5f; text-transform: uppercase; letter-spacing: .3px; margin-bottom: 8px; }
        .sig-entry { display: inline-block; margin-right: 24px; margin-bottom: 6px; font-size: 9.5pt; }
        .sig-line { display: inline-block; width: 118px; border-bottom: 1px solid #444; margin-left: 5px; vertical-align: bottom; height: 14px; }
        .notes-line { font-size: 9.5pt; }
        .notes-line .sig-line { width: 480px; }

        /* Items table */
        .items-wrap { padding: 4px 18px 16px; }
        table.doc-table { width: 100%; border-collapse: collapse; margin-top: 10px; }
        table.doc-table th { background: #1e3a5f; color: #fff; padding: 7px 8px; font-size: 7.5pt; text-transform: uppercase; letter-spacing: .3px; border: 1px solid #16304f; text-align: center; }
        table.doc-table td { padding: 5px 8px; border: 1px solid #dde2e9; font-size: 8.5pt; vertical-align: middle; }
        table.doc-table tr.row-alt td { background: #f2f6ff; }
        .pg-footer { text-align: center; font-size: 8pt; color: #666; padding: 8px 0 2px; }

        @media print {
            body { background: #fff; }
            #divContent { margin: 0; width: 100%; max-width: none; }
            #Button1 { display: none; }
            .doc-page { box-shadow: none; border: 1px solid #ccc; border-radius: 0; }
            .doc-title-bar { -webkit-print-color-adjust: exact; print-color-adjust: exact; }
            table.doc-table th { -webkit-print-color-adjust: exact; print-color-adjust: exact; }
            table.doc-table tr.row-alt td { -webkit-print-color-adjust: exact; print-color-adjust: exact; }
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
