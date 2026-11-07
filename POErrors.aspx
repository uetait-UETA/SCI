<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="POErrors.aspx.cs" Inherits="POErrors" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cphMain" runat="Server">

    <div class="container-fluid">
        <div class="row">
            <div class="col-md-12">
                <asp:Panel ID="pnlHeader" runat="server" CssClass="page-curl shadow-bottom">
                    <label id="labelForm" runat="server" class="PanelHeading">PO Errors</label>
                    <div class="row">
                        <div class="col-md-6 radio">
                            <asp:RadioButton ID="radioShowOpen" runat="server" GroupName="grpDisplay"
                                Text="Show unresolved errors only" Checked="True" AutoPostBack="false" />
                            <asp:RadioButton ID="radioShowAll" runat="server" GroupName="grpDisplay"
                                Text="Show all errors" AutoPostBack="false" />
                        </div>
                        <div class="col-md-6">
                            <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="mybtnmeduim" OnClick="btnSearch_Click" />
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </div>
        <div class="row">&nbsp;</div>
        <div class="row">
            <div class="col-md-12">
                <h3 class="myLabelFull">PO Errors</h3>
                <p style="color:#004990; font-size:9pt;">
                    Items added via "Add Item Not On Invoice" (ComprasDirectasDetail.aspx) that failed to post
                    because SAP has no usable cost for them (no OITW warehouse cost, no Price List 3 price).
                    Fix the cost in SAP, then click "Reprocess" below to retry that document without having
                    to reopen it and confirm the whole receipt again.
                </p>
                <asp:Label ID="lblResult" runat="server" Font-Bold="True" Font-Size="Medium" />
            </div>
        </div>
        <div class="row">&nbsp;</div>
        <div class="row">
            <div class="col-md-12">
                <tel:RadGrid ID="rgHead" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False"
                    MasterTableView-NoDetailRecordsText="No PO errors found"
                    AllowSorting="true" AllowPaging="True" PageSize="25"
                    OnNeedDataSource="rgHead_NeedDataSource" OnItemDataBound="rgHead_ItemDataBound"
                    OnItemCommand="rgHead_ItemCommand">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <MasterTableView Width="100%" AllowNaturalSort="false" DataKeyNames="Id">
                        <ItemStyle Wrap="false" Height="22px" />
                        <Columns>
                            <tel:GridTemplateColumn HeaderText="Doc" SortExpression="DocEntry" UniqueName="DocEntry" HeaderStyle-Width="70px">
                                <ItemTemplate>
                                    <asp:HyperLink ID="hlDocEntry" runat="server" ForeColor="#0066CC" />
                                </ItemTemplate>
                            </tel:GridTemplateColumn>
                            <tel:GridBoundColumn SortExpression="DocType" HeaderText="Type" HeaderButtonType="TextButton"
                                DataField="DocType" UniqueName="DocType" HeaderStyle-Width="60px" />
                            <tel:GridBoundColumn SortExpression="ItemCode" HeaderText="Item Code" HeaderButtonType="TextButton"
                                DataField="ItemCode" UniqueName="ItemCode" HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="ItemName" HeaderText="Description" HeaderButtonType="TextButton"
                                DataField="ItemName" UniqueName="ItemName" HeaderStyle-Width="220px" />
                            <tel:GridBoundColumn SortExpression="WhsCode" HeaderText="Whs" HeaderButtonType="TextButton"
                                DataField="WhsCode" UniqueName="WhsCode" HeaderStyle-Width="70px" />
                            <tel:GridBoundColumn SortExpression="Quantity" HeaderText="Qty" HeaderButtonType="TextButton"
                                DataField="Quantity" UniqueName="Quantity" HeaderStyle-Width="60px" DataFormatString="{0:N0}" />
                            <tel:GridBoundColumn SortExpression="Reason" HeaderText="Reason" HeaderButtonType="TextButton"
                                DataField="Reason" UniqueName="Reason" HeaderStyle-Width="260px" />
                            <tel:GridBoundColumn SortExpression="LoggedAt" HeaderText="Logged At" HeaderButtonType="TextButton"
                                DataField="LoggedAt" UniqueName="LoggedAt" HeaderStyle-Width="130px" DataFormatString="{0:MM/dd/yyyy HH:mm}" />
                            <tel:GridTemplateColumn HeaderText="Status" SortExpression="Resolved" UniqueName="Resolved" HeaderStyle-Width="90px">
                                <ItemTemplate>
                                    <asp:Label ID="lblStatus" runat="server" Font-Bold="true" />
                                </ItemTemplate>
                            </tel:GridTemplateColumn>
                            <tel:GridTemplateColumn HeaderText="" UniqueName="Reprocess" HeaderStyle-Width="90px">
                                <ItemTemplate>
                                    <asp:LinkButton ID="btnReprocess" runat="server" CommandName="Reprocess"
                                        CommandArgument='<%# Eval("DocEntry") + "|" + Eval("DocType") %>'
                                        Text="Reprocess" ForeColor="#0066CC" Font-Bold="true"
                                        OnClientClick="return confirm('Reprocess all pending extra items for this document in SAP?');" />
                                </ItemTemplate>
                            </tel:GridTemplateColumn>
                        </Columns>
                    </MasterTableView>
                    <ClientSettings>
                        <Resizing AllowColumnResize="true" />
                        <Scrolling AllowScroll="true" />
                    </ClientSettings>
                </tel:RadGrid>
            </div>
        </div>
    </div>

</asp:Content>
