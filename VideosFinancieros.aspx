<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="VideosFinancieros.aspx.cs" Inherits="VideosFinancieros" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">
    <div class="container-fluid">
        <div class="row">
            <div class="col-md-12">
                <asp:Panel ID="pnlAddApps" runat="server" CssClass="Panel" Height="65px">
                    <label class="PanelHeading">Search Financial Videos</label>
                    <div class="row">
                        <div class="col-md-12">
                            <label class="myLabelSmall">&nbsp; Cases</label>
                            <tel:RadTextBox ID="rtbCasos" runat="server" Width="300px" />
                            <tel:RadButton ID="rbtnSearch" runat="server" Text="Search" OnClick="rbtnSearch_Click" ToolTip="Search">
                                <Icon SecondaryIconCssClass="fa fa-search icon-green" SecondaryIconRight="4" SecondaryIconTop="5" />
                            </tel:RadButton>
                        </div>

                    </div>
                </asp:Panel>
            </div>
        </div>
        <div class="row" style="text-align: center;">
            <div class="col-md-12">
                <h3 class="myLabelFull">Financial Videos</h3>
            </div>
        </div>
        <div class="row">
            <div class="col-md-12">
                <tel:RadGrid ID="rgHead" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False"
                    AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="True" PageSize="15" CssClass="Panel"
                    OnNeedDataSource="rgHead_NeedDataSource" OnItemDataBound="rgHead_ItemDataBound">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <ExportSettings OpenInNewWindow="true" IgnorePaging="true" Excel-Format="Xlsx" HideStructureColumns="true" FileName="FinancialVideos" />
                    <MasterTableView Width="100%" AllowNaturalSort="false" DataKeyNames="FAQID" CommandItemDisplay="Top">
                        <CommandItemSettings ShowExportToExcelButton="false" ShowAddNewRecordButton="false" ShowRefreshButton="false" />
                        <Columns>
                            <tel:GridBoundColumn SortExpression="Casos" HeaderText="Cases" HeaderButtonType="TextButton" DataField="Casos" UniqueName="Casos" HeaderStyle-Width="300px" />
                            <tel:GridBoundColumn SortExpression="Solucion" HeaderText="Solution" HeaderButtonType="TextButton" DataField="Solucion" UniqueName="Solucion" />
                            <tel:GridTemplateColumn DataField="SolucionImage" HeaderText="Image" UniqueName="SolucionImage">
                                <ItemTemplate>
                                    <tel:RadBinaryImage ID="Image1" ImageUrl='<%# (string) Eval("SolucionImageUrl") %>' AlternateText='<%# (string) Eval("SolucionImageUrl") %>' runat="server" Width="80px" Height="80px" />
                                </ItemTemplate>
                            </tel:GridTemplateColumn>
                            <tel:GridHyperLinkColumn SortExpression="SolucionLink" HeaderText="Solution Link" HeaderButtonType="TextButton" DataTextField="SolucionLink" DataNavigateUrlFields="SolucionLink" UniqueName="SolucionLink" Target="_blank" />
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

