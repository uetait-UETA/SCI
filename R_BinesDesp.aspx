<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="R_BinesDesp.aspx.cs" Inherits="R_BinesDesp" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">
    <div class="container-fluid">
        <div class="row">
            <div class="col-md-12">
                <asp:Panel ID="pnlPromoHeader" runat="server" CssClass="Panel">
                    <label id="labelForm" runat="server" class="PanelHeading">Bins Dispatched from Colon ZL and Received in Tocumen</label>
                    <div class="row">
                        <div class="col-md-4">
                            &nbsp;&nbsp;
                            <label class="myLabelSmall">Order No.</label>
                            <tel:RadTextBox ID="rtbOrden" runat="server" Width="120px" />
                            <asp:HiddenField ID="hfOrden" runat="server" />
                            &nbsp;&nbsp;
                        </div>
                        <div class="col-md-4">
                            &nbsp;&nbsp;
                            <label class="myLabelSmall">Documents</label>
                            <tel:RadComboBox ID="rcbCorte" runat="server" Height="120px" DropDownAutoWidth="Disabled" Width="230px"
                                HighlightTemplatedItems="true"
                                AppendDataBoundItems="true"
                                EmptyMessage="Select Period" AutoPostBack="true" OnSelectedIndexChanged="rcbCorte_SelectedIndexChanged"
                                Font-Italic="false">
                                <ExpandAnimation Type="OutQuart" Duration="500" />
                                <CollapseAnimation Type="OutQuint" Duration="300" />
                                <Items>
                                    <tel:RadComboBoxItem Value="O" Text="Open" />
                                    <tel:RadComboBoxItem Value="C" Text="Closed" />
                                    <tel:RadComboBoxItem Value="T" Text="All" />
                                </Items>
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
                <h3 class="myLabelFull">Bins Dispatched from Colon ZL and Received in Tocumen</h3>
            </div>
        </div>
        <div class="row">
            <div class="col-md-12">
                <tel:RadGrid ID="rgHead" AllowSorting="True" CssClass="Panel" PageSize="7" AllowPaging="True" runat="server" ShowStatusBar="true" AutoGenerateColumns="False"
                    OnNeedDataSource="rgHead_NeedDataSource" OnDetailTableDataBind="rgHead_DetailTableDataBind" OnItemCommand="rgHead_ItemCommand"
                    OnExcelMLExportRowCreated="rgHead_ExcelMLExportRowCreated" OnExcelMLExportStylesCreated="rgHead_ExcelMLExportStylesCreated" 
                    Visible="false">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <ExportSettings OpenInNewWindow="true" IgnorePaging="true" Excel-Format="Xlsx" FileName="BinsDispatched" UseItemStyles="true" />
                    <MasterTableView Width="100%" DataKeyNames="OSDocNum" Name="BinesDesp" AllowNaturalSort="false" CommandItemDisplay="Top" ShowFooter="true" FooterStyle-Font-Bold="true">
                        <CommandItemSettings ShowExportToExcelButton="true" ShowAddNewRecordButton="false" />
                        <DetailTables>
                            <tel:GridTableView DataKeyNames="WmsBin" Name="Tasks" Width="100%" PageSize="15">
                                <Columns>
                                    <tel:GridHyperLinkColumn SortExpression="WmsBin" HeaderText="Wms Bin" HeaderButtonType="TextButton" DataTextField="WmsBin"
                                        UniqueName="WmsBin" DataNavigateUrlFields="WmsBin,OSDocNum" HeaderStyle-Width="150px" DataNavigateUrlFormatString="R_DetalledeItemsporBins.aspx?Bin={0}&Orden={1}" />
                                    <tel:GridBoundColumn SortExpression="CintilloBin1" HeaderText="Bin Tag 1" HeaderButtonType="TextButton"
                                        DataField="CintilloBin1" UniqueName="CintilloBin1" HeaderStyle-Width="150px" ItemStyle-Wrap="false" />
                                    <tel:GridBoundColumn SortExpression="CintilloBin2" HeaderText="Bin Tag 2" HeaderButtonType="TextButton"
                                        DataField="CintilloBin2" UniqueName="CintilloBin2" HeaderStyle-Width="150px" />
                                    <tel:GridBoundColumn SortExpression="estatus" HeaderText="Received" HeaderButtonType="TextButton"
                                        DataField="estatus" UniqueName="estatus" HeaderStyle-Width="150px" />
                                </Columns>
                            </tel:GridTableView>
                        </DetailTables>
                        <Columns>
                            <tel:GridBoundColumn SortExpression="OSDocNum" HeaderText="Sales Order" HeaderButtonType="TextButton"
                                DataField="OSDocNum" UniqueName="OSDocNum" HeaderStyle-Width="70px" />
                            <tel:GridBoundColumn SortExpression="ToCardCode" HeaderText="Location" HeaderButtonType="TextButton"
                                DataField="ToCardCode" UniqueName="ToCardCode" HeaderStyle-Width="70px" ItemStyle-Wrap="false" />
                            <tel:GridBoundColumn SortExpression="fechadesp" HeaderText="Dispatch Date" HeaderButtonType="TextButton"
                                DataField="fechadesp" UniqueName="fechadesp" HeaderStyle-Width="130px" ItemStyle-Wrap="false" />
                            <tel:GridBoundColumn SortExpression="fecharec" HeaderText="Receipt Date" HeaderButtonType="TextButton"
                                DataField="fecharec" UniqueName="fecharec" HeaderStyle-Width="60px" />
                            <tel:GridBoundColumn SortExpression="factDFB" HeaderText="Num. Fact. DFBUYING" HeaderButtonType="TextButton"
                                DataField="factDFB" UniqueName="factDFB" HeaderStyle-Width="150px" />
                            <tel:GridBoundColumn SortExpression="numapri" HeaderText="Num. APRI TOCUMEN" HeaderButtonType="TextButton"
                                DataField="numapri" UniqueName="numapri" HeaderStyle-Width="110px" />
                            <tel:GridBoundColumn SortExpression="DocNum" HeaderText="Num. GR TOCUMEN" HeaderButtonType="TextButton"
                                DataField="DocNum" UniqueName="DocNum" HeaderStyle-Width="150px" />
                            <tel:GridBoundColumn SortExpression="fromapp" HeaderText="Receipt Type" HeaderButtonType="TextButton"
                                DataField="fromapp" UniqueName="fromapp" HeaderStyle-Width="130px" />
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
                    <ExportSettings OpenInNewWindow="true" IgnorePaging="true" Excel-Format="Xlsx" HideStructureColumns="true" FileName="Exportar encabezado de transferencia" />
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

