<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="PreOrderManagement.aspx.cs" Inherits="PreOrderManagement" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">

    <style>
        #line {
            border-bottom: 1px black dotted;
            overflow: visible;
            height: 9px;
            margin: 5px 0 10px 0;
        }

            #line span {
                background-color: white;
                padding: 0 5px;
            }

        .RadGrid_Windows7 .rgRow td {
            border: solid 1px #005CC8;
        }

        .RadGrid_Windows7 .rgAltRow td {
            border: solid 1px #005CC8;
        }
    </style>

    <script lang="javascript" type="text/javascript">

        function printDiv(divName) {

            //printJS({
            //    printable: divName,
            //    type: 'html',
            //    targetStyles: ['*'],
            //    header: 'PrintJS - Print Form With Customized Header'
            //})

            //var printContents = document.getElementById(divName).innerHTML;
            //var originalContents = document.body.innerHTML;

            //document.body.innerHTML = printContents;

            //window.print();

            //document.body.innerHTML = originalContents;
            //return false;

            let printContents, popupWin;
            printContents = document.getElementById(divName).innerHTML;
            popupWin = window.open('', '_blank', 'top=0,left=0,height=100%,width=auto');
            popupWin.document.open();
            popupWin.document.write(`
              <html>
                <head>
                <link rel="stylesheet" href="https://stackpath.bootstrapcdn.com/bootstrap/4.1.3/css/bootstrap.min.css" integrity="sha384-MCw98/SFnGE8fJT3GXwEOngsV7Zt27NXFoaoApmYm81iuXoPkFOJwJ8ERdknLPMO" crossorigin="anonymous">
                </head>
                <style>

                </style>
            <body onload="window.print();window.close()">${printContents}</body>
              </html>`
            );
            popupWin.document.close();
        }

        function HidePnl() {
            //alert(document.getElementById('<%=pnlDetails.ClientID%>'));
            //var pnlDetails = document.getElementById('#<%= pnlDetails.ClientID %>');
            //document.getElementById('<%=pnlDetails.ClientID%>').style.display = "";
            document.getElementById('<%= pnlDetails.ClientID %>').style.display = "none";
            //return false;
        }
    </script>

    <div class="container-fluid">
        <div class="row">
            <div class="col-md-12">
                <asp:Panel ID="pnlPromoHeader" runat="server" CssClass="Panel">
                    <label class="PanelHeading">Web Orders Status</label>
                    <div class="row">
                        <div class="col-md-3">
                            &nbsp;&nbsp;
                            <label class="myLabelXtraSmall">Printed</label>
                            <tel:RadComboBox ID="rcbPrinted" runat="server" Height="60px" DropDownAutoWidth="Disabled" Width="100px"
                                HighlightTemplatedItems="true"
                                AppendDataBoundItems="true"
                                EmptyMessage="All"
                                AutoPostBack="true" CheckBoxes="false"
                                OnSelectedIndexChanged="rcbPrinted_SelectedIndexChanged"
                                Font-Italic="false">
                                <ExpandAnimation Type="OutQuart" Duration="500" />
                                <CollapseAnimation Type="OutQuint" Duration="300" />
                                <Items>
                                    <tel:RadComboBoxItem runat="server" Text="All" Selected="true" Value="0" />
                                    <tel:RadComboBoxItem runat="server" Text="Yes" Selected="true" Value="Y" />
                                    <tel:RadComboBoxItem runat="server" Text="No" Selected="true" Value="N" />
                                </Items>
                            </tel:RadComboBox>
                            &nbsp;&nbsp;
                        </div>
                        <div class="col-md-3">
                            &nbsp;&nbsp;
                            <label class="myLabelXtraSmall">Picked</label>
                            <tel:RadComboBox ID="rcbPicked" runat="server" Height="60px" DropDownAutoWidth="Disabled" Width="100px"
                                HighlightTemplatedItems="true"
                                AppendDataBoundItems="true"
                                EmptyMessage="All"
                                AutoPostBack="true" CheckBoxes="false"
                                OnSelectedIndexChanged="rcbPicked_SelectedIndexChanged"
                                Font-Italic="false">
                                <ExpandAnimation Type="OutQuart" Duration="500" />
                                <CollapseAnimation Type="OutQuint" Duration="300" />
                                <Items>
                                    <tel:RadComboBoxItem runat="server" Text="All" Selected="true" Value="0" />
                                    <tel:RadComboBoxItem runat="server" Text="Yes" Selected="true" Value="Y" />
                                    <tel:RadComboBoxItem runat="server" Text="No" Selected="true" Value="N" />
                                </Items>
                            </tel:RadComboBox>
                            &nbsp;&nbsp;
                        </div>
                        <div class="col-md-3">
                            &nbsp;&nbsp;
                            <label class="myLabelXtraSmall">Packed</label>
                            <tel:RadComboBox ID="rcbPacked" runat="server" Height="60px" DropDownAutoWidth="Disabled" Width="100px"
                                HighlightTemplatedItems="true"
                                AppendDataBoundItems="true"
                                EmptyMessage="All"
                                AutoPostBack="true" CheckBoxes="false"
                                OnSelectedIndexChanged="rcbPacked_SelectedIndexChanged"
                                Font-Italic="false">
                                <ExpandAnimation Type="OutQuart" Duration="500" />
                                <CollapseAnimation Type="OutQuint" Duration="300" />
                                <Items>
                                    <tel:RadComboBoxItem runat="server" Text="All" Selected="true" Value="0" />
                                    <tel:RadComboBoxItem runat="server" Text="Yes" Selected="true" Value="Y" />
                                    <tel:RadComboBoxItem runat="server" Text="No" Selected="true" Value="N" />
                                </Items>
                            </tel:RadComboBox>
                            &nbsp;&nbsp;
                        </div>
                        <div class="col-md-3">
                            &nbsp;&nbsp;
                            <label class="myLabelXtraSmall">Delivered</label>
                            <tel:RadComboBox ID="rcbDelivered" runat="server" Height="60px" DropDownAutoWidth="Disabled" Width="100px"
                                HighlightTemplatedItems="true"
                                AppendDataBoundItems="true"
                                EmptyMessage="All"
                                AutoPostBack="true" CheckBoxes="false"
                                OnSelectedIndexChanged="rcbDelivered_SelectedIndexChanged"
                                Font-Italic="false">
                                <ExpandAnimation Type="OutQuart" Duration="500" />
                                <CollapseAnimation Type="OutQuint" Duration="300" />
                                <Items>
                                    <tel:RadComboBoxItem runat="server" Text="All" Selected="true" Value="0" />
                                    <tel:RadComboBoxItem runat="server" Text="Yes" Selected="true" Value="Y" />
                                    <tel:RadComboBoxItem runat="server" Text="No" Selected="true" Value="N" />
                                </Items>
                            </tel:RadComboBox>
                            &nbsp;&nbsp;
                        </div>
                    </div>
                    <br />
                </asp:Panel>
            </div>
        </div>
        <div class="row">&nbsp;</div>

        <div class="row">
            <div class="col-md-12">
                <tel:RadGrid ID="rgHead" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False"
                    AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="True" PageSize="15" CssClass="Panel"
                    OnNeedDataSource="rgHead_NeedDataSource" OnItemDataBound="rgHead_ItemDataBound" OnItemCommand="rgHead_ItemCommand">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <ExportSettings OpenInNewWindow="true" IgnorePaging="true" Excel-Format="Xlsx" HideStructureColumns="true" FileName="WebOrdersStatus" />
                    <MasterTableView Width="100%" AllowNaturalSort="false" DataKeyNames="InvoiceNumber,MagentoOrderNumber" CommandItemDisplay="Top">
                        <CommandItemSettings ShowExportToExcelButton="false" ShowAddNewRecordButton="false" ShowRefreshButton="false" />
                        <Columns>
                            <tel:GridButtonColumn CommandName="View" Text="View" UniqueName="View" HeaderText="View" HeaderStyle-Width="60px" />
                            <tel:GridBoundColumn SortExpression="InvoiceNumber" HeaderText="Invoice No." HeaderButtonType="TextButton" DataField="InvoiceNumber" UniqueName="Casos" HeaderStyle-Width="60px" />
                            <tel:GridBoundColumn SortExpression="MagentoOrderNumber" HeaderText="Magento Order No." HeaderButtonType="TextButton" DataField="MagentoOrderNumber" UniqueName="MagentoOrderNumber" />
                            <tel:GridBoundColumn SortExpression="CustomerName" HeaderText="Customer Name" HeaderButtonType="TextButton" DataField="CustomerName" UniqueName="CustomerName" />
                            <tel:GridBoundColumn SortExpression="CustomerEmail" HeaderText="Email" HeaderButtonType="TextButton" DataField="CustomerEmail" UniqueName="CustomerEmail" />
                            <tel:GridBoundColumn SortExpression="CustomerPhoneNumber" HeaderText="Customer Phone" HeaderButtonType="TextButton" DataField="CustomerPhoneNumber" UniqueName="CustomerPhoneNumber" />
                            <tel:GridBoundColumn SortExpression="FlightDate" HeaderText="Flight Date" HeaderButtonType="TextButton" DataField="FlightDate" UniqueName="FlightDate" DataFormatString="{0:d}" />
                            <tel:GridBoundColumn SortExpression="FlightTime" HeaderText="Flight Time" HeaderButtonType="TextButton" DataField="FlightTime" UniqueName="FlightTime" DataFormatString="{0:hh\:mm}" />
                            <tel:GridBoundColumn SortExpression="isPrinted" HeaderText="isPrinted" HeaderButtonType="TextButton" DataField="isPrinted" UniqueName="isPrinted" Display="false" />
                            <tel:GridBoundColumn SortExpression="isPicked" HeaderText="isPicked" HeaderButtonType="TextButton" DataField="isPicked" UniqueName="isPicked" Display="false" />
                            <tel:GridBoundColumn SortExpression="isPacked" HeaderText="isPacked" HeaderButtonType="TextButton" DataField="isPacked" UniqueName="isPacked" Display="false" />
                            <tel:GridBoundColumn SortExpression="isDelivered" HeaderText="isDelivered" HeaderButtonType="TextButton" DataField="isDelivered" UniqueName="isDelivered" Display="false" />
                            <tel:GridButtonColumn CommandName="Print" Text="Print" UniqueName="Printed" HeaderText="Printed?" HeaderStyle-Width="60px" />
                            <tel:GridButtonColumn CommandName="Picked" Text="Picked" UniqueName="Picked" HeaderText="Picked?" HeaderStyle-Width="60px" />
                            <tel:GridButtonColumn CommandName="Packed" Text="Packed" UniqueName="Packed" HeaderText="Packed?" HeaderStyle-Width="60px" />
                            <tel:GridButtonColumn CommandName="Delivered" Text="Delivered" UniqueName="Delivered" HeaderText="Delivered?" HeaderStyle-Width="80px" />
                            <tel:GridBoundColumn SortExpression="LastUpdatedBy" HeaderText="Updated By" HeaderButtonType="TextButton" DataField="LastUpdatedBy" UniqueName="LastUpdatedBy" HeaderStyle-Width="120px" />
                            <%--<tel:GridHyperLinkColumn SortExpression="SolucionLink" HeaderText="Solucion Link" HeaderButtonType="TextButton" DataTextField="SolucionLink" DataNavigateUrlFields="SolucionLink" UniqueName="SolucionLink" Target="_blank" />--%>
                        </Columns>
                    </MasterTableView>
                    <ClientSettings>
                        <Resizing AllowColumnResize="true" />
                        <Selecting AllowRowSelect="true" />
                    </ClientSettings>
                </tel:RadGrid>
            </div>
        </div>

        <div class="row">
            &nbsp;
            <asp:HiddenField ID="hfInvoiceNumber" runat="server" Value="-1" />
            <asp:HiddenField ID="hfMagentoOrderNumber" runat="server" Value="-1" />
        </div>

        <div class="row">
            <div class="col-md-12">
                <asp:Panel ID="pnlDetails" runat="server" CssClass="PanelWhiteBG" style="display:none;">
                    <label class="PanelHeading">
                        Order Detail
                    </label>
                    <div class="row text-center">
                        <div class="col-sm-6 text-center">
                            <button class="btn btn-success hidden-print" type="submit" onclick="printDiv('divPrint')"><span class="glyphicon glyphicon-print" aria-hidden="true"></span>&nbsp;Print</button>
                        </div>
                        <div class="col-sm-6 text-center">
                            <button class="btn btn-danger hidden-print" type="button" onclick="HidePnl();"><span class="glyphicon glyphicon-exclamation-sign" aria-hidden="true"></span>&nbsp;Cancel</button>
                        </div>
                    </div>
                    <div class="row">&nbsp;</div>
                    <div class="container" id="divPrint" style="width: 98%">
                        <div class="row">
                            <div class="col-xs-6">
                                <ul class="myUL">
                                    <li>
                                        <label class="myLabelLarge">Magento Order No.</label>:
                                    <asp:Label CssClass="myLabelLarge" runat="server" ID="lblWebOrderNumber"></asp:Label>
                                    </li>
                                    <li>
                                        <label class="myLabelLarge">Customer Name</label>:
                                    <asp:Label CssClass="myLabelLarge" runat="server" ID="lblCustomerName"></asp:Label>
                                    </li>
                                    <li>
                                        <label class="myLabelLarge">Passport Number</label>:
                                    <asp:Label CssClass="myLabelLarge" runat="server" ID="lblPassportNumber"></asp:Label>
                                    </li>
                                    <li>
                                        <label class="myLabelLarge">Flight Number</label>:
                                    <asp:Label CssClass="myLabelLarge" runat="server" ID="lblFlight"></asp:Label>
                                    </li>
                                    <li>
                                        <label class="myLabelLarge">Package Pickup Date</label>:
                                    <asp:Label CssClass="myLabelLarge" runat="server" ID="lblPickupDate"></asp:Label>
                                    </li>
                                    <li>
                                        <label class="myLabelLarge">Print Date</label>:
                                    <asp:Label CssClass="myLabelLarge" runat="server" ID="lblPrintDate"></asp:Label>
                                    </li>
                                    <li>
                                        <label class="myLabelLarge">Status</label>:
                                    <asp:Label CssClass="myLabelLarge" runat="server" ID="lblEstatus"></asp:Label>
                                    </li>
                                    <li>
                                        <label class="myLabelLarge">Print User</label>:
                                    <asp:Label CssClass="myLabelLarge" runat="server" ID="lblUser"></asp:Label>
                                    </li>
                                    <li>
                                        <label class="myLabelSmall">Notes</label>:
                                    </li>
                                </ul>
                            </div>
                            <div class="col-xs-6">
                                <ul class="myUL">
                                    <li>
                                        <label class="myLabelLarge">Flight Date</label>:
                                    <asp:Label CssClass="myLabelLarge" runat="server" ID="lblFlightDate"></asp:Label>
                                    </li>
                                    <li>
                                        <label class="myLabelLarge">Flight Time</label>:
                                    <asp:Label CssClass="myLabelLarge" runat="server" ID="lblFlightTime"></asp:Label>
                                    </li>
                                    <li>
                                        <label class="myLabelLarge">Invoice No.</label>:
                                    <asp:Label CssClass="myLabelLarge" runat="server" ID="lblInvoiceNumber"></asp:Label>
                                    </li>
                                    <li>
                                        <label class="myLabelLarge">Total Products</label>:
                                    <asp:Label CssClass="myLabelLarge" runat="server" ID="lblQty"></asp:Label>
                                    </li>
                                    <li style="margin-top: 20px;">
                                        <label class="myLabelLarge">Picked By</label>:
                                        <span style="display: inline-block; border-bottom: 1px solid #000; width: 100px;"></span>
                                    </li>
                                    <li style="margin-top: 20px;">
                                        <label class="myLabelLarge">Packed By</label>:
                                        <span style="display: inline-block; border-bottom: 1px solid #000; width: 100px;"></span>
                                    </li>
                                    <li style="margin-top: 20px;">
                                        <label class="myLabelLarge">Delivered</label>:
                                        <span style="display: inline-block; border-bottom: 1px solid #000; width: 100px;"></span>
                                    </li>
                                    <%--<li>
                                    <label class="myLabelLarge">Phone Number</label>:
                                    <asp:Label CssClass="myLabelLarge" runat="server" ID="lblPhoneNumber"></asp:Label>
                                </li>--%>
                                </ul>
                            </div>
                        </div>
                        <div class="row" style="margin-left: 5px;">
                            <div class="col-xs-12">
                                <tel:RadGrid ID="rgDetail" runat="server" Width="90%" ShowStatusBar="true" AutoGenerateColumns="False" ExportSettings-Pdf-AllowPrinting="true"
                                    AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="false" Style="margin-left: -1px;" OnNeedDataSource="rgDetail_NeedDataSource" GridLines="Both">
                                    <SortingSettings EnableSkinSortStyles="false" />
                                    <ExportSettings OpenInNewWindow="true" IgnorePaging="true" Excel-Format="Xlsx" HideStructureColumns="true" FileName="WebOrdersStatus" Pdf-AllowPrinting="true" />
                                    <MasterTableView Width="100%" AllowNaturalSort="false" CommandItemDisplay="None">
                                        <CommandItemSettings ShowExportToExcelButton="false" ShowAddNewRecordButton="false" ShowRefreshButton="false" />
                                        <Columns>
                                            <tel:GridBoundColumn SortExpression="LineNumber" HeaderText="Line" HeaderButtonType="TextButton" DataField="LineNumber" UniqueName="LineNumber" HeaderStyle-Width="40px" />
                                            <tel:GridBoundColumn SortExpression="ProductCategory" HeaderText="Category" HeaderButtonType="TextButton" DataField="ProductCategoryDesc" UniqueName="ProductCategoryDesc" HeaderStyle-Width="120px" />
                                            <tel:GridBoundColumn SortExpression="SapProductId" HeaderText="SAP Code" HeaderButtonType="TextButton" DataField="SapProductId" UniqueName="SapProductId" HeaderStyle-Width="80px" />
                                            <tel:GridBoundColumn SortExpression="BarCode" HeaderText="Barcode" HeaderButtonType="TextButton" DataField="BarCode" UniqueName="BarCode" HeaderStyle-Width="80px" />
                                            <tel:GridBoundColumn SortExpression="Brand" HeaderText="Brand" HeaderButtonType="TextButton" DataField="BrandDesc" UniqueName="BrandDesc" HeaderStyle-Width="150px" />
                                            <tel:GridBoundColumn SortExpression="ProductDescription" HeaderText="Description" HeaderButtonType="TextButton" DataField="ProductDescription" UniqueName="ProductDescription" HeaderStyle-Width="300px" />
                                            <tel:GridBoundColumn SortExpression="Quantity" HeaderText="Quantity" HeaderButtonType="TextButton" DataField="Quantity" UniqueName="Quantity" HeaderStyle-Width="60px" />
                                        </Columns>
                                    </MasterTableView>
                                    <ClientSettings>
                                        <Resizing AllowColumnResize="true" />
                                        <Selecting AllowRowSelect="true" />
                                    </ClientSettings>
                                </tel:RadGrid>
                            </div>
                        </div>
                        <div class="row">&nbsp;</div>
                    </div>
                </asp:Panel>
            </div>

        </div>
    </div>

</asp:Content>


