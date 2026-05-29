<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="StoreBines.aspx.cs" Inherits="StoreBines" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">

    <style>
        .myLabelMedium, .myLabelSmall {
            padding-left: 5px;
        }

        .myImageButton {
            vertical-align: middle;
            height: 20px;
            width: 20px;
            margin-left: 5px;
        }
    </style>

    <div class="container" style="margin-left: 20px;">
        <div class="row">
            <div class="col-md-12">
                <div id="divMessage" runat="server" class="alert-danger" />
                <br />
            </div>
        </div>
        <div class="row">
            <div class="col-md-12">
                <asp:Panel ID="pnlHeader" runat="server" CssClass="page-curl shadow-bottom">
                    <label ID="labelForm" runat="server" class="PanelHeading">Bin by Item</label>
                    <div class="row">
                        <div class="col-md-5">
                            <ul class="myUL">
                                <li>
                                    <label class="myLabel">Operation</label>
                                    <asp:DropDownList ID="drpToWhsCode" runat="server" DataTextField="WHS" DataValueField="WhsCode" CssClass="myDdlLarge" />
                                </li>
                                <li>
                                    <label class="myLabel">Category</label>
                                    <asp:DropDownList ID="DropDownItmGrp" runat="server" DataTextField="GroupName" DataValueField="GroupCode" CssClass="myDdlLarge" OnDataBinding="DropDownItmGrp_DataBinding" OnDataBound="DropDownItmGrp_DataBound" OnLoad="DropDownItmGrp_Load" OnPreRender="DropDownItmGrp_PreRender" OnSelectedIndexChanged="DropDownItmGrp_SelectedIndexChanged" OnTextChanged="DropDownItmGrp_TextChanged" AutoPostBack="True" />
                                </li>
                                <li>
                                    <label class="myLabel" style="vertical-align: top;">Brand</label>
                                    <asp:ListBox ID="lstItemGroups" runat="server" CssClass="myDdlLarge" DataTextField="Brand" DataValueField="Brand" Height="105px" />
                                </li>
                                <li>
                                    <!-- //2019-ABR-09: Modificado por Aldo Reina, para la b�squeda por c�digo de barras: -->
                                    <%--<label class="myLabel" style="vertical-align: top;">Busqueda por Producto</label>--%>
                                    <label class="myLabel" style="vertical-align: top;">Product Code / Barcode</label>
                                    <asp:TextBox ID="ItemTextBox" Names="Items" runat="server" CssClass="myDdlLarge"></asp:TextBox>
                                </li>
                                <li>
                                    <!-- //2019-ABR-09: Modificado por David: -->
                                    <%--<label class="myLabel" style="vertical-align: top;">Busqueda por Bin</label>--%>
                                    <label class="myLabel" style="vertical-align: top;">Bin</label>
                                    <asp:TextBox ID="BinTextBox" Names="Bines" runat="server" CssClass="myDdlLarge"></asp:TextBox>
                                </li>
                                <!-- //2019-ABR-09: Agregado por Aldo Reina, para la b�squeda por c�digo de barras: -->
                                <li>
                                    <label class="myLabel" style="vertical-align:top;"></label>
                                    <asp:DropDownList ID="ItemList" ClientIDMode="Static" runat="server" Visible="false" AutoPostBack="True" OnSelectedIndexChanged="ItemList_SelectedIndexChanged" CssClass="myDdlLarge"></asp:DropDownList>
                                    <telerik:RadButton RenderMode="Lightweight" ID="rbtnCancel" runat="server" Text=" Cancel" OnClick="RbtnCancel_Click" Visible="false">
                                        <Icon PrimaryIconCssClass="rbCancel" PrimaryIconLeft="5" PrimaryIconBottom="11"></Icon>
                                    </telerik:RadButton>
                                </li>
                            </ul>
                        </div>
                        <div class="col-md-7">
                            <ul class="myUL">
                                <li class="radio">
                                    <asp:RadioButton ID="radioAllItems" runat="server" Checked="true" GroupName="grpItemsToDisplay" Text="View All Items" CssClass="myLabel" Visible="False" />
                                    <asp:RadioButton ID="radioHoldItems" runat="server" GroupName="grpItemsToDisplay" Text="View Hold Items Only" CssClass="myLabel" Visible="False" />
                                </li>
                                <li>
                                    <asp:Button ID="btnCreateWorksheet" runat="server" Text="View Values" OnClick="btnCreateWorksheet_Click" CssClass="mybtnmeduim" ToolTip="Create Worksheet" />&nbsp; 
                                    <asp:Button ID="btnSaveChanges" runat="server" OnClick="btnSaveChanges_Click" Text="Save Changes" CssClass="mybtnmeduim" ToolTip="Save Changes" />
                                    &nbsp; 
                                    <asp:Button ID="MinUpdBotton" runat="server" OnClick="MinUpdBotton_Click" Text="Update Minimums to" CssClass="mybtnlarge" ToolTip="Min Up" Visible="False" />
                                    &nbsp; 
                                    <asp:TextBox ID="PorcentajeTBox" runat="server" Width="20px" Text="80" Visible="False" />&nbsp;<asp:Label ID="PorcLabel" runat="server" Text="%" Visible="False" />
                                </li>
                                <li>&nbsp;
                                </li>
                                <li>
                                    <asp:CheckBox ID="NotMinMaxPlannedCheckBox" runat="server" Text="Unplanned products with available inventory in Warehouses" AutoPostBack="True" OnCheckedChanged="NotMinMaxPlannedCheckBox_CheckedChanged" CssClass="myCheckboxList myLabel" Visible="False" />
                                </li>
                                <li>
                                    <asp:Label ID="GreenLabel" runat="server" ForeColor="#009933" Text="Items with less than 30 days in the Operation are shown in green" Visible="False" />
                                </li>
                                <li>
                                    <asp:CheckBox ID="NotInSapCheckBox" runat="server" Text="Products not yet in SAP" AutoPostBack="True" OnCheckedChanged="NotInSapCheckBox_CheckedChanged" CssClass="myCheckboxList myLabel" Visible="False" />
                                </li>
                                <li>
                                    <asp:Button ID="btnExport" runat="server" Text="Export to Excel" OnClick="btnExport_Click" CssClass="mybtnmeduim" ToolTip="Export To Excel" />&nbsp; 
                                </li>
                            </ul>
                        </div>
                    </div>
                    <div class="row">&nbsp;</div>
                </asp:Panel>
            </div>
        </div>
        <div class="row">&nbsp;</div>
        <div class="row">&nbsp;</div>
        <div class="row">
            <div class="col-md-12">
                <tel:RadGrid  Visible="False" ID="GridView1" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False" HeaderStyle-Font-Underline="true"
                    AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="false" PageSize="15" CssClass="Panel"
                    DataSourceID="ObjectDataSource1" OnItemDataBound="GridView1_ItemDataBound">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <ExportSettings OpenInNewWindow="true" IgnorePaging="true" Excel-Format="Xlsx" HideStructureColumns="true" FileName="BinsExport" />
                    <MasterTableView Width="100%" AllowNaturalSort="false" DataKeyNames="itemcode" CommandItemDisplay="Top">
                        <CommandItemSettings ShowExportToExcelButton="false" ShowAddNewRecordButton="false" ShowRefreshButton="false" />
