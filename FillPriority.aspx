<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="FillPriority.aspx.cs" Inherits="FillPriority" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">
    <div class="container">
        <div class="row">
            <div class="col-md-7">
                <asp:Panel ID="pnlHeader" runat="server" CssClass="Panel">
                    <label ID="labelForm" runat="server" class="PanelHeading">Store / Warehouse Fill Priority</label>
                    <div class="row">
                        <div class="col-md-5">
                            &nbsp;&nbsp;
                            <label class="myLabelXtraSmall">Category</label>
                            <tel:RadComboBox ID="rcbCategory" runat="server" Height="120px" DropDownAutoWidth="Disabled"
                                HighlightTemplatedItems="true"
                                AppendDataBoundItems="true"
                                EmptyMessage="Select a Category"
                                AutoPostBack="true" CheckBoxes="false" EnableCheckAllItemsCheckBox="false"
                                OnSelectedIndexChanged="rcbCategory_SelectedIndexChanged"
                                Font-Italic="false">
                                <ExpandAnimation Type="OutQuart" Duration="500" />
                                <CollapseAnimation Type="OutQuint" Duration="300" />
                            </tel:RadComboBox>
                            &nbsp;&nbsp;
                        </div>
                        <div class="col-md-2">
                            &nbsp;&nbsp;
                            <tel:RadButton runat="server" ID="rbtnSave" Text="Save" OnClick="rbtnSave_Click" />
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </div>
        <div class="row">&nbsp;</div>
        <div class="row" id="divGrid" runat="server" style="display:none;">
            <div class="col-md-5">
                <tel:RadGrid ID="rgPriority" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False"
                    AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="True" PageSize="20" CssClass="Panel"
                    MasterTableView-NoDetailRecordsText="No data for your selection!"
                    OnNeedDataSource="rgPriority_NeedDataSource">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <MasterTableView Width="100%" AllowNaturalSort="false">
                        <Columns>
                            <tel:GridBoundColumn SortExpression="U_POSCode" HeaderText="POS Code" DataField="U_POSCode" UniqueName="U_POSCode" ReadOnly="true" HeaderStyle-Width="80px" />
                            <tel:GridBoundColumn SortExpression="Location" HeaderText="Warehouse" DataField="Location" UniqueName="Location" ReadOnly="true" HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="LocationName" HeaderText="Location Name" DataField="LocationName" UniqueName="LocationName" ReadOnly="true" />
                            <tel:GridTemplateColumn UniqueName="Priority" HeaderText="Priority" HeaderStyle-Width="70px" ItemStyle-HorizontalAlign="Center">
                                <ItemTemplate>
                                    <asp:TextBox ID="txtPriority" runat="server" Text='<%#Eval("Priority")%>' Width="50px" MaxLength="2" style="text-align:center;" />
                                </ItemTemplate>
                            </tel:GridTemplateColumn>
                        </Columns>
                    </MasterTableView>
                    <ClientSettings>
                        <Resizing AllowColumnResize="true" />
                        <Selecting AllowRowSelect="true" />
                    </ClientSettings>
                </tel:RadGrid>
            </div>
        </div>
        <div class="row" style="margin-top:16px;">
            <div class="col-md-7">
                <asp:Panel runat="server" CssClass="Panel">
                    <label class="PanelHeading">Import / Export</label>
                    <div class="row" style="margin:10px 0 6px 4px;">
                        <div class="col-md-12">
                            <asp:Button ID="btnExport" runat="server" Text="Export All to Excel" OnClick="btnExport_Click" CssClass="btn btn-default" />
                            <small style="margin-left:8px; color:gray;">Exports all categories x warehouses with current priorities (99 = no priority)</small>
                        </div>
                    </div>
                    <div class="row" style="margin:10px 0 6px 4px;">
                        <div class="col-md-12">
                            <asp:FileUpload ID="fuExcel" runat="server" />
                            <asp:Button ID="btnPreview" runat="server" Text="Preview" OnClick="btnPreview_Click" CssClass="btn btn-default" style="margin-left:8px;" />
                            <asp:Button ID="btnImport" runat="server" Text="Import" OnClick="btnImport_Click" CssClass="btn btn-primary" style="margin-left:4px;" Enabled="false" />
                            <small style="margin-left:8px; color:gray;">Excel columns: CategoryCode, POSCode, Priority (1-99)</small>
                        </div>
                    </div>
                    <div id="divImportGrid" runat="server" style="display:none; margin-top:10px;">
                        <asp:Label ID="lblImportMsg" runat="server" style="font-weight:bold;" /><br />
                        <asp:GridView ID="gvImport" runat="server" AutoGenerateColumns="false" CssClass="table table-bordered table-condensed" Width="100%">
                            <Columns>
                                <asp:BoundField DataField="CategoryCode" HeaderText="Cat. Code" ItemStyle-Width="80px" />
                                <asp:BoundField DataField="CategoryName" HeaderText="Category" />
                                <asp:BoundField DataField="POSCode"      HeaderText="POS Code"  ItemStyle-Width="100px" />
                                <asp:BoundField DataField="Priority"     HeaderText="Priority"  ItemStyle-Width="70px" ItemStyle-HorizontalAlign="Center" />
                                <asp:BoundField DataField="Status"       HeaderText="Status" />
                            </Columns>
                        </asp:GridView>
                    </div>
                </asp:Panel>
            </div>
        </div>
    </div>
</asp:Content>
