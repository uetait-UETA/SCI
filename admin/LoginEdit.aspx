<%@ Page Title="SMM Logins" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="LoginEdit.aspx.cs" Inherits="LoginEdit" %>

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

        <label  ID="labelForm" runat="server" class="PanelHeading">Credentials</label>
        
        
        <br />


    <tel:RadAjaxLoadingPanel ID="RadAjaxLoadingPanel1" runat="server"></tel:RadAjaxLoadingPanel>
    <tel:RadFormDecorator RenderMode="Lightweight" ID="RadFormDecorator1" runat="server" DecorationZoneID="demo" DecoratedControls="All" EnableRoundedCorners="false" />
    

        <%-- ══════════════════════════════════════════════════════════════════
             BLOQUE DE BUSQUEDA
             Va FUERA del div "demo" a proposito: esa es la zona del
             RadFormDecorator, y si estos controles cayeran dentro, el
             decorador los reestilizaria.
             Estilos en linea porque Bootstrap no se resuelve desde /admin/.
             No se usa el filtrado nativo de Telerik: el filtro se aplica al
             DataTable en gridLogins_NeedDataSource, de modo que la rejilla
             conserva su configuracion y su aspecto original.
             ══════════════════════════════════════════════════════════════════ --%>
        <asp:Panel ID="pnlBuscar" runat="server" DefaultButton="btnBuscar"
            style="width:80%; margin:0 auto 8px auto; border:1px solid #c8ccd4; background-color:#f4f6f9; padding:8px 10px;">
            <%-- Los siete filtros en una sola fila, igual que en Users.
                 Los anchos estan ajustados para que quepan sin desbordar
                 el 80% del panel, incluso en pantallas de 1366 px. --%>
            <table style="border-collapse:collapse;" cellpadding="0" cellspacing="0">
                <tr>
                    <td style="padding:0 6px 0 0;">
                        <span style="font:11px Arial,sans-serif; color:#555;">Login ID</span><br />
                        <asp:TextBox ID="txtFLoginID" runat="server" Width="85px" autocomplete="off" />
                    </td>
                    <td style="padding:0 6px;">
                        <span style="font:11px Arial,sans-serif; color:#555;">User</span><br />
                        <asp:TextBox ID="txtFUserFullName" runat="server" Width="125px" autocomplete="off" />
                    </td>
                    <td style="padding:0 6px;">
                        <span style="font:11px Arial,sans-serif; color:#555;">Role</span><br />
                        <asp:DropDownList ID="ddlFRole" runat="server" Width="185px" />
                    </td>
                    <td style="padding:0 6px;">
                        <span style="font:11px Arial,sans-serif; color:#555;">WHS Type</span><br />
                        <asp:DropDownList ID="ddlFTypeWhs" runat="server" Width="90px" />
                    </td>
                    <td style="padding:0 6px;">
                        <span style="font:11px Arial,sans-serif; color:#555;">NumPrints</span><br />
                        <asp:TextBox ID="txtFNumPrints" runat="server" Width="55px" autocomplete="off" />
                    </td>
                    <td style="padding:0 6px;">
                        <span style="font:11px Arial,sans-serif; color:#555;">Active</span><br />
                        <asp:DropDownList ID="ddlFActive" runat="server" Width="70px">
                            <asp:ListItem Text="(Todos)" Value="" />
                            <asp:ListItem Text="Si" Value="true" />
                            <asp:ListItem Text="No" Value="false" />
                        </asp:DropDownList>
                    </td>
                    <td style="padding:0 6px;">
                        <span style="font:11px Arial,sans-serif; color:#555;">Active PDT</span><br />
                        <asp:DropDownList ID="ddlFActivePdt" runat="server" Width="70px">
                            <asp:ListItem Text="(Todos)" Value="" />
                            <asp:ListItem Text="Si" Value="true" />
                            <asp:ListItem Text="No" Value="false" />
                        </asp:DropDownList>
                    </td>
                    <td style="padding:0 0 0 10px; vertical-align:bottom;">
                        <asp:Button ID="btnBuscar" runat="server" Text="Buscar" OnClick="btnBuscar_Click" Width="65px" />
                        <asp:Button ID="btnLimpiar" runat="server" Text="Limpiar" OnClick="btnLimpiar_Click" Width="65px" />
                    </td>
                    <td style="padding:0 0 0 10px; vertical-align:bottom;">
                        <asp:Label ID="lblResultado" runat="server" style="font:11px Arial,sans-serif; color:#1F3864;" />
                    </td>
                </tr>
            </table>
        </asp:Panel>

        <div id="demo" class="demo-container no-bg">

            <tel:RadGrid RenderMode="Lightweight" ID="gridLogins" runat="server" Width="80%" ShowStatusBar="true" AutoGenerateColumns="False"
            AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="True" PageSize="15" CssClass="Panel" ShowFooter="true"
            OnNeedDataSource="gridLogins_NeedDataSource" 
            OnUpdateCommand="gridLogins_UpdateCommand" 
            OnInsertCommand="gridLogins_InsertCommand" 
            style="margin-left:auto; margin-right:auto;">
            <PagerStyle Mode="Slider"></PagerStyle>
            <SortingSettings EnableSkinSortStyles="false" />
            <MasterTableView Width="100%" AllowNaturalSort="false" CommandItemDisplay="Top" DataKeyNames="LoginID" EditMode="PopUp">

                <Columns>
                    <tel:GridEditCommandColumn  UniqueName="EditCommandColumn"/>
                    <tel:GridBoundColumn SortExpression="LoginID" HeaderText="Login ID" HeaderButtonType="TextButton" DataField="LoginID" UniqueName="LoginID" />
                    <tel:GridBoundColumn SortExpression="UserFullName" HeaderText="User" HeaderButtonType="TextButton" DataField="UserFullName" UniqueName="UserFullName" />
                    <tel:GridBoundColumn SortExpression="Role" HeaderText="Role" HeaderButtonType="TextButton" DataField="Role" UniqueName="Role" />
                    <tel:GridBoundColumn SortExpression="TypeWhs" HeaderText="WHS Type" HeaderButtonType="TextButton" DataField="TypeWhs" UniqueName="TypeWhs" />
                    <tel:GridBoundColumn SortExpression="NumPrints" HeaderText="NumPrints" HeaderButtonType="TextButton" DataField="NumPrints" UniqueName="NumPrints" />
                    <tel:GridCheckBoxColumn SortExpression="Active" DataField="Active" HeaderText="Active" />
                    <tel:GridCheckBoxColumn SortExpression="Active_Pdt" DataField="Active_Pdt" HeaderText="Active PDT" />
                </Columns>

                <EditFormSettings UserControlName="LoginEditForm.ascx" EditFormType="WebUserControl">
                    <EditColumn UniqueName="EditCommandColumn1"></EditColumn>
                </EditFormSettings>

            </MasterTableView>
             <ClientSettings>
                <ClientEvents OnRowDblClick="RowDblClick" OnPopUpShowing="onPopUpShowing" />
            </ClientSettings>
            </tel:RadGrid>
        </div>

</asp:Content>
