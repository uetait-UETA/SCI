<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true"
    CodeFile="GoodsReceiptPO.aspx.cs" Inherits="GoodsReceiptPO" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">
    <div class="container-fluid">

        <%-- ── Filter panel ─────────────────────────────────────────────── --%>
        <div class="row">
            <div class="col-md-12">
                <asp:Panel ID="pnlFilters" runat="server" CssClass="Panel">
                    <label id="labelForm" runat="server" class="PanelHeading">Goods Receipt PO</label>
                    <div class="row">
                        <div class="col-md-3" style="text-align: center;">
                            <br />
                            <label class="myLabelMedium">Warehouse</label><br />
                            <tel:RadComboBox ID="rcbWarehouse" runat="server"
                                Width="250px"
                                EmptyMessage="Select Warehouse"
                                DropDownAutoWidth="Disabled"
                                AppendDataBoundItems="true"
                                AutoPostBack="true"
                                OnSelectedIndexChanged="rcbWarehouse_SelectedIndexChanged">
                            </tel:RadComboBox>
                        </div>

                        <div class="col-md-3" style="text-align: center;">
                            <label class="myLabelMedium">From Date</label><br />
                            <tel:RadDatePicker ID="rdpFromDate" runat="server" Width="140px">
                                <DateInput DateFormat="MM/dd/yyyy" />
                            </tel:RadDatePicker>
                            <br /><br />
                            <label class="myLabelMedium">To Date</label><br />
                            <tel:RadDatePicker ID="rdpToDate" runat="server" Width="140px">
                                <DateInput DateFormat="MM/dd/yyyy" />
                            </tel:RadDatePicker>
                        </div>

                        <div class="col-md-1" style="text-align: center;">
                            <br /><br /><br />
                            <tel:RadButton runat="server" ID="rbtnSearch" Text="Search"
                                OnClick="rbtnSearch_Click" />
                        </div>
                    </div>
                    <div class="row">&nbsp;</div>
                </asp:Panel>
            </div>
        </div>

        <div class="row">&nbsp;</div>

        <%-- ── Results grid ────────────────────────────────────────────── --%>
        <div class="row">
            <div class="col-md-12">
                <tel:RadGrid ID="rgInvoices" runat="server"
                    AllowSorting="True" CssClass="Panel"
                    PageSize="25" AllowPaging="True"
                    ShowStatusBar="true" AutoGenerateColumns="False"
                    OnNeedDataSource="rgInvoices_NeedDataSource"
                    OnItemCommand="rgInvoices_ItemCommand"
                    OnItemDataBound="rgInvoices_ItemDataBound"
                    Visible="true">
                    <PagerStyle Mode="Slider" />
                    <SortingSettings EnableSkinSortStyles="false" />
                    <MasterTableView Width="100%"
                        DataKeyNames="LocalDocEntry,SapTrReqEntry"
                        Name="PendingTransfers"
                        AllowNaturalSort="false"
                        CommandItemDisplay="None">
                        <Columns>
                            <tel:GridBoundColumn SortExpression="LocalDocNum"    HeaderText="Doc #"
                                DataField="LocalDocNum"    UniqueName="LocalDocNum"    HeaderStyle-Width="75px" />
                            <tel:GridBoundColumn SortExpression="DocDate"        HeaderText="Date"
                                DataField="DocDate"        UniqueName="DocDate"        HeaderStyle-Width="90px"
                                DataFormatString="{0:MM/dd/yyyy}" />
                            <tel:GridBoundColumn SortExpression="FromWhsCode"    HeaderText="From Whs"
                                DataField="FromWhsCode"    UniqueName="FromWhsCode"    HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="ToWhsCode"      HeaderText="To Whs"
                                DataField="ToWhsCode"      UniqueName="ToWhsCode"      HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="SapTrReqDocNum" HeaderText="SAP TR #"
                                DataField="SapTrReqDocNum" UniqueName="SapTrReqDocNum" HeaderStyle-Width="90px" />
                            <tel:GridBoundColumn SortExpression="DispatchUser"   HeaderText="Dispatched By"
                                DataField="DispatchUser"   UniqueName="DispatchUser"   HeaderStyle-Width="120px" />
                            <tel:GridTemplateColumn HeaderText="Action" UniqueName="ActionReceive"
                                HeaderStyle-Width="100px" AllowSorting="false">
                                <ItemTemplate>
                                    <asp:Button ID="btnReceive" runat="server"
                                        Text="Receive"
                                        CommandName="Receive"
                                        CssClass="btn btn-primary btn-sm"
                                        OnClientClick="return confirmReceive();" />
                                </ItemTemplate>
                            </tel:GridTemplateColumn>
                        </Columns>
                    </MasterTableView>
                    <ClientSettings>
                        <Resizing AllowColumnResize="true" />
                        <Selecting AllowRowSelect="true" />
                    </ClientSettings>
                </tel:RadGrid>
            </div>
        </div>
    </div>

    <script type="text/javascript">
        function confirmReceive() {
            return confirm(
                "WARNING!!\n\n" +
                "Are you sure you want to receive this transfer?\n\n" +
                "This will create an Inventory Transfer in SAP B1.\n\n" +
                "Click OK to confirm or Cancel to abort."
            );
        }
    </script>
</asp:Content>
