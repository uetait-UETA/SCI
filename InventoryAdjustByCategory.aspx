<%@ Page Title="Inventory Adjustment by Category" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="InventoryAdjustByCategory.aspx.cs" Inherits="InventoryAdjustByCategory" %>

<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">

    <div class="container" style="margin-left: 20px;">
        <div class="row">
            <div class="col-md-12"><br /></div>
        </div>
        <div class="row">
            <div class="col-md-10">
                <asp:Panel ID="pnlMain" runat="server" CssClass="page-curl shadow-bottom">
                    <label id="labelForm" runat="server" class="PanelHeading">Inventory Adjustment by Category</label>
                    <div class="row">
                        <div class="col-md-7">
                            <ul class="myUL">
                                <li>
                                    <label class="myLabel">Type</label>
                                    <asp:RadioButtonList ID="rblType" runat="server" RepeatDirection="Horizontal"
                                        style="display:inline-block; vertical-align:middle;" AutoPostBack="True"
                                        OnSelectedIndexChanged="rblType_SelectedIndexChanged">
                                        <asp:ListItem Text="Goods Receipt (OIGN)" Value="OIGN" Selected="True" />
                                        <asp:ListItem Text="&nbsp;&nbsp;Goods Issue (OIGE)" Value="OIGE" />
                                    </asp:RadioButtonList>
                                </li>
                                <li>
                                    <label class="myLabel">Warehouse</label>
                                    <asp:DropDownList ID="drpWhsCode" runat="server"
                                        DataTextField="WHS" DataValueField="WhsCode"
                                        CssClass="myDdlExtraLarge" />
                                </li>
                                <li>
                                    <label class="myLabel">Category</label>
                                    <asp:DropDownList ID="drpCategory" runat="server"
                                        DataTextField="GroupName" DataValueField="GroupCode"
                                        CssClass="myDdlLarge" AutoPostBack="True"
                                        OnSelectedIndexChanged="drpCategory_SelectedIndexChanged" />
                                </li>
                                <li>
                                    <label class="myLabel">Brand</label>
                                    <asp:DropDownList ID="drpBrand" runat="server" CssClass="myDdlLarge" />
                                </li>
                                <li>
                                    <label class="myLabel">&nbsp;</label>
                                    <asp:Button ID="btnLoad" runat="server" Text="Load Items"
                                        OnClick="btnLoad_Click" CssClass="mybtnmeduim" />
                                    &nbsp;
                                    <asp:Button ID="btnPreview" runat="server" Text="Preview"
                                        CssClass="mybtnmeduim"
                                        style="display:none; background:#1565c0; color:white; border-color:#1565c0;"
                                        OnClientClick="return previewItems();" />
                                    &nbsp;
                                    <asp:Panel ID="pnlCreate" runat="server" style="display:none;">
                                        <asp:Button ID="btnCreate" runat="server" Text="Create"
                                            OnClick="btnCreate_Click"
                                            OnClientClick="return confirm('WARNING!!\n\nAre you sure you want to create this inventory adjustment in SAP B1?\n\nClick OK to confirm or Cancel to abort.')"
                                            CssClass="mybtnmeduim"
                                            style="background:#2e7d32; color:white; border-color:#2e7d32;" />
                                    </asp:Panel>
                                </li>
                            </ul>
                        </div>
                    </div>
                    <div class="row">&nbsp;</div>
                </asp:Panel>
            </div>
        </div>

        <div class="row">&nbsp;</div>
        <div class="row">
            <div class="col-md-12">
                <asp:Label ID="lblMessage" runat="server" Font-Bold="True" Font-Size="Medium" />
            </div>
        </div>
        <div class="row">&nbsp;</div>
        <div class="row">
            <div class="col-md-10">
                <asp:GridView ID="gvItems" runat="server" CssClass="GridViewPanel"
                    EnableModelValidation="False" ForeColor="#333333"
                    EnableTheming="False" HorizontalAlign="Left"
                    AutoGenerateColumns="False" EnableViewState="True">
                    <AlternatingRowStyle BackColor="White" ForeColor="#284775" />
                    <HeaderStyle BackColor="#000084" Font-Bold="True" ForeColor="White" />
                    <RowStyle BackColor="#EEEEEE" ForeColor="#333333" />
                    <Columns>
                        <asp:BoundField DataField="ItemCode"   HeaderText="Item Code"   HeaderStyle-Width="100px" ReadOnly="True" />
                        <asp:BoundField DataField="U_CSKU_ID" HeaderText="CSKU ID"     HeaderStyle-Width="90px"  ReadOnly="True" />
                        <asp:BoundField DataField="ItemName"  HeaderText="Description" HeaderStyle-Width="340px" ReadOnly="True" />
                        <asp:BoundField DataField="OnHand" HeaderText="On Hand" DataFormatString="{0:N0}"
                            HeaderStyle-Width="70px" ItemStyle-HorizontalAlign="Right" ReadOnly="True" />
                        <asp:BoundField DataField="AvgPrice" HeaderText="Avg Price" DataFormatString="{0:N2}"
                            HeaderStyle-Width="80px" ItemStyle-HorizontalAlign="Right" ReadOnly="True" />
                        <asp:TemplateField HeaderText="Quantity" HeaderStyle-Width="90px" HeaderStyle-HorizontalAlign="Center">
                            <ItemTemplate>
                                <asp:HiddenField ID="hfItemCode" runat="server" Value='<%# Eval("ItemCode") %>' />
                                <asp:HiddenField ID="hfUType"    runat="server" Value='<%# Eval("U_Type")   %>' />
                                <asp:TextBox ID="tbQty" runat="server" Width="70px" style="text-align:right;"
                                    onkeypress="return event.charCode >= 49 && event.charCode <= 57 || (event.charCode === 48 && this.value.length > 0);"
                                    onpaste="return false;" />
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>
        </div>
    </div>

    <script type="text/javascript">
        function previewItems() {
            var grid = document.getElementById('<%= gvItems.ClientID %>');
            if (!grid) return false;
            var rows = grid.getElementsByTagName('tr');
            var count = 0;
            for (var i = 1; i < rows.length; i++) {
                var tb = rows[i].querySelector('input[type="text"]');
                var qty = tb ? (parseInt(tb.value, 10) || 0) : 0;
                if (qty > 0) count++;
            }
            if (count === 0) {
                alert('Please enter a quantity greater than 0 for at least one item.');
                return false;
            }
            for (var i = 1; i < rows.length; i++) {
                var tb = rows[i].querySelector('input[type="text"]');
                var qty = tb ? (parseInt(tb.value, 10) || 0) : 0;
                rows[i].style.display = qty > 0 ? '' : 'none';
            }
            var pnl = document.getElementById('<%= pnlCreate.ClientID %>');
            if (pnl) pnl.style.display = 'inline-block';
            return false;
        }
    </script>

</asp:Content>
