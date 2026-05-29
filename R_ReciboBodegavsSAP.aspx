<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="R_ReciboBodegavsSAP.aspx.cs" Inherits="R_ReciboBodegavsSAP" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">
    <div class="container-fluid">
        <div class="row">
            <div class="col-md-8">
                <asp:Panel ID="pnlPromoHeader" runat="server" CssClass="Panel">
                    <label class="PanelHeading">Warehouse Receipt vs SAP</label>
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
            <div class="col-md-8">
                <h3 class="myLabelFull">Warehouse Receipt vs SAP</h3>
            </div>
        </div>
        <div class="row" style="text-align: center;" runat="server" id="divHeading1" visible="false">
            <div class="col-md-8">
                <asp:Panel ID="Panel1" runat="server" CssClass="Panel">
                    <div class="row">
                        <div class="col-md-4">
                            <label class="myLabelLarge">#APRI / #Order:</label>
                            <asp:Label runat="server" ID="lblAPRI" CssClass="myLabelMedium" />
                        </div>
                        <div class="col-md-2">
                            <label class="myLabelMedium">Invoice:</label>
                            <asp:Label runat="server" ID="lblFactura" CssClass="myLabelMedium" />
                        </div>
                        <div class="col-md-2">
                            <label class="myLabelMedium">Status Rec:</label>
                            <asp:Label runat="server" ID="lblStatus" CssClass="myLabelMedium" />
                        </div>
						
						<%--2019-JUN-12, Modificación para v3.1, por Aldo Reina: Agregando el campo ScannedBarCode:--%>
                        <div class="col-md-2">
                            <label class="myLabelMedium">Status SAP:</label>
                            <asp:Label runat="server" ID="lblStatusSap" CssClass="myLabelMedium" />
                        </div>    
                        <%--2019-JUN-12, Fin Modificación para v3.1, por Aldo Reina: Agregando el campo ScannedBarCode--%>

                        <div class="col-md-2">
                            <label class="myLabelMedium">#Order: </label>
                            <asp:Label runat="server" ID="lblOrder" CssClass="myLabelMedium" />
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </div>
        <div class="row">&nbsp;
        </div>
        <div class="row">
            <div class="col-md-8">
                <tel:RadGrid ID="rgHead" AllowSorting="True" CssClass="Panel" PageSize="20" AllowPaging="True" runat="server" ShowStatusBar="true" AutoGenerateColumns="False"
                    OnNeedDataSource="rgHead_NeedDataSource" OnItemCommand="rgHead_ItemCommand"
                    OnExcelMLExportRowCreated="rgHead_ExcelMLExportRowCreated" OnExcelMLExportStylesCreated="rgHead_ExcelMLExportStylesCreated"
                    Visible="false">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <ExportSettings OpenInNewWindow="true" IgnorePaging="true" Excel-Format="Xlsx" FileName="WarehouseReceiptVsSAP" UseItemStyles="true" />
                    <MasterTableView Width="100%" DataKeyNames="ItemCode" Name="ReciboBodegavsSAP" AllowNaturalSort="false" CommandItemDisplay="Top" ShowFooter="true" FooterStyle-Font-Bold="true">
                        <CommandItemSettings ShowExportToExcelButton="true" ShowAddNewRecordButton="false" />
                        <Columns>
                            <tel:GridBoundColumn SortExpression="ItemCode" HeaderText="Item Code" HeaderButtonType="TextButton"
                                DataField="ItemCode" UniqueName="ItemCode" HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="BarCode" HeaderText="Bar Code" HeaderButtonType="TextButton"
                                DataField="BarCode" UniqueName="BarCode" HeaderStyle-Width="100px" />
                            
							<%--2019-JUN-12, Modificación para v3.1, por Aldo Reina: Agregando el campo ScannedBarCode:--%>
                            <tel:GridBoundColumn SortExpression="ScannedBarCode" HeaderText="Scanned Bar Code" HeaderButtonType="TextButton"
                                DataField="ScannedBarCode" UniqueName="ScannedBarCode" HeaderStyle-Width="160px" />
                            <%--2019-JUN-12, Fin Modificación para v3.1, por Aldo Reina: Agregando el campo ScannedBarCode--%>

							<tel:GridBoundColumn SortExpression="itembrand" HeaderText="Brand" HeaderButtonType="TextButton"
                                DataField="itembrand" UniqueName="itembrand" HeaderStyle-Width="160px" />
                            <tel:GridBoundColumn SortExpression="itemname" HeaderText="Description" HeaderButtonType="TextButton"
                                DataField="itemname" UniqueName="itemname" HeaderStyle-Width="350px" ItemStyle-Wrap="false" />
                            <tel:GridBoundColumn SortExpression="CantSAP" HeaderText="Cant SAP" HeaderButtonType="TextButton"
                                DataField="CantSAP" UniqueName="CantSAP" HeaderStyle-Width="130px" ItemStyle-Wrap="false" DataFormatString="{0:0.00}" />
                            <tel:GridBoundColumn SortExpression="CantReciboBodega" HeaderText="Warehouse Receipt" HeaderButtonType="TextButton"
                                DataField="CantReciboBodega" UniqueName="CantReciboBodega" HeaderStyle-Width="130px" ItemStyle-Wrap="false" DataFormatString="{0:0.00}" />
                            <tel:GridBoundColumn SortExpression="Dif" HeaderText="Dif" HeaderButtonType="TextButton"
                                DataField="Dif" UniqueName="Dif" HeaderStyle-Width="130px" ItemStyle-Wrap="false" DataFormatString="{0:0.00}" />
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
</asp:Content>

