<%@ Control Language="C#" AutoEventWireup="true" CodeFile="RoleEditForm.ascx.cs" Inherits="RoleEditForm"%>

<%@ Register TagPrefix="telerik" Namespace="Telerik.Web.UI" Assembly="Telerik.Web.UI" %>

<table id="Table2" width="100%" border="0" rules="none" style="border-collapse: collapse;">
    <tr class="EditFormHeader">
        <td colspan="2">
            <b>Role Details</b>
        </td>
    </tr>
    <tr>
        <td>
            <table id="Table3" width="450px" border="0" class="module" cellspacing="4" cellpadding="4">
                <tr>
                    <td class="title" style="font-weight: bold;" colspan="2">&nbsp;</td>
                </tr>
                <tr>
                    <td class="tdLabelBold">Role ID
                    </td>
                    <td class="tdValue">
                        <asp:Label ID="lblRoleId" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.ROLEID") %>' />
                        <span style="color:#888; font-size:11px;">(assigned automatically for a new role)</span>
                    </td>
                </tr>
                <tr>
                    <td class="tdLabelBold">Role Name (short code)
                    </td>
                    <td class="tdValue">
                        <asp:TextBox ID="txtRoleName" runat="server" MaxLength="100" Width="300px"
                            Text='<%# DataBinder.Eval(Container, "DataItem.ROLE_NAME") %>' TabIndex="1" />
                    </td>
                </tr>
                <tr>
                    <td class="tdLabelBold">Description
                    </td>
                    <td class="tdValue">
                        <asp:TextBox ID="txtRoleDescription" runat="server" MaxLength="150" Width="300px"
                            Text='<%# DataBinder.Eval(Container, "DataItem.ROLE_DESCRIPTION") %>' TabIndex="2" />
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
            <asp:Button ID="btnUpdate" Text='<%# (Container is GridEditFormInsertItem) ? "Add Role" : "Save" %>'
                runat="server" CommandName='<%# (Container is GridEditFormInsertItem) ? "PerformInsert" : "Update" %>'></asp:Button>&nbsp;
            <asp:Button ID="btnCancel" Text="Cancel" runat="server" CausesValidation="False" CommandName="Cancel"></asp:Button>
        </td>
    </tr>
</table>
