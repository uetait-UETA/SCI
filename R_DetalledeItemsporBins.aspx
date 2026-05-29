<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="R_DetalledeItemsporBins.aspx.cs" Inherits="R_DetalledeItemsporBins" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">
    <div class="container-fluid">
        <div class="row">
            <div class="col-md-8">
                <asp:Panel ID="pnlPromoHeader" runat="server" CssClass="Panel">
                    <label class="PanelHeading">Item Detail by Bins - TOCUMEN</label>
                    <div class="row">
                        <div class="col-md-4">
                            &nbsp;&nbsp;
                            <label class="myLabelSmall">Bin No.</label>
                            <tel:RadTextBox ID="rtbBin" runat="server" Width="120px" />
                            <asp:HiddenField ID="hfBin" runat="server" />
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
            <div class="col-md-8">
                <h3 class="myLabelFull">Item Detail by Bins - TOCUMEN</h3>
            </div>
        </div>
        <div class="row">
            <div class="col-md-8">
                <tel:RadGrid ID="rgHead" AllowSorting="True" CssClass="Panel" PageSize="20" AllowPaging="True" runat="server" ShowStatusBar="true" AutoGenerateColumns="False"
                    OnNeedDataSource="rgHead_NeedDataSource" OnItemCommand="rgHead_ItemCommand"
                    OnExcelMLExportRowCreated="rgHead_ExcelMLExportRowCreated" OnExcelMLExportStylesCreated="rgHead_ExcelMLExportStylesCreated" 
                    Visible="false">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <ExportSettings OpenInNewWindow="true" IgnorePaging="true" Excel-Format="Xlsx" FileName="BinItemDetail" UseItemStyles="true" />
                    <MasterTableView Width="100%" DataKeyNames="PRODUCT" Name="BinesDesp" AllowNaturalSort="false" CommandItemDisplay="Top" ShowFooter="true" FooterStyle-Font-Bold="true">
                        <CommandItemSettings ShowExportToExcelButton="true" ShowAddNewRecordButton="false" />
                        <Columns>
                            <tel:GridBoundColumn SortExpression="PRODUCT" HeaderText="Product" HeaderButtonType="TextButton"
                                DataField="PRODUCT" UniqueName="PRODUCT" HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="BarCode" HeaderText="Bar Code" HeaderButtonType="TextButton"
                                DataField="BarCode" UniqueName="BarCode" HeaderStyle-Width="150px" />
                            <tel:GridBoundColumn SortExpression="DESCRIPT" HeaderText="Description" HeaderButtonType="TextButton"
                                DataField="DESCRIPT" UniqueName="DESCRIPT" HeaderStyle-Width="400px" ItemStyle-Wrap="false" />
                            <tel:GridBoundColumn SortExpression="QTY" HeaderText="Quantity" HeaderButtonType="TextButton" 
                                DataField="QTY" UniqueName="QTY" HeaderStyle-Width="130px" ItemStyle-Wrap="false" DataFormatString="{0:N0}" />
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

