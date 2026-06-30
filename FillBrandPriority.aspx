<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="FillBrandPriority.aspx.cs" Inherits="FillBrandPriority" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">
    <div class="container">
        <div class="row">
            <div class="col-md-7">
                <asp:Panel ID="pnlHeader" runat="server" CssClass="Panel">
                    <label ID="labelForm" runat="server" class="PanelHeading">Brand Replenishment Priority</label>
                    <div class="row">
                        <div class="col-md-5">
                            &nbsp;&nbsp;
                            <label class="myLabelXtraSmall">Location (Warehouse)</label>
                            <tel:RadComboBox ID="rcbLocation" runat="server" Height="120px"
                                Width="250px" DropDownWidth="250px" DropDownAutoWidth="Disabled"
                                HighlightTemplatedItems="true"
                                AppendDataBoundItems="true"
                                EmptyMessage="Select a Location"
                                AutoPostBack="true" CheckBoxes="false" EnableCheckAllItemsCheckBox="false"
                                OnSelectedIndexChanged="rcbLocation_SelectedIndexChanged"
                                Font-Italic="false">
                                <ExpandAnimation Type="OutQuart" Duration="500" />
                                <CollapseAnimation Type="OutQuint" Duration="300" />
                            </tel:RadComboBox>
                            &nbsp;&nbsp;
                        </div>
                        <div class="col-md-2" style="padding-top:18px;">
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
                <tel:RadGrid ID="rgBrands" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False"
                    AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="True" PageSize="20" CssClass="Panel"
                    MasterTableView-NoDetailRecordsText="No brands configured for this location."
                    OnNeedDataSource="rgBrands_NeedDataSource"
                    OnItemCommand="rgBrands_ItemCommand">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <MasterTableView Width="100%" AllowNaturalSort="false">
                        <Columns>
                            <tel:GridBoundColumn SortExpression="Brand" HeaderText="Brand" DataField="Brand" UniqueName="Brand" ReadOnly="true" />
                            <tel:GridTemplateColumn UniqueName="Priority" HeaderText="Priority" HeaderStyle-Width="70px" ItemStyle-HorizontalAlign="Center">
                                <ItemTemplate>
                                    <asp:TextBox ID="txtPriority" runat="server" Text='<%#Eval("Priority")%>' Width="50px" MaxLength="2" style="text-align:center;" />
                                </ItemTemplate>
                            </tel:GridTemplateColumn>
                            <tel:GridButtonColumn ButtonType="LinkButton" CommandName="DeleteBrand" Text="Delete"
                                HeaderText="" HeaderStyle-Width="60px" ItemStyle-HorizontalAlign="Center" UniqueName="DeleteBrand" />
                        </Columns>
                    </MasterTableView>
                    <ClientSettings>
                        <Resizing AllowColumnResize="true" />
                        <Selecting AllowRowSelect="true" />
                    </ClientSettings>
                </tel:RadGrid>
            </div>
        </div>
        <div class="row" id="divAddBrand" runat="server" style="display:none; margin-top:12px;">
            <div class="col-md-6">
                <asp:Panel runat="server" CssClass="Panel">
                    <label class="PanelHeading">Add Brand</label>
                    <div class="row" style="margin:10px 0 6px 4px;">
                        <div class="col-md-5">
                            <label class="myLabelXtraSmall">Brand</label>
                            <tel:RadComboBox ID="rcbBrand" runat="server" Height="120px"
                                Width="200px" DropDownWidth="200px" DropDownAutoWidth="Disabled"
                                HighlightTemplatedItems="true"
                                AppendDataBoundItems="true"
                                EmptyMessage="Select a Brand"
                                Font-Italic="false">
                                <ExpandAnimation Type="OutQuart" Duration="500" />
                                <CollapseAnimation Type="OutQuint" Duration="300" />
                            </tel:RadComboBox>
                        </div>
                        <div class="col-md-2">
                            <label class="myLabelXtraSmall">Priority</label>
                            <asp:TextBox ID="txtNewPriority" runat="server" Width="50px" MaxLength="2" Text="99" style="text-align:center;" />
                        </div>
                        <div class="col-md-2" style="padding-top:18px;">
                            <tel:RadButton runat="server" ID="rbtnAdd" Text="Add" OnClick="rbtnAdd_Click" />
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </div>
    </div>
</asp:Content>
