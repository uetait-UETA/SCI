<%@ Page Title="SMM Users" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="UserEdit.aspx.cs" Inherits="UserEdit" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cphMain" runat="Server">

<link href="Default.css" rel="stylesheet" type="text/css" />

     <tel:RadCodeBlock ID="RadCodeBlock1" runat="server">
        <script type="text/javascript">
            function RowDblClick(sender, eventArgs) {
                sender.get_masterTableView().editItem(eventArgs.get_itemIndexHierarchical());
            }

            function onPopUpShowing(sender, args) {
                args.get_popUp().className += " popUpEditForm";
            }
        </script>
    </tel:RadCodeBlock>

    <tel:RadAjaxManager ID="RadAjaxManager1" runat="server">
        <AjaxSettings>
            <tel:AjaxSetting AjaxControlID="txtRejectedMessage">
                <UpdatedControls>
                    <tel:AjaxUpdatedControl ControlID="txtRejectedMessage" LoadingPanelID="RadAjaxLoadingPanel1"></tel:AjaxUpdatedControl>
                </UpdatedControls>
            </tel:AjaxSetting>
            <tel:AjaxSetting AjaxControlID="ConfiguratorPanel">
                <UpdatedControls>
                    <tel:AjaxUpdatedControl ControlID="txtRejectedMessage" LoadingPanelID="RadAjaxLoadingPanel1"></tel:AjaxUpdatedControl>
                </UpdatedControls>
            </tel:AjaxSetting>
        </AjaxSettings>
    </tel:RadAjaxManager>

    <tel:RadAjaxLoadingPanel ID="RadAjaxLoadingPanel1" runat="server"></tel:RadAjaxLoadingPanel>
    
    <tel:RadFormDecorator RenderMode="Lightweight" ID="RadFormDecorator1" runat="server" DecorationZoneID="demo1" DecoratedControls="All" EnableRoundedCorners="false" />

    <label id="labelForm" runat="server" class="PanelHeading">Users</label>
    <br />

    <%-- ══════════════════════════════════════════════════════════════════
         BLOQUE DE BUSQUEDA
         Va FUERA del div "demo1" a proposito: esa es la zona del
         RadFormDecorator, y si estos controles cayeran dentro, el
         decorador los reestilizaria.
         Estilos en linea porque Bootstrap no se resuelve desde /admin/.
         No se usa el filtrado nativo de Telerik: el filtro se aplica al
         DataTable en gridUsers_NeedDataSource, de modo que la rejilla
         conserva su configuracion y su aspecto original.
         ══════════════════════════════════════════════════════════════════ --%>
    <asp:Panel ID="pnlBuscar" runat="server" DefaultButton="btnBuscar"
        style="width:80%; margin:0 auto 8px auto; border:1px solid #c8ccd4; background-color:#f4f6f9; padding:8px 10px;">
        <table style="border-collapse:collapse;" cellpadding="0" cellspacing="0">
            <tr>
                <td style="padding:0 8px 0 0;">
                    <span style="font:11px Arial,sans-serif; color:#555;">First Name</span><br />
                    <asp:TextBox ID="txtFFirstName" runat="server" Width="110px" />
                </td>
                <td style="padding:0 8px;">
                    <span style="font:11px Arial,sans-serif; color:#555;">Other Names</span><br />
                    <asp:TextBox ID="txtFOtherNames" runat="server" Width="110px" />
                </td>
                <td style="padding:0 8px;">
                    <span style="font:11px Arial,sans-serif; color:#555;">Last Name 1</span><br />
                    <asp:TextBox ID="txtFLastName1" runat="server" Width="110px" />
                </td>
                <td style="padding:0 8px;">
                    <span style="font:11px Arial,sans-serif; color:#555;">Last Name 2</span><br />
                    <asp:TextBox ID="txtFLastName2" runat="server" Width="110px" />
                </td>
                <td style="padding:0 8px;">
                    <span style="font:11px Arial,sans-serif; color:#555;">Additional Info</span><br />
                    <asp:TextBox ID="txtFAdditionalInfo" runat="server" Width="110px" />
                </td>
                <td style="padding:0 8px;">
                    <span style="font:11px Arial,sans-serif; color:#555;">Active</span><br />
                    <asp:DropDownList ID="ddlFActive" runat="server" Width="80px">
                        <asp:ListItem Text="(Todos)" Value="" />
                        <asp:ListItem Text="Si" Value="true" />
                        <asp:ListItem Text="No" Value="false" />
                    </asp:DropDownList>
                </td>
                <td style="padding:0 0 0 12px; vertical-align:bottom;">
                    <asp:Button ID="btnBuscar" runat="server" Text="Buscar" OnClick="btnBuscar_Click" Width="70px" />
                    <asp:Button ID="btnLimpiar" runat="server" Text="Limpiar" OnClick="btnLimpiar_Click" Width="70px" />
                </td>
                <td style="padding:0 0 0 12px; vertical-align:bottom;">
                    <asp:Label ID="lblResultado" runat="server" style="font:11px Arial,sans-serif; color:#1F3864;" />
                </td>
            </tr>
        </table>
    </asp:Panel>

    <div id="demo1">
        <tel:RadGrid RenderMode="Lightweight" ID="gridUsers" runat="server" Width="80%" ShowStatusBar="true" AutoGenerateColumns="False" 
            AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="True" CssClass="Panel" PageSize="15" ShowFooter="true" 
            OnNeedDataSource="gridUsers_NeedDataSource" OnUpdateCommand="gridUsers_UpdateCommand" OnInsertCommand="gridUsers_InsertCommand" 
            Style="margin-left:auto;margin-right:auto;">
            <MasterTableView Width="100%" AllowNaturalSort="false" CommandItemDisplay="Top" DataKeyNames="UserId" EditMode="PopUp">
                <Columns>
                    <tel:GridEditCommandColumn UniqueName="EditCommandColumn" />
                    <tel:GridBoundColumn SortExpression="FirstName" HeaderText="First Name" HeaderButtonType="TextButton" DataField="FirstName" UniqueName="FirstName" />
                    <tel:GridBoundColumn SortExpression="OtherNames" HeaderText="Other Names" HeaderButtonType="TextButton" DataField="OtherNames" UniqueName="OtherNames" />
                    <tel:GridBoundColumn SortExpression="LastName1" HeaderText="Last Name 1" HeaderButtonType="TextButton" DataField="LastName1" UniqueName="LastName1" />
                    <tel:GridBoundColumn SortExpression="LastName2" HeaderText="Last Name 2" HeaderButtonType="TextButton" DataField="LastName2" UniqueName="LastName2" />
                    <tel:GridBoundColumn SortExpression="AdditionalInfo" HeaderText="Additional Info" HeaderButtonType="TextButton" DataField="AdditionalInfo" UniqueName="AdditionalInfo" />
                    <tel:GridCheckBoxColumn SortExpression="Active" DataField="Active" HeaderText="Active" />
                </Columns>
                <EditFormSettings UserControlName="UserEditForm.ascx" EditFormType="WebUserControl">
                    <EditColumn UniqueName="EditCommandColumn1"></EditColumn>
                </EditFormSettings>
            </MasterTableView>
            <PagerStyle Mode="Slider"></PagerStyle>
            <SortingSettings EnableSkinSortStyles="false" />
            <ClientSettings>
                <ClientEvents OnRowDblClick="RowDblClick" OnPopUpShowing="onPopUpShowing" />
            </ClientSettings>
        </tel:RadGrid>
    </div>
</asp:Content>
