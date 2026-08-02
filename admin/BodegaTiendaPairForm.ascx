<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BodegaTiendaPairForm.ascx.cs" Inherits="BodegaTiendaPairForm"%>

<%@ Register TagPrefix="telerik" Namespace="Telerik.Web.UI" Assembly="Telerik.Web.UI" %>

<table id="Table2" width="100%" border="0" rules="none"
    style="border-collapse: collapse;">
    <tr class="EditFormHeader">
        <td colspan="2">
            <b>Bodega-Tienda Pair Details</b>
        </td>
    </tr>
    <tr>
        <td>
            <table id="Table3" width="720px" border="0" class="module" cellspacing="4" cellpadding="6">
                <tr>
                    <td class="tdLabelBold" style="text-align:right; width:160px;">Bodega (source)
                    </td>
                    <td class="tdValue">
                        <asp:DropDownList ID="drpBodega" runat="server" Width="460px"
                            TabIndex="0" AppendDataBoundItems="True" DataValueField="WhsCode" DataTextField="DisplayName">
                            <asp:ListItem Selected="True" Text="Select" Value="">
                            </asp:ListItem>
                        </asp:DropDownList>
                    </td>
                </tr>
                <tr>
                    <td class="tdLabelBold" style="text-align:right; width:160px;">Tienda (destination)
                    </td>
                    <td class="tdValue">
                        <asp:DropDownList ID="drpTienda" runat="server" Width="460px"
                            TabIndex="1" AppendDataBoundItems="True" DataValueField="WhsCode" DataTextField="DisplayName">
                            <asp:ListItem Selected="True" Text="Select" Value="">
                            </asp:ListItem>
                        </asp:DropDownList>
                    </td>
                </tr>
                <tr>
                    <td class="tdLabelBold" style="text-align:right; width:160px;">Priority
                    </td>
                    <td class="tdValue">
                        <asp:TextBox ID="txtPriority" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.Priority") %>' TabIndex="2" Width="80px">
                        </asp:TextBox>
                    </td>
                </tr>
                <tr>
                    <td class="tdLabelBold" style="text-align:right; width:160px;">Active
                    </td>
                    <td class="tdValue">
                        <asp:CheckBox ID="chkActive" runat="server" TabIndex="3" />
                    </td>
                </tr>
            </table>
        </td>
    </tr>
    <tr>
        <td colspan="2" style="padding: 10px 0 8px 180px;">
            <asp:Button ID="btnUpdate" Text='<%# (Container is GridEditFormInsertItem) ? "Insert" : "Update" %>'
                runat="server" CommandName='<%# (Container is GridEditFormInsertItem) ? "PerformInsert" : "Update" %>'></asp:Button>&nbsp;
            <asp:Button ID="btnCancel" Text="Cancel" runat="server" CausesValidation="False" CommandName="Cancel"></asp:Button>
        </td>
    </tr>
</table>
