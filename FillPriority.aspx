<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="FillPriority.aspx.cs" Inherits="FillPriority" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">
    <div class="container">
        <div class="row">
            <div class="col-md-7">
                <asp:Panel ID="pnlHeader" runat="server" CssClass="Panel">
                    <label  ID="labelForm" runat="server" class="PanelHeading">Store / Warehouse Fill Priority</label>
                    <div class="row">
                        <%--<div class="col-md-5">
                            &nbsp;&nbsp;
                            <label class="myLabelXtraSmall">Company</label>
                            <tel:RadComboBox ID="rcbCompany" runat="server" Height="120px" DropDownAutoWidth="Disabled"
                                HighlightTemplatedItems="true"
                                AppendDataBoundItems="true"
                                EmptyMessage="Seleccione una Company"
                                AutoPostBack="true" CheckBoxes="false" EnableCheckAllItemsCheckBox="false"
                                OnSelectedIndexChanged="rcbCompany_SelectedIndexChanged"
                                Font-Italic="false">
                                <ExpandAnimation Type="OutQuart" Duration="500" />
                                <CollapseAnimation Type="OutQuint" Duration="300" />
                                <Items>
                                    <tel:RadComboBoxItem Value="DFATOCUMEN" Text="DFATOCUMEN" />
                                    <tel:RadComboBoxItem Value="LOPO" Text="LOPO" />
                                    <tel:RadComboBoxItem Value="TOCUBARINC" Text="TOCUBARINC" />
                                    <tel:RadComboBoxItem Value="TOCUMEN" Text="TOCUMEN" />
                                </Items>
                            </tel:RadComboBox>
                            &nbsp;&nbsp;
                        </div>--%>
                        <div class="col-md-5">
                            &nbsp;&nbsp;
                            <label class="myLabelXtraSmall">Category</label>
                            <tel:RadComboBox ID="rcbCategory" runat="server" Height="120px" DropDownAutoWidth="Disabled"
                                HighlightTemplatedItems="true"
                                AppendDataBoundItems="true"
                                EmptyMessage="Select a Category"
                                AutoPostBack="true" CheckBoxes="false" EnableCheckAllItemsCheckBox="false"
                                Localization-CheckAllString="Select all Categories" Localization-AllItemsCheckedString="All Categories selected"
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
                        <div class="col-md-6">
                            &nbsp;&nbsp;
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </div>
        <div class="row">&nbsp;</div><%--ClientSettings-ClientEvents-OnRowDblClick="OnRowDblClick" OnItemCommand="rgPriority_ItemCommand" AllowMultiRowEdit="true" OnPreRender="rgPriority_PreRender"--%>
        <div class="row" id="divGrid" runat="server" style="display:none;">
            <div class="col-md-7">
                <tel:RadGrid ID="rgPriority" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False"
                    AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="True" PageSize="15" CssClass="Panel" MasterTableView-NoDetailRecordsText="No data for your selection!"
                    OnNeedDataSource="rgPriority_NeedDataSource" OnItemDataBound="rgPriority_ItemDataBound">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <ExportSettings ExportOnlyData="true" OpenInNewWindow="true" IgnorePaging="true" Excel-Format="ExcelML" HideStructureColumns="true" FileName="Fill Priority" />
                    <MasterTableView Width="100%" AllowNaturalSort="false" EditMode="InPlace"> <%--CommandItemDisplay="Top"--%>
                        <CommandItemSettings ShowExportToExcelButton="true" ShowAddNewRecordButton="false" ShowRefreshButton="false" />
                        <EditFormSettings EditFormType="Template"></EditFormSettings>
                        <Columns>
                            <tel:GridBoundColumn SortExpression="U_POSCode" HeaderText="POS Code" HeaderButtonType="TextButton" DataField="U_POSCode" UniqueName="U_POSCode" ReadOnly="true" HeaderStyle-Width="80px" ItemStyle-Width="80px" />
                            <tel:GridBoundColumn SortExpression="Location" HeaderText="Warehouse" HeaderButtonType="TextButton" DataField="Location" UniqueName="Location" ReadOnly="true" HeaderStyle-Width="100px" ItemStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="LocationName" HeaderText="Location Name" HeaderButtonType="TextButton" DataField="LocationName" UniqueName="LocationName" ReadOnly="true" />
                            <tel:GridTemplateColumn UniqueName="Priority" HeaderText="Priority">
                                <ItemTemplate>
                                    <%--<%#DataBinder.Eval(Container.DataItem, "Priority")%>--%>
                                    <asp:HiddenField runat="server" ID="hfPriority" Value='<%#Eval("Priority")%>' />
                                    <tel:RadComboBox ID="rcbPriority" runat="server" />
                                </ItemTemplate>
                                <EditItemTemplate>
                                    <%--<tel:RadComboBox ID="rcbPriority" runat="server" DataTextField="Priority" DataValueField="Priority" OnItemsRequested="rcbPriority_ItemsRequested" />--%>
                                </EditItemTemplate>
                            </tel:GridTemplateColumn>
                            <%--<tel:GridDropDownColumn SortExpression="Priority" HeaderText="Priority" HeaderButtonType="TextButton" DataField="Priority" ListTextField="Priority" ListValueField="Priority" DataSourceID="dsPriority" />--%>
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
    <script type="text/javascript">
        function OnRowDblClick(sender, eventArgs) {
            var grid = $find("<%=rgPriority.ClientID %>");
            var master = grid.get_masterTableView();
            editedRow = eventArgs.get_itemIndexHierarchical();
            master.fireCommand("DoubleClickEdit", editedRow);
        }
    </script>
</asp:Content>

