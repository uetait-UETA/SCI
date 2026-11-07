<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="ProductLocator.aspx.cs" Inherits="ProductLocator" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">
    <div class="container">
        <div class="row">
            <div class="col-md-12">
                <asp:Panel ID="pnlHeader" runat="server" CssClass="page-curl shadow-bottom">
                    <label id="labelForm" runat="server" class="PanelHeading">Product Locator</label>
                    <div class="row">
                        <div class="col-md-12">
                            <ul class="myUL">
                                <li style="display: inline;">
                                    <!-- 2019-ABR-09: Comentado por Aldo Reina para la búsqueda por código de barras: -->
                                    <!-- <label class="myLabel">Articulo</label> -->
                                    <label class="myLabel">Item &#47; Barcode</label>
                                    <tel:RadTextBox ID="rtbItem" runat="server" />
                                    &nbsp;&nbsp;
                                    <label class="myLabel" style="width:auto; margin-right:4px;">Style</label>
                                    <tel:RadTextBox ID="rtbStyle" runat="server" Width="100px" />
                                    &nbsp;&nbsp;
                                    <label class="myLabel" style="width:auto; margin-right:4px;">Color</label>
                                    <tel:RadTextBox ID="rtbColor" runat="server" Width="100px" />
                                    &nbsp;&nbsp;
                                    <label class="myLabel" style="width:auto; margin-right:4px;">Size</label>
                                    <tel:RadTextBox ID="rtbSize" runat="server" Width="80px" />
                                    &nbsp;&nbsp;
                                    <%--<tel:RadSearchBox ID="rtbItem" runat="server" Width="300px"
                                        DropDownSettings-Width="300px" DropDownSettings-Height="200px"
                                        DataTextField="ItemCode" DataValueField="ItemCode"
                                        ShowSearchButton="false" MinFilterLength="3"
                                        Filter="StartsWith" DataSourceID="ItemAutoComplete" OnDataSourceSelect="rtbItem_DataSourceSelect" />
                                    <asp:SqlDataSource ID="ItemAutoComplete" runat="server" ConnectionString='<%$ ConnectionStrings:smm_latConnectionString %>' ProviderName='<%$ ConnectionStrings:smm_latConnectionString.ProviderName %>' />
                                    &nbsp;&nbsp;--%>

                            <!-- 2019-ABR-09: Agregado por Aldo Reina para la búsqueda por código de barras: -->
                                    <telerik:RadButton RenderMode="Lightweight" ID="rbtnCancel" runat="server" Text=" Cancel" OnClick="RbtnCancel_Click" Visible="False">
                                        <Icon PrimaryIconCssClass="rbCancel" PrimaryIconLeft="5" PrimaryIconBottom="11"></Icon>
                                    </telerik:RadButton>
                                    &nbsp;&nbsp;

                            <asp:DropDownList ID="ItemList" ClientIDMode="Static" runat="server" Visible="false" AutoPostBack="True" OnSelectedIndexChanged="ItemList_SelectedIndexChanged"></asp:DropDownList>
                                    <tel:RadButton RenderMode="Lightweight" ID="rbtnCancel1" runat="server" Text="Cancel" OnClick="RbtnCancel_Click" Visible="False" />
                                    <%--<asp:RequiredFieldValidator ID="rfv1" runat="server" Text="item is required" ControlToValidate="rtbItem" Style="display: none;" />--%>
                                    <telerik:RadButton RenderMode="Lightweight" ID="rbtnSearch" runat="server" Text=" Search" OnClick="rbtnSearch_Click">
                                        <Icon PrimaryIconCssClass="rbSearch" PrimaryIconLeft="5" PrimaryIconBottom="11"></Icon>
                                    </telerik:RadButton>
                                    &nbsp;&nbsp;
                                </li>
                            </ul>
                            <ul class="myUL" runat="server" id="ulData" visible="false">
                                <li>
                                    <label class="myLabelXLarge">RSKU Code</label>
                                    <label class="myLabelXXXXLarge" runat="server" id="lblCodSAP" style="color:red;"></label>
                                </li>
                                <li>
                                    <label class="myLabelXLarge">Description</label>
                                    <label class="myLabelXXXXLarge" runat="server" id="lblDesc" style="color:red;"></label>
                                </li>
                                <li>
                                    <label class="myLabelXLarge">Item Type:</label>
                                    <label class="myLabelXXXXLarge" runat="server" id="lblItemType" style="color:red;"></label>
                                    &nbsp;&nbsp;
                                    <label class="myLabelXLarge">Category:</label>
                                    <label class="myLabelXXXXLarge" runat="server" id="lblItmsGrpNam" style="color:red;"></label>
                                    &nbsp;&nbsp;
                                    <label class="myLabelXLarge">Retail Price:</label>
                                    <label class="myLabelXXXXLarge" runat="server" id="lblRetailPrice" style="color:red;"></label>
                                    &nbsp;&nbsp;
                                    <label class="myLabelXLarge">Units Per Case:</label>
                                    <label class="myLabelXXXXLarge" runat="server" id="lblUnitsPerCase" style="color:red;"></label>
                                </li>
                                <li>
                                    <label class="myLabelXLarge">CSKU ID:</label>
                                    <label class="myLabelXXXXLarge" runat="server" id="lblCskuId" style="color:red;"></label>
                                    &nbsp;&nbsp;
                                    <label class="myLabelXLarge">Style:</label>
                                    <label class="myLabelXXXXLarge" runat="server" id="lblStyle" style="color:red;"></label>
                                    &nbsp;&nbsp;
                                    <label class="myLabelXLarge">Color:</label>
                                    <label class="myLabelXXXXLarge" runat="server" id="lblColor" style="color:red;"></label>
                                    &nbsp;&nbsp;
                                    <label class="myLabelXLarge">Size:</label>
                                    <label class="myLabelXXXXLarge" runat="server" id="lblSize" style="color:red;"></label>
                                </li>
                                <li>
                                    <label class="myLabelXLarge">Class:</label>
                                    <label class="myLabelXXXXLarge" runat="server" id="lblClass" style="color:red;"></label>
                                    &nbsp;&nbsp;
                                    <label class="myLabelXLarge">Subclass:</label>
                                    <label class="myLabelXXXXLarge" runat="server" id="lblSubclass" style="color:red;"></label>
                                    &nbsp;&nbsp;
                                    <label class="myLabelXLarge">HS Code:</label>
                                    <label class="myLabelXXXXLarge" runat="server" id="lblHsCode" style="color:red;"></label>
                                    &nbsp;&nbsp;
                                    <label class="myLabelXLarge">Origin:</label>
                                    <label class="myLabelXXXXLarge" runat="server" id="lblOrigin" style="color:red;"></label>
                                </li>
                                <li>
                                    <label class="myLabelXLarge">Possible Barcodes</label>
                                    <label class="myLabelXXXXLarge" runat="server" id="lblBarCode" style="color:red; display:block; margin-left:10px; word-break:break-word;"></label>
                                </li>
                            </ul>
                        </div>
                        <div class="col-md-6">
                            
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </div>
        <div class="row">&nbsp;</div>
        <div class="row">&nbsp;</div>
        <div class="row">
            <div class="col-md-12">
                <tel:RadGrid ID="rgHead" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False" AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="false" PageSize="15" CssClass="Panel" Visible="false"
                    OnPageIndexChanged="rgHead_PageIndexChanged" OnSortCommand="rgHead_SortCommand" OnItemDataBound="rgHead_ItemDataBound">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <MasterTableView Width="100%" AllowNaturalSort="false" ShowFooter="true" FooterStyle-Font-Bold="true">
                        <Columns>
                            <tel:GridBoundColumn SortExpression="u_sap_store" HeaderText="SAP Store" HeaderButtonType="TextButton" DataField="u_sap_store" UniqueName="u_sap_store" HeaderStyle-Width="70px" />
                            <tel:GridBoundColumn SortExpression="pos_code" HeaderText="POS Code" HeaderButtonType="TextButton" DataField="pos_code" UniqueName="pos_code" HeaderStyle-Width="65px" />
                            <tel:GridBoundColumn SortExpression="loc" HeaderText="Loc" HeaderButtonType="TextButton" DataField="loc" UniqueName="loc" HeaderStyle-Width="60px" />
                            <tel:GridBoundColumn SortExpression="loc_name" HeaderText="Loc Name" HeaderButtonType="TextButton" DataField="loc_name" UniqueName="loc_name" HeaderStyle-Width="220px" />
                            <tel:GridBoundColumn SortExpression="soh" HeaderText="SOH" HeaderButtonType="TextButton" DataField="soh" UniqueName="soh" DataFormatString="{0:N0}" HeaderStyle-Width="55px" />
                            <tel:GridBoundColumn SortExpression="committed" HeaderText="Committed" HeaderTooltip="SAP OITW.IsCommited" HeaderButtonType="TextButton" DataField="committed" UniqueName="committed" DataFormatString="{0:N0}" HeaderStyle-Width="72px" />
                            <tel:GridBoundColumn SortExpression="reserved" HeaderText="Rsvd" HeaderTooltip="Reserved" HeaderButtonType="TextButton" DataField="reserved" UniqueName="reserved" DataFormatString="{0:N0}" HeaderStyle-Width="55px" />
                            <tel:GridBoundColumn SortExpression="soh_avail" HeaderText="Available SOH" HeaderButtonType="TextButton" DataField="soh_avail" UniqueName="soh_avail" DataFormatString="{0:N0}" HeaderStyle-Width="72px" />
                            <tel:GridBoundColumn SortExpression="local_in_transit" HeaderText="Local In Transit" HeaderButtonType="TextButton" DataField="local_in_transit" UniqueName="local_in_transit" DataFormatString="{0:N0}" HeaderStyle-Width="72px" />
                            <tel:GridBoundColumn SortExpression="colon_in_transit" HeaderText="Tor In Transit" HeaderButtonType="TextButton" DataField="colon_in_transit" UniqueName="colon_in_transit" DataFormatString="{0:N0}" HeaderStyle-Width="72px" />
                            <tel:GridBoundColumn SortExpression="total_in_transit" HeaderText="Total In Transit" HeaderButtonType="TextButton" DataField="total_in_transit" UniqueName="total_in_transit" DataFormatString="{0:N0}" HeaderStyle-Width="72px" />
                            <tel:GridBoundColumn SortExpression="net_inventory" HeaderText="Net Inventory" HeaderButtonType="TextButton" DataField="net_inventory" UniqueName="net_inventory" DataFormatString="{0:N0}" HeaderStyle-Width="72px" />
                            <tel:GridBoundColumn SortExpression="min" HeaderText="Min" HeaderButtonType="TextButton" DataField="min" UniqueName="min" DataFormatString="{0:N0}" HeaderStyle-Width="50px" />
                            <tel:GridBoundColumn SortExpression="max" HeaderText="Max" HeaderButtonType="TextButton" DataField="Max" UniqueName="Max" DataFormatString="{0:N0}" HeaderStyle-Width="50px" />
                        </Columns>
                    </MasterTableView>
                    <ClientSettings>
                        <Resizing AllowColumnResize="true" />
                        <Selecting AllowRowSelect="true" />
                    </ClientSettings>
                </tel:RadGrid>
            </div>
        </div>
        <div class="row">&nbsp;</div>
    </div>
</asp:Content>

