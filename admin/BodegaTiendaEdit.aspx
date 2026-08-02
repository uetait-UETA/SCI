<%@ Page Title="Bodega-Tienda" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="BodegaTiendaEdit.aspx.cs" Inherits="BodegaTiendaEdit" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cphMain" runat="Server">

<link href="../Default.css" rel="stylesheet" type="text/css" />

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

    <label ID="labelForm" runat="server" class="PanelHeading">Bodega-Tienda</label>
    <br />
    <br />

    <tel:RadAjaxLoadingPanel ID="RadAjaxLoadingPanel1" runat="server"></tel:RadAjaxLoadingPanel>
    <tel:RadFormDecorator RenderMode="Lightweight" ID="RadFormDecorator1" runat="server" DecorationZoneID="demo" DecoratedControls="All" EnableRoundedCorners="false" />

    <div id="demo" class="demo-container no-bg">

        <label class="PanelHeading" style="font-size:14px;">Pares Bodega &#8594; Tienda</label>
        <br /><br />
        <tel:RadGrid RenderMode="Lightweight" ID="gridPairs" runat="server" Width="90%" ShowStatusBar="true" AutoGenerateColumns="False"
            AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="True" PageSize="15" CssClass="Panel" ShowFooter="true"
            OnNeedDataSource="gridPairs_NeedDataSource"
            OnUpdateCommand="gridPairs_UpdateCommand"
            OnInsertCommand="gridPairs_InsertCommand"
            OnDeleteCommand="gridPairs_DeleteCommand"
            style="margin-left:auto; margin-right:auto;">
            <PagerStyle Mode="Slider"></PagerStyle>
            <SortingSettings EnableSkinSortStyles="false" />
            <MasterTableView Width="100%" AllowNaturalSort="false" CommandItemDisplay="Top" DataKeyNames="BodegaID,TiendaID" EditMode="PopUp">

                <Columns>
                    <tel:GridEditCommandColumn UniqueName="EditCommandColumn" />
                    <tel:GridButtonColumn UniqueName="DeleteCommandColumn" CommandName="Delete" Text="Deactivate" ConfirmText="Deactivate this pair? (It will stop running, but the row is kept — not deleted.)" />
                    <tel:GridBoundColumn SortExpression="BodegaID" HeaderText="Bodega Code" HeaderButtonType="TextButton" DataField="BodegaID" UniqueName="BodegaID" />
                    <tel:GridBoundColumn SortExpression="BodegaName" HeaderText="Bodega Name" HeaderButtonType="TextButton" DataField="BodegaName" UniqueName="BodegaName" />
                    <tel:GridBoundColumn SortExpression="TiendaID" HeaderText="Tienda Code" HeaderButtonType="TextButton" DataField="TiendaID" UniqueName="TiendaID" />
                    <tel:GridBoundColumn SortExpression="TiendaName" HeaderText="Tienda Name" HeaderButtonType="TextButton" DataField="TiendaName" UniqueName="TiendaName" />
                    <tel:GridBoundColumn SortExpression="Priority" HeaderText="Priority" HeaderButtonType="TextButton" DataField="Priority" UniqueName="Priority" />
                    <tel:GridCheckBoxColumn SortExpression="isActive" DataField="isActive" HeaderText="Active" />
                </Columns>

                <EditFormSettings UserControlName="BodegaTiendaPairForm.ascx" EditFormType="WebUserControl">
                    <PopUpSettings Height="320" Width="800" ScrollBars="Auto" />
                    <EditColumn UniqueName="EditCommandColumn1"></EditColumn>
                </EditFormSettings>

            </MasterTableView>
            <ClientSettings>
                <ClientEvents OnRowDblClick="RowDblClick" OnPopUpShowing="onPopUpShowing" />
            </ClientSettings>
        </tel:RadGrid>

        <br /><br />

        <label class="PanelHeading" style="font-size:14px;">Calendario (d&#237;as / horas por par)</label>
        <br /><br />
        <tel:RadGrid RenderMode="Lightweight" ID="gridSchedule" runat="server" Width="90%" ShowStatusBar="true" AutoGenerateColumns="False"
            AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="True" PageSize="15" CssClass="Panel" ShowFooter="true"
            OnNeedDataSource="gridSchedule_NeedDataSource"
            OnUpdateCommand="gridSchedule_UpdateCommand"
            OnInsertCommand="gridSchedule_InsertCommand"
            OnDeleteCommand="gridSchedule_DeleteCommand"
            style="margin-left:auto; margin-right:auto;">
            <PagerStyle Mode="Slider"></PagerStyle>
            <SortingSettings EnableSkinSortStyles="false" />
            <MasterTableView Width="100%" AllowNaturalSort="false" CommandItemDisplay="Top" DataKeyNames="BodegaID,TiendaID" EditMode="PopUp">

                <Columns>
                    <tel:GridEditCommandColumn UniqueName="EditCommandColumn" />
                    <tel:GridButtonColumn UniqueName="DeleteCommandColumn" CommandName="Delete" Text="Remove schedule" ConfirmText="Remove this schedule row? The pair will then run every day / any hour (default)." />
                    <tel:GridBoundColumn SortExpression="BodegaName" HeaderText="Bodega" HeaderButtonType="TextButton" DataField="BodegaName" UniqueName="BodegaName" />
                    <tel:GridBoundColumn SortExpression="TiendaName" HeaderText="Tienda" HeaderButtonType="TextButton" DataField="TiendaName" UniqueName="TiendaName" />
                    <tel:GridBoundColumn SortExpression="Days" HeaderText="Days" HeaderButtonType="TextButton" DataField="Days" UniqueName="Days" />
                    <tel:GridBoundColumn SortExpression="FromHrs" HeaderText="From" HeaderButtonType="TextButton" DataField="FromHrs" UniqueName="FromHrs" />
                    <tel:GridBoundColumn SortExpression="ToHrs" HeaderText="To" HeaderButtonType="TextButton" DataField="ToHrs" UniqueName="ToHrs" />
                    <tel:GridCheckBoxColumn SortExpression="isActive" DataField="isActive" HeaderText="Active" />
                </Columns>

                <EditFormSettings UserControlName="BodegaTiendaHrsForm.ascx" EditFormType="WebUserControl">
                    <PopUpSettings Height="480" Width="660" ScrollBars="Auto" />
                    <EditColumn UniqueName="EditCommandColumn1"></EditColumn>
                </EditFormSettings>

            </MasterTableView>
            <ClientSettings>
                <ClientEvents OnRowDblClick="RowDblClick" OnPopUpShowing="onPopUpShowing" />
            </ClientSettings>
        </tel:RadGrid>

    </div>

</asp:Content>
