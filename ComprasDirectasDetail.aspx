<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="ComprasDirectasDetail.aspx.cs" Inherits="ComprasDirectasDetail" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">
    <div class="container-fluid">

        <%-- Header info --%>
        <div class="row">
            <div class="col-md-12">
                <asp:Panel ID="pnlHeader" runat="server" CssClass="Panel">
                    <label class="PanelHeading">Direct Purchase — Discrepancy Entry</label>
                    <div class="row" style="margin-top:10px;">
                        <div class="col-md-1">
                            <label class="myLabelMedium">APRI #</label><br />
                            <asp:Label ID="lblApriNum"  runat="server" CssClass="myLabelMedium" />
                        </div>
                        <div class="col-md-2">
                            <label class="myLabelMedium">Date</label><br />
                            <asp:Label ID="lblDate"     runat="server" CssClass="myLabelMedium" />
                        </div>
                        <div class="col-md-4">
                            <label class="myLabelMedium">Vendor</label><br />
                            <asp:Label ID="lblVendor"   runat="server" CssClass="myLabelMedium" />
                        </div>
                        <div class="col-md-2">
                            <label class="myLabelMedium">Ref #</label><br />
                            <asp:Label ID="lblRefNum"   runat="server" CssClass="myLabelMedium" />
                        </div>
                        <div class="col-md-2">
                            <label class="myLabelMedium">Total</label><br />
                            <asp:Label ID="lblTotal"    runat="server" CssClass="myLabelMedium" />
                            &nbsp;<asp:Label ID="lblCurrency" runat="server" CssClass="myLabelMedium" />
                        </div>
                    </div>
                    <div class="row">&nbsp;</div>
                </asp:Panel>
            </div>
        </div>

        <div class="row">&nbsp;</div>

        <%-- Lines grid --%>
        <div class="row">
            <div class="col-md-12">
                <tel:RadGrid ID="rgLines" runat="server"
                    AllowSorting="False" CssClass="Panel"
                    ShowStatusBar="false" AutoGenerateColumns="False"
                    AllowPaging="False"
                    OnNeedDataSource="rgLines_NeedDataSource"
                    OnItemDataBound="rgLines_ItemDataBound">
                    <MasterTableView Width="100%"
                        DataKeyNames="LineNum"
                        Name="Lines"
                        CommandItemDisplay="None">
                        <Columns>
                            <tel:GridBoundColumn DataField="ItemCode"   HeaderText="Item Code"
                                UniqueName="ItemCode"   HeaderStyle-Width="120px" />
                            <tel:GridBoundColumn DataField="Dscription" HeaderText="Description"
                                UniqueName="Dscription" HeaderStyle-Width="280px" />
                            <tel:GridBoundColumn DataField="WhsCode"    HeaderText="Warehouse"
                                UniqueName="WhsCode"    HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn DataField="WhsType"    HeaderText="Type"
                                UniqueName="WhsType"    HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn DataField="Quantity"   HeaderText="APRI Qty"
                                UniqueName="Quantity"   HeaderStyle-Width="100px"
                                DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" />
                            <tel:GridTemplateColumn HeaderText="Received Qty"
                                UniqueName="ReceivedQty" HeaderStyle-Width="130px">
                                <ItemTemplate>
                                    <asp:TextBox ID="txtReceivedQty" runat="server"
                                        Width="90px" CssClass="form-control form-control-sm"
                                        style="display:inline;" />
                                </ItemTemplate>
                            </tel:GridTemplateColumn>
                        </Columns>
                    </MasterTableView>
                    <ClientSettings>
                        <Resizing AllowColumnResize="true" />
                    </ClientSettings>
                </tel:RadGrid>
            </div>
        </div>

        <div class="row">&nbsp;</div>

        <%-- Buttons --%>
        <div class="row">
            <div class="col-md-12">
                <asp:Panel ID="pnlButtons" runat="server" CssClass="Panel">
                    <div style="padding:10px;">
                        <asp:Button ID="btnConfirm" runat="server"
                            Text="Confirm Receipt"
                            CssClass="btn btn-success"
                            OnClick="btnConfirm_Click"
                            OnClientClick="return confirmSubmit();" />
                        &nbsp;&nbsp;
                        <asp:Button ID="btnCancel" runat="server"
                            Text="Cancel"
                            CssClass="btn btn-default"
                            OnClick="btnCancel_Click"
                            CausesValidation="false" />
                    </div>
                </asp:Panel>
            </div>
        </div>

    </div>

    <script type="text/javascript">
        function confirmSubmit() {
            return confirm(
                "WARNING!!\n\n" +
                "Confirm receipt with the quantities entered?\n\n" +
                "This will create a Goods Receipt PO in SAP B1.\n\n" +
                "Click OK to confirm or Cancel to abort."
            );
        }
    </script>
</asp:Content>
