<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="ComprasDirectas.aspx.cs" Inherits="ComprasDirectas" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">
    <div class="container-fluid">

        <div class="row">
            <div class="col-md-12">
                <asp:Panel ID="pnlFilters" runat="server" CssClass="Panel">
                    <label id="labelForm" runat="server" class="PanelHeading">Direct Purchase Receiving</label>
                    <div class="row">
                        <div class="col-md-2" style="text-align:center;">
                            <label class="myLabelMedium">From Date</label><br />
                            <tel:RadDatePicker ID="rdpFromDate" runat="server" Width="140px">
                                <DateInput DateFormat="MM/dd/yyyy" />
                            </tel:RadDatePicker>
                            <br /><br />
                            <label class="myLabelMedium">To Date</label><br />
                            <tel:RadDatePicker ID="rdpToDate" runat="server" Width="140px">
                                <DateInput DateFormat="MM/dd/yyyy" />
                            </tel:RadDatePicker>
                        </div>
                        <div class="col-md-1">
                            <label class="myLabelMedium">APRI #</label><br />
                            <asp:TextBox ID="txtDocNum" runat="server" Width="70px" style="height:26px; padding:2px 4px; border:1px solid #ccc; border-radius:3px;" />
                        </div>
                        <div class="col-md-3" style="text-align:center;">
                            <label class="myLabelMedium">To Location</label><br />
                            <tel:RadComboBox ID="rcbToLocation" runat="server"
                                Width="300px" DropDownWidth="350px"
                                AllowCustomText="false" Filter="Contains">
                            </tel:RadComboBox>
                        </div>
                        <div class="col-md-1" style="text-align:center;">
                            <br /><br />
                            <tel:RadButton runat="server" ID="rbtnSearch" Text="Search" OnClick="rbtnSearch_Click" />
                        </div>
                    </div>
                    <div class="row">&nbsp;</div>
                </asp:Panel>
            </div>
        </div>

        <div class="row">&nbsp;</div>

        <div class="row">
            <div class="col-md-12">
                <tel:RadGrid ID="rgInvoices" runat="server"
                    AllowSorting="True" CssClass="Panel"
                    PageSize="25" AllowPaging="True"
                    ShowStatusBar="true" AutoGenerateColumns="False"
                    OnNeedDataSource="rgInvoices_NeedDataSource"
                    OnItemCommand="rgInvoices_ItemCommand">
                    <PagerStyle Mode="Slider" />
                    <SortingSettings EnableSkinSortStyles="false" />
                    <MasterTableView Width="100%"
                        DataKeyNames="DocEntry,CardCode,DocNum"
                        Name="PendingCD"
                        AllowNaturalSort="false"
                        CommandItemDisplay="None">
                        <Columns>
                            <tel:GridHyperLinkColumn SortExpression="DocNum" HeaderText="APRI #"
                                DataTextField="DocNum" DataNavigateUrlFields="DocEntry"
                                DataNavigateUrlFormatString="ComprasDirectasDetail.aspx?docEntry={0}"
                                UniqueName="DocNum" HeaderStyle-Width="80px"
                                ItemStyle-ForeColor="#0066CC" />
                            <tel:GridBoundColumn SortExpression="DocDate"   HeaderText="Date"
                                DataField="DocDate"   UniqueName="DocDate"   HeaderStyle-Width="90px"
                                DataFormatString="{0:MM/dd/yyyy}" />
                            <tel:GridBoundColumn SortExpression="CardName"  HeaderText="Vendor"
                                DataField="CardName"  UniqueName="CardName"  HeaderStyle-Width="220px" />
                            <tel:GridBoundColumn SortExpression="NumAtCard" HeaderText="Ref #"
                                DataField="NumAtCard" UniqueName="NumAtCard" HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="DocTotal"  HeaderText="Total"
                                DataField="DocTotal"  UniqueName="DocTotal"  HeaderStyle-Width="110px"
                                DataFormatString="{0:N2}" ItemStyle-HorizontalAlign="Right" />
                            <tel:GridBoundColumn SortExpression="DocCur"    HeaderText="Currency"
                                DataField="DocCur"    UniqueName="DocCur"    HeaderStyle-Width="80px" />
                            <tel:GridTemplateColumn HeaderText="Action" UniqueName="ActionReceive"
                                HeaderStyle-Width="110px" AllowSorting="false"
                                ItemStyle-HorizontalAlign="Center">
                                <ItemTemplate>
                                    <asp:Button ID="btnReceive" runat="server"
                                        Text="Receive"
                                        CommandName="Receive"
                                        CssClass="btn btn-primary btn-sm"
                                        OnClientClick="return confirmReceive();" />
                                </ItemTemplate>
                            </tel:GridTemplateColumn>
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

    <script type="text/javascript">
        function confirmReceive() {
            return confirm(
                "WARNING!!\n\n" +
                "Are you sure you want to receive this Direct Purchase Invoice?\n\n" +
                "This will create a Goods Receipt PO in SAP B1.\n\n" +
                "Click OK to confirm or Cancel to abort."
            );
        }
    </script>
</asp:Content>
