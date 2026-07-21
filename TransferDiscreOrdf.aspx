<%@ Page Language="C#" AutoEventWireup="true" CodeFile="TransferDiscreOrdf.aspx.cs" Inherits="TransferDiscreOrdf" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">

<head id="Head1" runat="server">
    <title></title>
    <meta http-equiv="X-UA-Compatible" content="IE=edge" />
    <link href="BootStrap/css/bootstrap.css" rel="stylesheet" type="text/css" />
    <link href="Font-Awesome/css/font-awesome.css" rel="stylesheet" type="text/css" />
    <link href="Styles/StyleSheet.css" rel="stylesheet" type="text/css" />

</head>

<body>
    <a href="javascript:CloseOnReload();" class="modalClose"></a>
    <form id="form1" runat="server" onkeypress="test();">
        <div class="container-fluid">
            <asp:Panel ID="Panel1" runat="server" OnLoad="Panel1_Load">
                <asp:SqlDataSource ID="SqlDataSource1" runat="server" ConflictDetection="CompareAllValues"
                    ConnectionString="<%$ ConnectionStrings:smm_latConnectionString %>"
                    OldValuesParameterFormatString="original_{0}" SelectCommand="SELECT DocEntry, DocNum , DocDate, &#13;&#10;FromWhsCode+' - '+FromWhsName FromWhs, &#13;&#10;ToWhsCode+' - '+ToWhsName ToWhs,&#13;&#10;DocStatus, Dispatched, DispCompleted, Received, ReceCompleted, &#13;&#10;rtrim(convert(char(10),DocNumTraDis)) DocDisDis, &#13;&#10;rtrim(convert(char(10),DocNumTraRec)) DocDisRec,&#13;&#10;rtrim(convert(char(10),DocNumTraRec2)) DocDisRec2,&#13;&#10;userdispatch, userreceive&#13;&#10;FROM smm_Transdiscrep_odrf WHERE (CompanyId = @CompanyId) AND (DocEntry = @DocEntry)">
                    <SelectParameters>
                        <asp:Parameter Name="DocEntry" />
                        <asp:ControlParameter Name="CompanyId" ControlID="CompanyLabel" PropertyName="Text" Type="String" />
                    </SelectParameters>
                </asp:SqlDataSource>
                <div class="row">&nbsp;</div>
                <div class="row">
                    <div class="col-md-12">
                        <asp:Panel ID="Panel2" runat="server" CssClass="Panel">
                            <label class="PanelHeading">Draft Transfer Header</label>
                            <div class="row">&nbsp;</div>
                            <div class="row">
                                <div class="col-md-6">
                                    <asp:Label ID="CompanyLabel" runat="server" Text="" CssClass="myLabel" />
                                </div>
                                <div class="col-md-6 pull-right">
                                    <asp:Label ID="DocEntryLabel" runat="server" Text=""></asp:Label>
                                </div>
                            </div>

                            <div class="row">
                                <div class="col-md-12">
                                    <div style="width:950px; text-align:right;">
                                        <asp:Label ID="GtkConfLabel" runat="server" Text="" Visible="False"
                                            Font-Bold="True" ForeColor="Green" />
                                    </div>
                                    <asp:GridView ID="GridView1" runat="server" Style="position: relative" AutoGenerateColumns="False" DataSourceID="ObjectDataSource1" Height="3px" Width="950px" OnDataBound="GridView1_DataBound" OnPreRender="GridView1_PreRender" BackColor="White" BorderColor="#999999">
                                        <Columns>
                                            <asp:BoundField DataField="DocNumITR" HeaderText="ITR #" ReadOnly="True" SortExpression="DocNumITR">
                                                <ItemStyle Width="60px" HorizontalAlign="Center" Wrap="False" />
                                                <HeaderStyle Width="60px" Wrap="False" HorizontalAlign="Center" />
                                            </asp:BoundField>
                                            <asp:BoundField DataField="DocNum" HeaderText="Num" ReadOnly="True" SortExpression="DocNum" />
                                            <asp:BoundField DataField="DocDate" HeaderText="Date" SortExpression="DocDate" ReadOnly="True" />
                                            <asp:BoundField DataField="FromWhs" HeaderText="From Whs" ReadOnly="True" SortExpression="FromWhs" />
                                            <asp:BoundField DataField="ToWhs" HeaderText="To Whs" ReadOnly="True" SortExpression="ToWhs" />
                                            <asp:BoundField DataField="DocStatus" HeaderText="St" SortExpression="DocStatus" ReadOnly="True" />
                                            <asp:BoundField DataField="Dispatched" HeaderText="Di" SortExpression="Dispatched" ReadOnly="True" />
                                            <asp:BoundField DataField="DispCompleted" HeaderText="DC" SortExpression="DispCompleted" ReadOnly="True" />
                                            <asp:BoundField DataField="Received" HeaderText="Re" SortExpression="Received" ReadOnly="True" />
                                            <asp:BoundField DataField="ReceCompleted" HeaderText="RC" SortExpression="ReceCompleted" ReadOnly="True" />
                                            <asp:BoundField DataField="DocDisDis" HeaderText="DisNum" ReadOnly="True" SortExpression="DocDisDis" />
                                            <asp:BoundField DataField="DocDisRec" HeaderText="RecNum" ReadOnly="True" SortExpression="DocDisRec" />
                                            <asp:BoundField DataField="DocDisRec2" HeaderText="TrasNum" ReadOnly="True" SortExpression="DocDisRec2" />
                                            <asp:BoundField DataField="userdispatch" HeaderText="UserDis" ReadOnly="True" SortExpression="userdispatch" />
                                            <asp:BoundField DataField="userreceive" HeaderText="UserRec" ReadOnly="True" SortExpression="userreceive" />
                                            <asp:BoundField DataField="DispatchType" HeaderText="SisDes" ReadOnly="True" SortExpression="DispatchType" />
                                            <asp:BoundField DataField="ReceiveType" HeaderText="SisRec" ReadOnly="True" SortExpression="ReceiveType" />
                                            <asp:BoundField DataField="U_GTK_CONFIRMATION" HeaderText="GTKConf" ReadOnly="True" SortExpression="U_GTK_CONFIRMATION" Visible="False" />
                                            <asp:BoundField DataField="DocEntryITR" HeaderText="DocEntryITR" ReadOnly="True" SortExpression="DocEntryITR" Visible="False" />
                                            <asp:BoundField DataField="FromWhsType" HeaderText="FromWhsType" ReadOnly="True" SortExpression="FromWhsType" Visible="False" />
                                            <asp:BoundField DataField="DocEntryTraRec2" HeaderText="DocEntryTraRec2" ReadOnly="True" SortExpression="DocEntryTraRec2" Visible="False" />
                                        </Columns>
                                        <HeaderStyle BackColor="#000084" Font-Bold="True" Font-Size="Small" Font-Underline="True"
                                            ForeColor="White" />
                                    </asp:GridView>
                                </div>
                            </div>

                            <div class="row">&nbsp;</div>

                            <div class="row">
                                <div class="col-md-12">
                                    <asp:Button ID="btnPrint" runat="server" Style="position: relative" Text="Print" CssClass="mybtnmeduim" OnClick="btnPrint_Click" Font-Size="Small" />&nbsp;
                                    <asp:Button ID="Button2" runat="server" CssClass="mybtnmeduim" OnClick="Button2_Click1" Text="Receive" OnClientClick="return verifySORequest()" />&nbsp;
                                    <asp:Button ID="Button1" runat="server" Text="Button" OnClientClick="return verifySORequest()" CssClass="mybtnmeduim" OnClick="Button1_Click" />&nbsp;
                                    <asp:CheckBox ID="ZeroCheckBox" runat="server" Text="Accept Zero Quantities" ForeColor="Red" Visible="False" Checked="True" />
                                </div>
                            </div>

                            <div class="row">&nbsp;</div>

                            <div class="row">
                                <div class="col-md-12">
                                    <asp:Label ID="LabelCurUser" runat="server" Style="position: relative" Text="Label" CssClass="myLabel" />
                                </div>
                            </div>

                            <div class="row">&nbsp;</div>

                            <div class="row">
                                <div class="col-md-12">
                                    <asp:Label ID="LabelMsg" runat="server" Style="position: relative" Text="Label" Font-Underline="True" ForeColor="Red" />
                                </div>
                            </div>
                        </asp:Panel>
                    </div>
                </div>

                <div class="row">&nbsp;</div>

                <div class="row">
                    <div class="col-md-12">

                        <asp:GridView ID="GridView2" runat="server" AutoGenerateColumns="False" DataKeyNames="LineNum,DocEntry" CssClass="GridViewPanel"
                            DataSourceID="ObjectDataSource2" OnDataBound="GridView2_DataBound">
                            <Columns>
                                <%--0--%>
                                <asp:BoundField DataField="LineNum" HeaderText="Line" ReadOnly="True" SortExpression="LineNum">
                                    <ItemStyle HorizontalAlign="Center" />
                                </asp:BoundField>

                                <%--2--%>
                                <asp:BoundField DataField="Item" HeaderText="Item" ReadOnly="True" SortExpression="Item">
                                    <ItemStyle HorizontalAlign="Left" />
                                </asp:BoundField>

                                <%--3--%>
                                <asp:BoundField DataField="BarCode" HeaderText="Bar Code" ReadOnly="True" SortExpression="BarCode">
                                    <ItemStyle HorizontalAlign="Left" />
                                </asp:BoundField>

                                <%--4--%>
                                <asp:BoundField DataField="DraftQuantity" HeaderText="Draft" ReadOnly="True" SortExpression="DraftQuantity">
                                    <ItemStyle HorizontalAlign="Center" />
                                </asp:BoundField>

                                <%--5--%>
                                <asp:BoundField DataField="DispatchQuantity" HeaderText="Dispatch" SortExpression="DispatchQuantity" ReadOnly="True">
                                    <ItemStyle HorizontalAlign="Center" />
                                </asp:BoundField>

                                <%--6--%>
                                <asp:BoundField DataField="ReceivedQuantity" HeaderText="Receive" SortExpression="ReceivedQuantity" ReadOnly="True">
                                    <ItemStyle HorizontalAlign="Center" />
                                </asp:BoundField>

                                <%--7--%>
                                <asp:BoundField DataField="tmpQuantity" HeaderText="tmpQuantity" SortExpression="tmpQuantity">
                                    <ItemStyle HorizontalAlign="Center" />
                                </asp:BoundField>

                                <%--8--%>

                                <asp:TemplateField HeaderText="Actual Quantity" SortExpression="tmpQuantity">
                                    <ItemTemplate>
                                        <asp:TextBox ID="TextBox1" runat="server" Text='<%# Eval("tmpQuantity") %>' ReadOnly="True" BackColor="#F0F0F0">  </asp:TextBox>
                                        <asp:RegularExpressionValidator ID="RegularExpressionValidator1" runat="server" ErrorMessage="Enter whole numbers only"
                                            Style="position: relative" ControlToValidate="TextBox1" ValidationGroup="Check" ValidationExpression="^\d+$"></asp:RegularExpressionValidator></div>
                                    </ItemTemplate>
                                    <HeaderStyle HorizontalAlign="Center" />
                                    <ItemStyle HorizontalAlign="Center" />
                                </asp:TemplateField>

                                <%--9--%>
                                <asp:BoundField DataField="userrecscanner" HeaderText="UsuarioRec" ReadOnly="True" SortExpression="userrecscanner">
                                    <ItemStyle HorizontalAlign="Left" />
                                </asp:BoundField>

                            </Columns>
                            <RowStyle BackColor="#EFF3FB" />
                            <FooterStyle BackColor="#CCCCCC" Font-Bold="True" ForeColor="White" />
                            <PagerStyle BackColor="#999999" ForeColor="White" HorizontalAlign="Center" />
                            <SelectedRowStyle BackColor="#008A8C" Font-Bold="True" ForeColor="#333333" />
                            <HeaderStyle BackColor="#000084" Font-Bold="True" Font-Overline="False" Font-Size="Small"
                                Font-Underline="True" ForeColor="White" />
                            <EditRowStyle BackColor="#2461BF" />
                            <AlternatingRowStyle BackColor="Gainsboro" />
                        </asp:GridView>
                    </div>
                </div>
            </asp:Panel>
        </div>
        <asp:ObjectDataSource ID="ObjectDataSource1" runat="server"
            SelectMethod="GetTransdiscrepOrder" TypeName="Transfer">
            <SelectParameters>
                <asp:ControlParameter ControlID="CompanyLabel" Name="companyId"
                    PropertyName="Text" Type="String" />
                <asp:ControlParameter ControlID="DocEntryLabel" Name="DocEntry"
                    PropertyName="Text" Type="String" />
            </SelectParameters>
        </asp:ObjectDataSource>
        <asp:ObjectDataSource ID="ObjectDataSource2" runat="server"
            SelectMethod="GetTransdiscrepOrderDtl" TypeName="Transfer">
            <SelectParameters>
                <asp:ControlParameter ControlID="CompanyLabel" Name="companyId"
                    PropertyName="Text" Type="String" />
                <asp:ControlParameter ControlID="DocEntryLabel" Name="DocEntry"
                    PropertyName="Text" Type="String" />
            </SelectParameters>
        </asp:ObjectDataSource>
        &nbsp;
    </form>

    <script language="javascript" type="text/javascript">
        function verifySORequest() {
            var ok = confirm("ALERT!!\n\nQuantities in SAP will be updated.\n\nAre you sure you want to continue?");
            if (ok) {
                document.body.style.cursor = 'wait';
            }
            return ok;
        }

        function test() {
            if (window.event.keyCode == 13) {
                window.event.cancelBubble = true;
                window.event.returnValue = false;
            }
        }

        function GetRadWindow() {
            var oWindow = null;
            if (window.radWindow) oWindow = window.radWindow;
            else if (window.frameElement && window.frameElement.radWindow) oWindow = window.frameElement.radWindow;
            return oWindow;
        }

        function CloseOnReload() {
            if (window.parent && window.parent !== window && typeof window.parent.closeTransferWindow === 'function') {
                window.parent.closeTransferWindow(null);
            } else {
                var wnd = GetRadWindow();
                if (wnd) wnd.close();
                else window.close();
            }
        }

    </script>
</body>
</html>
