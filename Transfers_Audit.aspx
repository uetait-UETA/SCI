<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="Transfers_Audit.aspx.cs" Inherits="Transfers_Audit" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cphMain" runat="Server">

    <style>
        .myLabelMedium, .myLabelSmall
        {
            padding-left: 5px;
        }

        .myImageButton
        {
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
                <asp:Panel ID="pnlHeader" runat="server" CssClass="page-curl shadow-bottom">
                    <label  ID="labelForm" runat="server" class="PanelHeading">Transfer Audit</label>
                    <div class="row">
                        <div class="col-md-12" style="text-align: center;">
                            <%-- <asp:Label ID="CompanyLabel" runat="server" Text="Company" CssClass="myLabel"></asp:Label>--%>
                            <asp:Label ID="CompanyIdLabel" runat="server" Text="" CssClass="myLabel"></asp:Label>
                            <asp:Label ID="doQry" runat="server" Text="0" Visible="False"></asp:Label>
                        </div>
                    </div>
                    <div class="row">&nbsp;</div>
                    <div class="row">
                        <div class="col-md-1" style="text-align: center;">
                            <br />
                            <br />
                            <asp:Label ID="DocNumLabel" runat="server" Text="Draft Number" CssClass="myLabel"></asp:Label>
                            <asp:TextBox ID="txtDocNum" runat="server" OnPreRender="txtDocNum_PreRender" CssClass="TextboxSmall"></asp:TextBox>
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
                            <asp:Label ID="fromDateLabel" runat="server" Text="Date From" CssClass="myLabel"></asp:Label><br />
                            <telerik:RadDatePicker ID="FromDateTxt" runat="server" ShowPopupOnFocus="true" OnSelectedDateChanged="FromDateTxt_SelectedDateChanged" />
                            <br />
                            <br />
                            <asp:Label ID="toDateLabel" runat="server" Text="Date To" CssClass="myLabel"></asp:Label><br />
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
                    <MasterTableView Width="100%" AllowNaturalSort="false" DataKeyNames="Draft_Numero">
                        <Columns>
                            <tel:GridBoundColumn SortExpression="CompanyId" HeaderText="Company" HeaderButtonType="TextButton" DataField="CompanyId" UniqueName="CompanyId" HeaderStyle-Width="90px" />
                            <tel:GridBoundColumn SortExpression="Draft_Numero" HeaderText="Draft" HeaderButtonType="TextButton" DataField="Draft_Numero" UniqueName="Draft_Numero" />
                            <tel:GridBoundColumn SortExpression="DocNumITR" HeaderText="ITR #" HeaderButtonType="TextButton" DataField="DocNumITR" UniqueName="DocNumITR" DataFormatString="{0:#;-;}" HeaderStyle-Width="60px" ItemStyle-HorizontalAlign="Center" />
                            <tel:GridBoundColumn SortExpression="Nombre_Origen" HeaderText="Origin" HeaderButtonType="TextButton" DataField="Nombre_Origen" UniqueName="Nombre_Origen" HeaderStyle-Width="150px" />
                            <tel:GridBoundColumn SortExpression="Nombre_Destino" HeaderText="Destination" HeaderButtonType="TextButton" DataField="Nombre_Destino" UniqueName="Nombre_Destino" HeaderStyle-Width="150px" />
                            <tel:GridBoundColumn SortExpression="Estatus" HeaderText="St" HeaderButtonType="TextButton" DataField="Estatus" UniqueName="Estatus" />
                            <tel:GridBoundColumn SortExpression="Despachado" HeaderText="Des" HeaderButtonType="TextButton" DataField="Despachado" UniqueName="Despachado" />
                            <tel:GridBoundColumn SortExpression="Recibido" HeaderText="Rec" HeaderButtonType="TextButton" DataField="Recibido" UniqueName="Recibido" />
                            <tel:GridBoundColumn SortExpression="Usuario_Originador" HeaderText="Originator" HeaderButtonType="TextButton" DataField="Usuario_Originador" UniqueName="Usuario_Originador" HeaderStyle-Width="80px" />
                            <tel:GridBoundColumn SortExpression="Usuario_Despacho" HeaderText="Dispatcher" HeaderButtonType="TextButton" DataField="Usuario_Despacho" UniqueName="Usuario_Despacho" HeaderStyle-Width="90px" />
                            <tel:GridBoundColumn SortExpression="Usuario_Recibo" HeaderText="Receiver" HeaderButtonType="TextButton" DataField="Usuario_Recibo" UniqueName="Usuario_Recibo" HeaderStyle-Width="80px" />
                            <tel:GridBoundColumn SortExpression="Doc_Entry_Sap" HeaderText="Ent SAP" HeaderButtonType="TextButton" DataField="Doc_Entry_Sap" UniqueName="Doc_Entry_Sap" />
                            <tel:GridBoundColumn SortExpression="Doc_Num_Sap" HeaderText="Num SAP" HeaderButtonType="TextButton" DataField="Doc_Num_Sap" UniqueName="Doc_Num_Sap" />
                            <tel:GridBoundColumn SortExpression="Fecha_Originacion" HeaderText="Draft Date" HeaderButtonType="TextButton" DataField="Fecha_Originacion" UniqueName="Fecha_Originacion" DataFormatString="{0:d}" HeaderStyle-Width="70px" />
                            <tel:GridBoundColumn SortExpression="Fecha_Despacho" HeaderText="Dispatch Date" HeaderButtonType="TextButton" DataField="Fecha_Despacho" UniqueName="Fecha_Despacho" DataFormatString="{0:d}" HeaderStyle-Width="70px" />
                            <tel:GridBoundColumn SortExpression="Fecha_Recibo" HeaderText="Receipt Date" HeaderButtonType="TextButton" DataField="Fecha_Recibo" UniqueName="Fecha_Recibo" DataFormatString="{0:d}" HeaderStyle-Width="70px" />
                            <tel:GridBoundColumn SortExpression="Tiempo_Despachar" HeaderText="T_Des" HeaderButtonType="TextButton" DataField="Tiempo_Despachar" UniqueName="Tiempo_Despachar" />
                            <tel:GridBoundColumn SortExpression="Tiempo_Recibir" HeaderText="T_Rec" HeaderButtonType="TextButton" DataField="Tiempo_Recibir" UniqueName="Tiempo_Recibir" />
                            <tel:GridBoundColumn SortExpression="SistemaDes" HeaderText="SisDes" HeaderButtonType="TextButton" DataField="SistemaDes" UniqueName="SistemaDes" />
                            <tel:GridBoundColumn SortExpression="SistemaRec" HeaderText="SisRec" HeaderButtonType="TextButton" DataField="SistemaRec" UniqueName="SistemaRec" />
                        </Columns>
                    </MasterTableView>
                    <ClientSettings>
                        <Resizing AllowColumnResize="true" />
                        <Selecting AllowRowSelect="true" />
                    </ClientSettings>
                </tel:RadGrid>

                <%--<asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" CssClass="GridViewPanel"
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

                <asp:TemplateField HeaderText="Draft" SortExpression="Draft_Numero">
                    <ItemTemplate><%#Eval("Draft_Numero")%></ItemTemplate>
                </asp:TemplateField>

                <asp:TemplateField HeaderText="Origen" SortExpression="Nombre_Origen">
                    <ItemTemplate><%#Eval("Nombre_Origen")%></ItemTemplate>
                </asp:TemplateField>

                <asp:TemplateField HeaderText="Destino" SortExpression="Nombre_Destino">
                    <ItemTemplate><%#Eval("Nombre_Destino")%></ItemTemplate>
                </asp:TemplateField>

                <asp:TemplateField HeaderText="Stat" SortExpression="Estatus">
                    <ItemTemplate><%#Eval("Estatus")%></ItemTemplate>
                    <ItemStyle HorizontalAlign="Center" />
                </asp:TemplateField>

                <asp:TemplateField HeaderText="Des" SortExpression="Despachado">
                    <ItemTemplate><%#Eval("Despachado")%></ItemTemplate>
                    <ItemStyle HorizontalAlign="Center" />
                </asp:TemplateField>

                <asp:TemplateField HeaderText="Rec" SortExpression="Recibido">
                    <ItemTemplate><%#Eval("Recibido")%></ItemTemplate>
                    <ItemStyle HorizontalAlign="Center" />
                </asp:TemplateField>

                <asp:TemplateField HeaderText="Originator" SortExpression="Usuario_Originador">
                    <ItemTemplate><%#Eval("Usuario_Originador")%></ItemTemplate>
                    <ItemStyle HorizontalAlign="Center" />
                </asp:TemplateField>

                <asp:TemplateField HeaderText="Dispatcher" SortExpression="Usuario_Despacho">
                    <ItemTemplate><%#Eval("Usuario_Despacho")%></ItemTemplate>
                    <ItemStyle HorizontalAlign="Center" />
                </asp:TemplateField>

                <asp:TemplateField HeaderText="Receiver" SortExpression="Usuario_Recibo">
                    <ItemTemplate><%#Eval("Usuario_Recibo")%></ItemTemplate>
                    <ItemStyle HorizontalAlign="Center" />
                </asp:TemplateField>

                <asp:TemplateField HeaderText="Ent_Sap" SortExpression="Doc_Entry_Sap">
                    <ItemTemplate><%#Eval("Doc_Entry_Sap")%></ItemTemplate>
                    <ItemStyle HorizontalAlign="Center" />
                </asp:TemplateField>

                <asp:TemplateField HeaderText="Num_Sap" SortExpression="Doc_Num_Sap">
                    <ItemTemplate><%#Eval("Doc_Num_Sap")%></ItemTemplate>
                    <ItemStyle HorizontalAlign="Center" />
                </asp:TemplateField>

                <asp:TemplateField HeaderText="Fe_Draft" SortExpression="Fecha_Originacion">
                    <ItemTemplate><%# String.Format("{0:MM/dd/yyyy}", Eval("Fecha_Originacion"))%></ItemTemplate>
                    <ItemStyle HorizontalAlign="Center" />
                </asp:TemplateField>

                <asp:TemplateField HeaderText="Fe_Despacho" SortExpression="Fecha_Despacho">
                    <ItemTemplate><%# String.Format("{0:MM/dd/yyyy}", Eval("Fecha_Despacho"))%></ItemTemplate>
                    <ItemStyle HorizontalAlign="Center" />
                </asp:TemplateField>

                <asp:TemplateField HeaderText="Fe_Recibo" SortExpression="Fecha_Recibo">
                    <ItemTemplate><%# String.Format("{0:MM/dd/yyyy}", Eval("Fecha_Recibo"))%></ItemTemplate>
                    <ItemStyle HorizontalAlign="Center" />
                </asp:TemplateField>

                <asp:TemplateField HeaderText="T_Des" SortExpression="Tiempo_Despachar">
                    <ItemTemplate><%#Eval("Tiempo_Despachar")%></ItemTemplate>
                </asp:TemplateField>

                <asp:TemplateField HeaderText="T_Rec" SortExpression="Tiempo_Recibir">
                    <ItemTemplate><%#Eval("Tiempo_Recibir")%></ItemTemplate>
                </asp:TemplateField>
                
                <asp:TemplateField HeaderText="SistemaDes" SortExpression="SistemaDes">
		                    <ItemTemplate><%#Eval("SistemaDes")%></ItemTemplate>
		                    <ItemStyle HorizontalAlign="Center" />
                </asp:TemplateField>
            
            
		<asp:TemplateField HeaderText="SistemaRec" SortExpression="SistemaRec">
			    <ItemTemplate><%#Eval("SistemaRec")%></ItemTemplate>
			    <ItemStyle HorizontalAlign="Center" />
                </asp:TemplateField>
                
            </Columns>

        </asp:GridView>--%>

                <%--<asp:ObjectDataSource ID="ObjectDataSource1" runat="server"
            SelectMethod="GetTransferAudit" TypeName="Transfer">
            <SelectParameters>
                <asp:ControlParameter ControlID="StatusRadioButtonList" Name="statusDoc" PropertyName="SelectedValue" Type="String" />
                <asp:ControlParameter ControlID="txtDocNum" Name="txtDocNum" PropertyName="Text" Type="String" />             
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

