<%@ Control Language="C#" AutoEventWireup="true" CodeFile="LoginEditForm.ascx.cs" Inherits="LoginEditForm"%>

<%@ Register TagPrefix="telerik" Namespace="Telerik.Web.UI" Assembly="Telerik.Web.UI" %>

<table id="Table2" width="100%" border="0" rules="none"
    style="border-collapse: collapse;">
    <tr class="EditFormHeader">
        <td colspan="2">
            <b>Login Details</b>
        </td>
    </tr>
    <tr>
        <td>
            <table id="Table3" width=" 600px" border="0" class="module" cellspacing="4" cellpadding="4" >
                <tr>
                    <td class="title" style="font-weight: bold;" colspan="2">&nbsp;</td>
                </tr>
                <tr>
                    <td class="tdLabelBold">User
                    </td>
                    <td class="tdValue">
                        <asp:DropDownList ID="drpUsers" runat="server" Width="280px" 
                            TabIndex="0" AppendDataBoundItems="True" DataValueField="UserID" DataTextField="UserFullName">
                            <asp:ListItem Selected="True" Text="Select" Value="">
                            </asp:ListItem>
                        </asp:DropDownList>


                        <%--<asp:Label ID="lblUser" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.UserFullName") %>' TabIndex="0" >
                        </asp:Label>--%>
                    </td>
                </tr>
                <tr>
                    <td class="tdLabelBold">Login ID
                    </td>
                    <td class="tdValue">
                        <asp:TextBox ID="txtLoginID" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.LoginID") %>' TabIndex="1" Width="280px">
                        </asp:TextBox>
                    </td>
                </tr>
                <tr>
                    <td class="tdLabelBold">Password
                    </td>
                    <td class="tdValue">
                        <asp:TextBox ID="txtPassWd" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.PassWd") %>' TabIndex="2" Width="280px">
                        </asp:TextBox>
                    </td>
                </tr>
                <tr>
                    <td class="tdLabelBold">Role
                    </td>
                    <td class="tdValue">
                         <asp:DropDownList ID="drpRoles" runat="server" Width="280px" 
                            TabIndex="3" AppendDataBoundItems="True" DataValueField="RoleID" DataTextField="Role_Description">
                            <asp:ListItem Selected="True" Text="Select" Value="">
                            </asp:ListItem>
                        </asp:DropDownList>
                    </td>
                </tr>
                <tr>
                    <td class="tdLabelBold">Type Whs
                    </td>
                    <td class="tdValue">
                        <asp:DropDownList ID="drpTypeWhs" runat="server" TabIndex="4" AppendDataBoundItems="True" Width="280px">
                            <asp:ListItem Text="Select" Value=""/>
                            <asp:ListItem Text="TIENDA" Value="TIENDA"/>
                            <asp:ListItem Text="BODEGA" Value="BODEGA"/>
                            <asp:ListItem Text="BODTIE" Value="BODTIE"/>
                        </asp:DropDownList>
                    </td>
                </tr>
                <tr>
                    <td class="tdLabelBold">Number of Prints
                    </td>
                    <td class="tdValue">
                        <asp:TextBox ID="txtNumPrints" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.NumPrints") %>' TabIndex="5" Width="280px"/>
                    </td>
                </tr>
                <tr>
                    <td class="tdLabelBold">Active
                    </td>
                    <td class="tdValue">
                        <asp:CheckBox ID="chkActive" runat="server" TabIndex="6" />
                    </td>
                </tr>
                <tr>
                    <td class="tdLabelBold">Active PDT
                    </td>
                    <td class="tdValue">
                        <asp:CheckBox ID="chkActivePdt" runat="server" TabIndex="7" />
                    </td>
                </tr>
            </table>
        </td>
    </tr>
    <tr>
        <td colspan="2"></td>
    </tr>
    <tr>
        <td align="right" colspan="2">
            <asp:Button ID="btnUpdate" Text='<%# (Container is GridEditFormInsertItem) ? "Insert" : "Update" %>'
                runat="server" CommandName='<%# (Container is GridEditFormInsertItem) ? "PerformInsert" : "Update" %>'></asp:Button>&nbsp;
            <asp:Button ID="btnCancel" Text="Cancel" runat="server" CausesValidation="False" CommandName="Cancel"></asp:Button>
        </td>
    </tr>
</table>
