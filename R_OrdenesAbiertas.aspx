<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="R_OrdenesAbiertas.aspx.cs" Inherits="R_OrdenesAbiertas" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">
    <div class="container-fluid">
        <div class="row">
            <div class="col-md-12">
                <asp:Panel ID="pnlPromoHeader" runat="server" CssClass="Panel">
                    <label class="PanelHeading">Open Orders</label>
                    <div class="row">
                        <div class="col-md-4">
                            &nbsp;&nbsp;
                            <label class="myLabelXtraSmall">Cut</label>
                            <tel:RadComboBox ID="rcbCorte" runat="server" Height="120px" DropDownAutoWidth="Disabled" Width="230px"
                                HighlightTemplatedItems="true"
                                AppendDataBoundItems="true"
                                EmptyMessage="Select Cut" AutoPostBack="true" OnSelectedIndexChanged="rcbCorte_SelectedIndexChanged"
                                Font-Italic="false">
                                <ExpandAnimation Type="OutQuart" Duration="500" />
                                <CollapseAnimation Type="OutQuint" Duration="300" />
                                <Items>
                                    <tel:RadComboBoxItem Value="1" Text="Open" />
                                    <tel:RadComboBoxItem Value="2" Text="Closed" />
                                    <tel:RadComboBoxItem Value="3" Text="All" />
                                </Items>
                            </tel:RadComboBox>
                            &nbsp;&nbsp;
                        </div>
                        <div class="col-md-4">
                            &nbsp;&nbsp;
                            <label class="myLabelSmall">Order No.</label>
                            <tel:RadTextBox ID="rtbOrden" runat="server" Width="120px" />
                            <asp:HiddenField ID="hfOrden" runat="server" />
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
                <h3 class="myLabelFull">Sales Order and Labeling Cut - in Colon</h3>
            </div>
        </div>
        <div class="row">
            <div class="col-md-12">
                <tel:RadGrid ID="rgOrdenes" AllowSorting="True" CssClass="Panel" PageSize="7" AllowPaging="True" runat="server" ShowStatusBar="true" AutoGenerateColumns="False"
                    OnNeedDataSource="rgOrdenes_NeedDataSource" OnDetailTableDataBind="rgOrdenes_DetailTableDataBind" OnItemCommand="rgOrdenes_ItemCommand"
                    OnExcelMLExportRowCreated="rgOrdenes_ExcelMLExportRowCreated" OnExcelMLExportStylesCreated="rgOrdenes_ExcelMLExportStylesCreated" 
                    Visible="false">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <ExportSettings OpenInNewWindow="true" IgnorePaging="true" Excel-Format="Xlsx" FileName="OpenOrders" UseItemStyles="true" />
                    <MasterTableView Width="100%" DataKeyNames="OrdenDeVenta" Name="Ordenes" AllowNaturalSort="false" CommandItemDisplay="Top" ShowFooter="true" FooterStyle-Font-Bold="true">
                        <CommandItemSettings ShowExportToExcelButton="true" ShowAddNewRecordButton="false" />
                        <DetailTables>
                            <tel:GridTableView DataKeyNames="ITEMCODE" Name="Tasks" Width="100%" PageSize="15">
                                <Columns>
                                    <tel:GridBoundColumn SortExpression="ITEMCODE" HeaderText="Item Code" HeaderButtonType="TextButton"
                                        DataField="ITEMCODE" UniqueName="ITEMCODE" HeaderStyle-Width="50px" />
                                    <tel:GridBoundColumn SortExpression="DSCRIPTION" HeaderText="Description" HeaderButtonType="TextButton"
                                        DataField="DSCRIPTION" UniqueName="DSCRIPTION" HeaderStyle-Width="300px" ItemStyle-Wrap="false" />
                                    <tel:GridBoundColumn SortExpression="Quantity" HeaderText="Unit Quantity" HeaderButtonType="TextButton"
                                        DataField="Quantity" UniqueName="Quantity" HeaderStyle-Width="90px" />
                                    <tel:GridBoundColumn SortExpression="Price" HeaderText="Price" HeaderButtonType="TextButton"
                                        DataField="Price" UniqueName="Price" HeaderStyle-Width="90px" />
                                </Columns>
                            </tel:GridTableView>
                        </DetailTables>
                        <Columns>
                            <tel:GridBoundColumn SortExpression="OrdenDeVenta" HeaderText="Sales Order" HeaderButtonType="TextButton"
                                DataField="OrdenDeVenta" UniqueName="OrdenDeVenta" HeaderStyle-Width="70px" />
                            <tel:GridBoundColumn SortExpression="DESP" HeaderText="Dispatched" HeaderButtonType="TextButton"
                                DataField="DESP" UniqueName="DESP" HeaderStyle-Width="70px" ItemStyle-Wrap="false" />
                            <tel:GridBoundColumn SortExpression="Delivery" HeaderText="Delivery" HeaderButtonType="TextButton"
                                DataField="Delivery" UniqueName="Delivery" HeaderStyle-Width="70px" ItemStyle-Wrap="false" />
                            <tel:GridBoundColumn SortExpression="A_R_Invoice" HeaderText="A/R Invoice" HeaderButtonType="TextButton"
                                DataField="A_R_Invoice" UniqueName="A_R_Invoice" HeaderStyle-Width="60px" />
                            <tel:GridBoundColumn SortExpression="ItmsGrpNam" HeaderText="Category" HeaderButtonType="TextButton"
                                DataField="ItmsGrpNam" UniqueName="ItmsGrpNam" HeaderStyle-Width="150px" />
                            <tel:GridBoundColumn SortExpression="CardCode" HeaderText="Card Code" HeaderButtonType="TextButton"
                                DataField="CardCode" UniqueName="CardCode" HeaderStyle-Width="110px" />
                            <tel:GridBoundColumn SortExpression="Comments" HeaderText="Comments" HeaderButtonType="TextButton"
                                DataField="Comments" UniqueName="Comments" HeaderStyle-Width="150px" />
                            <tel:GridBoundColumn SortExpression="Fecha" HeaderText="Date" HeaderButtonType="TextButton"
                                DataField="Fecha" UniqueName="Fecha" HeaderStyle-Width="130px" />
                            <tel:GridBoundColumn SortExpression="TOTLABEL" HeaderText="Case Quantity" HeaderButtonType="TextButton"
                                DataField="TOTLABEL" UniqueName="TOTLABEL" HeaderStyle-Width="60px" Aggregate="CountDistinct" FooterAggregateFormatString="{0:N0}" />
                            <tel:GridBoundColumn SortExpression="PesoTotal" HeaderText="Total Weight" HeaderButtonType="TextButton"
                                DataField="PesoTotal" UniqueName="PesoTotal" HeaderStyle-Width="50px" DataFormatString="{0:N2}" Aggregate="Sum" FooterAggregateFormatString="{0:N2}" />
                            <tel:GridBoundColumn SortExpression="CUBE_CRTN" HeaderText="Total Volume" HeaderButtonType="TextButton"
                                DataField="CUBE_CRTN" UniqueName="CUBE_CRTN" HeaderStyle-Width="70px" DataFormatString="{0:N2}" Aggregate="Sum" FooterAggregateFormatString="{0:N2}" />
                            <tel:GridBoundColumn SortExpression="CantidaddeUnidades" HeaderText="Unit Quantity" HeaderButtonType="TextButton"
                                DataField="CantidaddeUnidades" UniqueName="CantidaddeUnidades" HeaderStyle-Width="90px" DataFormatString="{0:N0}" Aggregate="Sum" FooterAggregateFormatString="{0:N0}" />
                            <tel:GridBoundColumn SortExpression="LineTotal" HeaderText="Total" HeaderButtonType="TextButton"
                                DataField="LineTotal" UniqueName="LineTotal" HeaderStyle-Width="70px" DataFormatString="{0:C2}" Aggregate="Sum" FooterAggregateFormatString="{0:C2}" />
                            <tel:GridBoundColumn SortExpression="Avance" HeaderText="% Progress" HeaderButtonType="TextButton"
                                DataField="Avance" UniqueName="Avance" HeaderStyle-Width="70px" DataFormatString="{0:P0}" />
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

