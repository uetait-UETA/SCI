<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="R_InventarioDeTiendasTocumen.aspx.cs" Inherits="R_InventarioDeTiendasTocumen" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">
    <div class="container-fluid">
        <div class="row">
            <div class="col-md-12">
                <asp:Panel ID="pnlPromoHeader" runat="server" CssClass="Panel">
                    <label class="PanelHeading">Store Inventory</label>
                    <div class="row">
                        <div class="col-md-4">
                            &nbsp;&nbsp;
                            <label class="myLabelSmall">Group:</label>
                            <tel:RadComboBox ID="rcbGrupo" runat="server" Height="120px" DropDownAutoWidth="Disabled" Width="230px"
                                HighlightTemplatedItems="true"
                                AppendDataBoundItems="true" CheckBoxes="true"
                                EmptyMessage="Select Group" AutoPostBack="true" OnSelectedIndexChanged="rcbGrupo_SelectedIndexChanged"
                                Font-Italic="false">
                                <ExpandAnimation Type="OutQuart" Duration="500" />
                                <CollapseAnimation Type="OutQuint" Duration="300" />
                            </tel:RadComboBox>
                            &nbsp;&nbsp;
                        </div>
                        <div class="col-md-4">
                            &nbsp;&nbsp;
                            <label class="myLabelSmall">Brand</label>
                            <tel:RadComboBox ID="rcbMarca" runat="server" Height="120px" DropDownAutoWidth="Disabled" Width="230px"
                                HighlightTemplatedItems="true"
                                AppendDataBoundItems="true"
                                EmptyMessage="Select Brand" AutoPostBack="true" OnSelectedIndexChanged="rcbMarca_SelectedIndexChanged"
                                Font-Italic="false">
                                <ExpandAnimation Type="OutQuart" Duration="500" />
                                <CollapseAnimation Type="OutQuint" Duration="300" />
                            </tel:RadComboBox>
                            &nbsp;&nbsp;
                        </div>
                        <div class="col-md-4">
                            &nbsp;&nbsp;
                            <tel:RadButton runat="server" ID="rbtnView" Text="View Report" OnClick="rbtnView_Click" />
                            &nbsp;&nbsp;
                            <tel:RadButton runat="server" ID="rbtnExport" Text="Export to Excel" OnClick="rbtnExport_Click" Visible="false" />
                        </div>
                    </div>
                    <div class="row">&nbsp;</div>
                </asp:Panel>
            </div>
        </div>
        <div class="row" style="text-align: center;" runat="server" id="divHeading" visible="false">
            <div class="col-md-12">
                <h3 class="myLabelFull">Store Inventory</h3>
            </div>
        </div>
        <div class="row">
            <div class="col-md-12">
                <tel:RadPivotGrid ID="rpgHead" AllowSorting="true" CssClass="Panel" PageSize="20" AllowPaging="True" runat="server" 
                    AutoGenerateColumns="False" ShowFilterHeaderZone="false"
                    OnNeedDataSource="rpgHead_NeedDataSource" Visible="false" RowTableLayout="Tabular" 
                    ShowColumnHeaderZone="false" ShowRowHeaderZone="true" ShowDataHeaderZone="false" 
                    Height="400px" OnPivotGridCellExporting="rpgHead_PivotGridCellExporting" OnCellDataBound="rpgHead_CellDataBound">
                    <TotalsSettings ColumnsSubTotalsPosition="None" ColumnGrandTotalsPosition="None" RowsSubTotalsPosition="None" />
                    <ClientSettings>
                        <Scrolling AllowVerticalScroll="true"></Scrolling>
                    </ClientSettings>
                    <ExportSettings OpenInNewWindow="true" IgnorePaging="true" Excel-Format="Xlsx" FileName="StoreInventoryTocumen" UseItemStyles="true" />
                    <Fields>
                        <tel:PivotGridRowField DataField="ItmsGrpNam" UniqueName="ItmsGrpNam" CellStyle-Width="120px" />
                        <tel:PivotGridRowField DataField="U_BRAND" UniqueName="U_BRAND" CellStyle-Width="120px" />
                        <tel:PivotGridRowField DataField="u_class" UniqueName="u_class" CellStyle-Width="95px" />
                        <tel:PivotGridRowField DataField="ItemCode" UniqueName="ItemCode" Caption="ItemCode" CellStyle-Width="95px" DataFormatString="{0:0}" />
                        <tel:PivotGridRowField DataField="BarCode" UniqueName="BarCode" Caption="BarCode" CellStyle-Width="95px" DataFormatString="{0:0}" />
                        <tel:PivotGridRowField DataField="ItemName" UniqueName="ItemName" CellStyle-Width="300px" />
                        <tel:PivotGridColumnField DataField="cia" />
                        <tel:PivotGridColumnField DataField="WhsCode" />
                        <tel:PivotGridAggregateField DataField="OnHand" Caption="OnHand" Aggregate="Sum" DataFormatString="{0:N0}"  />
                        <tel:PivotGridAggregateField DataField="OnOrder" Aggregate="Sum" DataFormatString="{0:N0}" />
                        <tel:PivotGridAggregateField DataField="IsCommited" Aggregate="Sum" DataFormatString="{0:N0}" />
                    </Fields>
                </tel:RadPivotGrid>
            </div>
        </div>
    </div>
</asp:Content>

