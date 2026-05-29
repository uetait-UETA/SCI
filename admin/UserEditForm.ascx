<%@ Control Language="C#" AutoEventWireup="true" CodeFile="UserEditForm.ascx.cs" Inherits="UserEditForm"%>

<%@ Register TagPrefix="telerik" Namespace="Telerik.Web.UI" Assembly="Telerik.Web.UI" %>

<table id="Table2" width="100%" border="0" rules="none"
    style="border-collapse: collapse;">
    <tr class="EditFormHeader">
        <td colspan="2">
            <b>User Details</b>
        </td>
    </tr>
    <tr>
        <td>
            <table id="Table3" width="450px" border="0" class="module" cellspacing="4" cellpadding="4" >
                <tr>
                    <td class="title" style="font-weight: bold;" colspan="2">&nbsp;</td>
                </tr>
                <tr>
                    <td class="tdLabelBold">First Name
                    </td>
                    <td class="tdValue">
                        <asp:TextBox ID="txtFirstName" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.FirstName") %>' TabIndex="1">
                        </asp:TextBox>
                    </td>
                </tr>
                <tr>
                    <td class="tdLabelBold">Other Names
                    </td>
                    <td class="tdValue">
                        <asp:TextBox ID="txtOtherNames" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.OtherNames") %>'>' TabIndex="2">
                        </asp:TextBox>
                    </td>
                </tr>
                <tr>
                    <td class="tdLabelBold">Last Name 1
                    </td>
                    <td class="tdValue">
                        <asp:TextBox ID="txtLastName1" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.LastName1") %>'>' TabIndex="3">
                        </asp:TextBox>
                    </td>
                </tr>
                <tr>
                    <td class="tdLabelBold">Last Name 2
                    </td>
                    <td class="tdValue">
                        <asp:TextBox ID="txtLastName2" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.LastName2") %>'>' TabIndex="4">
                        </asp:TextBox>
                    </td>
                </tr>
                <tr>
                    <td class="tdLabelBold">Additional Info
                    </td>
                    <td class="tdValue">
                        <asp:TextBox ID="txtAdditionalInfo" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.AdditionalInfo") %>'>' TabIndex="5">
                        </asp:TextBox>
                    </td>
                </tr>

                <tr>
                    <td class="tdLabelBold">Active
                    </td>
                    <td class="tdValue" >
                        <asp:CheckBox ID="chkActive" runat="server" TabIndex="6" />
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
                                    <asp:Button ID="btnCancel" Text="Cancel" runat="server" CausesValidation="False"
                                        CommandName="Cancel"></asp:Button>
        </td>
    </tr>
</table>
