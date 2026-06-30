<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="FillPriority.aspx.cs" Inherits="FillPriority" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">
    <div class="container">
        <div class="row">
            <div class="col-md-10">
                <asp:Panel ID="pnlHeader" runat="server" CssClass="Panel">
                    <label ID="labelForm" runat="server" class="PanelHeading">Fill Category Priority</label>
                    <div class="row" style="margin-bottom:6px;">
                        <div class="col-md-3">
                            &nbsp;&nbsp;
                            <label class="myLabelXtraSmall">Category</label>
                            <tel:RadComboBox ID="rcbCategoryFilter" runat="server" Height="150px"
                                Width="200px" DropDownWidth="220px" DropDownAutoWidth="Disabled"
                                HighlightTemplatedItems="true" AppendDataBoundItems="true"
                                AutoPostBack="true" CheckBoxes="false" EnableCheckAllItemsCheckBox="false"
                                OnSelectedIndexChanged="rcbCategoryFilter_SelectedIndexChanged"
                                Font-Italic="false">
                                <Items>
                                    <tel:RadComboBoxItem Text="(All Categories)" Value="" />
                                </Items>
                                <ExpandAnimation Type="OutQuart" Duration="500" />
                                <CollapseAnimation Type="OutQuint" Duration="300" />
                            </tel:RadComboBox>
                        </div>
                        <div class="col-md-3">
                            &nbsp;&nbsp;
                            <label class="myLabelXtraSmall">Warehouse</label>
                            <tel:RadComboBox ID="rcbWhsFilter" runat="server" Height="150px"
                                Width="200px" DropDownWidth="220px" DropDownAutoWidth="Disabled"
                                HighlightTemplatedItems="true" AppendDataBoundItems="true"
                                AutoPostBack="true" CheckBoxes="false" EnableCheckAllItemsCheckBox="false"
                                OnSelectedIndexChanged="rcbWhsFilter_SelectedIndexChanged"
                                Font-Italic="false">
                                <Items>
                                    <tel:RadComboBoxItem Text="(All Warehouses)" Value="" />
                                </Items>
                                <ExpandAnimation Type="OutQuart" Duration="500" />
                                <CollapseAnimation Type="OutQuint" Duration="300" />
                            </tel:RadComboBox>
                        </div>
                        <div class="col-md-2" style="padding-top:18px;">
                            <tel:RadButton runat="server" ID="rbtnSave" Text="Save" OnClick="rbtnSave_Click" />
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </div>
        <div class="row">&nbsp;</div>
        <div class="row">
            <div class="col-md-10">
                <tel:RadGrid ID="rgPriority" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False"
                    AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="True" PageSize="25" CssClass="Panel"
                    MasterTableView-NoDetailRecordsText="No priorities configured. Use the Add panel below to create entries."
                    OnNeedDataSource="rgPriority_NeedDataSource"
                    OnItemCommand="rgPriority_ItemCommand">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <MasterTableView Width="100%" AllowNaturalSort="false">
                        <Columns>
                            <tel:GridBoundColumn SortExpression="CategoryCode" HeaderText="Cat. Code" DataField="CategoryCode"
                                UniqueName="CategoryCode" ReadOnly="true" HeaderStyle-Width="70px" ItemStyle-HorizontalAlign="Center" />
                            <tel:GridBoundColumn SortExpression="CategoryName" HeaderText="Category" DataField="CategoryName"
                                UniqueName="CategoryName" ReadOnly="true" />
                            <tel:GridBoundColumn SortExpression="WhsCode" HeaderText="Whs Code" DataField="WhsCode"
                                UniqueName="WhsCode" ReadOnly="true" HeaderStyle-Width="80px" />
                            <tel:GridBoundColumn SortExpression="WhsName" HeaderText="Warehouse" DataField="WhsName"
                                UniqueName="WhsName" ReadOnly="true" />
                            <tel:GridBoundColumn SortExpression="U_POSCode" HeaderText="POS Code" DataField="U_POSCode"
                                UniqueName="U_POSCode" ReadOnly="true" HeaderStyle-Width="70px" ItemStyle-HorizontalAlign="Center" />
                            <tel:GridTemplateColumn UniqueName="Priority" HeaderText="Priority" HeaderStyle-Width="70px" ItemStyle-HorizontalAlign="Center">
                                <ItemTemplate>
                                    <asp:TextBox ID="txtPriority" runat="server" Text='<%#Eval("Priority")%>' Width="50px" MaxLength="2" style="text-align:center;" />
                                </ItemTemplate>
                            </tel:GridTemplateColumn>
                            <tel:GridButtonColumn ButtonType="LinkButton" CommandName="DeleteRow" Text="Delete"
                                HeaderText="" HeaderStyle-Width="60px" ItemStyle-HorizontalAlign="Center" UniqueName="DeleteRow" />
                        </Columns>
                    </MasterTableView>
                    <ClientSettings>
                        <Resizing AllowColumnResize="true" />
                        <Selecting AllowRowSelect="true" />
                    </ClientSettings>
                </tel:RadGrid>
            </div>
        </div>
        <div class="row" style="margin-top:12px;">
            <div class="col-md-8">
                <asp:Panel runat="server" CssClass="Panel">
                    <label class="PanelHeading">Add Priority</label>
                    <div class="row" style="margin:10px 0 6px 4px;">
                        <div class="col-md-4">
                            <label class="myLabelXtraSmall">Category</label>
                            <tel:RadComboBox ID="rcbAddCategory" runat="server" Height="150px"
                                Width="200px" DropDownWidth="220px" DropDownAutoWidth="Disabled"
                                HighlightTemplatedItems="true" AppendDataBoundItems="true"
                                EmptyMessage="Select a Category" Font-Italic="false">
                                <ExpandAnimation Type="OutQuart" Duration="500" />
                                <CollapseAnimation Type="OutQuint" Duration="300" />
                            </tel:RadComboBox>
                        </div>
                        <div class="col-md-4">
                            <label class="myLabelXtraSmall">Warehouse</label>
                            <tel:RadComboBox ID="rcbAddWarehouse" runat="server" Height="150px"
                                Width="200px" DropDownWidth="220px" DropDownAutoWidth="Disabled"
                                HighlightTemplatedItems="true" AppendDataBoundItems="true"
                                EmptyMessage="Select a Warehouse" Font-Italic="false">
                                <ExpandAnimation Type="OutQuart" Duration="500" />
                                <CollapseAnimation Type="OutQuint" Duration="300" />
                            </tel:RadComboBox>
                        </div>
                        <div class="col-md-2">
                            <label class="myLabelXtraSmall">Priority</label>
                            <asp:TextBox ID="txtAddPriority" runat="server" Width="50px" MaxLength="2" Text="99" style="text-align:center;" />
                        </div>
                        <div class="col-md-2" style="padding-top:18px;">
                            <tel:RadButton runat="server" ID="rbtnAdd" Text="Add" OnClick="rbtnAdd_Click" />
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </div>
        <div class="row" style="margin-top:16px;">
            <div class="col-md-8">
                <asp:Panel runat="server" CssClass="Panel">
                    <label class="PanelHeading">Import / Export</label>
                    <div class="row" style="margin:10px 0 6px 4px;">
                        <div class="col-md-12">
                            <asp:Button ID="btnExport" runat="server" Text="Export All to Excel" OnClick="btnExport_Click" CssClass="btn btn-default" />
                            <small style="margin-left:8px; color:gray;">Exports all categories x warehouses with current priorities (99 = no priority set)</small>
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
