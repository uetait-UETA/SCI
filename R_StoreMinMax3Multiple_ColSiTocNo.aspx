<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="R_StoreMinMax3Multiple_ColSiTocNo.aspx.cs" Inherits="R_StoreMinMax3Multiple_ColSiTocNo" %>

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
                    <label id="labelForm" runat="server" class="PanelHeading">Analysis of References Without Stock in Operation</label>
                    <div class="row">
                        <div class="col-md-5">
                            <ul class="myUL">
                                <li>
                                    <label class="myLabel">Operation</label>
                                    <tel:RadComboBox ID="drpToWhsCode" runat="server" 
                                        DataTextField="WHS" DataValueField="WhsCode"
                                        Height="120px" Width="250px"
                                        DropDownAutoWidth="Disabled" 
                                        AppendDataBoundItems="true" CheckBoxes="true"
                                        EmptyMessage="Select Location(s)"
                                        AutoPostBack="false" 
                                        Font-Italic="false"
                                        EnableCheckAllItemsCheckBox="true"
                                        Localization-CheckAllString="All Locations">
                                        <ExpandAnimation Type="OutQuart" Duration="500" />
                                        <CollapseAnimation Type="OutQuint" Duration="300" />
                                    </tel:RadComboBox>
                                </li>
                                <li style="margin-top:5px;">
                                    <label class="myLabel" style="vertical-align: top;">Category</label>
                                    <tel:RadComboBox ID="DropDownItmGrp" runat="server" 
                                        DataTextField="GroupName" DataValueField="GroupCode"
                                        Height="120px" Width="250px"
                                        DropDownAutoWidth="Disabled" 
                                        AppendDataBoundItems="true" CheckBoxes="true"
                                        EmptyMessage="Select Category(s)" AutoPostBack="True"
                                        OnSelectedIndexChanged="DropDownItmGrp_RadSelectedIndexChanged"
                                        EnableCheckAllItemsCheckBox="true"
                                        Localization-CheckAllString="All Categories"
                                        Font-Italic="false">
                                        <ExpandAnimation Type="OutQuart" Duration="500" />
                                        <CollapseAnimation Type="OutQuint" Duration="300" />
                                    </tel:RadComboBox>
                                </li>
                                <li style="margin-top:5px;">
                                    <%--2021-ABR-05: Modificaci�n para selecci�n m�ltiple de categor�a y marca, por Aldo Reina:--%>
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
                                <li style="margin-top:5px;">
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
                                    <asp:RadioButton ID="radioAllItems" runat="server" Checked="true" GroupName="grpItemsToDisplay" Text="View All Items" CssClass="myLabel" Visible="false" />
                                    <asp:RadioButton ID="radioHoldItems" runat="server" GroupName="grpItemsToDisplay" Text="View Hold Items Only" CssClass="myLabel" Visible="false" />
                                </li>
                                <li>
                                    <asp:Button ID="btnCreateWorksheet" runat="server" Text="View Values" OnClick="btnCreateWorksheet_Click" CssClass="mybtnmeduim" ToolTip="Create Worksheet" />&nbsp; 
                                </li>
                                <li>&nbsp;
                                </li>
                                <%--<li>
                                    <asp:CheckBox ID="NotMinMaxPlannedCheckBox" runat="server" Text="Productos no planificados con inventario disponible en Bodegas" AutoPostBack="True" OnCheckedChanged="NotMinMaxPlannedCheckBox_CheckedChanged" CssClass="myCheckboxList myLabel" />
                                </li>
                                <li>
                                    <asp:Label ID="GreenLabel" runat="server" ForeColor="#009933" Text="Verde representa art�culos que tienen menos de 30 d�as en la operaci�n" Visible="False" />
                                </li>
                                <li>
                                    <asp:CheckBox ID="NotInSapCheckBox" runat="server" Text="Productos que no existen en SAP aun" AutoPostBack="True" OnCheckedChanged="NotInSapCheckBox_CheckedChanged" CssClass="myCheckboxList myLabel" />
                                </li>--%>
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
                        <Columns>
                            <tel:GridBoundColumn SortExpression="locname" HeaderText="Location" HeaderButtonType="TextButton" DataField="locname" UniqueName="locname" HeaderStyle-Width="140px" />
                            <tel:GridBoundColumn SortExpression="marca" HeaderText="Brand" HeaderButtonType="TextButton" DataField="marca" UniqueName="marca" HeaderStyle-Width="90px" />
                            <tel:GridTemplateColumn UniqueName="TemplateColumn1" HeaderText="Item" SortExpression="ITEM" HeaderStyle-Width="70px" DataType="System.String">
                                <ItemTemplate>
                                </ItemTemplate>
                            </tel:GridTemplateColumn>
                            <tel:GridBoundColumn SortExpression="BarCode" HeaderText="Bar Code" HeaderButtonType="TextButton" DataField="BarCode" UniqueName="BarCode" HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="ITEMNAME" HeaderText="Description" HeaderButtonType="TextButton" DataField="ITEMNAME" UniqueName="ITEMNAME" HeaderStyle-Width="380px" />
                            <tel:GridBoundColumn SortExpression="CASE_PACK" HeaderText="Items per Box" HeaderButtonType="TextButton" DataField="CASE_PACK" UniqueName="CASE_PACK" Display="false" />
                            <tel:GridBoundColumn SortExpression="OnHand" HeaderText="OnHand" HeaderButtonType="TextButton" DataField="OnHand" UniqueName="OnHand" DataFormatString="{0:N0}" HeaderStyle-Width="60px" Display="false" />
                            <tel:GridBoundColumn SortExpression="MIN_QTY" HeaderText="Minimum" HeaderButtonType="TextButton" DataField="MIN_QTY" UniqueName="MIN_QTY" DataFormatString="{0:N0}" HeaderStyle-Width="55px" />
                            <tel:GridBoundColumn SortExpression="MAX_QTY" HeaderText="Maximum" HeaderButtonType="TextButton" DataField="MAX_QTY" UniqueName="MAX_QTY" DataFormatString="{0:N0}" HeaderStyle-Width="55px" />
                            <tel:GridBoundColumn SortExpression="replacement_item" HeaderText="Replacement Item" HeaderButtonType="TextButton" DataField="replacement_item" UniqueName="replacement_item" Display="false" />
                            <tel:GridBoundColumn SortExpression="COMMENT" HeaderText="Comment" HeaderButtonType="TextButton" DataField="COMMENT" UniqueName="COMMENT" Display="false" />
                            <tel:GridBoundColumn SortExpression="ColonOnHand" HeaderText="Colon Inventory" HeaderButtonType="TextButton" DataField="ColonOnHand" UniqueName="ColonOnHand" DataFormatString="{0:N0}" HeaderStyle-Width="60px" />
                        </Columns>
                    </MasterTableView>
                    <ClientSettings>
                        <Resizing AllowColumnResize="true" />
                        <Selecting AllowRowSelect="true" />
                        <Scrolling AllowScroll="true" UseStaticHeaders="true" />
                    </ClientSettings>
                </tel:RadGrid>
                <asp:ObjectDataSource ID="ObjectDataSource1" runat="server" SelectMethod="GetMinMaxQuantities_ColSiTocNo" TypeName="Reports" OnSelected="ObjectDataSource1_Selected">
                    <SelectParameters>
                        <asp:Parameter Name="store" Type="String" />
                        <asp:Parameter Name="depts" Type="String" />
                        <asp:Parameter Name="brands" Type="String" />
                        <asp:Parameter Name="displayAll" Type="String" />
                        <asp:Parameter Name="NoPlanned" Type="String" />
                        <asp:Parameter Name="NoInSap" Type="String" />
                        <asp:Parameter Name="Item" Type="String" />
                        <asp:Parameter Name="companyId" Type="String" />
                    </SelectParameters>
                </asp:ObjectDataSource>
            </div>
        </div>
    </div>

    <script type="text/javascript">
        var storesList = document.getElementById('<%=drpToWhsCode.ClientID%>');
        var categoriesList = document.getElementById('<%=DropDownItmGrp.ClientID%>');
        var brandsList = document.getElementById('<%=lstItemGroups.ClientID%>');

        function selectAllStores() {
            
        }

        function selectAllCategories() {

        }

        function selectAllBrands() {

        }
    </script>


</asp:Content>