<%--                        <ColumnGroups>
                            <tel:GridColumnGroup HeaderText="Inventario" Name="WhsOnHand" HeaderStyle-HorizontalAlign="Center" />
                        </ColumnGroups>--%>
                        <Columns>
                            <tel:GridBoundColumn SortExpression="itemcode" HeaderText="Item" HeaderButtonType="TextButton" DataField="itemcode" UniqueName="itemcode" HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="itemname" HeaderText="Description" HeaderButtonType="TextButton" DataField="itemname" UniqueName="itemname" HeaderStyle-Width="400px" />
                            <tel:GridBoundColumn SortExpression="itmsgrpnam" HeaderText="Category" HeaderButtonType="TextButton" DataField="itmsgrpnam" UniqueName="itmsgrpnam" HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="u_brand" HeaderText="Brand" HeaderButtonType="TextButton" DataField="u_brand" UniqueName="u_brand"  HeaderStyle-Width="100px" />
                            <tel:GridTemplateColumn HeaderText="bin" SortExpression="bin" HeaderStyle-Width="65px">
                                <ItemTemplate>
                                    <asp:TextBox ID="txtBin" runat="server" CssClass="txt60" Text='<%#Bind("bin","{0:#}") %>' Width="55px"></asp:TextBox>
                                </ItemTemplate>
                            </tel:GridTemplateColumn>
