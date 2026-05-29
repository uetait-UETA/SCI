<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="WhsItemBin.aspx.cs" Inherits="WhsItemBin" %>

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
                                    <label class="myLabel">Location</label>
                                    <asp:DropDownList ID="drpToWhsCode" runat="server" DataTextField="WHS"
                                    DataValueField="WhsCode" 
                                width="200px"> </asp:DropDownList>     

                                </li>
                                <li>
                                    <label class="myLabel">Category</label>
                                    <asp:DropDownList ID="DropDownItmGrp" runat="server" DataTextField="GroupName"
                                    DataValueField="GroupCode" 
                                width="200px" OnDataBinding="DropDownItmGrp_DataBinding" OnDataBound="DropDownItmGrp_DataBound" OnLoad="DropDownItmGrp_Load" OnPreRender="DropDownItmGrp_PreRender" OnSelectedIndexChanged="DropDownItmGrp_SelectedIndexChanged" OnTextChanged="DropDownItmGrp_TextChanged" AutoPostBack="True">
                                </asp:DropDownList>      </li>
                                <li>
                                    <label class="myLabel" style="vertical-align: top;">Brand</label>
                                    <asp:ListBox ID="lstItemGroups" runat="server" width="200px" 
                                DataTextField="Brand" DataValueField="Brand" Height="105px" 
                                    SelectionMode="Multiple">
                            </asp:ListBox>   </li>
                                <li>
                                    <label class="myLabel" style="vertical-align: top;">Product Code / Barcode</label>
                                    <asp:TextBox ID="idArticuloTBox" runat="server" Width="159px" Height="19px"></asp:TextBox>
                                </li>
                                <li>
                                    <label class="myLabel" style="vertical-align: top;">Bin</label>
                                    <asp:TextBox ID="idBinTBox" runat="server" Width="159px" Height="19px"></asp:TextBox>
                                </li>
                                <li>
                                    <label class="myLabel" style="vertical-align:top;"></label>
                                </li>
                            </ul>
                        </div>

                        <div class="col-md-7">
                            <ul class="myUL">
                                <li class="radio">
                                     </li>
                                <li>
                                       <asp:Button ID="btnCreateWorksheet" runat="server" 
                                Text="View Values" onclick="btnCreateWorksheet_Click" Height="36px" 
                                Width="120px" /> &nbsp; 
                                   <asp:Button ID="btnSaveChanges" runat="server" 
                                onclick="btnSaveChanges_Click" Text="Save Changes" width="120px" 
                                Height="36px" />   &nbsp; 
                                </li>
                                <li>&nbsp;
                                </li>
                                <li> 
                                </li>
                                <li>
                                </li>
                                <li>
                                </li>
                                <li>
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


                            <asp:GridView ID="GridView1" runat="server" BackColor="White" PageSize="15" CssClass="Panel" Width="100%"
                                BorderColor="#999999" BorderStyle="None" BorderWidth="1px" CellPadding="3" 
                                DataSourceID="ObjectDataSource1" GridLines="Vertical" AllowSorting="True" HeaderStyle-Font-Underline="true"
                                AutoGenerateColumns="False" 
                                onrowcreated="GridView1_RowCreated" 
                                onrowdatabound="GridView1_RowDataBound" 
                                onselectedindexchanged="GridView1_SelectedIndexChanged" 
                                OnRowCancelingEdit="GridView1_RowCancelingEdit"
                                OnRowDeleting="GridView1_RowDeleting" 
                                OnRowEditing="GridView1_RowEditing" 
                                OnRowUpdating="GridView1_RowUpdating"
                                DataKeyNames="whscode,itemcode,bin"
                                AllowPaging="True" >
                                <RowStyle BackColor="#EEEEEE" ForeColor="Black" />
                                <FooterStyle BackColor="#CCCCCC" ForeColor="Black" />
                                <PagerStyle BackColor="#999999" ForeColor="Black" HorizontalAlign="Left" />
                                <SelectedRowStyle BackColor="#008A8C" Font-Bold="True" ForeColor="White" />
                                <HeaderStyle BackColor="#000084" Font-Bold="True" ForeColor="White" />
                                <AlternatingRowStyle BackColor="Gainsboro" />
                                <Columns>
                                      <asp:TemplateField HeaderText="Item" SortExpression="itemcode">
                                        <ItemTemplate>
                                            <%# Eval("itemcode").ToString().Trim()%>
                                            <asp:HiddenField ID="hdnLoc" runat="server" Value='<%#Eval("whscode").ToString().Trim()%>' />
                                            <asp:HiddenField ID="hdnItem" runat="server" Value='<%#Eval("itemcode").ToString().Trim()%>' />
                                             <asp:HiddenField ID="HiddenBin" runat="server" Value='<%#Eval("bin") == DBNull.Value ? "" : Eval("bin").ToString().Trim()%>' />
                                        </ItemTemplate>
                                            <ItemStyle HorizontalAlign="Center"  Font-Bold="True" />
                                            <HeaderStyle HorizontalAlign="Center"  VerticalAlign="Bottom" />
                                    </asp:TemplateField>
                                    <asp:TemplateField HeaderText="Description" SortExpression="itemname" >
                                        <ItemTemplate>
                                            <%# Eval("itemname").ToString().Trim()%>
                                        </ItemTemplate>
                                            <ItemStyle HorizontalAlign="Left"  Width="350px" />
                                            <HeaderStyle HorizontalAlign="Left" VerticalAlign="Bottom" />
                                    </asp:TemplateField>
                                    <asp:TemplateField HeaderText="Category" SortExpression="itmsgrpnam" >
                                        <ItemTemplate>
                                            <%# Eval("itmsgrpnam").ToString().Trim()%>
                                        </ItemTemplate>
                                            <ItemStyle HorizontalAlign="Left"  Width="90px" />
                                            <HeaderStyle HorizontalAlign="Left" VerticalAlign="Bottom" />
                                    </asp:TemplateField>
                                    <asp:TemplateField HeaderText="Brand" SortExpression="u_brand" >
                                        <ItemTemplate>
                                            <%# Eval("u_brand").ToString().Trim()%>
                                        </ItemTemplate>
                                            <ItemStyle HorizontalAlign="Left"  Width="90px" />
                                            <HeaderStyle HorizontalAlign="Left" VerticalAlign="Bottom" />
                                    </asp:TemplateField>
                                     <asp:TemplateField HeaderText="Bin" SortExpression="bin" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center">
                                        <ItemTemplate>
                                            <asp:TextBox ID="txtbin" runat="server" Width="80" CssClass="txt60" Text='<%#Eval("bin") == DBNull.Value ? "" : Eval("bin").ToString().Trim()%>'></asp:TextBox>
                                        </ItemTemplate>
                                            <ItemStyle HorizontalAlign="Center" />
                                            <HeaderStyle HorizontalAlign="Center" VerticalAlign="Bottom" />
                                    </asp:TemplateField>
                                      <asp:TemplateField  HeaderText="Insert Bin" >
                                          <ItemStyle HorizontalAlign="Center" VerticalAlign="Bottom" Width="60px" />  
                                        <ItemTemplate>                              
                                              <asp:Button ID="InsertBtn"  runat="server"  Width="60" Text="Insert" onclick="InsertBtn_Click" ></asp:Button>
                                         </ItemTemplate>
                                            <ItemStyle HorizontalAlign="Right" />
                                            <HeaderStyle HorizontalAlign="Center" VerticalAlign="Bottom" />
                                    </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Bin Nuevo"  HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center">
                                        <ItemTemplate>
                                            <asp:TextBox ID="txtInsbin" runat="server"  Width="80" CssClass="txt60" ></asp:TextBox>
                                        </ItemTemplate>
                                            <ItemStyle HorizontalAlign="Center" />
                                            <HeaderStyle HorizontalAlign="Center" VerticalAlign="Bottom" />
                                    </asp:TemplateField>
                                     <asp:TemplateField HeaderText="Delete" >
                                        <ItemTemplate>                             
                                              <asp:Button ID="DeleteBtn" runat="server"  Width="60" Text="Delete" onclick="DeleteBtn_Click" ></asp:Button>
                                         </ItemTemplate>
                                            <ItemStyle HorizontalAlign="Center" VerticalAlign="Bottom" Width="60px" />
                                            <HeaderStyle HorizontalAlign="Center" VerticalAlign="Bottom" />
                                    </asp:TemplateField>
                                </Columns>           
                            </asp:GridView>
                            <asp:ObjectDataSource ID="ObjectDataSource1" runat="server" 
                                SelectMethod="GetItemBin" TypeName="WMS" 
                                onselected="ObjectDataSource1_Selected">
                                <SelectParameters>
                                    <asp:ControlParameter ControlID="drpToWhsCode" Name="store" 
                                        PropertyName="SelectedValue" Type="String" />
                                        <asp:Parameter  Name="depts" Type="String" />
                                    <asp:Parameter Name="brands" Type="String" />
                                    <asp:ControlParameter ControlID="idArticuloTBox" Name="lidArticulo" 
                                        PropertyName="Text" Type="String" />
                                    <asp:ControlParameter ControlID="idBinTBox" Name="lidBinTBox" 
                                        PropertyName="Text" Type="String" />                                    
                                </SelectParameters>
                            </asp:ObjectDataSource>

                            </div>
        </div>
    </div>
<%--                        &nbsp;
                            </td>
                    </tr>
                </table>
                </fieldset>--%>
     
     
     <script language="javascript" type="text/javascript">
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

