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
