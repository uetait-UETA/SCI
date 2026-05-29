<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="createTransferByExcel.aspx.cs" Inherits="createTransferByExcel" %>


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
            <div class="col-md-8">
                <asp:Panel ID="pnlHeader" runat="server" CssClass="page-curl shadow-bottom">
                    <label  ID="labelForm" runat="server" class="PanelHeading">Create Transfer by Excel</label>
                    <div class="row">
                        <div class="col-md-12">
                            <ul class="myUL">
                                <li>
                                    <label class="myLabel">&nbsp;</label>
                                    <asp:Label ID="LabDocEntry" runat="server" CssClass="myLabel" ForeColor="#cc3333" />
                                </li>
                                <li>
                                    <label class="myLabel">Source</label>
                                    <asp:DropDownList ID="drpFromWhsCode" runat="server" DataTextField="WHS" DataValueField="WhsCode" AutoPostBack="True" CssClass="myDdlLarge" OnSelectedIndexChanged="drpFromWhsCode_SelectedIndexChanged" />
                                </li>
                                <li>
                                    <label class="myLabel">Destination</label>
                                    <asp:DropDownList ID="drpToWhsCode" runat="server" DataTextField="WHS" DataValueField="WhsCode" AutoPostBack="True" CssClass="myDdlLarge" />
                                </li>
                                <li>
                                    <label class="myLabel" style="vertical-align: top;">Excel File</label>
                                    <asp:FileUpload ID="FileUpload1" runat="server" ClientIDMode="Static" style="display:none;" />
                                    <button type="button" class="mybtnmeduim" onclick="document.getElementById('FileUpload1').click(); return false;">Choose File</button>
                                    &nbsp;
                                    <span id="spnFileName" style="display:inline-block; min-width:340px; max-width:480px; border:1px solid #ccc; padding:3px 8px; background:white; vertical-align:middle; color:#555; font-size:13px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap;" title="">No file chosen</span>
                                </li>
                                <li>&nbsp;
                                </li>
                                <li>
                                    <label class="myLabel">&nbsp;</label>
                                    <asp:Button ID="btnCreateDraft" runat="server" CssClass="mybtnlarge" Text="Create Draft Transfer" OnClick="btnCreateDraft_Click" OnClientClick="javascript: return verifySORequestCreTra()" />
                                    <asp:Button ID="btnCreateTransfer" runat="server" CssClass="mybtnlarge" Text="Submit Draft" OnClick="btnCreateTransfer_Click" OnClientClick="javascript: return verifySORequestCreTra()" />
                                    <asp:Button ID="btnUdateDraft" runat="server" CssClass="mybtnlarge" Text="Refresh Quantities" OnClick="btnUdateDraft_Click" />
                                    <asp:Button ID="btnCancel" runat="server" CssClass="mybtnlarge" Text="Delete Draft" OnClick="btnCancel_Click" />
                                </li>
                                <li>&nbsp;
                                </li>
                                <li>
                                    <label class="myLabel">&nbsp;</label>
                                    <asp:TextBox ID="DocEntry" runat="server" OnLoad="Page_Load" CssClass="myDdlLarge" Visible="False">0</asp:TextBox>
                                </li>
                                <li>&nbsp;
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
                <asp:Label ID="Label3" runat="server" CssClass="myLabel" ForeColor="#CC0000" Text="PRODUCTS FOUND IN SAP WITH ON-HAND"></asp:Label>
            </div>
        </div>
        <div class="row">&nbsp;</div>
        <div class="row">
            <div class="col-md-8">
                <asp:GridView ID="GridView1" runat="server" DataSourceID="SqlDataSource1" CssClass="GridViewPanel"
                    EnableModelValidation="True" AutoGenerateColumns="False" DataKeyNames="LineNum,DocEntry" EnableTheming="False" EnableViewState="False" AllowPaging="False">
                    <RowStyle BackColor="#EEEEEE" ForeColor="Black" />
                    <FooterStyle BackColor="#CCCCCC" ForeColor="Black" />
                    <PagerStyle BackColor="#999999" ForeColor="Black" HorizontalAlign="Center" />
                    <SelectedRowStyle BackColor="#008A8C" Font-Bold="True" ForeColor="White" />
                    <HeaderStyle BackColor="#004990" Font-Bold="True" ForeColor="White" HorizontalAlign="Center" />
                    <AlternatingRowStyle BackColor="Gainsboro" />
                    <Columns>
                        <asp:BoundField DataField="LineNum" HeaderText="Line" ReadOnly="True" SortExpression="LineNum" />
                        <asp:BoundField DataField="ItemCode" HeaderText="ItemCode" SortExpression="ItemCode" />
                        <asp:BoundField DataField="ItemName" HeaderText="ItemName" SortExpression="ItemName" ItemStyle-HorizontalAlign="Left" ItemStyle-Width="350px" />
                        <asp:BoundField DataField="onhand" HeaderText="Onhand" SortExpression="onhand" />
                        <asp:TemplateField HeaderText="Quantity" SortExpression="DraftQuantity">
                            <ItemTemplate>
                                <asp:TextBox ID="TextBox1" runat="server" Text='<%# Bind("DraftQuantity") %>' CssClass="myLabel" Font-Size="X-Small"></asp:TextBox>
                                <asp:Label ID="Label1" runat="server" Text='<%# Bind("DraftQuantity") %>' CssClass="myLabel"></asp:Label>

                            </ItemTemplate>
                            <ItemTemplate>
                                <asp:Label ID="Label1" runat="server" Text='<%# Bind("DraftQuantity") %>' CssClass="myLabel"></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Enter Quantity" SortExpression="DraftQuantity">
                            <ItemTemplate>
                                <asp:TextBox ID="TextBox1" runat="server" Text='<%# Eval("DraftQuantity") %>' CssClass="myLabel">  </asp:TextBox>
                                <asp:RegularExpressionValidator ID="RegularExpressionValidator1" runat="server" ErrorMessage="Enter integers only"
                                    Style="position: relative" ControlToValidate="TextBox1" ValidationGroup="Check" ValidationExpression="^\d+$"></asp:RegularExpressionValidator>
                            </ItemTemplate>
                            <HeaderStyle HorizontalAlign="Center" />
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
                <asp:SqlDataSource ID="SqlDataSource1" runat="server" ConnectionString="<%$ ConnectionStrings:smm_latConnectionString %>" 
                    SelectCommandType="StoredProcedure"
                    SelectCommand="SMM_S_TransXsap_drf1" 
                    ConflictDetection="CompareAllValues" 
                    DeleteCommand="DELETE FROM [smm_TransXsap_drf1] WHERE [CompanyId] = @CompanyId AND [LineNum] = @original_LineNum AND [DocEntry] = @original_DocEntry AND (([ItemCode] = @original_ItemCode) OR ([ItemCode] IS NULL AND @original_ItemCode IS NULL)) AND (([ItemName] = @original_ItemName) OR ([ItemName] IS NULL AND @original_ItemName IS NULL)) AND (([DraftQuantity] = @original_DraftQuantity) OR ([DraftQuantity] IS NULL AND @original_DraftQuantity IS NULL))" 
                    InsertCommand="INSERT INTO [smm_TransXsap_drf1] ([CompanyId], [LineNum], [ItemCode], [ItemName], [DraftQuantity], [DocEntry]) VALUES (@CompanyId, @LineNum, @ItemCode, @ItemName, @DraftQuantity, @DocEntry)" 
                    OldValuesParameterFormatString="original_{0}" 
                    UpdateCommand="UPDATE [smm_TransXsap_drf1] SET [ItemCode] = @ItemCode, [ItemName] = @ItemName, [DraftQuantity] = @DraftQuantity WHERE [CompanyId] = @CompanyId AND [LineNum] = @original_LineNum AND [DocEntry] = @original_DocEntry AND (([ItemCode] = @original_ItemCode) OR ([ItemCode] IS NULL AND @original_ItemCode IS NULL)) AND (([ItemName] = @original_ItemName) OR ([ItemName] IS NULL AND @original_ItemName IS NULL)) AND (([DraftQuantity] = @original_DraftQuantity) OR ([DraftQuantity] IS NULL AND @original_DraftQuantity IS NULL))">
                    <DeleteParameters>
                        <asp:SessionParameter Name="CompanyId" Type="String" SessionField="CompanyId" />
                        <asp:Parameter Name="original_LineNum" Type="Int32" />
                        <asp:Parameter Name="original_DocEntry" Type="Int32" />
                        <asp:Parameter Name="original_ItemCode" Type="String" />
                        <asp:Parameter Name="original_ItemName" Type="String" />
                        <asp:Parameter Name="original_DraftQuantity" Type="Decimal" />
                    </DeleteParameters>
                    <InsertParameters>
                        <asp:SessionParameter Name="CompanyId" Type="String" SessionField="CompanyId" />
                        <asp:Parameter Name="LineNum" Type="Int32" />
                        <asp:Parameter Name="ItemCode" Type="String" />
                        <asp:Parameter Name="ItemName" Type="String" />
                        <asp:Parameter Name="DraftQuantity" Type="Decimal" />
                        <asp:Parameter Name="DocEntry" Type="Int32" />
                    </InsertParameters>
                    <SelectParameters>
                        <asp:SessionParameter Name="CompanyId" Type="String" SessionField="CompanyId" />
                        <asp:ControlParameter ControlID="DocEntry" Name="DocEntry" PropertyName="Text" Type="Int32" />
                    </SelectParameters>
                    <UpdateParameters>
                        <asp:SessionParameter Name="CompanyId" Type="String" SessionField="CompanyId" />
                        <asp:Parameter Name="ItemCode" Type="String" />
                        <asp:Parameter Name="ItemName" Type="String" />
                        <asp:Parameter Name="DraftQuantity" Type="Decimal" />
                        <asp:Parameter Name="original_LineNum" Type="Int32" />
                        <asp:Parameter Name="original_DocEntry" Type="Int32" />
                        <asp:Parameter Name="original_ItemCode" Type="String" />
                        <asp:Parameter Name="original_ItemName" Type="String" />
                        <asp:Parameter Name="original_DraftQuantity" Type="Decimal" />
                    </UpdateParameters>
                </asp:SqlDataSource>
            </div>
        </div>
        <div class="row">&nbsp;</div>
        <div class="row">
            <div class="col-md-12">
                <asp:Label ID="Label2" runat="server" Font-Bold="True" CssClass="myLabel" ForeColor="#CC0000" Text="PRODUCTS IN EXCEL FILE"></asp:Label>
            </div>
        </div>
        <div class="row">&nbsp;</div>
        <div class="row">
            <div class="col-md-8">
                <asp:GridView ID="GridView2" runat="server" CssClass="GridViewPanel"
                        EnableModelValidation="False" EnableTheming="False" EnableViewState="False" HorizontalAlign="Left" AllowPaging="False">
                        <AlternatingRowStyle BackColor="White" ForeColor="#284775" />
                        <EditRowStyle BackColor="#999999" />
                        <FooterStyle BackColor="#CCCCCC" Font-Bold="True" ForeColor="White" />
                        <HeaderStyle BackColor="#000084" Font-Bold="True" ForeColor="White" />
                        <PagerStyle BackColor="#999999" ForeColor="White" HorizontalAlign="Left" />
                        <RowStyle BackColor="#EEEEEE" ForeColor="#333333" HorizontalAlign="Left" />
                        <SelectedRowStyle BackColor="#008A8C" Font-Bold="True" ForeColor="#333333" />

                    </asp:GridView>
            </div>
        </div>
    </div>

    <script type="text/javascript">
        document.getElementById('FileUpload1').addEventListener('change', function () {
            var display = document.getElementById('spnFileName');
            var name = this.files && this.files.length > 0 ? this.files[0].name : 'No file chosen';
            display.textContent = name;
            display.title = name;
        });
    </script>

</asp:Content>

