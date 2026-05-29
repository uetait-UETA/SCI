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
