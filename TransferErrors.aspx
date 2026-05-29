<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="TransferErrors.aspx.cs" Inherits="TransferErrors" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cphMain" runat="Server">

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
                    <label  ID="labelForm" runat="server" class="PanelHeading">Transfer Errors</label>
                    <div class="row">
                        <div class="col-md-6 radio">
                            <asp:RadioButton ID="radioShowOpen" runat="server" GroupName="grpDisplay" Text="Show uncorrected errors only" Checked="True" type="radio" />
                            <asp:RadioButton ID="radioShowAll" runat="server" GroupName="grpDisplay" Text="Show all errors" type="radio" />
                        </div>
                        <div class="col-md-6">
                            <asp:Button ID="BtnSearch" runat="server" Text="Search" CssClass="mybtnmeduim" OnClick="btnSearch_Click" />
                            <asp:Button ID="Button2" runat="server" OnClick="Button2_Click" Text="Update" CssClass="mybtnmeduim" />
                            <%--<asp:Button ID="Button1" runat="server" Text="Buscar" CssClass="mybtnmeduim" OnClick="btnSearch_Click" />
                            <asp:Button ID="Button3" runat="server" OnClick="Button2_Click" Text="Actualizar" CssClass="mybtnmeduim" />--%>
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </div>
        <div class="row">&nbsp;</div>
        <div class="row">
            <div class="col-md-12">
                <h3 class="myLabelFull">Transfer Errors</h3>
                <asp:Label ID="CompanyIdLabel" runat="server" Text="" Visible="false" CssClass="myLabel" />
            </div>
        </div>
        <div class="row">&nbsp;</div>
        <div class="row">
            <div class="col-md-12">
                <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" AllowSorting="True" AllowPaging="True" PageSize="50" CssClass="GridViewPanel"
                    DataSourceID="ObjectDataSource1" OnDataBinding="GridView1_DataBinding" OnRowDataBound="GridView1_RowDataBound">

                    <RowStyle BackColor="#EEEEEE" ForeColor="Black" />
                    <FooterStyle BackColor="#CCCCCC" ForeColor="Black" />
                    <PagerStyle BackColor="#999999" ForeColor="Black" HorizontalAlign="Center" />
                    <SelectedRowStyle BackColor="#008A8C" Font-Bold="True" ForeColor="White" />
                    <HeaderStyle BackColor="#000084" Font-Bold="True" ForeColor="White" />
                    <AlternatingRowStyle BackColor="Gainsboro" />
                    <Columns>
                        <asp:TemplateField HeaderText="Entry" SortExpression="DocEntryOri">
                            <ItemTemplate><%#Eval("DocEntryOri")%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Line" SortExpression="line">
                            <ItemTemplate><%#Eval("line")%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Date" SortExpression="docdate">
                            <ItemTemplate><%#Eval("docdate", "{0:MM/dd/yyyy}")%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="FromWhs" SortExpression="fromwhscode">
                            <ItemTemplate><%#Eval("fromwhscode")%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="ToWhs" SortExpression="towhscode">
                            <ItemTemplate><%#Eval("towhscode")%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="ToOriWhs" SortExpression="tooriwhscode">
                            <ItemTemplate><%#Eval("tooriwhscode")%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Item" SortExpression="itemcode">
                            <ItemTemplate><%#Eval("itemcode")%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Left" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Qty" SortExpression="quantity">
                            <ItemTemplate><%#Eval("quantity")%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="User" SortExpression="userapp">
                            <ItemTemplate><%#Eval("userapp")%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Error" SortExpression="error_message">
                            <ItemTemplate><%#Eval("error_message")%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Left" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="F" SortExpression="fixed">
                            <ItemTemplate>
                                <asp:TextBox ID="TextBox1" runat="server" Text='<%# Eval("Fixed") %>' MaxLength="10" Width="10" Enabled="false">  </asp:TextBox>
                            </ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Error Fixed">
                            <ItemTemplate>
                                <asp:RadioButtonList ID="rblFixed" runat="server" RepeatDirection="Horizontal">
                                    <asp:ListItem Value="1" Text="Fixed"></asp:ListItem>
                                    <asp:ListItem Value="0" Text="No Fixed"></asp:ListItem>
                                </asp:RadioButtonList>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="S">
                            <ItemTemplate>
                                <asp:Label ID="lblSelected" runat="server" Text='<%# Eval("Fixed") %>'>
                                </asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="DE">
                            <ItemTemplate>
                                <asp:Label ID="DE" runat="server" Text='<%# Eval("DocEntryOri") %>'>
                                </asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Ln">
                            <ItemTemplate>
                                <asp:Label ID="Ln" runat="server" Text='<%# Eval("line") %>'>
                                </asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>

                    </Columns>

                </asp:GridView>
                <asp:ObjectDataSource ID="ObjectDataSource1" runat="server"
                    SelectMethod="GetTransferErrors" TypeName="Transfer">
                    <SelectParameters>
                        <asp:Parameter Name="ShowAll" Type="Boolean" />
                        <asp:ControlParameter ControlID="CompanyIdLabel" Name="CompanyId"
                            PropertyName="Text" Type="String" />
                    </SelectParameters>
                </asp:ObjectDataSource>
            </div>
        </div>
    </div>

</asp:Content>

