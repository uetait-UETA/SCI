<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="Login1.aspx.cs" Inherits="Login1" %>

<%--<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>--%>

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

    <div class="container" style="margin-left: 20px;">
        <div class="row">
            <div class="col-md-12">
                <div id="divMessage" runat="server" class="alert-danger" />
                <asp:Label ID="Label3" runat="server" Text="" Font-Bold="False" Font-Italic="False" Font-Names="Tahoma" ForeColor="red"></asp:Label>
                <br />
            </div>
        </div>
        <div class="row">
            <div class="col-md-6">
                <asp:Panel ID="pnlAddApps" runat="server" CssClass="page-curl shadow-bottom">
                    <label class="PanelHeading">Login</label>
                    <div class="row">
                        <div class="col-md-12">
                            <ul class="myUL">
                                <li>
                                    <label class="myLabel" id="lblCompany" runat="server">Select Company</label>
                                    <asp:DropDownList ID="companyDDL" runat="server" DataSourceID="companyDSDDL"
                                        DataTextField="CompanyName" DataValueField="SmmNumId" AutoPostBack="False"
                                        CssClass="myDdlExtraLarge" />
                                </li>
                                <li id="liBranch" runat="server" visible="false">
                                    <label class="myLabel" id="lblBranchLogin" runat="server">Select Branch</label>
                                    <asp:DropDownList ID="branchDDL" runat="server" CssClass="myDdlExtraLarge" />
                                </li>
                                <li>
                                    <label class="myLabel" id="Label1" runat="server">User</label>
                                    <asp:TextBox ID="UserField1" runat="server" OnLoad="Page_Load" AutoCompleteType="disabled"
                                        ToolTip="User to log into the system" Width="183px"></asp:TextBox>
                                </li>
                                <li>
                                    <label class="myLabel" id="Label2" runat="server">Password</label>
                                    <asp:TextBox ID="Passwd1" runat="server" TextMode="Password" ToolTip="User Password"
                                        Width="183px"></asp:TextBox>
                                </li>
                                <li>&nbsp;
                                </li>
                                <li>
                                    <label class="myLabel">&nbsp;</label>
                                    <asp:Button ID="LogInBtn1" runat="server" OnClick="LogInBtn1_Click" Text="Log On" OnPreRender="LogInBtn1_PreRender" CssClass="mybtnmeduim" />
                                    &nbsp;&nbsp;&nbsp;
                                    <asp:Button ID="LogOutBtn1" runat="server" Text="Log Out" Height="21px" OnClick="LogOutBtn1_Click" Width="80px" OnPreRender="LogOutBtn1_PreRender" Visible="False" CssClass="mybtnmeduim" />
                                </li>
                                <li>
                                    <asp:Label ID="tmpLabel" runat="server"></asp:Label></li>
                            </ul>
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </div>
    </div>

    <asp:GridView ID="GridView1" runat="server"
        AutoGenerateColumns="False" DataSourceID="SqlDataSource1">
        <Columns>
            <asp:BoundField DataField="MsgSumary" HeaderText="Title" HtmlEncode="false"
                SortExpression="MsgSumary" />
            <asp:BoundField DataField="MsgText" HeaderText="Notice" HtmlEncode="false"
                SortExpression="MsgText" />
            <asp:BoundField DataField="MsgStartDate" HeaderText="Start Date"
                SortExpression="MsgStartDate" Visible="False" />
            <asp:BoundField DataField="MsgEndDate" HeaderText="End Date"
                SortExpression="MsgEndDate" Visible="False" />
        </Columns>
    </asp:GridView>

    <asp:SqlDataSource ID="SqlDataSource1" runat="server"
        ConnectionString="<%$ ConnectionStrings:smm_latConnectionString %>"
        SelectCommand="SELECT [MsgId], [MsgSumary], [MsgText], CONVERT(VARCHAR(10),msgstartdate,101) as msgstartdate, CONVERT(VARCHAR(10),MsgEndDate,101) as MsgEndDate  FROM [MsgMaintenance] where getdate() between msgstartdate and msgEndDate ORDER BY [MsgSumary]"></asp:SqlDataSource>

    <asp:ObjectDataSource ID="companyDSDDL" runat="Server"
        SelectMethod="GetCompanies" TypeName="SqlObjs"></asp:ObjectDataSource>


</asp:Content>

