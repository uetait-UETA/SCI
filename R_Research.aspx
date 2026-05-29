<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="R_Research.aspx.cs" Inherits="R_Research" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cphMain" runat="Server">

    <style>
        .myLabelMedium, .myLabelSmall {
            padding-left: 5px;
        }

        .myImageButton {
            vertical-align: middle;
            height: 20px;
            width: 20px;
            margin-left: 5px;
        }
    </style>
    <asp:HiddenField ID="hfFromDate" runat="server" />
    <asp:HiddenField ID="hfToDate" runat="server" />
    <asp:HiddenField ID="hfScreenWidth" runat="server" />
    <asp:HiddenField ID="hfScreenHeight" runat="server" />

    <div class="container-fluid">
        <div class="row">
            <div class="col-md-12">
                <div id="divMessage" runat="server" class="alert-danger" />
                <br />
            </div>
        </div>
        <div class="row">
            <div class="col-md-12">
                <asp:Panel ID="pnlHeader" runat="server" CssClass="Panel">
                    <label class="PanelHeading">Research (<asp:Label ID="CompanyLabel" runat="server" Text="Company"></asp:Label>)</label>
                    <ul class="myUL">
                        <li style="display:inline;">
                            <label class="myLabelSmall">Item</label>
                            <tel:RadTextBox runat="server" ID="rtbItem" />
                            <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="mybtn" OnClick="btnSearch_Click" />
                        </li>
                    </ul>
                </asp:Panel>
            </div>
        </div>
        <div class="row">&nbsp;</div>
        <div class="row">&nbsp;</div>
        <div class="row">
            <div class="col-md-12">
                <tel:RadGrid ID="rgHead" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False"
                    AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="True" PageSize="15" CssClass="Panel"
                    OnNeedDataSource="rgHead_NeedDataSource" OnItemCommand="rgHead_ItemCommand">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <ExportSettings OpenInNewWindow="true" IgnorePaging="true" Excel-Format="Xlsx" FileName="Research" UseItemStyles="true" />
                    <MasterTableView Width="100%" AllowNaturalSort="false" CommandItemDisplay="Top">
                        <CommandItemSettings ShowExportToExcelButton="true" ShowRefreshButton="true" ShowAddNewRecordButton="false" />
                        <Columns>
                            <tel:GridBoundColumn SortExpression="DocDate" HeaderText="Date" HeaderButtonType="TextButton" DataField="DocDate" UniqueName="DocDate" DataFormatString="{0:d}" HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="DocNum" HeaderText="DocNum" HeaderButtonType="TextButton" DataField="DocNum" UniqueName="DocNum" HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="Transfer" HeaderText="# Transfer" HeaderButtonType="TextButton" DataField="Transfer" UniqueName="Transfer" HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="From" HeaderText="From" HeaderButtonType="TextButton" DataField="From" UniqueName="From" HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="To" HeaderText="To" HeaderButtonType="TextButton" DataField="To" UniqueName="To" HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="ItemCode" HeaderText="Item" HeaderButtonType="TextButton" DataField="ItemCode" UniqueName="ItemCode" HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="Dscription" HeaderText="Description" HeaderButtonType="TextButton" DataField="Dscription" UniqueName="Dscription" HeaderStyle-Width="400px" />
                            <tel:GridBoundColumn SortExpression="Quantity" HeaderText="Quantity" HeaderButtonType="TextButton" DataField="Quantity" UniqueName="Quantity" DataFormatString="{0:N0}" HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="U_Receive" HeaderText="U_Receive" HeaderButtonType="TextButton" DataField="U_Receive" UniqueName="U_Receive" HeaderStyle-Width="150px" />
                            <tel:GridBoundColumn SortExpression="U_Despatch" HeaderText="U_Despatch" HeaderButtonType="TextButton" DataField="U_Despatch" UniqueName="U_Despatch" HeaderStyle-Width="150px" />
                        </Columns>
                    </MasterTableView>
                    <ClientSettings>
                        <Resizing AllowColumnResize="true" />
                        <Selecting AllowRowSelect="true" />
                    </ClientSettings>
                </tel:RadGrid>
            </div>
        </div>
    </div>
    <div class="row">
        <tel:RadWindow ID="rwTransfers" runat="server" Modal="true" OpenerElementID=""
            Title="" Behaviors="Reload, Move, Resize" VisibleTitlebar="false" VisibleStatusbar="false"
            RegisterWithScriptManager="true" Style="display: inline; overflow: hidden;" ShowContentDuringLoad="false" Top="-10" Left="100" />
    </div>

    <script type="text/javascript">
        var myHiddenWidth = document.getElementById('<%= hfScreenWidth.ClientID %>');
        var myHiddenHeight = document.getElementById('<%= hfScreenHeight.ClientID %>');

        if (myHiddenWidth) {
            myHiddenWidth.value = window.innerWidth;//screen.width;
        }

        if (myHiddenHeight) {
            myHiddenHeight.value = window.innerHeight;//screen.height;
        }

    </script>

</asp:Content>

