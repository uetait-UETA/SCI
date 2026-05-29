<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="Transfers_Audit_Item.aspx.cs" Inherits="Transfers_Audit_Item" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cphMain" runat="Server">

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
    <asp:HiddenField ID="hfFromDate" runat="server" />
    <asp:HiddenField ID="hfToDate" runat="server" />
    <asp:HiddenField ID="hfScreenWidth" runat="server" />
    <asp:HiddenField ID="hfScreenHeight" runat="server" />

    <div class="container-fluid">
        <div class="row">
            <div class="col-md-12">
                <div id="divMessage" runat="server" class="alert-danger" />
                <br />
            </div>
        </div>
        <div class="row">
            <div class="col-md-12">
                <asp:Panel ID="pnlHeader" runat="server" CssClass="Panel">
                    <label id="labelForm" runat="server" class="PanelHeading">Transfer Audit by Product</label>
                    <div class="row">
                        <div class="col-md-12" style="text-align: center;">
                            <asp:Label ID="CompanyIdLabel" runat="server" Text="" CssClass="myLabel"></asp:Label>
                            <asp:Label ID="doQry" runat="server" Text="0" Visible="False"></asp:Label>
                        </div>
                    </div>
                    <div class="row">&nbsp;</div>
                    <div class="row">
                        <div class="col-md-1" style="text-align: center;">
                            <!-- //2019-ABR-10: Modificado por Aldo Reina, para la b�squeda por c�digo de barras: -->
                            <%--<asp:Label ID="DocNumLabel0" runat="server" Text="Codigo Articulo" CssClass="myLabel"></asp:Label>--%>
                            <asp:Label ID="DocNumLabel0" runat="server" Text="Item Code / Barcode" CssClass="myLabel"></asp:Label>
                            <asp:TextBox ID="ItemCodeTbox" runat="server" OnPreRender="txtDocNum_PreRender" CssClass="Textbox"></asp:TextBox>
                            <%--<tel:RadSearchBox ID="ItemCodeTbox" runat="server"  CssClass="TextboxMedium"
                                DropDownSettings-Width="100px" DropDownSettings-Height="200px"
                                DataTextField="ItemCode" DataValueField="ItemCode"
                                ShowSearchButton="false" MinFilterLength="3"
                                Filter="StartsWith" DataSourceID="ItemAutoComplete" OnDataSourceSelect="ItemCodeTbox_DataSourceSelect" />--%>
                            <asp:SqlDataSource ID="ItemAutoComplete" runat="server" ConnectionString='<%$ ConnectionStrings:smm_latConnectionString %>' ProviderName='<%$ ConnectionStrings:smm_latConnectionString.ProviderName %>' />
                            <!-- //2019-ABR-10: Agregado por Aldo Reina, para la b�squeda por c�digo de barras: -->
                            <telerik:RadButton RenderMode="Lightweight" ID="rbtnCancel" runat="server" Text=" Cancel" OnClick="RbtnCancel_Click" Visible="False">
                                <Icon PrimaryIconCssClass="rbCancel" PrimaryIconLeft="5" PrimaryIconBottom="11"></Icon>
                            </telerik:RadButton>
                            <asp:DropDownList ID="ItemList" ClientIDMode="Static" runat="server" Visible="false" AutoPostBack="True" OnSelectedIndexChanged="ItemList_SelectedIndexChanged"></asp:DropDownList>

                            <br />
                            <asp:Label ID="DocNumLabel" runat="server" Text="Draft Number" CssClass="myLabel"></asp:Label>
                            <asp:TextBox ID="txtDocNum" runat="server" OnPreRender="txtDocNum_PreRender" CssClass="TextboxMedium"></asp:TextBox>
                        </div>
                        <div class="col-md-1" style="text-align: center;">
                            <br />
                            <br />
                            <br />
                            <asp:DropDownList ID="andOrDropDownList1" runat="server" CssClass="myDdlXSmall">
                                <asp:ListItem Selected="True">AND</asp:ListItem>
                                <asp:ListItem>OR</asp:ListItem>
                            </asp:DropDownList>
                        </div>
                        <div class="col-md-2" style="text-align: center;">
                            <asp:Label ID="fromDateLabel" runat="server" Text="From Date" CssClass="myLabel"></asp:Label><br />
                            <telerik:RadDatePicker ID="FromDateTxt" runat="server" ShowPopupOnFocus="true" OnSelectedDateChanged="FromDateTxt_SelectedDateChanged" />
                            <br />
                            <br />
                            <asp:Label ID="toDateLabel" runat="server" Text="To Date" CssClass="myLabel"></asp:Label><br />
                            <telerik:RadDatePicker ID="toDateTxt" runat="server" ShowPopupOnFocus="true" OnSelectedDateChanged="toDateTxt_SelectedDateChanged" />
                            <br />
                            <br />
                        </div>
                        <div class="col-md-1" style="text-align: center;">
                            <br />
                            <br />
                            <br />
                            <asp:DropDownList ID="andOrDropDownList2" runat="server" CssClass="myDdlXSmall">
                                <asp:ListItem Selected="True">AND</asp:ListItem>
                                <asp:ListItem>OR</asp:ListItem>
                            </asp:DropDownList>
                        </div>
                        <div class="col-md-2" style="text-align: center;">
                            <asp:Label ID="fromWhsLabel" runat="server" Text="Origin" CssClass="myLabel"></asp:Label><br />
                            <asp:DropDownList ID="drpFromWhsCode" runat="server" DataTextField="WHS" DataValueField="WhsCode" CssClass="myDdlMedium" /><br />
                            <br />
                            <asp:Label ID="toWhsLabel" runat="server" Text="Destination" CssClass="myLabel" /><br />
                            <asp:DropDownList ID="drpToWhsCode" runat="server" DataTextField="WHS" DataValueField="WhsCode" CssClass="myDdlMedium" /><br />
                        </div>
                        <div class="col-md-1" style="text-align: center;">
                            <br />
                            <br />
                            <br />
                            <asp:DropDownList ID="andOrDropDownList3" runat="server" CssClass="myDdlXSmall">
                                <asp:ListItem Selected="True">AND</asp:ListItem>
                                <asp:ListItem>OR</asp:ListItem>
                            </asp:DropDownList>
                        </div>
                        <div class="col-md-2" style="text-align: center;">
                            <asp:RadioButtonList ID="StatusRadioButtonList" runat="server" RepeatLayout="Flow" RepeatDirection="Vertical" CssClass="radio" Style="padding: 0 0 0 15px">
                                <asp:ListItem Selected="True" Value="O">&quot;Open  &quot;</asp:ListItem>
                                <asp:ListItem Value="C">Closed</asp:ListItem>
                                <asp:ListItem Value="All">&quot;All. .  . .&quot;</asp:ListItem>
                            </asp:RadioButtonList><br />
                            <asp:Label ID="categoryLabel" runat="server" Text="Category" CssClass="myLabel" Visible="False"></asp:Label><br />
                            <asp:DropDownList ID="drpItemGroups" runat="server" DataTextField="GroupName" DataValueField="GroupCode" Visible="False" CssClass="myDdlMedium" />
                        </div>
                        <div class="col-md-1" style="text-align: center;">
                            <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="mybtn" OnClick="btnSearch_Click" /><br />
                            <asp:Button ID="ExportToExcel" runat="server" Text="To Excel" CssClass="mybtn" OnClick="ExportToExcel_Click" />
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </div>

        <div class="row">&nbsp;</div>
        <div class="row">&nbsp;</div>
        <div class="row">
            <div class="col-md-12">

                <tel:RadGrid ID="rgHead" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False"
                    AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="True" PageSize="15" CssClass="Panel"
                    OnNeedDataSource="rgHead_NeedDataSource">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <MasterTableView Width="100%" AllowNaturalSort="false">
                        <Columns>
                            <tel:GridBoundColumn SortExpression="CompanyId" HeaderText="Company" HeaderButtonType="TextButton" DataField="CompanyId" UniqueName="CompanyId" HeaderStyle-Width="90px" />
                            <tel:GridBoundColumn SortExpression="Draft_Numero" HeaderText="Draft" HeaderButtonType="TextButton" DataField="Draft_Numero" UniqueName="Draft_Numero" />
                            <tel:GridBoundColumn SortExpression="Fecha_Originacion" HeaderText="Origin Date" HeaderButtonType="TextButton" DataField="Fecha_Originacion" UniqueName="Fecha_Originacion" DataFormatString="{0:d}" />
                            <tel:GridBoundColumn SortExpression="Nombre_Origen" HeaderText="Origin" HeaderButtonType="TextButton" DataField="Nombre_Origen" UniqueName="Nombre_Origen" HeaderStyle-Width="150px" />
                            <tel:GridBoundColumn SortExpression="Nombre_Destino" HeaderText="Destination" HeaderButtonType="TextButton" DataField="Nombre_Destino" UniqueName="Nombre_Destino" HeaderStyle-Width="150px" />
                            <tel:GridBoundColumn SortExpression="Estatus" HeaderText="St" HeaderButtonType="TextButton" DataField="Estatus" UniqueName="Estatus" />
                            <tel:GridBoundColumn SortExpression="LineNum" HeaderText="Line" HeaderButtonType="TextButton" DataField="LineNum" UniqueName="LineNum" />
                            <tel:GridBoundColumn SortExpression="ItemCode" HeaderText="Item Code" HeaderButtonType="TextButton" DataField="ItemCode" UniqueName="ItemCode" />
                            <tel:GridBoundColumn SortExpression="BarCode" HeaderText="Bar Code" HeaderButtonType="TextButton" DataField="BarCode" UniqueName="BarCode" />
                            <tel:GridBoundColumn SortExpression="ItemName" HeaderText="Item Description" HeaderButtonType="TextButton" DataField="ItemName" UniqueName="ItemName" HeaderStyle-Width="250px" />
                            <tel:GridBoundColumn SortExpression="DraftQuantity" HeaderText="QO" HeaderButtonType="TextButton" DataField="DraftQuantity" UniqueName="DraftQuantity" />
                            <tel:GridBoundColumn SortExpression="DispatchQuantity" HeaderText="QD" HeaderButtonType="TextButton" DataField="DispatchQuantity" UniqueName="DispatchQuantity" />
                            <tel:GridBoundColumn SortExpression="ReceivedQuantity" HeaderText="QR" HeaderButtonType="TextButton" DataField="ReceivedQuantity" UniqueName="ReceivedQuantity" />
                            <tel:GridBoundColumn SortExpression="Price" HeaderText="Price" HeaderButtonType="TextButton" DataField="Price" UniqueName="Price" DataFormatString="{0:C2}" />
                        </Columns>
                    </MasterTableView>
                    <ClientSettings>
                        <Resizing AllowColumnResize="true" />
                        <Selecting AllowRowSelect="true" />
                    </ClientSettings>
                </tel:RadGrid>

                <%--<asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False"
                    BackColor="White" BorderColor="#999999" BorderStyle="None" BorderWidth="1px"
                    CellPadding="3" GridLines="Vertical" AllowSorting="True"
                    DataSourceID="ObjectDataSource1" AllowPaging="True" PageSize="50"
                    OnDataBound="GridView1_DataBound">

                    <RowStyle BackColor="#EEEEEE" ForeColor="Black" />
                    <FooterStyle BackColor="#CCCCCC" ForeColor="Black" />
                    <PagerStyle BackColor="#999999" ForeColor="Black" HorizontalAlign="Center" />
                    <SelectedRowStyle BackColor="#008A8C" Font-Bold="True" ForeColor="White" />
                    <HeaderStyle BackColor="#000084" Font-Bold="True" ForeColor="White" />
                    <AlternatingRowStyle BackColor="Gainsboro" />

                    <Columns>

                        <asp:TemplateField HeaderText="DocNum" SortExpression="Draft_Numero">
                            <ItemTemplate><%#Eval("Draft_Numero")%></ItemTemplate>
                        </asp:TemplateField>


                        <asp:TemplateField HeaderText="Fecha_Ori" SortExpression="Fecha_Originacion">
                            <ItemTemplate><%# String.Format("{0:MM/dd/yyyy}", Eval("Fecha_Originacion"))%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>


                        <asp:TemplateField HeaderText="Origen" SortExpression="Nombre_Origen">
                            <ItemTemplate><%#Eval("Nombre_Origen")%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Destino" SortExpression="Nombre_Destino">
                            <ItemTemplate><%#Eval("Nombre_Destino")%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="St" SortExpression="Estatus">
                            <ItemTemplate><%#Eval("Estatus")%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Linea" SortExpression="LineNum">
                            <ItemTemplate><%#Eval("LineNum")%></ItemTemplate>
                        </asp:TemplateField>


                        <asp:TemplateField HeaderText="CodArticulo" SortExpression="ItemCode">
                            <ItemTemplate><%#Eval("ItemCode")%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Descripcion Articulo" SortExpression="ItemName">
                            <ItemTemplate><%#Eval("ItemName")%></ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="QO" SortExpression="DraftQuantity">
                            <ItemTemplate><%#Eval("DraftQuantity")%></ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="QD" SortExpression="DispatchQuantity">
                            <ItemTemplate><%#Eval("DispatchQuantity")%></ItemTemplate>
                        </asp:TemplateField>


                        <asp:TemplateField HeaderText="QR" SortExpression="ReceivedQuantity">
                            <ItemTemplate><%#Eval("ReceivedQuantity")%></ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Precio" SortExpression="Price">
                            <ItemTemplate><%#Eval("Price")%></ItemTemplate>
                        </asp:TemplateField>
                    </Columns>

                </asp:GridView>--%>

                <%--<asp:ObjectDataSource ID="ObjectDataSource1" runat="server"
                    SelectMethod="GetTransferAuditItem" TypeName="Transfer">
                    <SelectParameters>
                        <asp:ControlParameter ControlID="StatusRadioButtonList" Name="statusDoc" PropertyName="SelectedValue" Type="String" />
                        <asp:ControlParameter ControlID="txtDocNum" Name="txtDocNum" PropertyName="Text" Type="String" />
                        <asp:ControlParameter ControlID="ItemCodeTbox" Name="ItemCodeTbox" PropertyName="Text" Type="String" />
                        <asp:ControlParameter ControlID="FromDateTxt" Name="FromDateTxt" PropertyName="Text" Type="String" />
                        <asp:ControlParameter ControlID="toDateTxt" Name="toDateTxt" PropertyName="Text" Type="String" />
                        <asp:ControlParameter ControlID="drpFromWhsCode" Name="fromLocTxt" PropertyName="Text" Type="String" />
                        <asp:ControlParameter ControlID="drpToWhsCode" Name="toLocTxt" PropertyName="Text" Type="String" />
                        <asp:ControlParameter ControlID="drpItemGroups" Name="categoryTxt" PropertyName="Text" Type="String" />
                        <asp:ControlParameter ControlID="andOrDropDownList1" Name="andOr1" PropertyName="Text" Type="String" />
                        <asp:ControlParameter ControlID="andOrDropDownList2" Name="andOr2" PropertyName="Text" Type="String" />
                        <asp:ControlParameter ControlID="andOrDropDownList3" Name="andOr3" PropertyName="Text" Type="String" />
                        <asp:ControlParameter ControlID="doQry" Name="doQry" PropertyName="Text" Type="String" />
                        <asp:ControlParameter ControlID="CompanyIdLabel" Name="CompanyId" PropertyName="Text" Type="String" />
                    </SelectParameters>
                </asp:ObjectDataSource>--%>
            </div>
        </div>
    </div>

    <script type="text/javascript">

        function displayTransfer() {
            var DocEntry = document.getElementById("<%=txtDocNum.ClientID%>").value;

            if (DocEntry != "") {
                var url = "TransferDetails.aspx?DocEntry=" + DocEntry;
                popUpReport(url);
                return false;
            }
            else {
                return true;
            }
        }

    </script>

</asp:Content>


