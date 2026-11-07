<%@ Page Title="Inventory Adjustment by Items" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="InventoryAdjustByItems.aspx.cs" Inherits="InventoryAdjustByItems" %>

<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">

    <div class="container" style="margin-left: 20px;">
        <div class="row">
            <div class="col-md-12"><br /></div>
        </div>
        <div class="row">
            <div class="col-md-10">
                <asp:Panel ID="pnlMain" runat="server" CssClass="page-curl shadow-bottom">
                    <label id="labelForm" runat="server" class="PanelHeading">Inventory Adjustment by Items</label>
                    <div class="row">
                        <div class="col-md-8">
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
                                        CssClass="myDdlExtraLarge"
                                        AutoPostBack="True"
                                        OnSelectedIndexChanged="drpWhsCode_SelectedIndexChanged"
                                        onchange="updateGtkVisibility(this);" />
                                </li>
                                <li>
                                    <label class="myLabel">Item / Barcode</label>
                                    <asp:TextBox ID="tbItemCode" runat="server" Width="200px"
                                        style="vertical-align:middle;" />
                                    &nbsp;
                                    <asp:Button ID="btnAddItem" runat="server" Text="Add Item"
                                        OnClick="btnAddItem_Click" CssClass="mybtnmeduim" />
                                </li>
                                <li>
                                    <label class="myLabel">GTK Confirmation</label>
                                    <asp:TextBox ID="tbGTKHeader" runat="server" Width="150px" MaxLength="50"
                                        style="background-color:lightgray;" />
                                </li>
                                <li>
                                    <label class="myLabel">&nbsp;</label>
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
                    AutoGenerateColumns="False" EnableViewState="True"
                    OnRowDeleting="gvItems_RowDeleting"
                    OnRowDataBound="gvItems_RowDataBound">
                    <AlternatingRowStyle BackColor="White" ForeColor="#284775" />
                    <HeaderStyle BackColor="#000084" Font-Bold="True" ForeColor="White" />
                    <RowStyle BackColor="#EEEEEE" ForeColor="#333333" />
                    <Columns>
                        <asp:BoundField DataField="ItemCode"   HeaderText="Item Code"   HeaderStyle-Width="100px" ReadOnly="True" />
                        <asp:BoundField DataField="U_CSKU_ID" HeaderText="CSKU ID"     HeaderStyle-Width="90px"  ReadOnly="True" />
                        <asp:BoundField DataField="ItemName"  HeaderText="Description" HeaderStyle-Width="240px" ReadOnly="True" />
                        <asp:BoundField DataField="OnHand"    HeaderText="On Hand"     DataFormatString="{0:N0}"
                            HeaderStyle-Width="70px" ItemStyle-HorizontalAlign="Right" ReadOnly="True" />
                        <asp:TemplateField HeaderText="Avg Price" HeaderStyle-Width="90px" HeaderStyle-HorizontalAlign="Center">
                            <ItemTemplate>
                                <asp:TextBox ID="tbAvgPrice" runat="server" Width="78px" CssClass="tbAvgPrice" style="text-align:right;"
                                    Text='<%# Convert.ToDecimal(Eval("AvgPrice")) > 0 ? Convert.ToDecimal(Eval("AvgPrice")).ToString("0.00") : "" %>'
                                    onkeypress="return event.charCode >= 48 && event.charCode <= 57 || (event.charCode === 46 && this.value.indexOf('.') < 0);"
                                    onpaste="return false;" />
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Quantity" HeaderStyle-Width="90px" HeaderStyle-HorizontalAlign="Center">
                            <ItemTemplate>
                                <asp:HiddenField ID="hfItemCode" runat="server" Value='<%# Eval("ItemCode") %>' />
                                <asp:HiddenField ID="hfUType"    runat="server" Value='<%# Eval("U_Type")   %>' />
                                <asp:TextBox ID="tbQty" runat="server" Width="70px" CssClass="tbQty" style="text-align:right;"
                                    Text='<%# Convert.ToInt32(Eval("Qty")) > 0 ? Eval("Qty").ToString() : "" %>'
                                    onkeypress="return event.charCode >= 49 && event.charCode <= 57 || (event.charCode === 48 && this.value.length > 0);"
                                    onpaste="return false;" />
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="" HeaderStyle-Width="40px" ItemStyle-HorizontalAlign="Center">
                            <ItemTemplate>
                                <asp:LinkButton ID="lbDelete" runat="server" CommandName="Delete"
                                    CausesValidation="False"
                                    OnClientClick="return confirm('Remove this item from the list?');"
                                    style="color:#c62828; font-weight:bold; font-size:16px; text-decoration:none;"
                                    ToolTip="Remove">&#x2715;</asp:LinkButton>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>
        </div>
    </div>

    <asp:HiddenField ID="hfWhsUType" runat="server" Value="" />

    <script type="text/javascript">
        function previewItems() {
            var grid = document.getElementById('<%= gvItems.ClientID %>');
            if (!grid) return false;
            var rows = grid.getElementsByTagName('tr');
            var whsType    = document.getElementById('<%= hfWhsUType.ClientID %>').value;
            var isDutyFree = whsType.toLowerCase() === 'duty free';

            // Pass 1: must have at least one row with qty > 0
            var count = 0;
            for (var i = 1; i < rows.length; i++) {
                var tb = rows[i].querySelector('input.tbQty');
                if (tb && (parseInt(tb.value, 10) || 0) > 0) count++;
            }
            if (count === 0) {
                alert('Please enter a quantity greater than 0 for at least one item.');
                return false;
            }

            // Pass 2: validate cost and GTK for rows with qty > 0
            var hasNoCost = false, hasNoGTK = false;
            for (var i = 1; i < rows.length; i++) {
                var tbQty = rows[i].querySelector('input.tbQty');
                var qty   = tbQty ? (parseInt(tbQty.value, 10) || 0) : 0;
                if (qty <= 0) continue;

                var tbCost = rows[i].querySelector('input.tbAvgPrice');
                var cost   = tbCost ? (parseFloat(tbCost.value) || 0) : 0;
                if (cost <= 0) hasNoCost = true;

                if (isDutyFree) {
                    var tbGTKH = document.getElementById('<%= tbGTKHeader.ClientID %>');
                    if (!tbGTKH || !tbGTKH.value.trim()) hasNoGTK = true;
                }
            }
            if (hasNoCost) {
                alert('All items with quantity must have an Avg Price greater than 0.');
                return false;
            }
            if (hasNoGTK) {
                alert('This warehouse is Duty Free — GTK confirmation number is required for all items with quantity.');
                return false;
            }

            // Pass 3: hide rows with qty = 0
            for (var i = 1; i < rows.length; i++) {
                var tb = rows[i].querySelector('input.tbQty');
                var qty = tb ? (parseInt(tb.value, 10) || 0) : 0;
                rows[i].style.display = qty > 0 ? '' : 'none';
            }
            var pnl = document.getElementById('<%= pnlCreate.ClientID %>');
            if (pnl) pnl.style.display = 'inline-block';
            return false;
        }
        // Enable GTK field only for Duty Free warehouses
        function updateGtkVisibility(ddl) {
            if (!ddl) ddl = document.getElementById('<%= drpWhsCode.ClientID %>');
            var tb = document.getElementById('<%= tbGTKHeader.ClientID %>');
            var hf = document.getElementById('<%= hfWhsUType.ClientID %>');
            if (!ddl || !tb) return;
            var opt = ddl.options[ddl.selectedIndex];
            var utype = opt ? (opt.getAttribute('data-utype') || '') : '';
            var isDutyFree = utype.toLowerCase() === 'duty free';
            tb.readOnly = !isDutyFree;
            tb.style.backgroundColor = isDutyFree ? 'white' : 'lightgray';
            if (!isDutyFree) tb.value = '';
            if (hf) hf.value = utype;
        }
        updateGtkVisibility(null);
    </script>

</asp:Content>
