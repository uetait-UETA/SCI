<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="DeliveryErrors.aspx.cs" Inherits="DeliveryErrors" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>


<asp:Content ID="Content1" ContentPlaceHolderID="cphMain" runat="Server">

    <div class="container-fluid">
        <div class="row" style="text-align: center;">
            <div class="col-md-12">
                &nbsp;
                <asp:Label ID="CompanyIdLabel" runat="server" Text="" CssClass="myLabelLarge"></asp:Label>
                <asp:HiddenField ID="hfNewSku" runat="server" />
            </div>
        </div>
        <div class="row">&nbsp;</div>
        <div class="row">
            <div class="col-md-12">
                <asp:Panel ID="pnlHeader" runat="server" CssClass="Panel">
                    <label id="labelForm" runat="server" class="PanelHeading">Delivery Errors</label>
                    <tel:RadGrid ID="rgHead" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False"
                        MasterTableView-NoDetailRecordsText="No delivery errors found"
                        AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="True" PageSize="15"
                        OnNeedDataSource="rgHead_NeedDataSource"
                        OnUpdateCommand="rgHead_UpdateCommand" Style="margin-left: 0px;">
                        <PagerStyle Mode="Slider"></PagerStyle>
                        <SortingSettings EnableSkinSortStyles="false" />
                        <MasterTableView Width="100%" AllowNaturalSort="false" EditMode="InPlace" DataKeyNames="id,sales_id,whs_code">
                            <ItemStyle Wrap="false" Height="22px" />
                            <Columns>
                                <tel:GridEditCommandColumn UniqueName="EditColumn" ButtonType="ImageButton" HeaderStyle-Width="40px" />
                                <tel:GridBoundColumn SortExpression="id" HeaderText="ID" HeaderButtonType="TextButton" DataField="id" UniqueName="id" HeaderStyle-Width="80px" ReadOnly="true" />
                                <tel:GridBoundColumn SortExpression="storenum" HeaderText="Store #" HeaderButtonType="TextButton" DataField="storenum" UniqueName="storenum" HeaderStyle-Width="170px" ReadOnly="true" />
                                <tel:GridHyperLinkColumn SortExpression="skunum" HeaderText="Item" HeaderButtonType="TextButton" DataTextField="skunum" UniqueName="skunum" HeaderStyle-Width="70px"
                                    DataNavigateUrlFields="skunum" DataNavigateUrlFormatString="R_Kardex.aspx?item={0}" Target="sci_detail" />
                                <tel:GridBoundColumn SortExpression="OldBarCode" HeaderText="Old Bar Code Item" HeaderButtonType="TextButton" DataField="OldBarCode" UniqueName="OldBarCode" HeaderStyle-Width="80px" ReadOnly="true" />
                                <tel:GridTemplateColumn HeaderText="New Item" SortExpression="new_sku" UniqueName="new_sku" HeaderStyle-Width="70px" ItemStyle-Wrap="false">
                                    <ItemTemplate>
                                        <%# Eval("new_sku") %>
                                    </ItemTemplate>
                                    <EditItemTemplate>
                                        <tel:RadSearchBox ID="rtbItem" runat="server" Width="300px"
                                            DropDownSettings-Width="400px" DropDownSettings-Height="200px"
                                            DataTextField="ItemName" DataValueField="ItemCode" OnSearch="rtbItem_Search"
                                            ShowSearchButton="false" MinFilterLength="3"
                                            Filter="StartsWith" DataSourceID="ItemAutoComplete" OnDataSourceSelect="rtbItem_DataSourceSelect" />
                                    </EditItemTemplate>
                                </tel:GridTemplateColumn>
                                <tel:GridBoundColumn SortExpression="NewBarCode" HeaderText="New Bar Code Item" HeaderButtonType="TextButton" DataField="NewBarCode" UniqueName="NewBarCode" HeaderStyle-Width="150px" ReadOnly="true" />
                                <tel:GridBoundColumn SortExpression="description" HeaderText="Item Desc." HeaderButtonType="TextButton" DataField="description" UniqueName="description" HeaderStyle-Width="200px" ReadOnly="true" />
                                <tel:GridBoundColumn SortExpression="TicketNo" HeaderText="Trans #" HeaderButtonType="TextButton" DataField="TicketNo" UniqueName="TicketNo" HeaderStyle-Width="130px" ReadOnly="true" />
                                <tel:GridBoundColumn SortExpression="itemnum" HeaderText="Line #" HeaderButtonType="TextButton" DataField="itemnum" UniqueName="itemnum" HeaderStyle-Width="60px" ReadOnly="true" />
                                <tel:GridBoundColumn SortExpression="itemdatetime" HeaderText="Trans Date" HeaderButtonType="TextButton" DataField="itemdatetime" UniqueName="itemdatetime" DataFormatString="{0:d}" HeaderStyle-Width="80px" ReadOnly="true" />
                                <tel:GridBoundColumn SortExpression="sale_qty" HeaderText="Trans Qty" HeaderButtonType="TextButton" DataField="sale_qty" UniqueName="sale_qty" DataFormatString="{0:N0}" HeaderStyle-Width="60px" ReadOnly="true" />
                                <tel:GridBoundColumn SortExpression="whs_qty" HeaderText="Whs Qty" HeaderButtonType="TextButton" DataField="whs_qty" UniqueName="whs_qty" DataFormatString="{0:N0}" HeaderStyle-Width="60px" ReadOnly="true" />
                                <tel:GridTemplateColumn HeaderText="Whs Code" SortExpression="whs_code" UniqueName="whs_code" HeaderStyle-Width="100px" ItemStyle-Wrap="false">
                                    <ItemTemplate>
                                        <%# Eval("whs_code") %>
                                    </ItemTemplate>
                                    <EditItemTemplate>
                                        <asp:TextBox ID="txtWhsCode" runat="server" Text='<%# Eval("whs_code") %>'
                                            Width="90px" MaxLength="20" />
                                    </EditItemTemplate>
                                </tel:GridTemplateColumn>
                                <tel:GridTemplateColumn SortExpression="error_message" HeaderText="Error Message" UniqueName="error_message" HeaderStyle-Width="200px">
                                    <ItemTemplate>
                                        <span title='<%# Eval("error_message") %>' style="cursor:help;"><%# Eval("error_message") %></span>
                                    </ItemTemplate>
                                </tel:GridTemplateColumn>
                            </Columns>
                        </MasterTableView>
                        <ClientSettings>
                            <Resizing AllowColumnResize="true" />
                            <Selecting AllowRowSelect="true" />
                            <Scrolling AllowScroll="true" />
                        </ClientSettings>
                    </tel:RadGrid>
                </asp:Panel>
            </div>
        </div>
    </div>

    <asp:SqlDataSource ID="ItemAutoComplete" runat="server" ConnectionString='<%$ ConnectionStrings:smm_latConnectionString %>' ProviderName='<%$ ConnectionStrings:smm_latConnectionString.ProviderName %>' />
</asp:Content>