<%--                            <tel:GridTemplateColumn HeaderText="M�ximo" SortExpression="MAX_QTY" HeaderStyle-Width="65px">
                                <ItemTemplate>
                                    <asp:TextBox ID="txtMax" runat="server" CssClass="txt60" Text='<%#Bind("MAX_QTY","{0:#}") %>' Width="55px"></asp:TextBox>
                                </ItemTemplate>
                            </tel:GridTemplateColumn>
                            <tel:GridTemplateColumn HeaderText="Hold" HeaderStyle-Width="50px" UniqueName="HOLD">
                                <ItemTemplate>
                                    <asp:CheckBox ID="chkHold" runat="server" Checked='<% #Bind("HOLD")%>' />
                                </ItemTemplate>
                            </tel:GridTemplateColumn>
                            <tel:GridTemplateColumn HeaderText="Replacement Item" SortExpression="REPLACEMENT_ITEM" Display="false">
                                <ItemTemplate>
                                    <asp:TextBox ID="txtReplacementItem" runat="server" CssClass="txt60" Text='<%#Bind("replacement_item")%>'></asp:TextBox>
                                </ItemTemplate>
                            </tel:GridTemplateColumn>
                            <tel:GridTemplateColumn HeaderText="Comment" SortExpression="COMMENT" Display="false">
                                <ItemTemplate>
                                    <asp:TextBox ID="txtComment" runat="server" CssClass="txt160" Text='<%#Bind("COMMENT")%>'></asp:TextBox>
                                </ItemTemplate>
                            </tel:GridTemplateColumn>--%>
                        </Columns>
                    </MasterTableView>
                    <ClientSettings>
                        <Resizing AllowColumnResize="true" />
                        <Selecting AllowRowSelect="true" />
                        <Scrolling AllowScroll="true" UseStaticHeaders="true" />
                    </ClientSettings>
                </tel:RadGrid>
                <asp:ObjectDataSource ID="ObjectDataSource1" runat="server" SelectMethod="GetItemBin" TypeName="WMS" OnSelected="ObjectDataSource1_Selected">
                    <SelectParameters>
                        <asp:ControlParameter ControlID="drpToWhsCode" Name="store" PropertyName="SelectedValue" Type="String" />
                        <asp:Parameter Name="depts" Type="String" />
                        <asp:Parameter Name="brands" Type="String" />
                        <asp:ControlParameter ControlID="ItemTextBox" Name="lidArticulo"  PropertyName="Text" Type="String" />
                         <asp:ControlParameter ControlID="BinTextBox" Name="lidBinTBox" PropertyName="Text" Type="String" /> 
                        <asp:Parameter Name="CompanyId" Type="String" />
