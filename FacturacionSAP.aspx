<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="FacturacionSAP.aspx.cs" Inherits="FacturacionSAP" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cphMain" runat="Server">

    <div class="container-fluid">
        <div class="row">
            <div class="col-md-12">
                <asp:Panel ID="pnlHeader" runat="server" CssClass="page-curl shadow-bottom">
                    <label id="labelForm" runat="server" class="PanelHeading">SAP Invoicing (OINV)</label>
                    <div class="row" style="margin-top:12px; padding-left:20px;">
                        <div class="col-md-2">
                            <label class="myLabelMedium" style="width:auto;">From</label><br />
                            <tel:RadDatePicker ID="rdpDesde" runat="server" Width="140px" MinDate="2024-01-01">
                                <DateInput DateFormat="MM/dd/yyyy" />
                            </tel:RadDatePicker>
                        </div>
                        <div class="col-md-2">
                            <label class="myLabelMedium" style="width:auto;">To</label><br />
                            <tel:RadDatePicker ID="rdpHasta" runat="server" Width="140px" MinDate="2024-01-01">
                                <DateInput DateFormat="MM/dd/yyyy" />
                            </tel:RadDatePicker>
                        </div>
                        <div class="col-md-8" style="margin-top:22px; padding-left:60px;">
                            <asp:Button ID="btnPreview" runat="server" Text="Search"
                                CssClass="mybtnmeduim" Style="width:150px;" OnClick="btnPreview_Click" CausesValidation="false" />
                            &nbsp;&nbsp;
                            <asp:Button ID="btnUpload" runat="server" Text="Upload Selected"
                                CssClass="mybtnmeduim" Style="width:150px;" OnClick="btnUpload_Click" CausesValidation="false"
                                OnClientClick="return confirmUpload(this, false);" />
                            &nbsp;&nbsp;
                            <%-- Enabled with appSetting FacturacionSAP_RetryPayments = true --%>
                            <asp:Button ID="btnRetryPayments" runat="server" Text="Retry Payments" Visible="false"
                                CssClass="mybtnmeduim" Style="width:150px;" OnClick="btnRetryPayments_Click" CausesValidation="false"
                                OnClientClick="return confirmRetry(this);" />
                            <br />
                            <asp:Label ID="lblLastRun" runat="server" Font-Size="8pt" ForeColor="Gray" Style="margin-top:4px; display:inline-block;" />
                            <br />
                            <asp:CheckBox ID="chkHideManual" runat="server" Checked="true" AutoPostBack="true"
                                OnCheckedChanged="chkHideManual_CheckedChanged"
                                Text="Hide items needing manual resolution in SAP (bost_Close)"
                                Font-Size="8pt" ForeColor="Gray" Style="margin-top:4px;" />
                            &nbsp;&nbsp;
                            <asp:CheckBox ID="chkShowInvoiced" runat="server" Checked="false" AutoPostBack="true"
                                OnCheckedChanged="chkShowInvoiced_CheckedChanged"
                                Text="Show invoiced (OINV/ORIN already in SAP)"
                                Font-Size="8pt" ForeColor="Gray" Style="margin-top:4px;" />
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </div>
        <div class="row">
            <div class="col-md-12" style="margin-top:4px;">
                <a href="javascript:void(0);" onclick="toggleFacturacionLegend(this); return false;"
                    style="font-size:7pt; color:#666; line-height:1; display:inline-block;">Show description</a>
                <p id="pLegend" style="color:#004990; font-size:9pt; display:none; margin-top:4px; margin-bottom:4px;">
                    "Search" calculates the pending invoices live from the POS sales of your branch
                    (la_store_sales) and their deliveries in SAP - nothing is created. "Total (SAP)" and "Items"
                    are the open ODLN/ORDN lines (plus additional expenses, minus mixed-transaction returns)
                    that would be copied into the invoice. "Upload Selected" creates the OINV/ORIN (and its
                    payment/refund) in SAP <b>immediately</b>, right from this page - this re-validates everything
                    at that moment (deliveries, payment totals, tender mappings) before creating anything, so it
                    can still come back "Blocked". The result is logged in "Upload Requests" below.
                    When the site is configured for night uploads, "Upload Selected" only sets the groups to
                    <b>In Progress</b>; the night process uploads them (with the same validations) and writes the
                    result in "Upload Requests".
                    Each row above is one Store/Duty Type/Day/Sale-or-Return group; checking any row for a given
                    Store/Duty Type/Day and clicking "Upload Selected" uploads <b>all</b> non-blocked groups for
                    that same Store/Duty Type/Day.
                </p>
                <script type="text/javascript">
                    // Guards against a double-click triggering two overlapping uploads. The disable is
                    // deferred with setTimeout(0) so the form submission fires first.
                    var facturacionUploadInFlight = false;
                    // queued = appSetting FacturacionSAP_UploadMode = Queue (set from the code-behind).
                    function confirmUpload(btn, queued) {
                        if (facturacionUploadInFlight) return false;
                        var msg = queued
                            ? 'Set the selected groups to In Progress? They will be uploaded to SAP B1 by the night process.'
                            : 'Create the selected invoices/credit memos in SAP B1 now? This cannot be undone from this screen.';
                        if (!confirm(msg))
                            return false;
                        facturacionUploadInFlight = true;
                        setTimeout(function () {
                            btn.disabled = true;
                            btn.value = 'Processing...';
                        }, 0);
                        return true;
                    }

                    function confirmRetry(btn) {
                        if (facturacionUploadInFlight) return false;
                        if (!confirm('Create the missing payments/refunds in SAP B1 for the selected invoiced rows (payment pending)?'))
                            return false;
                        facturacionUploadInFlight = true;
                        setTimeout(function () {
                            btn.disabled = true;
                            btn.value = 'Processing...';
                        }, 0);
                        return true;
                    }

                    function toggleFacturacionLegend(link) {
                        var p = document.getElementById('pLegend');
                        var hidden = p.style.display === 'none';
                        p.style.display = hidden ? '' : 'none';
                        link.innerHTML = hidden ? 'Hide description' : 'Show description';
                    }
                </script>
                <asp:Label ID="lblResult" runat="server" Font-Bold="True" Font-Size="Medium" />
            </div>
        </div>
        <div class="row" style="margin-top:6px;">
            <div class="col-md-12">
                <tel:RadGrid ID="rgHead" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False"
                    MasterTableView-NoDetailRecordsText="No pending invoices for this range."
                    AllowSorting="true" AllowPaging="True" PageSize="200"
                    OnNeedDataSource="rgHead_NeedDataSource" OnItemDataBound="rgHead_ItemDataBound"
                    OnItemCommand="rgHead_ItemCommand">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <MasterTableView Width="100%" AllowNaturalSort="false" DataKeyNames="CardCode,CardName,TransType,FechaDoc,Blocked,EsDevolucion,PaymentPending,InProgress">
                        <ItemStyle Wrap="false" Height="22px" />
                        <Columns>
                            <tel:GridTemplateColumn HeaderText="" UniqueName="Select" HeaderStyle-Width="40px">
                                <ItemTemplate>
                                    <asp:CheckBox ID="chkSelect" runat="server" />
                                </ItemTemplate>
                            </tel:GridTemplateColumn>
                            <tel:GridBoundColumn SortExpression="FechaDoc" HeaderText="Date" HeaderButtonType="TextButton"
                                DataField="FechaDoc" UniqueName="FechaDoc" HeaderStyle-Width="90px" DataFormatString="{0:MM/dd/yyyy}" />
                            <tel:GridBoundColumn SortExpression="CardCode" HeaderText="CardCode" HeaderButtonType="TextButton"
                                DataField="CardCode" UniqueName="CardCode" HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="CardName" HeaderText="CardName" HeaderButtonType="TextButton"
                                DataField="CardName" UniqueName="CardName" HeaderStyle-Width="200px" />
                            <tel:GridTemplateColumn HeaderText="Type" SortExpression="EsDevolucion" UniqueName="TypeColumn" HeaderStyle-Width="90px">
                                <ItemTemplate>
                                    <asp:Label ID="lblTipo" runat="server" />
                                </ItemTemplate>
                            </tel:GridTemplateColumn>
                            <tel:GridBoundColumn SortExpression="ItemCount" HeaderText="Items" HeaderButtonType="TextButton"
                                DataField="ItemCount" UniqueName="ItemCount" HeaderStyle-Width="60px" DataFormatString="{0:N0}" />
                            <tel:GridBoundColumn SortExpression="InvoiceTotal" HeaderText="Total (SAP)" HeaderButtonType="TextButton"
                                DataField="InvoiceTotal" UniqueName="InvoiceTotal" HeaderStyle-Width="100px" DataFormatString="{0:N2}" />
                            <tel:GridBoundColumn SortExpression="PosTotal" HeaderText="Total (POS)" HeaderButtonType="TextButton"
                                DataField="PosTotal" UniqueName="PosTotal" HeaderStyle-Width="100px" DataFormatString="{0:N2}" />
                            <tel:GridTemplateColumn HeaderText="Status" SortExpression="Blocked" UniqueName="Blocked" HeaderStyle-Width="340px">
                                <ItemTemplate>
                                    <asp:Label ID="lblStatus" runat="server" Font-Bold="true" />
                                </ItemTemplate>
                            </tel:GridTemplateColumn>
                            <tel:GridTemplateColumn HeaderText="" UniqueName="Detail" HeaderStyle-Width="60px">
                                <ItemTemplate>
                                    <asp:LinkButton ID="btnDetail" runat="server" CommandName="ShowDetail" Text="Detail" ForeColor="#0066CC" />
                                </ItemTemplate>
                            </tel:GridTemplateColumn>
                        </Columns>
                    </MasterTableView>
                    <ClientSettings>
                        <Resizing AllowColumnResize="true" />
                        <Scrolling AllowScroll="true" />
                    </ClientSettings>
                </tel:RadGrid>
            </div>
        </div>
        <div class="row" style="margin-top:10px;">
            <div class="col-md-12">
                <asp:Panel ID="pnlDetail" runat="server" Visible="false" CssClass="Panel" Style="padding:10px;">
                    <label class="myLabelMedium" style="width:auto;">Mismatched Transactions - </label>
                    <asp:Label ID="lblDetailHeader" runat="server" Font-Bold="true" />
                    <p style="color:#666; font-size:8pt; margin-top:4px;">
                        Compares each individual transaction's sale amount against what it actually collected
                        in payments - only the ones that don't match are shown below (that's the actual cause
                        of the "total doesn't match" block). A transaction with <b>both</b> a sale and a return
                        (mixed ticket) is compared at its net amount, since its return goes as a negative line
                        in the same invoice.
                    </p>
                    <asp:Label ID="lblNoMismatches" runat="server" Font-Bold="true" ForeColor="Green" Text="No mismatched transactions found - every transaction's payments match its sale amount individually." Visible="false" />
                    <tel:RadGrid ID="rgMismatches" runat="server" Width="100%" ShowStatusBar="false" AutoGenerateColumns="False"
                        AllowPaging="True" PageSize="15">
                        <PagerStyle Mode="Slider"></PagerStyle>
                        <MasterTableView Width="100%" AllowNaturalSort="false">
                            <ItemStyle Wrap="false" Height="20px" />
                            <Columns>
                                <tel:GridBoundColumn HeaderText="Transnum" DataField="Transnum" UniqueName="Transnum" HeaderStyle-Width="100px" />
                                <tel:GridBoundColumn HeaderText="Reg No." DataField="RegNo" UniqueName="RegNo" HeaderStyle-Width="80px" />
                                <tel:GridBoundColumn HeaderText="Event No." DataField="EventNo" UniqueName="EventNo" HeaderStyle-Width="90px" />
                                <tel:GridBoundColumn HeaderText="TransactionID" DataField="TransactionID" UniqueName="TransactionID" HeaderStyle-Width="120px" />
                                <tel:GridBoundColumn HeaderText="Sale Amount" DataField="SalesTotal" UniqueName="SalesTotal" HeaderStyle-Width="110px" DataFormatString="{0:N2}" />
                                <tel:GridBoundColumn HeaderText="Payments Collected" DataField="PaymentsTotal" UniqueName="PaymentsTotal" HeaderStyle-Width="130px" DataFormatString="{0:N2}" />
                                <tel:GridBoundColumn HeaderText="Difference" DataField="Difference" UniqueName="Difference" HeaderStyle-Width="100px" DataFormatString="{0:N2}"
                                    ItemStyle-ForeColor="Firebrick" ItemStyle-Font-Bold="true" />
                            </Columns>
                        </MasterTableView>
                    </tel:RadGrid>

                    <asp:Panel ID="pnlPendingItems" runat="server" Visible="false" Style="margin-top:14px;">
                        <label class="myLabelMedium" style="width:auto;">Pending Delivery Items</label>
                        <p style="color:#666; font-size:8pt; margin-top:4px;">
                            POS lines of this store/day that still have no ODLN/ORDN in SAP (or are not yet a
                            negative invoice line) - these are what blocks the invoice as "Pending deliveries".
                            Status shows the delivery error to fix (stock, cost, missing item, mapping...); lines
                            with no error just have not been processed yet and will go on the next sync run.
                        </p>
                        <tel:RadGrid ID="rgPendingItems" runat="server" Width="100%" ShowStatusBar="false" AutoGenerateColumns="False"
                            AllowPaging="True" PageSize="20">
                            <PagerStyle Mode="Slider"></PagerStyle>
                            <MasterTableView Width="100%" AllowNaturalSort="false">
                                <ItemStyle Wrap="false" Height="20px" />
                                <Columns>
                                    <tel:GridBoundColumn HeaderText="Transnum" DataField="Transnum" UniqueName="Transnum" HeaderStyle-Width="100px" />
                                    <tel:GridBoundColumn HeaderText="Reg No." DataField="RegNo" UniqueName="RegNo" HeaderStyle-Width="70px" />
                                    <tel:GridBoundColumn HeaderText="Event No." DataField="EventNo" UniqueName="EventNo" HeaderStyle-Width="80px" />
                                    <tel:GridBoundColumn HeaderText="TransactionID" DataField="TransactionID" UniqueName="TransactionID" HeaderStyle-Width="100px" />
                                    <tel:GridBoundColumn HeaderText="Type" DataField="Tipo" UniqueName="Tipo" HeaderStyle-Width="60px" />
                                    <tel:GridBoundColumn HeaderText="Item" DataField="ItemCode" UniqueName="ItemCode" HeaderStyle-Width="90px" />
                                    <tel:GridBoundColumn HeaderText="Description" DataField="Descripcion" UniqueName="Descripcion" HeaderStyle-Width="180px" />
                                    <tel:GridBoundColumn HeaderText="Qty" DataField="Qty" UniqueName="Qty" HeaderStyle-Width="50px" DataFormatString="{0:N0}" />
                                    <tel:GridBoundColumn HeaderText="Amount" DataField="Monto" UniqueName="Monto" HeaderStyle-Width="100px" DataFormatString="{0:N2}" />
                                    <tel:GridBoundColumn HeaderText="Whs" DataField="WhsCode" UniqueName="WhsCode" HeaderStyle-Width="60px" />
                                    <tel:GridBoundColumn HeaderText="Status" DataField="Estado" UniqueName="Estado" HeaderStyle-Width="420px"
                                        ItemStyle-ForeColor="Firebrick" ItemStyle-Wrap="true" />
                                </Columns>
                            </MasterTableView>
                        </tel:RadGrid>
                    </asp:Panel>
                </asp:Panel>
            </div>
        </div>
        <div class="row" style="margin-top:16px;">
            <div class="col-md-12">
                <h3 class="myLabelFull">Upload Requests</h3>
                <tel:RadGrid ID="rgRequests" runat="server" Width="100%" ShowStatusBar="false" AutoGenerateColumns="False"
                    MasterTableView-NoDetailRecordsText="No upload requests for this range yet."
                    AllowSorting="true" AllowPaging="True" PageSize="25"
                    OnNeedDataSource="rgRequests_NeedDataSource" OnItemDataBound="rgRequests_ItemDataBound">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <MasterTableView Width="100%" AllowNaturalSort="false" DataKeyNames="Id">
                        <ItemStyle Wrap="false" Height="22px" />
                        <Columns>
                            <tel:GridBoundColumn SortExpression="CardCode" HeaderText="CardCode" HeaderButtonType="TextButton"
                                DataField="CardCode" UniqueName="CardCode" HeaderStyle-Width="100px" />
                            <tel:GridBoundColumn SortExpression="FechaDoc" HeaderText="Date" HeaderButtonType="TextButton"
                                DataField="FechaDoc" UniqueName="FechaDoc" HeaderStyle-Width="90px" DataFormatString="{0:MM/dd/yyyy}" />
                            <tel:GridBoundColumn SortExpression="RequestedBy" HeaderText="Requested By" HeaderButtonType="TextButton"
                                DataField="RequestedBy" UniqueName="RequestedBy" HeaderStyle-Width="110px" />
                            <tel:GridBoundColumn SortExpression="RequestedAt" HeaderText="Requested At" HeaderButtonType="TextButton"
                                DataField="RequestedAt" UniqueName="RequestedAt" HeaderStyle-Width="130px" DataFormatString="{0:MM/dd/yyyy HH:mm}" />
                            <tel:GridTemplateColumn HeaderText="Status" SortExpression="ProcessedAt" UniqueName="ProcessedAt" HeaderStyle-Width="90px">
                                <ItemTemplate>
                                    <asp:Label ID="lblRequestStatus" runat="server" Font-Bold="true" />
                                </ItemTemplate>
                            </tel:GridTemplateColumn>
                            <tel:GridBoundColumn SortExpression="Summary" HeaderText="Summary" HeaderButtonType="TextButton"
                                DataField="Summary" UniqueName="Summary" HeaderStyle-Width="400px" HtmlEncode="true" />
                        </Columns>
                    </MasterTableView>
                    <ClientSettings>
                        <Resizing AllowColumnResize="true" />
                        <Scrolling AllowScroll="true" />
                    </ClientSettings>
                </tel:RadGrid>
            </div>
        </div>
    </div>

</asp:Content>
