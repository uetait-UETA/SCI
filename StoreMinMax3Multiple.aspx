<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="StoreMinMax3Multiple.aspx.cs" Inherits="StoreMinMax3Multiple" %>

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
                    <label id="labelForm" runat="server" class="PanelHeading">Min-Max by Category and Brand</label>
                    <div class="row">
                        <div class="col-md-5">
                            <ul class="myUL">
                                <li>
                                    <label class="myLabel">Operation</label>
                                    <tel:RadComboBox ID="drpToWhsCode" runat="server"
                                        DataTextField="WHS" DataValueField="WhsCode"
                                        Height="120px" Width="300px"
                                        DropDownAutoWidth="Disabled"
                                        AppendDataBoundItems="true" CheckBoxes="False"
                                        EmptyMessage="Select a Location" AutoPostBack="False"
                                        Font-Italic="False"
                                        EnableCheckAllItemsCheckBox="true"
                                        Localization-CheckAllString="All Locations">
                                        <ExpandAnimation Type="OutQuart" Duration="500" />
                                        <CollapseAnimation Type="OutQuint" Duration="300" />
                                    </tel:RadComboBox>
                                    &nbsp;&nbsp;
                                    <label class="myLabel">Category</label>
                                    <tel:RadComboBox ID="DropDownItmGrp" runat="server"
                                        DataTextField="GroupName" DataValueField="GroupCode"
                                        Height="120px" Width="300px"
                                        DropDownAutoWidth="Disabled"
                                        AppendDataBoundItems="true" CheckBoxes="true"
                                        EmptyMessage="Select Category(s)" AutoPostBack="True"
                                        OnSelectedIndexChanged="DropDownItmGrp_RadSelectedIndexChanged"
                                        EnableCheckAllItemsCheckBox="true"
                                        Localization-CheckAllString="All Categories"
                                        Font-Italic="False">
                                        <ExpandAnimation Type="OutQuart" Duration="500" />
                                        <CollapseAnimation Type="OutQuint" Duration="300" />
                                    </tel:RadComboBox>
                                    &nbsp;&nbsp;
                                    <label class="myLabel" style="vertical-align: top;">Brand</label>
                                    <tel:RadListBox ID="lstItemGroups" runat="server"
                                        DataTextField="brand" DataValueField="valor"
                                        Height="105px" Width="250px"
                                        DropDownAutoWidth="Disabled"
                                        AppendDataBoundItems="true" CheckBoxes="true"
                                        EmptyMessage="Select Brand(s)" AutoPostBack="false"
                                        Font-Italic="false"
                                        ShowCheckAll="true"
                                        Localization-CheckAll="All Brands">
                                    </tel:RadListBox>
                                </li>
                                <li style="margin-top:5px;">
                                    <!-- //2019-ABR-09: Modificado por Aldo Reina, para la b�squeda por c�digo de barras: -->
                                    <%--<label class="myLabel" style="vertical-align: top;">Busqueda por Producto</label>--%>
                                    <label class="myLabel" style="vertical-align: top;">Product Code / Barcode</label>
                                    <asp:TextBox ID="ItemTextBox" runat="server" CssClass="myDdlLarge" Width="250px"></asp:TextBox>
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
                                    <asp:RadioButton ID="radioAllItems" runat="server" Checked="true" GroupName="grpItemsToDisplay" Text="View All Items" CssClass="myLabel" />
                                    <asp:RadioButton ID="radioHoldItems" runat="server" GroupName="grpItemsToDisplay" Text="View Hold Items Only" CssClass="myLabel" />
                                </li>
                                <li>
                                    <asp:Button ID="btnCreateWorksheet" runat="server" Text="View Values" OnClick="btnCreateWorksheet_Click" CssClass="mybtnmeduim" ToolTip="Create Worksheet" />&nbsp; 
                                    <%--<asp:Button ID="btnSaveChanges" runat="server" OnClick="btnSaveChanges_Click" Text="Salvar cambios" CssClass="mybtnmeduim" ToolTip="Save Changes" />
                                    &nbsp; 
                                    <asp:Button ID="MinUpdBotton" runat="server" OnClick="MinUpdBotton_Click" Text="Actualiza Minimos al" CssClass="mybtnlarge" ToolTip="Min Up" />
                                    &nbsp; 
                                    <asp:TextBox ID="PorcentajeTBox" runat="server" Width="20px" Text="80" />&nbsp;<asp:Label ID="PorcLabel" runat="server" Text="%" />--%>
                                </li>
                                <li>&nbsp;
                                </li>
                                <li>
                                    <asp:CheckBox ID="NotMinMaxPlannedCheckBox" runat="server" Text="Unplanned products with available inventory in Warehouses" AutoPostBack="True" OnCheckedChanged="NotMinMaxPlannedCheckBox_CheckedChanged" CssClass="myCheckboxList myLabel" />
                                </li>
                                <li>
                                    <asp:Label ID="GreenLabel" runat="server" ForeColor="#009933" Text="Items with less than 30 days in the Operation are shown in green" Visible="False" />
                                </li>
                                <li>
                                    <asp:CheckBox ID="NotInSapCheckBox" runat="server" Text="Products not yet in SAP" AutoPostBack="True" OnCheckedChanged="NotInSapCheckBox_CheckedChanged" CssClass="myCheckboxList myLabel" />
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
                <tel:RadGrid ID="GridView1" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False" HeaderStyle-Font-Underline="true"
                    AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="false" PageSize="15" CssClass="Panel"
                    DataSourceID="ObjectDataSource1">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <ExportSettings OpenInNewWindow="true" IgnorePaging="true" Excel-Format="Xlsx" HideStructureColumns="true" FileName="MinMaxExport" />
                    <MasterTableView Width="100%" AllowNaturalSort="false" DataKeyNames="ITEM" CommandItemDisplay="Top">
                        <CommandItemSettings ShowExportToExcelButton="false" ShowAddNewRecordButton="false" ShowRefreshButton="false" />
                        <ColumnGroups>
                            <tel:GridColumnGroup HeaderText="Inventory" Name="WhsOnHand" HeaderStyle-HorizontalAlign="Center" />
                        </ColumnGroups>
                        <Columns>
                            <tel:GridBoundColumn SortExpression="marca" HeaderText="Brand" HeaderButtonType="TextButton" DataField="marca" UniqueName="marca" HeaderStyle-Width="120px" />
                            <tel:GridTemplateColumn UniqueName="TemplateColumn1" HeaderText="Item" SortExpression="ITEM" HeaderStyle-Width="70px" DataType="System.String">
                                <ItemTemplate>
                                </ItemTemplate>
                            </tel:GridTemplateColumn>
                            <tel:GridBoundColumn SortExpression="BarCode" HeaderText="Bar Code" HeaderButtonType="TextButton" DataField="BarCode" UniqueName="BarCode" HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="ITEMNAME" HeaderText="Description" HeaderButtonType="TextButton" DataField="ITEMNAME" UniqueName="ITEMNAME" HeaderStyle-Width="400px" />
                            <tel:GridBoundColumn SortExpression="CASE_PACK" HeaderText="Items per Box" HeaderButtonType="TextButton" DataField="CASE_PACK" UniqueName="CASE_PACK" Display="false" />
                            <tel:GridBoundColumn SortExpression="OnHand" HeaderText="OnHand" HeaderButtonType="TextButton" DataField="OnHand" UniqueName="OnHand" DataFormatString="{0:N0}" HeaderStyle-Width="58px" />
                            <tel:GridBoundColumn SortExpression="MIN_QTY" HeaderText="Minimum" HeaderButtonType="TextButton" DataField="MIN_QTY" UniqueName="MIN_QTY" DataFormatString="{0:N0}" HeaderStyle-Width="58px" />
                            <tel:GridBoundColumn SortExpression="MAX_QTY" HeaderText="Maximum" HeaderButtonType="TextButton" DataField="MAX_QTY" UniqueName="MAX_QTY" DataFormatString="{0:N0}" HeaderStyle-Width="58px" />
                            <tel:GridCheckBoxColumn SortExpression="HOLD" HeaderText="Hold" HeaderButtonType="TextButton" DataField="HOLD" UniqueName="HOLD" HeaderStyle-Width="50px" Display="false" />
              
                            <tel:GridBoundColumn SortExpression="replacement_item" HeaderText="Replacement Item" HeaderButtonType="TextButton" DataField="replacement_item" UniqueName="replacement_item" Display="false" />
                            <tel:GridBoundColumn SortExpression="COMMENT" HeaderText="Comment" HeaderButtonType="TextButton" DataField="COMMENT" UniqueName="COMMENT" Display="false" />
                             </Columns>
                    </MasterTableView>
                    <ClientSettings>
                        <Resizing AllowColumnResize="true" />
                        <Selecting AllowRowSelect="true" />
                        <Scrolling AllowScroll="true" UseStaticHeaders="true" />
                    </ClientSettings>
                </tel:RadGrid>
                <asp:ObjectDataSource ID="ObjectDataSource1" runat="server" SelectMethod="GetMinMaxQuantities_Mult" TypeName="Reports" OnSelected="ObjectDataSource1_Selected">
                    <SelectParameters>
                        <asp:Parameter Name="store" Type="String" />
                        <asp:Parameter Name="depts" Type="String" />
                        <asp:Parameter Name="brands" Type="String" />
                        <asp:Parameter Name="displayAll" Type="String" />
                        <asp:Parameter Name="NoPlanned" Type="String" />
                        <asp:Parameter Name="NoInSap" Type="String" />
                        <asp:Parameter Name="Item" Type="String" />
                        <asp:Parameter Name="companyId" Type="String" />
                        <asp:Parameter Name="control" Type="String" />
                        <asp:Parameter Name="whsTypes" Type="String" />
                    </SelectParameters>
                </asp:ObjectDataSource>
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