<%--                        <asp:Parameter Name="Bins" Type="String" />--%>

                    </SelectParameters>
                </asp:ObjectDataSource>

                <%--<asp:GridView ID="GridView1" runat="server" CssClass="GridViewPanel"
                    DataSourceID="ObjectDataSource1" GridLines="Vertical" AllowSorting="True"
                    AutoGenerateColumns="False" PageSize="1000" EnableModelValidation="True"
                    OnRowCreated="GridView1_RowCreated" OnRowDataBound="GridView1_RowDataBound" AllowPaging="True">
                    <RowStyle BackColor="#EEEEEE" ForeColor="Black" />
                    <FooterStyle BackColor="#CCCCCC" ForeColor="Black" />
                    <PagerStyle BackColor="#999999" ForeColor="Black" HorizontalAlign="Center" />
                    <SelectedRowStyle BackColor="#008A8C" Font-Bold="True" ForeColor="White" />
                    <HeaderStyle BackColor="#004990" Font-Bold="True" ForeColor="White" HorizontalAlign="Center" />
                    <AlternatingRowStyle BackColor="Gainsboro" />

                    <Columns>

                        <asp:TemplateField HeaderText="Category" SortExpression="itmsgrpnam" Visible="False">
                            <ItemTemplate>
                                <%# Eval("itmsgrpnam").ToString().Trim()%>
                            </ItemTemplate>
                            <ItemStyle HorizontalAlign="Left" />
                            <HeaderStyle HorizontalAlign="Left" VerticalAlign="Bottom" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Art�culo" SortExpression="item" HeaderStyle-Width="100px" ItemStyle-Width="100px">
                            <ItemTemplate>
                                <%# Eval("item").ToString().Trim()%>
                                <asp:HiddenField ID="hdnLoc" runat="server" Value='<%#Eval("loc").ToString().Trim()%>' />
                                <asp:HiddenField ID="hdnItem" runat="server" Value='<%#Eval("item").ToString().Trim()%>' />
                                <asp:HiddenField ID="hdnItemDesc" runat="server" Value='<%#Eval("itemname").ToString().Trim()%>' />
                                <asp:HiddenField ID="hdnCasePack" runat="server" Value='<%#Eval("case_pack").ToString().Trim()%>' />
                                <asp:HiddenField ID="hdnOnHand" runat="server" Value='<%#Eval("Onhand").ToString().Trim()%>' />
                            </ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" Font-Bold="True" />
                            <HeaderStyle HorizontalAlign="Center" VerticalAlign="Bottom" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Description" SortExpression="itemname" HeaderStyle-Width="350px" ItemStyle-Width="350px">
                            <ItemTemplate>
                                <%# Eval("itemname").ToString().Trim()%>
                            </ItemTemplate>
                            <ItemStyle HorizontalAlign="Left" />
                            <HeaderStyle HorizontalAlign="Left" VerticalAlign="Bottom" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Art�culos por Caja" SortExpression="case_pack" Visible="False">
                            <ItemTemplate>
                                <%# Eval("case_pack").ToString().Trim()%>
                            </ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" Width="80px" />
                            <HeaderStyle HorizontalAlign="Left" VerticalAlign="Bottom" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="OnHand" SortExpression="Onhand" HeaderStyle-Width="100px" ItemStyle-Width="100px">
                            <ItemTemplate>
                                <span title="Store onhand"><%# Eval("Onhand").ToString().Trim()%></span>
                            </ItemTemplate>
                            <ItemStyle HorizontalAlign="Right" />
                            <HeaderStyle HorizontalAlign="Center" VerticalAlign="Bottom" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="M�nimo" SortExpression="min_qty" HeaderStyle-Width="100px" ItemStyle-Width="100px">
                            <ItemTemplate>
                                <asp:TextBox ID="txtMin" runat="server" CssClass="txt60" Text='<%#Bind("min_qty","{0:#}") %>'></asp:TextBox>
                            </ItemTemplate>
                            <ItemStyle HorizontalAlign="Right" />
                            <HeaderStyle HorizontalAlign="Center" VerticalAlign="Bottom" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="M�ximo" SortExpression="max_qty" HeaderStyle-Width="100px" ItemStyle-Width="100px">
                            <ItemTemplate>
                                <asp:TextBox ID="txtMax" runat="server" CssClass="txt60" Text='<%#Bind("max_qty","{0:#}") %>'></asp:TextBox>
                            </ItemTemplate>
                            <ItemStyle HorizontalAlign="Right" />
                            <HeaderStyle HorizontalAlign="Center" VerticalAlign="Bottom" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="A" Visible="True">
                            <ItemTemplate>
                                <asp:CheckBox ID="chkHold" runat="server"
                                    Checked='<% #Bind("hold")%>' />
                            </ItemTemplate>
                            <HeaderStyle HorizontalAlign="Center" VerticalAlign="Bottom" Font-Overline="False" Font-Underline="True" />
                            <ItemStyle HorizontalAlign="Center" Width="30px" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Replacement&lt;br&gt;Item" SortExpression="replacement_item" Visible="False">
                            <ItemTemplate>
                                <asp:TextBox ID="txtReplacementItem" runat="server" CssClass="txt60" Text='<%#Bind("replacement_item")%>'></asp:TextBox>
                            </ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                            <HeaderStyle HorizontalAlign="Center" VerticalAlign="Bottom" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Comment" SortExpression="Comment" Visible="False">
                            <ItemTemplate>
                                <asp:TextBox ID="txtComment" runat="server" CssClass="txt160" Text='<%#Bind("Comment")%>'></asp:TextBox>
                            </ItemTemplate>
                            <ItemStyle HorizontalAlign="Left" />
                            <HeaderStyle HorizontalAlign="Center" VerticalAlign="Bottom" />
                        </asp:TemplateField>

                    </Columns>

                </asp:GridView>--%>
            </div>
        </div>
    </div>

    <script type="text/javascript">
        function generateWorksheet() {
            url = "MinMaxWorksheet.aspx";
            var depts = "";
            var store = "";

            var lstItemGroups = document.getElementById("<%=lstItemGroups.ClientID%>");
            var drpToWhsCode = document.getElementById("<%=drpToWhsCode.ClientID%>");

            store = drpToWhsCode.options[drpToWhsCode.selectedIndex].value;

            for (var i = 0; i < lstItemGroups.options.length; i++) {
                if (lstItemGroups.options[i].selected) {
                    depts += lstItemGroups.options[i].value + ",";
                }
            }

            if (depts.length > 0 && store.length > 0) {
                depts = depts.substr(0, depts.length - 1);
                url += "?store=" + store + "&depts=" + depts;

                popUpReport(url);
            }
            else {

            }
        }

        function generateReport() {
            url = "MinMaxReport.aspx";
            var depts = "";
            var store = "";

            var lstItemGroups = document.getElementById("<%=lstItemGroups.ClientID%>");
            var drpToWhsCode = document.getElementById("<%=drpToWhsCode.ClientID%>");

            store = drpToWhsCode.options[drpToWhsCode.selectedIndex].value;

            for (var i = 0; i < lstItemGroups.options.length; i++) {
                if (lstItemGroups.options[i].selected) {
                    depts += lstItemGroups.options[i].value + ",";
                }
            }

            if (depts.length > 0 && store.length > 0) {
                depts = depts.substr(0, depts.length - 1);
                url += "?store=" + store + "&depts=" + depts;

                popUpReport(url);
            }
            else {

            }
        }
    </script>


</asp:Content>

