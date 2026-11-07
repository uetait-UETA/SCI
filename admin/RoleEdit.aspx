<%@ Page Title="Roles" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="RoleEdit.aspx.cs" Inherits="RoleEdit" %>

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

    <tel:RadAjaxManager ID="RadAjaxManager1" runat="server">
        <AjaxSettings>
            <tel:AjaxSetting AjaxControlID="ddlRoles">
                <UpdatedControls>
                    <tel:AjaxUpdatedControl ControlID="gridPermissions" LoadingPanelID="RadAjaxLoadingPanel1" />
                    <tel:AjaxUpdatedControl ControlID="lblPermissionsHeader" />
                </UpdatedControls>
            </tel:AjaxSetting>
        </AjaxSettings>
    </tel:RadAjaxManager>
    <tel:RadAjaxLoadingPanel ID="RadAjaxLoadingPanel1" runat="server"></tel:RadAjaxLoadingPanel>
    <tel:RadFormDecorator RenderMode="Lightweight" ID="RadFormDecorator1" runat="server" DecorationZoneID="demo" DecoratedControls="All" EnableRoundedCorners="false" />

    <label id="labelForm" runat="server" class="PanelHeading">Roles</label>
    <p style="color:#666; font-size:8pt; margin:6px 0 14px;">
        Manage the roles used throughout the system, and what each role can see/do on every
        screen. Double-click a role below to rename it or change its description. Access
        changes in the "Screen Access" section below take effect immediately.
    </p>

    <div id="demo" class="demo-container no-bg">
        <asp:Label ID="lblRolesMsg" runat="server" Style="display:block; margin-bottom:8px;" />
        <tel:RadGrid RenderMode="Lightweight" ID="gridRoles" runat="server" Width="60%" ShowStatusBar="true" AutoGenerateColumns="False"
            AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="True" PageSize="10" CssClass="Panel" ShowFooter="true"
            OnNeedDataSource="gridRoles_NeedDataSource" OnUpdateCommand="gridRoles_UpdateCommand" OnInsertCommand="gridRoles_InsertCommand"
            OnSelectedIndexChanged="gridRoles_SelectedIndexChanged"
            style="margin-left:auto; margin-right:auto;">
            <PagerStyle Mode="Slider"></PagerStyle>
            <SortingSettings EnableSkinSortStyles="false" />
            <MasterTableView Width="100%" AllowNaturalSort="false" CommandItemDisplay="Top" DataKeyNames="ROLEID" EditMode="PopUp">
                <Columns>
                    <tel:GridEditCommandColumn UniqueName="EditCommandColumn" />
                    <tel:GridButtonColumn ButtonType="LinkButton" Text="View Access" CommandName="Select" UniqueName="ViewAccess"
                        HeaderStyle-Width="110px" ItemStyle-Wrap="false" ItemStyle-Width="110px" />
                    <tel:GridBoundColumn SortExpression="ROLE_DESCRIPTION" HeaderText="Role" HeaderButtonType="TextButton" DataField="ROLE_DESCRIPTION" UniqueName="ROLE_DESCRIPTION" />
                    <tel:GridBoundColumn SortExpression="ROLE_NAME" HeaderText="Short Code" HeaderButtonType="TextButton" DataField="ROLE_NAME" UniqueName="ROLE_NAME" />
                    <tel:GridBoundColumn SortExpression="ROLEID" HeaderText="Role ID" HeaderButtonType="TextButton" DataField="ROLEID" UniqueName="ROLEID" ReadOnly="true" HeaderStyle-Width="70px" />
                </Columns>
                <EditFormSettings UserControlName="RoleEditForm.ascx" EditFormType="WebUserControl">
                    <EditColumn UniqueName="EditCommandColumn1"></EditColumn>
                </EditFormSettings>
            </MasterTableView>
            <ClientSettings>
                <ClientEvents OnRowDblClick="RowDblClick" OnPopUpShowing="onPopUpShowing" />
            </ClientSettings>
        </tel:RadGrid>
    </div>

    <br />
    <label class="PanelHeading" id="lblPermissionsHeader" runat="server">Screen Access</label>
    <p style="color:#666; font-size:8pt; margin:6px 0 14px;">
        Pick a role (or click "View Access" on a row above) to see and change what it can do
        on every screen: <b>Full Access</b> (can use everything), <b>Read-Only</b> (can view
        but not change anything), or <b>No Access</b> (the screen is hidden/blocked for this
        role). Changing the dropdown and clicking Update saves that one screen immediately.
    </p>
    <div style="margin-bottom:10px; margin-left:20px;">
        <label class="myLabelMedium" style="width:auto;">Role:&nbsp;</label>
        <asp:DropDownList ID="ddlRoles" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlRoles_SelectedIndexChanged"
            DataTextField="ROLE_DESCRIPTION" DataValueField="ROLEID" Width="280px" />
    </div>

    <asp:Label ID="lblPermissionsMsg" runat="server" Style="display:block; margin-bottom:8px;" />
    <tel:RadGrid RenderMode="Lightweight" ID="gridPermissions" runat="server" Width="70%" ShowStatusBar="true" AutoGenerateColumns="False"
        AllowSorting="true" AllowPaging="True" PageSize="20" CssClass="Panel" ShowFooter="true"
        OnNeedDataSource="gridPermissions_NeedDataSource" OnUpdateCommand="gridPermissions_UpdateCommand"
        OnItemDataBound="gridPermissions_ItemDataBound"
        style="margin-left:auto; margin-right:auto;">
        <PagerStyle Mode="Slider"></PagerStyle>
        <SortingSettings EnableSkinSortStyles="false" />
        <MasterTableView Width="100%" AllowNaturalSort="false" CommandItemDisplay="None" DataKeyNames="ControlName" EditMode="InPlace">
            <Columns>
                <tel:GridEditCommandColumn UniqueName="EditCommandColumn" ButtonType="LinkButton" />
                <tel:GridTemplateColumn HeaderText="Menu" UniqueName="Menu" HeaderStyle-Width="130px">
                    <ItemTemplate><%# MenuGroupFor(Eval("ControlName").ToString()) %></ItemTemplate>
                </tel:GridTemplateColumn>
                <tel:GridBoundColumn SortExpression="ControlName" HeaderText="Screen" HeaderButtonType="TextButton" DataField="ControlName" UniqueName="ControlName" ReadOnly="true" HeaderStyle-Width="220px" />
                <tel:GridBoundColumn SortExpression="ControlDesc" HeaderText="Description" HeaderButtonType="TextButton" DataField="ControlDesc" UniqueName="ControlDesc" ReadOnly="true" />
                <tel:GridTemplateColumn HeaderText="Full Access" UniqueName="AccessFull" HeaderStyle-Width="90px" ItemStyle-HorizontalAlign="Center">
                    <ItemTemplate><%# Eval("AccessType").ToString() == "F" ? "<span style=\"color:#000;font-weight:bold;font-size:14pt;\">&#10003;</span>" : "" %></ItemTemplate>
                    <EditItemTemplate>
                        <asp:RadioButton ID="rbFull" runat="server" GroupName="AccessType" />
                    </EditItemTemplate>
                </tel:GridTemplateColumn>
                <tel:GridTemplateColumn HeaderText="Read-Only" UniqueName="AccessReadOnly" HeaderStyle-Width="90px" ItemStyle-HorizontalAlign="Center">
                    <ItemTemplate><%# Eval("AccessType").ToString() == "R" ? "<span style=\"color:#000;font-weight:bold;font-size:14pt;\">&#10003;</span>" : "" %></ItemTemplate>
                    <EditItemTemplate>
                        <asp:RadioButton ID="rbReadOnly" runat="server" GroupName="AccessType" />
                    </EditItemTemplate>
                </tel:GridTemplateColumn>
                <tel:GridTemplateColumn HeaderText="No Access" UniqueName="AccessNone" HeaderStyle-Width="90px" ItemStyle-HorizontalAlign="Center">
                    <ItemTemplate><%# (Eval("AccessType").ToString() != "F" && Eval("AccessType").ToString() != "R") ? "<span style=\"color:#000;font-weight:bold;font-size:14pt;\">&#10003;</span>" : "" %></ItemTemplate>
                    <EditItemTemplate>
                        <asp:RadioButton ID="rbNoAccess" runat="server" GroupName="AccessType" />
                    </EditItemTemplate>
                </tel:GridTemplateColumn>
            </Columns>
        </MasterTableView>
    </tel:RadGrid>

    <br /><br />
    <label class="PanelHeading">Add Screen</label>
    <p style="color:#666; font-size:8pt; margin:6px 0 10px;">
        Registers an existing, already-built screen so it shows up in "Screen Access" above and
        can be granted to roles. This does <b>not</b> create the screen itself — that still
        needs to be built first; use this once it's ready. Not something you'll need often.
    </p>
    <div style="margin-bottom:18px;">
        <asp:Label ID="lblAddScreenMsg" runat="server" Style="display:block; margin-bottom:8px;" />
        <label class="myLabelMedium" style="width:auto;">Screen (.aspx file name):&nbsp;</label>
        <asp:TextBox ID="txtNewControlName" runat="server" Width="220px" MaxLength="250" placeholder="e.g. NewScreen.aspx" />
        &nbsp;&nbsp;
        <label class="myLabelMedium" style="width:auto;">Description:&nbsp;</label>
        <asp:TextBox ID="txtNewControlDesc" runat="server" Width="300px" MaxLength="250" />
        &nbsp;&nbsp;
        <asp:Button ID="btnAddScreen" runat="server" Text="Add Screen" OnClick="btnAddScreen_Click" CausesValidation="false" />
    </div>

</asp:Content>
