<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="R_ReciboBodegaSAPAPRIvsGRPO.aspx.cs" Inherits="R_ReciboBodegaSAPAPRIvsGRPO" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">
    <div class="container-fluid">
        <div class="row">
            <div class="col-md-12">
                <asp:Panel ID="pnlPromoHeader" runat="server" CssClass="Panel">
                    <label class="PanelHeading">Warehouse Receipt SAP APRI VS GRPOs</label>
                    <div class="row">
                        <div class="col-md-4">
                            &nbsp;&nbsp;
                            <label class="myLabelLarge">#APRI / #Order</label>
                            <tel:RadTextBox ID="rtbOrden" runat="server" Width="120px" />
                            <asp:HiddenField ID="hfOrden" runat="server" />
                            &nbsp;&nbsp;
                        </div>
                        <div class="col-md-4">
                            &nbsp;&nbsp;
                            <label class="myLabelXtraSmall">Company</label>
                            <tel:RadComboBox ID="rcbCorte" runat="server" Height="120px" DropDownAutoWidth="Disabled" Width="230px" Enabled="false"
                                HighlightTemplatedItems="true"
                                AppendDataBoundItems="true"
                                EmptyMessage="Select Company" AutoPostBack="true" OnSelectedIndexChanged="rcbCorte_SelectedIndexChanged"
                                Font-Italic="false">
                                <ExpandAnimation Type="OutQuart" Duration="500" />
                                <CollapseAnimation Type="OutQuint" Duration="300" />
                            </tel:RadComboBox>
                            &nbsp;&nbsp;
                        </div>
                        <div class="col-md-4">
                            &nbsp;&nbsp;
                            <tel:RadButton runat="server" ID="rbtnView" Text="View Report" OnClick="rbtnView_Click" />
                        </div>
                    </div>
                    <div class="row">&nbsp;</div>
                </asp:Panel>
            </div>
        </div>
        <div class="row" style="text-align: center;" runat="server" id="divHeading" visible="false">
            <div class="col-md-12">
                <h3 class="myLabelFull">Warehouse Receipt SAP APRI VS GRPOs</h3>
            </div>
        </div>
        <div class="row">
            <div class="col-md-12">
                <tel:RadGrid ID="rgHead" AllowSorting="True" CssClass="Panel" PageSize="15" AllowPaging="True" runat="server" ShowStatusBar="true" AutoGenerateColumns="False"
                    OnNeedDataSource="rgHead_NeedDataSource" OnDetailTableDataBind="rgHead_DetailTableDataBind" OnItemCommand="rgHead_ItemCommand"
                    OnExcelMLExportRowCreated="rgHead_ExcelMLExportRowCreated" OnExcelMLExportStylesCreated="rgHead_ExcelMLExportStylesCreated" 
                    Visible="false">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <ExportSettings OpenInNewWindow="true" IgnorePaging="true" Excel-Format="Xlsx" FileName="WarehouseReceiptSAPAPRIvsGRPOs" UseItemStyles="true" />
                    <MasterTableView Width="100%" DataKeyNames="APRIDocNum,OrderNum" Name="BinesDesp" AllowNaturalSort="false" CommandItemDisplay="Top" ShowFooter="true" FooterStyle-Font-Bold="true">
                        <CommandItemSettings ShowExportToExcelButton="true" ShowAddNewRecordButton="false" />
                        <DetailTables>
                            <tel:GridTableView DataKeyNames="APRIDocNum" Name="Tasks" Width="100%" PageSize="15">
                                <Columns>
                                    <tel:GridBoundColumn SortExpression="APRIItemCode" HeaderText="APRI Item Code" HeaderButtonType="TextButton"
                                        DataField="APRIItemCode" UniqueName="APRIItemCode" HeaderStyle-Width="90px" ItemStyle-Wrap="false" />
                                    <tel:GridBoundColumn SortExpression="BarCode" HeaderText="Bar Code" HeaderButtonType="TextButton"
                                        DataField="BarCode" UniqueName="BarCode" HeaderStyle-Width="90px" ItemStyle-Wrap="false" />
                                    <tel:GridBoundColumn SortExpression="APRIItemName" HeaderText="APRI Item Name" HeaderButtonType="TextButton"
                                        DataField="APRIItemName" UniqueName="APRIItemName" HeaderStyle-Width="300px" ItemStyle-Wrap="false" />
                                    <tel:GridBoundColumn SortExpression="APRIQtyUnits" HeaderText="APRI Qty" HeaderButtonType="TextButton"
                                        DataField="APRIQtyUnits" UniqueName="APRIQtyUnits" HeaderStyle-Width="90px" DataFormatString="{0:0.00}" />
                                    <tel:GridBoundColumn SortExpression="RECIBOQtyUnits" HeaderText="RECIBO Qty" HeaderButtonType="TextButton"
                                        DataField="RECIBOQtyUnits" UniqueName="RECIBOQtyUnits" HeaderStyle-Width="90px" DataFormatString="{0:0.00}" />
                                    <tel:GridBoundColumn SortExpression="GRPO_1_TotalQtyUnits" HeaderText="GRPO 1 Total Qty" HeaderButtonType="TextButton"
                                        DataField="GRPO_1_TotalQtyUnits" UniqueName="GRPO_1_TotalQtyUnits" HeaderStyle-Width="90px" DataFormatString="{0:0.00}" />
                                    <tel:GridBoundColumn SortExpression="GRPO_2_TotalQtyUnits" HeaderText="GRPO 2 Total Qty" HeaderButtonType="TextButton"
                                        DataField="GRPO_2_TotalQtyUnits" UniqueName="GRPO_2_TotalQtyUnits" HeaderStyle-Width="150px" DataFormatString="{0:0.00}" />
                                    <tel:GridBoundColumn SortExpression="Dif" HeaderText="Dif" HeaderButtonType="TextButton"
                                        DataField="Dif" UniqueName="Dif" HeaderStyle-Width="70px" DataFormatString="{0:0.00}" />
                                </Columns>
                            </tel:GridTableView>
                        </DetailTables>
                        <Columns>
                            <tel:GridBoundColumn SortExpression="APRIDocNum" HeaderText="#APRI" HeaderButtonType="TextButton"
                                DataField="APRIDocNum" UniqueName="APRIDocNum" HeaderStyle-Width="70px" />
                            <tel:GridBoundColumn SortExpression="OrderNum" HeaderText="#Order" HeaderButtonType="TextButton"
                                DataField="OrderNum" UniqueName="OrderNum" HeaderStyle-Width="70px" ItemStyle-Wrap="false" />
                            <tel:GridBoundColumn SortExpression="ItemCount" HeaderText="Items" HeaderButtonType="TextButton"
                                DataField="ItemCount" UniqueName="ItemCount" HeaderStyle-Width="90px" ItemStyle-Wrap="false" />
                            <tel:GridBoundColumn SortExpression="APRIQtyUnits" HeaderText="APRI Qty" HeaderButtonType="TextButton"
                                DataField="APRIQtyUnits" UniqueName="APRIQtyUnits" HeaderStyle-Width="90px" DataFormatString="{0:0.00}" />
                            <tel:GridBoundColumn SortExpression="RECIBOQtyUnits" HeaderText="RECIBO Qty" HeaderButtonType="TextButton"
                                DataField="RECIBOQtyUnits" UniqueName="RECIBOQtyUnits" HeaderStyle-Width="90px" DataFormatString="{0:0.00}" />
                            <tel:GridBoundColumn SortExpression="GRPO_1_DocNum" HeaderText="#GRPO 1" HeaderButtonType="TextButton"
                                DataField="GRPO_1_DocNum" UniqueName="GRPO_1_DocNum" HeaderStyle-Width="110px" />
                            <tel:GridBoundColumn SortExpression="GRPO_1_TotalQtyUnits" HeaderText="GRPO 1 Total Qty" HeaderButtonType="TextButton"
                                DataField="GRPO_1_TotalQtyUnits" UniqueName="GRPO_1_TotalQtyUnits" HeaderStyle-Width="90px" DataFormatString="{0:0.00}" />
                            <tel:GridBoundColumn SortExpression="GRPO_2_DocNum" HeaderText="#GRPO 2" HeaderButtonType="TextButton"
                                DataField="GRPO_2_DocNum" UniqueName="GRPO_2_DocNum" HeaderStyle-Width="110px" />
                            <tel:GridBoundColumn SortExpression="GRPO_2_TotalQtyUnits" HeaderText="GRPO 2 Total Qty" HeaderButtonType="TextButton"
                                DataField="GRPO_2_TotalQtyUnits" UniqueName="GRPO_2_TotalQtyUnits" HeaderStyle-Width="90px" DataFormatString="{0:0.00}" />
                            <tel:GridBoundColumn SortExpression="Dif" HeaderText="Dif" HeaderButtonType="TextButton"
                                DataField="Dif" UniqueName="Dif" HeaderStyle-Width="70px" DataFormatString="{0:0.00}" />
                        </Columns>
                    </MasterTableView>
                    <ClientSettings>
                        <Resizing AllowColumnResize="true" />
                        <Selecting AllowRowSelect="true" />
                    </ClientSettings>
                </tel:RadGrid>
                <%--<tel:RadGrid ID="rgHead" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False"
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
                </tel:RadGrid>--%>
            </div>
        </div>
    </div>
</asp:Content>

