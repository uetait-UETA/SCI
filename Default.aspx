<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">
    <div class="container-fluid">
        <div class="row">
            <div class="col-md-12">
                <asp:Panel ID="pnlPromoHeader" runat="server" CssClass="Panel">
                    <label class="PanelHeading">Search Transfers / Orders</label>
                    <div class="row">
                        <div class="col-md-3">
                            &nbsp;&nbsp;
                            <label class="myLabelXtraSmall">Category</label>
                            <tel:RadComboBox ID="rcbCategory" runat="server" Height="120px" DropDownAutoWidth="Disabled" Width="230px"
                                HighlightTemplatedItems="true"
                                AppendDataBoundItems="true"
                                EmptyMessage="Select a category"
                                AutoPostBack="true" CheckBoxes="true" EnableCheckAllItemsCheckBox="true" Localization-CheckAllString="Select all categories" Localization-AllItemsCheckedString="All categories selected"
                                OnSelectedIndexChanged="rcbCategory_SelectedIndexChanged"
                                Font-Italic="false">
                                <ExpandAnimation Type="OutQuart" Duration="500" />
                                <CollapseAnimation Type="OutQuint" Duration="300" />
                            </tel:RadComboBox>
                            &nbsp;&nbsp;
                        </div>
                        <div class="col-md-4">
                            &nbsp;&nbsp;
                            <label class="myLabelXtraSmall">Operation</label>
                            <tel:RadComboBox ID="rcbWhs" runat="server" Height="120px" DropDownAutoWidth="Disabled" Width="280px"
                                HighlightTemplatedItems="true"
                                AppendDataBoundItems="true"
                                EmptyMessage="Select To Loc"
                                AutoPostBack="true" CheckBoxes="true" EnableCheckAllItemsCheckBox="true" Localization-CheckAllString="Select All" Localization-AllItemsCheckedString="All Locs Selected"
                                OnSelectedIndexChanged="rcbWhs_SelectedIndexChanged"
                                Font-Italic="false">
                                <ExpandAnimation Type="OutQuart" Duration="500" />
                                <CollapseAnimation Type="OutQuint" Duration="300" />
                            </tel:RadComboBox>
                            &nbsp;&nbsp;
                        </div>
                        <div class="col-md-5">
                            <asp:RadioButtonList ID="rbStatus" runat="server" RepeatLayout="Table"
                                AutoPostBack="true" CssClass="radio"
                                RepeatDirection="Horizontal"
                                OnSelectedIndexChanged="rbStatus_SelectedIndexChanged">
                                <asp:ListItem Text="Open Orders Only" Value="O" Selected="True" />
                                <asp:ListItem Text="Closed Orders Only" Value="C" />
                                <asp:ListItem Text="All Orders" Value="A" />
                            </asp:RadioButtonList>
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </div>
        <div class="row" style="text-align: center;">
            <div class="col-md-12">
                <h3 class="myLabelFull">INTERNAL TRANSFERS</h3>
            </div>
        </div>
        <div class="row">
            <div class="col-md-12">
                <tel:RadGrid ID="rgHead" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False"
                    AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="True" PageSize="15" CssClass="Panel"
                    OnNeedDataSource="rgHead_NeedDataSource" OnItemCommand="rgHead_ItemCommand">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <ExportSettings OpenInNewWindow="true" IgnorePaging="true" Excel-Format="Xlsx" HideStructureColumns="true" FileName="Export Transfer Header" />
                    <MasterTableView Width="100%" AllowNaturalSort="false" DataKeyNames="transfer" CommandItemDisplay="Top">
                        <CommandItemSettings ShowExportToExcelButton="true" ShowAddNewRecordButton="false" ShowRefreshButton="false" />
                        <Columns>
                            <tel:GridButtonColumn UniqueName="transfer" ButtonType="LinkButton" HeaderText="Transfer / Order" DataTextField="transfer" CommandName="TRANSFER" ItemStyle-Font-Underline="true" />
                            <tel:GridBoundColumn SortExpression="company" HeaderText="Company" HeaderButtonType="TextButton" DataField="company" UniqueName="company" />
                            <tel:GridBoundColumn SortExpression="from_loc" HeaderText="From Loc" HeaderButtonType="TextButton" DataField="from_loc" UniqueName="from_loc" />
                            <tel:GridBoundColumn SortExpression="from_locName" HeaderText="From Loc Name" HeaderButtonType="TextButton" DataField="from_locName" UniqueName="from_locName" />
                            <tel:GridBoundColumn SortExpression="to_loc" HeaderText="To Loc" HeaderButtonType="TextButton" DataField="to_loc" UniqueName="to_loc" />
                            <tel:GridBoundColumn SortExpression="to_locName" HeaderText="To Loc Name" HeaderButtonType="TextButton" DataField="to_locName" UniqueName="to_locName" />
                            <tel:GridBoundColumn SortExpression="TransferDate" HeaderText="Tsf. Date" HeaderButtonType="TextButton" DataField="TransferDate" UniqueName="TransferDate" DataFormatString="{0:d}" />
                            <tel:GridBoundColumn SortExpression="Transfer_Status" HeaderText="Status" HeaderButtonType="TextButton" DataField="Transfer_Status" UniqueName="Transfer_Status" />
                            <tel:GridBoundColumn SortExpression="UserDispatch" HeaderText="Dispatch" HeaderButtonType="TextButton" DataField="UserDispatch" UniqueName="UserDispatch" />
                            <tel:GridBoundColumn SortExpression="UserReceive" HeaderText="Receive" HeaderButtonType="TextButton" DataField="UserReceive" UniqueName="UserReceive" />
                        </Columns>
                    </MasterTableView>
                    <ClientSettings>
                        <Resizing AllowColumnResize="true" />
                        <Selecting AllowRowSelect="true" />
                    </ClientSettings>
                </tel:RadGrid>
            </div>
        </div>
        
        <div class="row">
            &nbsp;
        </div>
        <div class="row">
            <div class="col-md-12">
                <tel:RadGrid ID="rgDetail" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False"
                    AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="True" PageSize="15" CssClass="Panel" Visible="false" OnNeedDataSource="rgDetail_NeedDataSource">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <ExportSettings ExportOnlyData="true" OpenInNewWindow="true" IgnorePaging="true" Excel-Format="ExcelML" HideStructureColumns="true" FileName="Export Transfer Details" />
                    <MasterTableView Width="100%" AllowNaturalSort="false" CommandItemDisplay="Top">
                        <CommandItemSettings ShowExportToExcelButton="true" ShowAddNewRecordButton="false" ShowRefreshButton="false" />
                        <Columns>
                            <tel:GridBoundColumn SortExpression="item" HeaderText="Item" HeaderButtonType="TextButton" DataField="item" UniqueName="item" HeaderStyle-Width="75px" />
                            <tel:GridBoundColumn SortExpression="itemName" HeaderText="Item Name" HeaderButtonType="TextButton" DataField="itemName" UniqueName="itemName" HeaderStyle-Width="360px" />
                            <tel:GridBoundColumn SortExpression="from_loc_soh" HeaderText="From Loc SOH" HeaderButtonType="TextButton" DataField="from_loc_soh" UniqueName="from_loc_soh" DataFormatString="{0:N0}" HeaderStyle-Width="80px" />
                            <tel:GridBoundColumn SortExpression="to_loc_soh" HeaderText="To Loc SOH" HeaderButtonType="TextButton" DataField="to_loc_soh" UniqueName="to_loc_soh" DataFormatString="{0:N0}" HeaderStyle-Width="80px" />
                            <tel:GridBoundColumn SortExpression="Reserved" HeaderText="Reserved" HeaderButtonType="TextButton" DataField="Reserved" UniqueName="Reserved" DataFormatString="{0:N0}" HeaderStyle-Width="80px" />
                            <tel:GridBoundColumn SortExpression="In_Transit" HeaderText="In Transit" HeaderButtonType="TextButton" DataField="In_Transit" UniqueName="In_Transit" DataFormatString="{0:N0}" HeaderStyle-Width="80px" />
                            <tel:GridBoundColumn SortExpression="Received" HeaderText="Received" HeaderButtonType="TextButton" DataField="Received" UniqueName="Received" DataFormatString="{0:N0}" HeaderStyle-Width="80px" />
                            <tel:GridBoundColumn SortExpression="Transfer_OpenClose" HeaderText="Tsf. Open/Close" HeaderButtonType="TextButton" DataField="Transfer_OpenClose" UniqueName="Transfer_OpenClose" HeaderStyle-Width="80px" />
                            <tel:GridBoundColumn SortExpression="TransferDate" HeaderText="Tsf. Date" HeaderButtonType="TextButton" DataField="TransferDate" UniqueName="TransferDate" DataFormatString="{0:d}" HeaderStyle-Width="65px" />
                            <tel:GridBoundColumn SortExpression="Transfer_Status" HeaderText="Status" HeaderButtonType="TextButton" DataField="Transfer_Status" UniqueName="Transfer_Status" HeaderStyle-Width="65px" />
                            <tel:GridBoundColumn SortExpression="UserDispatch" HeaderText="Dispatch" HeaderButtonType="TextButton" DataField="UserDispatch" UniqueName="UserDispatch" HeaderStyle-Width="65px" />
                            <tel:GridBoundColumn SortExpression="UserReceive" HeaderText="Receive" HeaderButtonType="TextButton" DataField="UserReceive" UniqueName="UserReceive" HeaderStyle-Width="65px" />
                        </Columns>
                    </MasterTableView>
                    <ClientSettings>
                        <Resizing AllowColumnResize="true" />
                        <Selecting AllowRowSelect="true" />
                    </ClientSettings>
                </tel:RadGrid>
            </div>
        </div>

        <asp:Panel ID="PanelExternalCEDIData" runat="server">
            <div class="row" style="text-align: center;">
                <div class="col-md-12">
                    <h3 class="myLabelFull">MERCHANDISE IN TRANSIT FROM COLON</h3>
                </div>
            </div>
            <div class="row">
                <div class="col-md-12">
                    <tel:RadGrid ID="rgCIHead" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False" AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="True" PageSize="15" CssClass="Panel"
                        OnNeedDataSource="rgCIHead_NeedDataSource" OnItemCommand="rgCIHead_ItemCommand">
                        <PagerStyle Mode="Slider"></PagerStyle>
                        <SortingSettings EnableSkinSortStyles="false" />
                        <ExportSettings OpenInNewWindow="true" IgnorePaging="true" Excel-Format="Xlsx" HideStructureColumns="true" FileName="Transit Header Export" />
                        <MasterTableView Width="100%" AllowNaturalSort="false" DataKeyNames="APInvoiceDocEntry" CommandItemDisplay="Top">
                            <CommandItemSettings ShowExportToExcelButton="true" ShowAddNewRecordButton="false" ShowRefreshButton="false" />
                            <Columns>
                                <tel:GridButtonColumn UniqueName="APInvoiceDocEntry" ButtonType="LinkButton" HeaderText="APInvoiceDocEntry" DataTextField="APInvoiceDocEntry" CommandName="APInvoiceDocEntry" ItemStyle-Font-Underline="true" HeaderStyle-Width="80px" />
                                <tel:GridBoundColumn SortExpression="APInvoiceNum" HeaderText="APInvoiceNum" HeaderButtonType="TextButton" DataField="APInvoiceNum" UniqueName="APInvoiceNum" HeaderStyle-Width="80px" />
                                <tel:GridBoundColumn SortExpression="APInvoiceDate" HeaderText="APInvoiceDate" HeaderButtonType="TextButton" DataField="APInvoiceDate" UniqueName="APInvoiceDate" HeaderStyle-Width="80px" DataFormatString="{0:d}" />
                                <tel:GridBoundColumn SortExpression="CardCode" HeaderText="CardCode" HeaderButtonType="TextButton" DataField="CardCode" UniqueName="CardCode" HeaderStyle-Width="80px" />
                                <tel:GridBoundColumn SortExpression="SourceCompany" HeaderText="SourceCompany" HeaderButtonType="TextButton" DataField="SourceCompany" UniqueName="SourceCompany" HeaderStyle-Width="120px" />
                                <tel:GridBoundColumn SortExpression="SourceSalesOrder" HeaderText="SourceSalesOrder" HeaderButtonType="TextButton" DataField="SourceSalesOrder" UniqueName="SourceSalesOrder" HeaderStyle-Width="80px" />
                                <tel:GridBoundColumn SortExpression="SourceInvoice" HeaderText="SourceInvoice" HeaderButtonType="TextButton" DataField="SourceInvoice" UniqueName="SourceInvoice" HeaderStyle-Width="80px" />
                                <tel:GridBoundColumn SortExpression="ToLoc" HeaderText="ToLoc" HeaderButtonType="TextButton" DataField="ToLoc" UniqueName="ToLoc" HeaderStyle-Width="80px" />
                                <tel:GridBoundColumn SortExpression="ToLocName" HeaderText="ToLocName" HeaderButtonType="TextButton" DataField="ToLocName" UniqueName="ToLocName" HeaderStyle-Width="180px" />
                                <tel:GridBoundColumn SortExpression="NumberOfItems" HeaderText="NumberOfItems" HeaderButtonType="TextButton" DataField="NumberOfItems" UniqueName="NumberOfItems" HeaderStyle-Width="80px" />
                                <tel:GridBoundColumn SortExpression="TotalQuantity" HeaderText="TotalQuantity" HeaderButtonType="TextButton" DataField="TotalQuantity" UniqueName="TotalQuantity" DataFormatString="{0:N0}" HeaderStyle-Width="80px" />
                            </Columns>
                        </MasterTableView>
                        <ClientSettings>
                            <Resizing AllowColumnResize="true" />
                            <Selecting AllowRowSelect="true" />
                        </ClientSettings>
                    </tel:RadGrid>
                </div>
            </div>
            <div class="row">&nbsp;</div>
            <div class="row">
                <div class="col-md-12">
                    <tel:RadGrid ID="rgCIDetail" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False"
                        AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="True" PageSize="15" CssClass="Panel" Visible="false" OnNeedDataSource="rgCIDetail_NeedDataSource">
                        <PagerStyle Mode="Slider"></PagerStyle>
                        <SortingSettings EnableSkinSortStyles="false" />
                        <ExportSettings ExportOnlyData="true" OpenInNewWindow="true" IgnorePaging="true" Excel-Format="ExcelML" HideStructureColumns="true" FileName="Transit Detail Export" />
                        <MasterTableView Width="100%" AllowNaturalSort="false" CommandItemDisplay="Top">
                            <CommandItemSettings ShowExportToExcelButton="true" ShowAddNewRecordButton="false" ShowRefreshButton="false" />
                            <Columns>
                                <tel:GridBoundColumn SortExpression="APInvoiceDocEntry" HeaderText="APInvoice DocEntry" HeaderButtonType="TextButton" DataField="APInvoiceDocEntry" UniqueName="APInvoiceDocEntry" />
                                <tel:GridBoundColumn SortExpression="APInvoiceNum" HeaderText="APInvoice #" HeaderButtonType="TextButton" DataField="APInvoiceNum" UniqueName="APInvoiceNum" />
                                <tel:GridBoundColumn SortExpression="APInvoiceDate" HeaderText="APInvoice Date" HeaderButtonType="TextButton" DataField="APInvoiceDate" UniqueName="APInvoiceDate" DataFormatString="{0:d}" />
                                <tel:GridBoundColumn SortExpression="SourceCompany" HeaderText="Source Company" HeaderButtonType="TextButton" DataField="SourceCompany" UniqueName="SourceCompany" />
                                <tel:GridBoundColumn SortExpression="SourceSalesOrder" HeaderText="Source Sales Order" HeaderButtonType="TextButton" DataField="SourceSalesOrder" UniqueName="SourceSalesOrder" />
                                <tel:GridBoundColumn SortExpression="SourceInvoice" HeaderText="Source Invoice" HeaderButtonType="TextButton" DataField="SourceInvoice" UniqueName="SourceInvoice" />
                                <tel:GridBoundColumn SortExpression="CardCode" HeaderText="Card Code" HeaderButtonType="TextButton" DataField="CardCode" UniqueName="CardCode" />
                                <tel:GridBoundColumn SortExpression="ItemCode" HeaderText="Item Code" HeaderButtonType="TextButton" DataField="ItemCode" UniqueName="ItemCode" />
                                <tel:GridBoundColumn SortExpression="Dscription" HeaderText="Item Name" HeaderButtonType="TextButton" DataField="Dscription" UniqueName="Dscription" HeaderStyle-Width="375px" />
                                <tel:GridBoundColumn SortExpression="Quantity" HeaderText="Quantity" HeaderButtonType="TextButton" DataField="Quantity" UniqueName="Quantity" DataFormatString="{0:N0}" />
                                <tel:GridBoundColumn SortExpression="ToLoc" HeaderText="To Loc" HeaderButtonType="TextButton" DataField="ToLoc" UniqueName="ToLoc" />
                                <tel:GridBoundColumn SortExpression="ToLocName" HeaderText="To Loc Name" HeaderButtonType="TextButton" DataField="ToLocName" UniqueName="ToLocName" HeaderStyle-Width="160px" />
                            </Columns>
                        </MasterTableView>
                        <ClientSettings>
                            <Resizing AllowColumnResize="true" />
                            <Selecting AllowRowSelect="true" />
                        </ClientSettings>
                    </tel:RadGrid>
                </div>
            </div>
        
            <div class="row" style="text-align: center;">
                <div class="col-md-12">

                </div>
            </div>
            <div class="row">
                <div class="col-md-12">
                    <tel:RadGrid ID="rgOrdenes" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False" AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="True" PageSize="15" CssClass="Panel"
                            OnItemCommand="rgOrdenes_ItemCommand">
                        <PagerStyle Mode="Slider"></PagerStyle>
                        <SortingSettings EnableSkinSortStyles="false" />
                        <ExportSettings OpenInNewWindow="true" IgnorePaging="true" Excel-Format="Xlsx" HideStructureColumns="true" FileName="Orders Being Processed in Colon" />
                        <MasterTableView Width="100%" AllowNaturalSort="false" DataKeyNames="OrdenVenta" CommandItemDisplay="Top">
                            <CommandItemSettings ShowExportToExcelButton="true" ShowAddNewRecordButton="false" ShowRefreshButton="false" />
                            <Columns>
                                <tel:GridButtonColumn UniqueName="OrdenVenta" ButtonType="LinkButton" HeaderText="Sales Order" DataTextField="OrdenVenta" CommandName="OrdenVenta" ItemStyle-Font-Underline="true" HeaderStyle-Width="130px" />
                                <tel:GridBoundColumn SortExpression="Fecha" HeaderText="Date" HeaderButtonType="TextButton" DataField="Fecha" UniqueName="Fecha" DataFormatString="{0:d}" />
                                <tel:GridBoundColumn SortExpression="CardCode" HeaderText="CardCode" HeaderButtonType="TextButton" DataField="CardCode" UniqueName="CardCode" />
                                <tel:GridBoundColumn SortExpression="CardName" HeaderText="CardName" HeaderButtonType="TextButton" DataField="CardName" UniqueName="CardName" HeaderStyle-Width="350px" />
                                <tel:GridBoundColumn SortExpression="Categoria" HeaderText="Category" HeaderButtonType="TextButton" DataField="Categoria" UniqueName="Categoria" />
                            </Columns>
                        </MasterTableView>
                        <ClientSettings>
                            <Resizing AllowColumnResize="true" />
                            <Selecting AllowRowSelect="true" />
                        </ClientSettings>
                    </tel:RadGrid>
                </div>
            </div>

            <div class="row">&nbsp;</div>
            <div class="row">
                <div class="col-md-12">
                    <tel:RadGrid ID="rgOrdenesDetail" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False"
                        AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="True" PageSize="15" CssClass="Panel" Visible="false">
                        <PagerStyle Mode="Slider"></PagerStyle>
                        <SortingSettings EnableSkinSortStyles="false" />
                        <ExportSettings ExportOnlyData="true" OpenInNewWindow="true" IgnorePaging="true" Excel-Format="ExcelML" HideStructureColumns="true" FileName="Orders Detail Export" />
                        <MasterTableView Width="100%" AllowNaturalSort="false" CommandItemDisplay="Top">
                            <CommandItemSettings ShowExportToExcelButton="true" ShowAddNewRecordButton="false" ShowRefreshButton="false" />
                            <Columns>
                                <tel:GridBoundColumn SortExpression="ItemCode" HeaderText="ItemCode" HeaderButtonType="TextButton" DataField="ItemCode" UniqueName="ItemCode" />
                                <tel:GridBoundColumn SortExpression="Descripcion" HeaderText="Description" HeaderButtonType="TextButton" DataField="Descripcion" UniqueName="Descripcion" HeaderStyle-Width="310px" />
                                <tel:GridBoundColumn SortExpression="Cantidad" HeaderText="Quantity" HeaderButtonType="TextButton" DataField="Cantidad" UniqueName="Cantidad" DataFormatString="{0:N0}" />
                            </Columns>
                        </MasterTableView>
                        <ClientSettings>
                            <Resizing AllowColumnResize="true" />
                            <Selecting AllowRowSelect="true" />
                        </ClientSettings>
                    </tel:RadGrid>
                </div>
            </div>
        </asp:Panel>
        
    </div>
</asp:Content>

