<%@ Page Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="TransferDelete.aspx.cs" Inherits="TransferDelete" %>
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

    <asp:HiddenField ID="hfScreenWidth" runat="server" />
    <asp:HiddenField ID="hfScreenHeight" runat="server" />

    <div class="container" style="margin-left: 20px;">
        <div class="row">
            <div class="col-md-12">
                <div id="divMessage" runat="server" class="alert-danger" />
                <br />
            </div>
        </div>
        <div class="row">
            <div class="col-md-12">
                <asp:Label ID="CompanyIdLabel" runat="server" Text="Label"></asp:Label>
            </div>
        </div>
        
        
        <div class="row">
            <div class="col-md-12">
            <asp:Panel ID="pnlHeader" runat="server" CssClass="page-curl shadow-bottom">
	                    <label id="labelForm" runat="server" class="PanelHeading">Transfer Deletion</label>	     
                <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" AllowSorting="True" CssClass="GridViewPanel"
                    DataSourceID="ObjectDataSource1" AllowPaging="True" PageSize="50" EnableModelValidation="True">

                    <RowStyle BackColor="#EEEEEE" ForeColor="Black" />
                    <FooterStyle BackColor="#CCCCCC" ForeColor="Black" />
                    <PagerStyle BackColor="#999999" ForeColor="Black" HorizontalAlign="Center" />
                    <SelectedRowStyle BackColor="#008A8C" Font-Bold="True" ForeColor="White" />
                    <HeaderStyle BackColor="#000084" Font-Bold="True" ForeColor="White" />
                    <AlternatingRowStyle BackColor="Gainsboro" />

                    <Columns>

                        <asp:TemplateField HeaderText="Doc #" SortExpression="DocEntry">
                            <ItemTemplate>
                                <%--<%#"<a href=\"javascript:popUpReport4('TransferDetailsPrint.aspx?DocEntry=" + Eval("DocEntry").ToString().Trim() + "')\">" + Eval("DocEntry") + "</a>"%>--%>
                                <asp:LinkButton ID="lnkbDocEntry" runat="server" Text='<%#Eval("DocEntry").ToString().Trim()%>' OnClick="lnkbDocEntry_Click"></asp:LinkButton>
                                <asp:HiddenField ID="hdnDocEntry" runat="server" Value='<%#Eval("DocEntry").ToString().Trim()%>' />
                            </ItemTemplate>
                            <HeaderStyle VerticalAlign="Bottom"></HeaderStyle>
                            <ItemStyle HorizontalAlign="Center"></ItemStyle>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Date" SortExpression="DocDate">
                            <ItemTemplate><%# String.Format("{0:MM/dd/yyyy}", Eval("DocDate"))%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Stat" SortExpression="DocStatus">
                            <ItemTemplate><%#Eval("DocStatus")%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="From" SortExpression="FromLocName">
                            <ItemTemplate><%#Eval("FromLocName")%></ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="To" SortExpression="ToLocName">
                            <ItemTemplate><%#Eval("ToLocName")%></ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Category" SortExpression="Category">
                            <ItemTemplate><%#Eval("Category")%></ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="TotalLines" SortExpression="TotalLines">
                            <ItemTemplate><%#Eval("TotalLines")%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="TotalQty" SortExpression="TotalQty">
                            <ItemTemplate><%#String.Format("{0:#,###}", Eval("TotalQty"))%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="D" SortExpression="dispatched">
                            <ItemTemplate><%#String.Format("{0:#,###}", Eval("dispatched"))%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="R" SortExpression="received">
                            <ItemTemplate><%#String.Format("{0:#,###}", Eval("received"))%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Dc" SortExpression="DispCompleted">
                            <ItemTemplate><%#String.Format("{0:#,###}", Eval("DispCompleted"))%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Rc" SortExpression="ReceCompleted">
                            <ItemTemplate><%#String.Format("{0:#,###}", Eval("ReceCompleted"))%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Delete" ItemStyle-Width="76px" HeaderStyle-Width="76px">
                            <ItemTemplate>
                                <asp:Button ID="DeleteBtn" runat="server" CssClass="mybtn" Text="Delete" OnClick="DeleteBtn_Click" OnClientClick="return confirm('Do you want to delete this transfer?')"></asp:Button>
                            </ItemTemplate>
                        </asp:TemplateField>

                    </Columns>

                </asp:GridView>
              </asp:Panel>
                <asp:ObjectDataSource ID="ObjectDataSource1" runat="server" 
                    SelectMethod="GetTransferDraftsToDelete" 
                    TypeName="Transfer">
                    <SelectParameters>
                        <asp:ControlParameter ControlID="CompanyIdLabel" Name="CompanyId" Type="String" />
                    </SelectParameters>
                </asp:ObjectDataSource>
            </div>
        </div>
    </div>

    <div class="row">
        <tel:RadWindow ID="rwTransfers" runat="server" Modal="true" OpenerElementID=""
            Title="" Behaviors="Reload, Move, Resize" VisibleTitlebar="false" VisibleStatusbar="false"
            RegisterWithScriptManager="true" Style="display: inline; overflow: hidden;" ShowContentDuringLoad="false" Top="-10" Left="100" />
    </div>

    <script type="text/javascript">
        var myHiddenWidth = document.getElementById('<%= hfScreenWidth.ClientID %>');
        var myHiddenHeight = document.getElementById('<%= hfScreenHeight.ClientID %>');

        if (myHiddenWidth) {
            myHiddenWidth.value = window.innerWidth;//screen.width;
        }

        if (myHiddenHeight) {
            myHiddenHeight.value = window.innerHeight;//screen.height;
        }

        function DeleteTransfer() {
            var DocEntry = 1

            if (DocEntry != "") {
                var url = "deleteTransfer.aspx?DocEntry=" + DocEntry;
                popUpReport(url);
                return false;
            }
            else {
                return true;
            }
        }

        function verifyDeleteTra() {
            return confirm("WARNING!!\n\nAre you sure you want to Delete this Transfer?\n\nClick OK to Delete or Cancel to abort");
        }

    </script>

</asp:Content>

