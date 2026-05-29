<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="sapInventory.aspx.cs" Inherits="sapInventory" %>

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

    <div class="container-fluid" style="margin-left: 20px;">
        <div class="row">
            <div class="col-md-12">
                <div id="divMessage" runat="server" class="alert-danger" />
                <br />
            </div>
        </div>
        <div class="row">
            <div class="col-md-12">
                <asp:Panel ID="pnlHeader" runat="server" CssClass="Panel">
                    <label class="PanelHeading">SAP Stock</label>
                    <div class="row">
                        <div class="col-md-6">
                            <ul class="myUL">
                                <li>
                                    <label class="myLabel">&nbsp;</label>
                                    <asp:Label ID="brandsLabel" runat="server" Text="N" Visible="False" class="myLabel" />
                                    <asp:Label ID="CompanyIdLabel" runat="server" Text="" class="myLabel" />
                                </li>
                                <li>
                                    <label class="myLabel">Location</label>
                                    <%--<asp:DropDownList ID="drpToWhsCode" runat="server" DataTextField="WhsName" DataValueField="WhsCode" CssClass="myDdlLarge" />--%>
                                    <tel:RadComboBox ID="drpToWhsCode" runat="server" Height="240px" DropDownAutoWidth="Disabled" Width="380px"
                                        HighlightTemplatedItems="true"
                                        AppendDataBoundItems="true"
                                        EmptyMessage="Select a Location"
                                        AutoPostBack="false" CheckBoxes="true" EnableCheckAllItemsCheckBox="true"
                                        Localization-CheckAllString="Select all Locations"
                                        Localization-AllItemsCheckedString="All Locations selected"
                                        Font-Italic="false">
                                        <ExpandAnimation Type="OutQuart" Duration="500" />
                                        <CollapseAnimation Type="OutQuint" Duration="300" />
                                    </tel:RadComboBox>
                                </li>
                                <li>
                                    <label class="myLabel">Category</label>
                                    <%--<asp:DropDownList ID="drpItemGroups" runat="server" DataTextField="GroupName" DataValueField="GroupCode" CssClass="myDdlLarge" AutoPostBack="True" 
                                        OnSelectedIndexChanged="drpItemGroups_SelectedIndexChanged" />--%>
                                    <tel:RadComboBox ID="drpItemGroups" runat="server" Height="240px" DropDownAutoWidth="Disabled" Width="380px"
                                        HighlightTemplatedItems="true"
                                        AppendDataBoundItems="true"
                                        EmptyMessage="Select a Category"
                                        AutoPostBack="true" CheckBoxes="true" EnableCheckAllItemsCheckBox="true" OnSelectedIndexChanged="drpItemGroups_SelectedIndexChanged"
                                        Localization-CheckAllString="Select all Categories"
                                        Localization-AllItemsCheckedString="All Categories selected"
                                        Font-Italic="false">
                                        <ExpandAnimation Type="OutQuart" Duration="500" />
                                        <CollapseAnimation Type="OutQuint" Duration="300" />
                                    </tel:RadComboBox>
                                </li>
                                <li>
                                    <label class="myLabel" style="vertical-align: top;">Brand</label>
                                    <asp:ListBox ID="lstItemGroups" runat="server" CssClass="myDdlLarge" DataTextField="Brand" DataValueField="Brand" Height="105px" SelectionMode="Multiple" />
                                </li>
                                <li>
                                    <!-- //2019-ABR-10: Modificado por Aldo Reina, para la b�squeda por c�digo de barras: -->
                                    <%--<label class="myLabel">Articulo</label>--%>
                                    <label id="lblArtCodBarra" class="myLabel">Item / Barcode</label>
                                    <asp:TextBox ID="ArticuloTextBox" runat="server" CssClass="myDdlLarge"></asp:TextBox>
                                    
                                    <!-- //2019-ABR-10: Agregado por Aldo Reina, para la b�squeda por c�digo de barras: -->
                                    <asp:DropDownList ID="ItemList" ClientIDMode="Static" runat="server" Visible="false" AutoPostBack="True" OnSelectedIndexChanged="ItemList_SelectedIndexChanged"></asp:DropDownList>
                                    <telerik:RadButton RenderMode="Lightweight" ID="rbtnCancel" runat="server" Text=" Cancel" OnClick="RbtnCancel_Click" Visible="False">
                                        <Icon PrimaryIconCssClass="rbCancel" PrimaryIconLeft="5" PrimaryIconBottom="11"></Icon>
                                    </telerik:RadButton>
                                    
                                    <asp:Label ID="TypeSearchLabel" runat="server" Text="N" Visible="False" class="myLabel"></asp:Label>
                                </li>
                                <li>
				                    <label class="myLabel">Period (Optional)</label>
				                    <tel:RadComboBox ID="drpCortes" runat="server" Height="240px" DropDownAutoWidth="Disabled" Width="260px"
				                        HighlightTemplatedItems="true"
				                        AppendDataBoundItems="true"
				                        EmptyMessage="Select a Period"
				                        AutoPostBack="true" CheckBoxes="true" EnableCheckAllItemsCheckBox="true"
				                        Localization-CheckAllString="Select all Periods"
				                        Localization-AllItemsCheckedString="All Periods selected"
				                        Font-Italic="false">
				                        <ExpandAnimation Type="OutQuart" Duration="500" />
				                        <CollapseAnimation Type="OutQuint" Duration="300" />
				                    </tel:RadComboBox>
                                </li>
                                <li>
                                    <label class="myLabel">&nbsp;</label>
                                    <asp:Button ID="buscarButton" runat="server" OnClick="buscarButton_Click" OnLoad="buscarButton_Load" Text="Search" CssClass="mybtn" />
                                    <asp:Button ID="btnExport" runat="server" Text="Export to Excel" OnClick="btnExport_Click" CssClass="mybtnmeduim" />
                                </li>
                            </ul>
                        </div>

                    </div>
                </asp:Panel>
            </div>
        </div>
        <div class="row">&nbsp;</div>
        <div class="row">&nbsp;</div>
        <div class="row">
            <div class="col-md-12">
                <%--<asp:GridView ID="GridView1" runat="server" DataSourceID="ObjectDataSource1" CssClass="GridViewPanel"
                    BackColor="White" BorderColor="#999999" BorderStyle="None" BorderWidth="1px"
                    CellPadding="3" GridLines="Vertical"
                    Font-Size="9pt">
                    <RowStyle BackColor="#EEEEEE" ForeColor="Black" />
                    <FooterStyle BackColor="#CCCCCC" ForeColor="Black" />
                    <PagerStyle BackColor="#999999" ForeColor="Black" HorizontalAlign="Center" />
                    <SelectedRowStyle BackColor="#008A8C" Font-Bold="True" ForeColor="White" />
                    <HeaderStyle BackColor="#000084" Font-Bold="True" ForeColor="White" />
                    <AlternatingRowStyle BackColor="Gainsboro" />
                </asp:GridView>
                <asp:ObjectDataSource ID="ObjectDataSource1" runat="server"
                    SelectMethod="getOnhandItems" TypeName="Reports">
                    <SelectParameters>
                        <asp:ControlParameter ControlID="drpToWhsCode" Name="store" PropertyName="SelectedValue" Type="String" />
                        <asp:ControlParameter ControlID="drpItemGroups" Name="itmgrp" PropertyName="SelectedValue" Type="String" />
                        <asp:ControlParameter ControlID="brandsLabel" Name="brand" PropertyName="Text" Type="String" />
                        <asp:ControlParameter ControlID="ArticuloTextBox" Name="itemCode" PropertyName="Text" Type="String" />
                        <asp:ControlParameter ControlID="TypeSearchLabel" Name="typeQry" PropertyName="Text" Type="String" />
                        <asp:ControlParameter ControlID="drpCortes" Name="cortes" PropertyName="SelectedValue" Type="String" />
                        <asp:ControlParameter ControlID="CompanyIdLabel" Name="CompanyId" PropertyName="Text" Type="String" />                        
                    </SelectParameters>
                </asp:ObjectDataSource>--%>
                <asp:GridView ID="GridView1" runat="server" CssClass="Panel"
                    BackColor="White" BorderColor="#999999" BorderStyle="None" BorderWidth="1px" AutoGenerateColumns="false"
                    CellPadding="3" GridLines="Vertical"
                    Font-Size="9pt">
                    <RowStyle BackColor="#EEEEEE" ForeColor="Black" />
                    <FooterStyle BackColor="#CCCCCC" ForeColor="Black" />
                    <PagerStyle BackColor="#999999" ForeColor="Black" HorizontalAlign="Center" />
                    <SelectedRowStyle BackColor="#008A8C" Font-Bold="True" ForeColor="White" />
                    <HeaderStyle BackColor="#000084" Font-Bold="True" ForeColor="White" />
                    <AlternatingRowStyle BackColor="Gainsboro" />
                    <Columns>
                        <asp:BoundField DataField="U_POSCode" HeaderText="POS Code" HeaderStyle-Width="100px" />
                        <asp:BoundField DataField="Ubicacion" HeaderText="Location" HeaderStyle-Width="100px" />
                        <asp:BoundField DataField="Codigo_Articulo" HeaderText="Item Code" HeaderStyle-Width="100px" />
                        <asp:BoundField DataField="BarCode" HeaderText="Bar Code" HeaderStyle-Width="100px" />
                        <asp:BoundField DataField="Nombre_Articulo" HeaderText="Item Name" HeaderStyle-Width="380px" />
                        <asp:BoundField DataField="Categoria" HeaderText="Category" HeaderStyle-Width="80px" />
                        <asp:BoundField DataField="Nombre_Categoria" HeaderText="Category Name" HeaderStyle-Width="150px" />
                        <asp:BoundField DataField="Marca" HeaderText="Brand" HeaderStyle-Width="100px" />
                        <asp:BoundField DataField="Clase" HeaderText="Class" HeaderStyle-Width="100px" />
                        <asp:BoundField DataField="Existencia" HeaderText="Stock" HeaderStyle-Width="100px" />
                    </Columns>
                </asp:GridView>
                <%--<tel:RadGrid ID="rgHead" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False"
                    AllowSorting="False" AllowMultiRowSelection="False" AllowPaging="false" CssClass="Panel" Visible="false"
                    OnNeedDataSource="rgHead_NeedDataSource" OnExcelExportCellFormatting="rgHead_ExcelExportCellFormatting" OnExportCellFormatting="rgHead_ExportCellFormatting">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <ExportSettings OpenInNewWindow="true" IgnorePaging="true" HideStructureColumns="true" FileName="Exportar Inventario" />
                    <MasterTableView Width="100%" AllowNaturalSort="false" CommandItemDisplay="Top">
                        <CommandItemSettings ShowExportToExcelButton="true" ShowAddNewRecordButton="false" ShowRefreshButton="false" />
                        <Columns>
                            <tel:GridBoundColumn SortExpression="Ubicacion" HeaderText="Ubicacion" HeaderButtonType="TextButton" DataField="Ubicacion" UniqueName="Ubicacion" HeaderStyle-Width="120px" />
                            <tel:GridBoundColumn SortExpression="Codigo_Articulo" HeaderText="Codigo Articulo" HeaderButtonType="TextButton" DataField="Codigo_Articulo" UniqueName="Codigo_Articulo" HeaderStyle-Width="100px" DataFormatString="" />
                            <tel:GridBoundColumn SortExpression="Nombre_Articulo" HeaderText="Nombre Articulo" HeaderButtonType="TextButton" DataField="Nombre_Articulo" UniqueName="Nombre_Articulo" HeaderStyle-Width="380px" />
                            <tel:GridBoundColumn SortExpression="Categoria" HeaderText="Categoria" HeaderButtonType="TextButton" DataField="Categoria" UniqueName="Categoria" HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="Nombre_Categoria" HeaderText="Nombre Categoria" HeaderButtonType="TextButton" DataField="Nombre_Categoria" UniqueName="Nombre_Categoria" HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="Marca" HeaderText="Marca" HeaderButtonType="TextButton" DataField="Marca" UniqueName="Marca" HeaderStyle-Width="120px" />
                            <tel:GridBoundColumn SortExpression="Clase" HeaderText="Clase" HeaderButtonType="TextButton" DataField="Marca" UniqueName="Clase" HeaderStyle-Width="120px" />
                            <tel:GridBoundColumn SortExpression="Existencia" HeaderText="Existencia" HeaderButtonType="TextButton" DataField="Existencia" UniqueName="Existencia" HeaderStyle-Width="120px" />
                        </Columns>
                    </MasterTableView>
                    <ClientSettings>
                        <Resizing AllowColumnResize="true" />
                        <Selecting AllowRowSelect="true" />
                    </ClientSettings>
                </tel:RadGrid>--%>

            </div>
        </div>
    </div>

</asp:Content>
