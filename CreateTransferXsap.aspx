<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="CreateTransferXsap.aspx.cs" Inherits="CreateTransferXsap" %>


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
                    <label  ID="labelForm" runat="server" class="PanelHeading">Create Manual Transfer</label>
                    <div class="row">
                        <div class="col-md-12">
                            <ul class="myUL">
                                <li>
                                    <label class="myLabel">&nbsp;</label>
                                    <asp:Label ID="CompanyIdLabel" runat="server" CssClass="myLabel" />
                                </li>
                                <li>
                                    <label class="myLabel">Source</label>
                                    <asp:DropDownList ID="drpFromWhsCode" runat="server" DataTextField="WHS" DataValueField="WhsCode" AutoPostBack="True" CssClass="myDdlLarge" OnSelectedIndexChanged="drpFromWhsCode_SelectedIndexChanged" />
                                    <asp:Label ID="LabDocEntry" runat="server" Font-Bold="True" ForeColor="#CC3333"></asp:Label>
                                </li>
                                <li>
                                    <label class="myLabel">Destination</label>
                                    <asp:DropDownList ID="drpToWhsCode" runat="server" DataTextField="WHS" DataValueField="WhsCode" AutoPostBack="False" CssClass="myDdlLarge" />
                                    <asp:TextBox ID="DocEntry" runat="server" OnLoad="Page_Load" Height="8px" Width="8px" Visible="False">0</asp:TextBox>
                                </li>
                                <li>
                                    <label class="myLabel" style="vertical-align: top;">Category</label>
                                    <asp:DropDownList ID="drpItemGroups" runat="server" CssClass="myDdlLarge" DataTextField="GroupName" DataValueField="GroupCode" AutoPostBack="True" OnSelectedIndexChanged="drpItemGroups_SelectedIndexChanged" />
                                </li>
                                <li>
                                    <label class="myLabel" style="vertical-align: top;">Brand</label>
                                    <asp:ListBox ID="lstItemGroups" runat="server" CssClass="myDdlLarge" DataTextField="Brand" DataValueField="Brand" Height="105px" SelectionMode="Multiple" />
                                </li>
                                <li>
                                    <label class="myLabel" style="vertical-align: top;">Product Code &#47; Barcode</label>
                                    <asp:TextBox ID="ItemTextBox" runat="server" CssClass="myDdlLarge"></asp:TextBox>
                                </li>
                                <li>
                                    <label class="myLabel" style="vertical-align:top;"></label>
                                    <asp:DropDownList ID="ItemList" ClientIDMode="Static" runat="server" Visible="false" AutoPostBack="True" OnSelectedIndexChanged="ItemList_SelectedIndexChanged" CssClass="myDdlLarge"></asp:DropDownList>
                                    <telerik:RadButton RenderMode="Lightweight" ID="rbtnCancel" runat="server" Text=" Cancel" OnClick="RbtnCancel_Click" Visible="false">
                                        <Icon PrimaryIconCssClass="rbCancel" PrimaryIconLeft="5" PrimaryIconBottom="11"></Icon>
                                    </telerik:RadButton>
                                </li>
                                <li>
                                    <label class="myLabel">&nbsp;</label>
                                    <asp:Button ID="btnCreateDraft" runat="server" CssClass="mybtnlarge" Text="Create Draft Transfer" OnClick="btnCreateDraft_Click" OnClientClick="javascript: return verifySORequestCreTra()" />
                                    <asp:Button ID="btnCreateTransfer" runat="server" CssClass="mybtnlarge" Text="Submit Draft" OnClick="btnCreateTransfer_Click" OnClientClick="javascript: return verifySORequestCreTra()" />
                                    <asp:Button ID="btnUdateDraft" runat="server" CssClass="mybtnlarge" Text="Refresh Quantities" OnClick="btnUdateDraft_Click" />
                                    <asp:Button ID="btnCancel" runat="server" CssClass="mybtnlarge" Text="Delete Draft" OnClick="btnCancel_Click" />
                                </li>
                            </ul>
                        </div>
                    </div>
                    <div class="row">&nbsp;</div>
                </asp:Panel>
            </div>
        </div>
        <div class="row">&nbsp;</div>
        <div class="row">
            <div class="col-md-12">
                <asp:GridView ID="GridView1" runat="server" DataSourceID="ObjectDataSource1" 
                    EnableModelValidation="True" AutoGenerateColumns="False" DataKeyNames="LineNum,DocEntry" CssClass="GridViewPanel">
                    <RowStyle BackColor="#EEEEEE" ForeColor="Black" />
                    <FooterStyle BackColor="#CCCCCC" ForeColor="Black" />
                    <PagerStyle BackColor="#999999" ForeColor="Black" HorizontalAlign="Center" />
                    <SelectedRowStyle BackColor="#008A8C" Font-Bold="True" ForeColor="White" />
                    <HeaderStyle BackColor="#004990" Font-Bold="True" ForeColor="White" HorizontalAlign="Center" />
                    <AlternatingRowStyle BackColor="Gainsboro" />
                    <Columns>
                        <asp:BoundField DataField="LineNum" HeaderText="Line" ReadOnly="True" SortExpression="LineNum" />
                        <asp:BoundField DataField="ItemCode" HeaderText="ItemCode" SortExpression="ItemCode" ItemStyle-Width="80px" />
                        <asp:BoundField DataField="BarCode" HeaderText="Bar Code" SortExpression="BarCode" ItemStyle-Width="120px" />
                        <asp:BoundField DataField="ItemName" HeaderText="ItemName" SortExpression="ItemName" ItemStyle-Width="400px" />
                        <asp:BoundField DataField="onhand" HeaderText="Onhand" SortExpression="onhand" />
                        <asp:TemplateField HeaderText="Quantity" SortExpression="DraftQuantity">
                            <ItemTemplate>
                                <asp:TextBox ID="TextBox1" runat="server" Text='<%# Bind("DraftQuantity") %>'></asp:TextBox>
                                <asp:Label ID="Label1" runat="server" Text='<%# Bind("DraftQuantity") %>'></asp:Label>
                            </ItemTemplate>
                            <ItemTemplate>
                                <asp:Label ID="Label1" runat="server" Text='<%# Bind("DraftQuantity") %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Enter Quantity" SortExpression="DraftQuantity">
                            <ItemTemplate>
                                <asp:TextBox ID="TextBox1" runat="server" Text='<%# Eval("DraftQuantity") %>'>  </asp:TextBox>
                                <asp:RegularExpressionValidator ID="RegularExpressionValidator1" runat="server" ErrorMessage="Enter integers only" 
                                    Style="position: relative" ControlToValidate="TextBox1" ValidationGroup="Check" ValidationExpression="^\d+$" />
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>

                <asp:ObjectDataSource ID="ObjectDataSource1" runat="server" SelectMethod="GetTransXsapDtl" TypeName="Transfer">
                    <SelectParameters>
                        <asp:ControlParameter ControlID="DocEntry" Name="DocEntry" PropertyName="Text" Type="String" />
                        <asp:ControlParameter ControlID="CompanyIdLabel" Name="CompanyId" PropertyName="Text" Type="String" />
                    </SelectParameters>            
                </asp:ObjectDataSource>
            </div>
        </div>
    </div>
</asp:Content>

