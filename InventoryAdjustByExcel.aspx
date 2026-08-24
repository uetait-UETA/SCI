<%@ Page Title="Inventory Adjustment by Excel" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="InventoryAdjustByExcel.aspx.cs" Inherits="InventoryAdjustByExcel" %>

<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">

    <div class="container" style="margin-left: 20px;">
        <div class="row">
            <div class="col-md-12"><br /></div>
        </div>
        <div class="row">
            <div class="col-md-9">
                <asp:Panel ID="pnlMain" runat="server" CssClass="page-curl shadow-bottom">
                    <label id="labelForm" runat="server" class="PanelHeading">Inventory Adjustment by Excel</label>
                    <div class="row">
                        <div class="col-md-7">
                            <ul class="myUL">
                                <li>
                                    <label class="myLabel">Type</label>
                                    <asp:RadioButtonList ID="rblType" runat="server" RepeatDirection="Horizontal"
                                        style="display:inline-block; vertical-align:middle;">
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
                                    <label class="myLabel">Excel File</label>
                                    <asp:FileUpload ID="FileUpload1" runat="server" ClientIDMode="Static" style="display:none;" />
                                    <button type="button" class="mybtnmeduim"
                                        onclick="document.getElementById('FileUpload1').click(); return false;">Choose File</button>
                                    &nbsp;
                                    <span id="spnFileName"
                                        style="display:inline-block; width:220px; border:1px solid #ccc; padding:3px 8px;
                                               background:white; vertical-align:middle; color:#555; font-size:13px;
                                               overflow:hidden; text-overflow:ellipsis; white-space:nowrap;" title="">No file chosen</span>
                                </li>
                                <li>
                                    <label class="myLabel">&nbsp;</label>
                                    <%-- Confirm button: only visible after validation passes --%>
                                    <asp:Panel ID="pnlConfirm" runat="server" Visible="false" style="display:inline-block;">
                                        <asp:Button ID="btnConfirm" runat="server" Text="Create"
                                            OnClick="btnConfirm_Click"
                                            OnClientClick="return confirm('WARNING!!\n\nAre you sure you want to create this inventory adjustment in SAP B1?\n\nClick OK to confirm or Cancel to abort.')"
                                            CssClass="mybtnmeduim"
                                            style="background:#2e7d32; color:white; border-color:#2e7d32;" />
                                    </asp:Panel>
                                </li>
                            </ul>
                        </div>
                        <div class="col-md-5">
                            <div style="margin-top:14px; padding:10px 14px; border:1px solid #c8daea; background:#f0f6fb; border-radius:4px; font-size:12px; color:#333;">
                                <strong>Expected Excel format:</strong><br /><br />
                                <table style="border-collapse:collapse; font-size:12px;">
                                    <tr style="background:#000084; color:white;">
                                        <td style="padding:3px 12px; border:1px solid #aaa;">ItemCode</td>
                                        <td style="padding:3px 12px; border:1px solid #aaa;">Quantity</td>
                                    </tr>
                                    <tr style="background:#eeeeee;">
                                        <td style="padding:3px 12px; border:1px solid #aaa;">ITEM-001</td>
                                        <td style="padding:3px 12px; border:1px solid #aaa;">10</td>
                                    </tr>
                                    <tr style="background:white;">
                                        <td style="padding:3px 12px; border:1px solid #aaa;">ITEM-002</td>
                                        <td style="padding:3px 12px; border:1px solid #aaa;">5</td>
                                    </tr>
                                </table>
                                <br />
                                <span style="color:#888;">Row 1 = header (required)</span>
                            </div>
                            <div style="margin-top:10px; padding:8px 14px; border:1px solid #f5c6cb; background:#fff3f3; border-radius:4px; font-size:11px; color:#555;">
                                <strong style="color:#c00;">Validations:</strong><br />
                                &#9679; Item must exist in OITW for the selected warehouse<br />
                                &#9679; OIGN: AvgPrice &gt; 0 in OITW<br />
                                &#9679; OIGE: OnHand &ge; Quantity in OITW
                            </div>
                        </div>
                    </div>
                    <div class="row">&nbsp;</div>
                </asp:Panel>
            </div>
        </div>

        <asp:HiddenField ID="hdnAction" runat="server" Value="" />

        <div class="row">&nbsp;</div>
        <div class="row">
            <div class="col-md-12">
                <asp:Label ID="lblMessage" runat="server" Font-Bold="True" Font-Size="Medium" />
            </div>
        </div>
        <div class="row">&nbsp;</div>
        <div class="row">
            <div class="col-md-5">
                <asp:GridView ID="gvData" runat="server" CssClass="GridViewPanel"
                    EnableModelValidation="False" ForeColor="#333333"
                    EnableTheming="False" EnableViewState="False" HorizontalAlign="Left">
                    <AlternatingRowStyle BackColor="White" ForeColor="#284775" />
                    <HeaderStyle BackColor="#000084" Font-Bold="True" ForeColor="White" />
                    <RowStyle BackColor="#EEEEEE" ForeColor="#333333" HorizontalAlign="Left" />
                </asp:GridView>
            </div>
        </div>
    </div>

    <script type="text/javascript">
        (function () {
            var fileInput = document.getElementById('FileUpload1');
            var hdnAction = document.getElementById('<%= hdnAction.ClientID %>');
            var spnFile   = document.getElementById('spnFileName');

            fileInput.addEventListener('change', function () {
                var name = this.files && this.files.length > 0 ? this.files[0].name : 'No file chosen';
                spnFile.textContent = name;
                spnFile.title       = name;

                if (this.files && this.files.length > 0) {
                    hdnAction.value = 'validate';
                    document.forms[0].submit();
                }
            });
        })();
    </script>

</asp:Content>
