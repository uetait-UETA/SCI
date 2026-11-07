<%@ Page Title="Print Price Label" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="PrintPriceLabel.aspx.cs" Inherits="PrintPriceLabel" %>

<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">

    <div class="container" style="margin-left:20px;">
        <div class="row"><div class="col-md-12"><br /></div></div>
        <div class="row">
            <div class="col-md-8">
                <asp:Panel ID="pnlMain" runat="server" CssClass="page-curl shadow-bottom" DefaultButton="btnSearch">
                    <label id="labelForm" runat="server" class="PanelHeading">Print Price Label</label>
                    <div class="row">
                        <div class="col-md-10">
                            <ul class="myUL">
                                <li>
                                    <label class="myLabel">Item / Barcode</label>
                                    <asp:TextBox ID="tbItemCode" runat="server" Width="220px"
                                        style="vertical-align:middle;" />
                                    &nbsp;
                                    <asp:Button ID="btnSearch" runat="server" Text="Search"
                                        OnClick="btnSearch_Click" CssClass="mybtnmeduim" />
                                </li>
                                <li>
                                    <label class="myLabel">Printer</label>
                                    <asp:DropDownList ID="drpPrinter" runat="server" CssClass="myDdlExtraLarge" />
                                </li>
                                <li>
                                    <label class="myLabel">Quantity</label>
                                    <asp:TextBox ID="tbQty" runat="server" Text="1" Width="60px"
                                        style="text-align:center;"
                                        onkeypress="return event.charCode >= 49 && event.charCode <= 57 || (event.charCode === 48 && this.value.length > 0);"
                                        onpaste="return false;" />
                                </li>
                            </ul>
                        </div>
                    </div>
                    <div class="row">&nbsp;</div>
                </asp:Panel>
            </div>
        </div>

        <div class="row">&nbsp;</div>

        <!-- Item result panel -->
        <asp:Panel ID="pnlItem" runat="server" Visible="False">
            <div class="row">
                <div class="col-md-8">
                    <asp:Panel ID="pnlItemCard" runat="server" CssClass="page-curl shadow-bottom">

                        <!-- Label preview (1.5" × 1.0" scale ~3x) -->
                        <div style="border:2px solid #444; border-radius:3px; padding:6px 8px;
                                    background:#fff; width:290px; font-family:'Courier New',monospace;">
                            <div style="font-size:11px; color:#555; border-bottom:1px solid #ccc;
                                        padding-bottom:3px; margin-bottom:3px; white-space:nowrap; overflow:hidden;">
                                <asp:Label ID="lblItemCode" runat="server" Font-Bold="True" />
                                &nbsp;&nbsp;
                                <asp:Label ID="lblItemName" runat="server"
                                    style="font-size:11px; color:#333;" /></div>
                            <div>
                                <asp:Label ID="lblPrice" runat="server"
                                    style="font-size:32px; font-weight:bold; color:#1a237e; letter-spacing:1px;" />
                                &nbsp;
                                <asp:Label ID="lblCurrency" runat="server"
                                    style="font-size:12px; color:#777; vertical-align:bottom;" />
                            </div>
                            <!-- hidden key used by rblPriceList reload -->
                            <asp:Label ID="lblBarcode"    runat="server" Visible="False" />
                            <asp:Label ID="lblBranchName" runat="server" Visible="False" />
                        </div>

                        <div style="margin-top:16px;">
                            <asp:Button ID="btnPrint" runat="server" Text="Print Label"
                                OnClick="btnPrint_Click" CssClass="mybtnmeduim"
                                style="background:#1565c0; color:white; border-color:#1565c0; font-size:15px; padding:8px 20px;" />
                        </div>

                        <div style="margin-top:10px;">
                            <asp:Label ID="lblResult" runat="server" Font-Bold="True" Font-Size="Medium" />
                        </div>

                    </asp:Panel>
                </div>
            </div>
        </asp:Panel>

        <!-- Message (errors before item found) -->
        <div class="row">
            <div class="col-md-8">
                <asp:Label ID="lblMessage" runat="server" Font-Bold="True" Font-Size="Medium" />
            </div>
        </div>
    </div>

    <script type="text/javascript">
        // Auto-focus the item code box on load
        window.onload = function () {
            var tb = document.getElementById('<%= tbItemCode.ClientID %>');
            if (tb) tb.focus();
        };
    </script>

</asp:Content>
