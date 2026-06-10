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
                    <tel:RadGrid ID="rgHead" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False" MasterTableView-NoDetailRecordsText="No delivery errors found"
                        AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="True" PageSize="15" DataSourceID="ObjectDataSource1" OnUpdateCommand="rgHead_UpdateCommand" Style="margin-left: 0px;">
                        <PagerStyle Mode="Slider"></PagerStyle>
                        <SortingSettings EnableSkinSortStyles="false" />
                        <MasterTableView Width="100%" AllowNaturalSort="false" EditMode="InPlace" DataKeyNames="id">
                            <ItemStyle Wrap="false" Height="22px" />
                            <Columns>
                                <%--<tel:GridButtonColumn UniqueName="transfer" ButtonType="LinkButton" HeaderText="Transfer / Order" DataTextField="transfer" CommandName="TRANSFER" ItemStyle-Font-Underline="true" />--%>
                                <tel:GridEditCommandColumn UniqueName="EditColumn" ButtonType="ImageButton" HeaderStyle-Width="40px" />
                                <tel:GridBoundColumn SortExpression="id" HeaderText="ID" HeaderButtonType="TextButton" DataField="id" UniqueName="id" HeaderStyle-Width="80px" ReadOnly="true" />
                                <tel:GridBoundColumn SortExpression="storenum" HeaderText="Store #" HeaderButtonType="TextButton" DataField="storenum" UniqueName="storenum" HeaderStyle-Width="170px" ReadOnly="true" />
                                <tel:GridBoundColumn SortExpression="skunum" HeaderText="Item" HeaderButtonType="TextButton" DataField="skunum" UniqueName="skunum" HeaderStyle-Width="70px" ReadOnly="true" />
                                <tel:GridBoundColumn SortExpression="OldBarCode" HeaderText="Old Bar Code Item" HeaderButtonType="TextButton" DataField="OldBarCode" UniqueName="OldBarCode" HeaderStyle-Width="80px" ReadOnly="true" />
                                <%--<tel:GridBoundColumn SortExpression="new_sku" HeaderText="New Item" HeaderButtonType="TextButton" DataField="new_sku" UniqueName="new_sku" HeaderStyle-Width="90px" />--%>
                                <tel:GridTemplateColumn HeaderText="New Item" SortExpression="new_sku" UniqueName="new_sku" HeaderStyle-Width="70px" ItemStyle-Wrap="false">
                                    <ItemTemplate>
                                        <%# Eval("new_sku") %>
                                    </ItemTemplate>
                                    <EditItemTemplate>
                                        <%--<tel:RadTextBox runat="server" ID="rtbNewSku" Text='<%# Eval("new_sku") %>' />--%>
                                        <tel:RadSearchBox ID="rtbItem" runat="server" Width="300px"
                                            DropDownSettings-Width="400px" DropDownSettings-Height="200px"
                                            DataTextField="ItemName" DataValueField="ItemCode" OnSearch="rtbItem_Search"
                                            ShowSearchButton="false" MinFilterLength="3"
                                            Filter="StartsWith" DataSourceID="ItemAutoComplete" OnDataSourceSelect="rtbItem_DataSourceSelect" />
                                    </EditItemTemplate>
                                </tel:GridTemplateColumn>
                                <tel:GridBoundColumn SortExpression="NewBarCode" HeaderText="New Bar Code Item" HeaderButtonType="TextButton" DataField="NewBarCode" UniqueName="NewBarCode" HeaderStyle-Width="150px" ReadOnly="true" />
                                <tel:GridBoundColumn SortExpression="description" HeaderText="Item Desc." HeaderButtonType="TextButton" DataField="description" UniqueName="description" HeaderStyle-Width="200px" ReadOnly="true" />
                                <tel:GridBoundColumn SortExpression="transnum" HeaderText="Trans #" HeaderButtonType="TextButton" DataField="transnum" UniqueName="transnum" HeaderStyle-Width="130px" ReadOnly="true" />
                                <tel:GridBoundColumn SortExpression="itemnum" HeaderText="Line #" HeaderButtonType="TextButton" DataField="itemnum" UniqueName="itemnum" HeaderStyle-Width="60px" ReadOnly="true" />
                                <tel:GridBoundColumn SortExpression="itemdatetime" HeaderText="Trans Date" HeaderButtonType="TextButton" DataField="itemdatetime" UniqueName="itemdatetime" DataFormatString="{0:d}" HeaderStyle-Width="80px" ReadOnly="true" />
                                <tel:GridBoundColumn SortExpression="sale_qty" HeaderText="Trans Qty" HeaderButtonType="TextButton" DataField="sale_qty" UniqueName="sale_qty" DataFormatString="{0:N0}" HeaderStyle-Width="60px" ReadOnly="true" />
                                <tel:GridBoundColumn SortExpression="whs_qty" HeaderText="Whs Qty" HeaderButtonType="TextButton" DataField="whs_qty" UniqueName="whs_qty" DataFormatString="{0:N0}" HeaderStyle-Width="60px" ReadOnly="true" />
                                <tel:GridBoundColumn SortExpression="whs_code" HeaderText="Whs Code" HeaderButtonType="TextButton" DataField="whs_code" UniqueName="whs_code" HeaderStyle-Width="80px" ReadOnly="true" />
                                <tel:GridBoundColumn SortExpression="error_message" HeaderText="Error Message" HeaderButtonType="TextButton" DataField="error_message" UniqueName="error_message" ReadOnly="true" HeaderStyle-Width="200px" />
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
    <asp:ObjectDataSource ID="ObjectDataSource1" runat="server" SelectMethod="GetDeliveryErrors" TypeName="Delivery" UpdateMethod="UpdateDeliveryItemNumber">
        <SelectParameters>
            <asp:ControlParameter ControlID="CompanyIdLabel" Name="CompanyId" PropertyName="Text" Type="String" />
        </SelectParameters>
        <UpdateParameters>
            <asp:Parameter Name="id" Type="String" />
            <asp:Parameter Name="new_sku" Type="String" />
        </UpdateParameters>
    </asp:ObjectDataSource>
</asp:Content>

