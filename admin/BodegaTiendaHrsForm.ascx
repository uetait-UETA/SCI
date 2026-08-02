<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BodegaTiendaHrsForm.ascx.cs" Inherits="BodegaTiendaHrsForm"%>

<%@ Register TagPrefix="telerik" Namespace="Telerik.Web.UI" Assembly="Telerik.Web.UI" %>

<table id="Table2" width="100%" border="0" rules="none"
    style="border-collapse: collapse;">
    <tr class="EditFormHeader">
        <td colspan="2">
            <b>Schedule Details</b>
        </td>
    </tr>
    <tr>
        <td>
            <table id="Table3" width="600px" border="0" class="module" cellspacing="4" cellpadding="4">
                <tr>
                    <td class="title" style="font-weight: bold;" colspan="2">&nbsp;</td>
                </tr>
                <tr>
                    <td class="tdLabelBold">Bodega &#8594; Tienda
                    </td>
                    <td class="tdValue">
                        <asp:DropDownList ID="drpPair" runat="server" Width="380px"
                            TabIndex="0" AppendDataBoundItems="True" DataValueField="PairKey" DataTextField="DisplayName">
                            <asp:ListItem Selected="True" Text="Select" Value="">
                            </asp:ListItem>
                        </asp:DropDownList>
                        <br />
                        <small style="color:gray;">Only pairs already active in "Pares Bodega -&gt; Tienda" show up here.</small>
                    </td>
                </tr>
                <tr>
                    <td class="tdLabelBold" style="vertical-align:top;">Days
                    </td>
                    <td class="tdValue">
                        <asp:CheckBox ID="chkAllDays" runat="server" Text="All days" AutoPostBack="false" /><br />
                        <asp:CheckBox ID="chkMon" runat="server" Text="Monday" />
                        <asp:CheckBox ID="chkTue" runat="server" Text="Tuesday" />
                        <asp:CheckBox ID="chkWed" runat="server" Text="Wednesday" /><br />
                        <asp:CheckBox ID="chkThu" runat="server" Text="Thursday" />
                        <asp:CheckBox ID="chkFri" runat="server" Text="Friday" /><br />
                        <asp:CheckBox ID="chkSat" runat="server" Text="Saturday" />
                        <asp:CheckBox ID="chkSun" runat="server" Text="Sunday" />
                        <br />
                        <small style="color:gray;">Check "All days", or check individual days. If none are checked, it defaults to "All days".</small>
                    </td>
                </tr>
                <tr>
                    <td class="tdLabelBold">From
                    </td>
                    <td class="tdValue">
                        <telerik:RadTimePicker ID="timeFrom" runat="server" Width="140px">
                            <TimeView Columns="1" />
                            <DateInput DateFormat="HH:mm" />
                        </telerik:RadTimePicker>
                    </td>
                </tr>
                <tr>
                    <td class="tdLabelBold">To
                    </td>
                    <td class="tdValue">
                        <telerik:RadTimePicker ID="timeTo" runat="server" Width="140px">
                            <TimeView Columns="1" />
                            <DateInput DateFormat="HH:mm" />
                        </telerik:RadTimePicker>
                    </td>
                </tr>
                <tr>
                    <td class="tdLabelBold">Active
                    </td>
                    <td class="tdValue">
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
            <asp:Button ID="btnCancel" Text="Cancel" runat="server" CausesValidation="False" CommandName="Cancel"></asp:Button>
        </td>
    </tr>
</table>
