<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="R_OrdenesConProblemas.aspx.cs" Inherits="R_OrdenesConProblemas" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">
    <div class="container-fluid">
        <div class="row">
            <div class="col-md-12">
                <asp:Panel ID="pnlPromoHeader" runat="server" CssClass="Panel">
                    <label class="PanelHeading">Orders with Issues</label>
                    <div class="row">
                        <%--<div class="col-md-4">
                            &nbsp;&nbsp;
                            <label class="myLabelSmall">Date: </label>
                            <tel:RadTextBox ID="rtbOrden" runat="server" Width="120px" />
                            <asp:HiddenField ID="hfOrden" runat="server" />
                            &nbsp;&nbsp;
                        </div>--%>
                        <div class="col-md-4">
                            &nbsp;&nbsp;
                            <label class="myLabelSmall">Date: </label>
                            <telerik:RadDatePicker ID="rdpDate" runat="server" ShowPopupOnFocus="true" Width="130px" />
                            <asp:HiddenField ID="hfDate" runat="server" />
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
                <h3 class="myLabelFull">Orders with Issues</h3>
            </div>
        </div>
        <div class="row">
            <div class="col-md-12">
                <tel:RadGrid ID="rgHead" AllowSorting="True" CssClass="Panel" PageSize="20" AllowPaging="True" runat="server" ShowStatusBar="true" AutoGenerateColumns="False"
                    OnNeedDataSource="rgHead_NeedDataSource" OnItemCommand="rgHead_ItemCommand"
                    Visible="false">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <ExportSettings OpenInNewWindow="true" IgnorePaging="true" Excel-Format="Xlsx" FileName="OrdersWithIssues" UseItemStyles="true" />
                    <MasterTableView Width="100%" DataKeyNames="OSDocNum" Name="BinesDesp" AllowNaturalSort="false" CommandItemDisplay="Top" ShowFooter="true" FooterStyle-Font-Bold="true">
                        <CommandItemSettings ShowExportToExcelButton="true" ShowAddNewRecordButton="false" />
                        <Columns>
                            <tel:GridBoundColumn SortExpression="OSDocNum" HeaderText="Sales Order" HeaderButtonType="TextButton"
                                DataField="OSDocNum" UniqueName="OSDocNum" HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="Created_Date" HeaderText="Date" HeaderButtonType="TextButton"
                                DataField="Created_Date" UniqueName="Created_Date" HeaderStyle-Width="150px" ItemStyle-Wrap="false" />
                            <tel:GridBoundColumn SortExpression="OriClient" HeaderText="Ori Client" HeaderButtonType="TextButton"
                                DataField="OriClient" UniqueName="OriClient" HeaderStyle-Width="110px" ItemStyle-Wrap="false" />
                            <tel:GridBoundColumn SortExpression="DesCompanyID" HeaderText="Des Company ID" HeaderButtonType="TextButton"
                                DataField="DesCompanyID" UniqueName="DesCompanyID" HeaderStyle-Width="120px" ItemStyle-Wrap="false" />
                            <tel:GridBoundColumn SortExpression="ErrorPosted" HeaderText="Error Posted" HeaderButtonType="TextButton"
                                DataField="ErrorPosted" UniqueName="ErrorPosted" />
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

