<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="R_Kardex.aspx.cs" Inherits="R_Kardex" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">
    <div class="container-fluid">
        <div class="row">
            <div class="col-md-12">
                <asp:Panel ID="pnlPromoHeader" runat="server" CssClass="Panel">

                    <asp:HiddenField ID="hfFromDate" runat="server" />
                    <asp:HiddenField ID="hfToDate" runat="server" />
                    <label class="PanelHeading">Inventory Movement by Items</label>
                    <div class="row">
                        <div class="col-md-12">
                            <ul class="myUL">
                                <li style="display: inline;">&nbsp;&nbsp;
                                    <label class="myLabelXtraSmall">Company</label>
                                    <tel:RadComboBox ID="rcbCorte" runat="server" Height="120px" DropDownAutoWidth="Disabled" Width="120px" Enabled="false"
                                        HighlightTemplatedItems="true"
                                        AppendDataBoundItems="true"
                                        EmptyMessage="Select Company" AutoPostBack="true" OnSelectedIndexChanged="rcbCorte_SelectedIndexChanged"
                                        Font-Italic="false">
                                        <ExpandAnimation Type="OutQuart" Duration="500" />
                                        <CollapseAnimation Type="OutQuint" Duration="300" />
                                    </tel:RadComboBox>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
                                </li>
                                <li style="display: inline;">
                                    <label class="myLabelXtraSmall">Group</label>
                                    <tel:RadComboBox ID="rcbGrupo" runat="server" Height="200px" DropDownAutoWidth="Disabled" Width="180px"
                                        HighlightTemplatedItems="true"
                                        AppendDataBoundItems="true"
                                        EmptyMessage="Select Group"
                                        Font-Italic="false">
                                        <ExpandAnimation Type="OutQuart" Duration="500" />
                                        <CollapseAnimation Type="OutQuint" Duration="300" />
                                    </tel:RadComboBox>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
                                </li>
                                <li style="display: inline;">
                                    <label class="myLabelMedium">Item &#47; BarCode</label>
                                    <tel:RadTextBox ID="rtbItem" runat="server" Width="100px" />&nbsp;&nbsp;
                                    <asp:DropDownList ID="ItemList" ClientIDMode="Static" runat="server" Visible="false" AutoPostBack="True" OnSelectedIndexChanged="ItemList_SelectedIndexChanged"></asp:DropDownList>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
                                </li>
                                <li style="display: inline;">
                                    <asp:Label ID="fromDateLabel" runat="server" Text="From Date" CssClass="myLabel" />
                                    <telerik:RadDatePicker ID="FromDateTxt" runat="server" ShowPopupOnFocus="true" Width="130px" />&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
                                </li>
                                <li style="display: inline;">
                                    <asp:Label ID="toDateLabel" runat="server" Text="To Date" CssClass="myLabel" />
                                    <telerik:RadDatePicker ID="toDateTxt" runat="server" ShowPopupOnFocus="true" Width="130px" />&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
                                </li>
                                <li style="display: inline;">

                                    <tel:RadButton RenderMode="Lightweight" ID="rbtnCancel1" runat="server" Text="Cancel" OnClick="RbtnCancel_Click" Visible="False" />
                                    &nbsp;&nbsp;
                                    &nbsp;&nbsp;
                                    <tel:RadButton runat="server" ID="rbtnView" Text="View Report" OnClick="rbtnView_Click" />
                                </li>
                            </ul>
                        </div>
                    </div>
                    <div class="row">&nbsp;</div>
                </asp:Panel>
            </div>
        </div>
        <div class="row" style="text-align: center;" runat="server" id="divHeading" visible="false">
            <div class="col-md-8">
                <h3 class="myLabelFull">Inventory Movement by Items</h3>
            </div>
        </div>
        <div class="row">
            &nbsp;
        </div>
        <div class="row">
            <div class="col-sm-8">
                <asp:Panel ID="pnlInfo" runat="server" CssClass="Panel" Visible="false">
                    <ul class="myUL">
                        <li>
                            <br />
                            <label class="myLabelXLarge">Item</label>
                            <tel:RadLabel CssClass="myLabel" runat="server" ID="rlblItem" />
                        </li>
                        <li>
                            <label class="myLabelXLarge">Description</label>
                            <tel:RadLabel CssClass="myLabelXXXXLarge" runat="server" ID="rlblDesc" />
                        </li>
                        <li>
                            <label class="myLabelXLarge">Possible Barcodes</label>
                            <tel:RadLabel CssClass="myLabelXXXXLarge" runat="server" ID="rlblBarCode" />
                        </li>
                    </ul>
                </asp:Panel>
            </div>
        </div>

        <div class="row">
            &nbsp;
        </div>
        <div class="row">
            <div class="col-md-8">
                <tel:RadGrid ID="rgHead" AllowSorting="True" CssClass="Panel" PageSize="20" AllowPaging="false" runat="server" ShowStatusBar="true" AutoGenerateColumns="False"
                    OnNeedDataSource="rgHead_NeedDataSource" OnItemCommand="rgHead_ItemCommand" 
                    Visible="false">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <ExportSettings OpenInNewWindow="true" IgnorePaging="true" Excel-Format="Xlsx" FileName="InventoryMovementByItems" UseItemStyles="true" />
                    <MasterTableView Width="100%" DataKeyNames="TransNum" Name="MovimientodeInventarioporItems" AllowNaturalSort="false"
                        CommandItemDisplay="Top" ShowFooter="true" FooterStyle-Font-Bold="true" ShowGroupFooter="true">
                        <CommandItemSettings ShowExportToExcelButton="false" ShowAddNewRecordButton="false" ShowExportToCsvButton="true" />
                        <Columns>
                            <tel:GridBoundColumn SortExpression="TransNum" HeaderText="Trans Num" HeaderButtonType="TextButton"
                                DataField="TransNum" UniqueName="TransNum" HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="DocDate" HeaderText="Doc Date" HeaderButtonType="TextButton"
                                DataField="DocDate" UniqueName="DocDate" HeaderStyle-Width="120px" DataFormatString="{0:d}" />
                            <tel:GridBoundColumn SortExpression="BaseDoc" HeaderText="Base Doc" HeaderButtonType="TextButton"
                                DataField="BaseDoc" UniqueName="BaseDoc" HeaderStyle-Width="120px" />
                            <tel:GridBoundColumn SortExpression="CreatedBy" HeaderText="Num. Doc." HeaderButtonType="TextButton"
                                DataField="CreatedBy" UniqueName="CreatedBy" HeaderStyle-Width="120px" ItemStyle-Wrap="false" />
                            <tel:GridBoundColumn SortExpression="NomDoc" HeaderText="Nom. Doc." HeaderButtonType="TextButton"
                                DataField="NomDoc" UniqueName="NomDoc" HeaderStyle-Width="130px" ItemStyle-Wrap="false" />
                            <tel:GridBoundColumn SortExpression="ItemCode" HeaderText="Item Code" HeaderButtonType="TextButton"
                                DataField="ItemCode" UniqueName="ItemCode" HeaderStyle-Width="130px" ItemStyle-Wrap="false" />
                            <tel:GridBoundColumn SortExpression="Warehouse" HeaderText="Warehouse" HeaderButtonType="TextButton"
                                DataField="Warehouse" UniqueName="Warehouse" HeaderStyle-Width="130px" ItemStyle-Wrap="false" FooterText="Total:" />
                            <tel:GridBoundColumn SortExpression="U_POSCode" HeaderText="POS Code" HeaderButtonType="TextButton"
                                DataField="U_POSCode" UniqueName="U_POSCode" HeaderStyle-Width="100px" ItemStyle-Wrap="false" />
                            <tel:GridBoundColumn SortExpression="QtyTrans" HeaderText="Qty Trans" HeaderButtonType="TextButton"
                                DataField="QtyTrans" UniqueName="QtyTrans" HeaderStyle-Width="130px" ItemStyle-Wrap="false" Aggregate="Sum" DataFormatString="{0:N0}" />
                            <tel:GridBoundColumn SortExpression="CurrentBalance" HeaderText="Balance" HeaderButtonType="TextButton"
                                DataField="CurrentBalance" UniqueName="CurrentBalance" HeaderStyle-Width="130px" ItemStyle-Wrap="false" DataFormatString="{0:N0}" />
                            <tel:GridBoundColumn Display="false" Visible="false" SortExpression="Balance" HeaderText="Balance Global" HeaderButtonType="TextButton"
                                DataField="Balance" UniqueName="Balance" HeaderStyle-Width="130px" ItemStyle-Wrap="false" />
                        </Columns>
                        <GroupByExpressions>
                            <telerik:GridGroupByExpression>
                                <GroupByFields>
                                    <telerik:GridGroupByField FieldName="ItemCode"></telerik:GridGroupByField>
                                </GroupByFields>
                                <SelectFields>
                                    <telerik:GridGroupByField FieldName="ItemCode" HeaderText="Item Code"></telerik:GridGroupByField>
                                </SelectFields>
                            </telerik:GridGroupByExpression>
                        </GroupByExpressions>
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

