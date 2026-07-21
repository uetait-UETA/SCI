<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="Transfers.aspx.cs" Inherits="Transfers" %>
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
                    <label  ID="labelForm" runat="server" class="PanelHeading">View Transfers</label>
                    <div class="row">
                        <div class="col-md-12" style="text-align: center;">
                            <asp:Label ID="CompanyLabel" runat="server" Text="Company" CssClass="myLabel"></asp:Label>
                        </div>
                    </div>
                    <div class="row">&nbsp;</div>
                    <div class="row">
                        <div class="col-md-1" style="text-align: center;">
                            <br />
                            <br />
                            <asp:Label ID="DocNumLabel" runat="server" Text="DocNum" CssClass="myLabel" /><br />
                            <asp:TextBox ID="txtDocNum" runat="server" OnPreRender="txtDocNum_PreRender" CssClass="TextboxSmall" />
                        </div>
                        <div class="col-md-1" style="text-align: center;">
                            <br />
                            <br />
                            <br />
                            <asp:DropDownList ID="andOrDropDownList1" runat="server" CssClass="myDdlXSmall">
                                <asp:ListItem Selected="True">AND</asp:ListItem>
                                <asp:ListItem>OR</asp:ListItem>
                            </asp:DropDownList>
                        </div>
                        <div class="col-md-2" style="text-align: center;">
                            <asp:Label ID="fromDateLabel" runat="server" Text="From Date" CssClass="myLabel" /><br />
                            <telerik:RadDatePicker ID="FromDateTxt" runat="server" ShowPopupOnFocus="true" OnSelectedDateChanged="FromDateTxt_SelectedDateChanged" />
                            <br />
                            <br />
                            <asp:Label ID="toDateLabel" runat="server" Text="To Date" CssClass="myLabel" /><br />
                            <telerik:RadDatePicker ID="toDateTxt" runat="server" ShowPopupOnFocus="true" OnSelectedDateChanged="toDateTxt_SelectedDateChanged" />
                            <br />
                            <br />
                        </div>
                        <div class="col-md-1" style="text-align: center;">
                            <br />
                            <br />
                            <br />
                            <asp:DropDownList ID="andOrDropDownList2" runat="server" CssClass="myDdlXSmall">
                                <asp:ListItem Selected="True">AND</asp:ListItem>
                                <asp:ListItem>OR</asp:ListItem>
                            </asp:DropDownList>
                        </div>
                        <div class="col-md-2" style="text-align: center;">
                            <asp:Label ID="fromWhsLabel" runat="server" Text="From Location" CssClass="myLabel" /><br />
                            <asp:DropDownList ID="drpFromWhsCode" runat="server" DataTextField="WHS" DataValueField="WhsCode" CssClass="myDdlMedium" /><br />
                            <br />
                            <asp:Label ID="toWhsLabel" runat="server" Text="To Location" CssClass="myLabel" /><br />
                            <asp:DropDownList ID="drpToWhsCode" runat="server" DataTextField="WHS" DataValueField="WhsCode" CssClass="myDdlMedium" /><br />
                        </div>
                        <div class="col-md-1" style="text-align: center;">
                            <br />
                            <br />
                            <br />
                            <asp:DropDownList ID="andOrDropDownList3" runat="server" CssClass="myDdlXSmall">
                                <asp:ListItem Selected="True">AND</asp:ListItem>
                                <asp:ListItem>OR</asp:ListItem>
                            </asp:DropDownList>
                        </div>
                        <div class="col-md-2" style="text-align: center;">
                            <asp:RadioButtonList ID="StatusRadioButtonList" runat="server" RepeatLayout="Flow" RepeatDirection="Vertical" CssClass="radio" Style="padding: 0 0 0 15px">
                                <asp:ListItem Selected="True" Value="O">&quot;Open  &quot;</asp:ListItem>
                                <asp:ListItem Value="C">Closed</asp:ListItem>
                                <asp:ListItem Value="All">&quot;All. .  . .&quot;</asp:ListItem>
                            </asp:RadioButtonList><br />
                            <asp:Label ID="categoryLabel" runat="server" Text="Category" CssClass="myLabel" /><br />
                            <asp:DropDownList ID="drpItemGroups" runat="server" DataTextField="GroupName" DataValueField="GroupCode" CssClass="myDdlMedium" />
                        </div>
                        <div class="col-md-1" style="text-align: center;">
                            <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="mybtn" OnClick="btnSearch_Click" />
                        </div>
                    </div>
                    <div class="row">&nbsp;</div>
                </asp:Panel>
            </div>
        </div>
        <div class="row">&nbsp;</div>
        <div class="row">&nbsp;</div>
		<%-- 2021-MAR-15: Agregado para modificación de la impresión masiva, por Aldo Reina: --%>
        <div class="row">
            <div class="col-md-12">
                <asp:RadioButtonList ID="RadioButtonListType" runat="server" AutoPostBack="false" RepeatLayout="Table" RepeatDirection="Horizontal" CssClass="radio" Style="position:relative; top:12px; display:inline;">
                    <asp:ListItem Selected="True" Value="detail">Details</asp:ListItem>
                    <asp:ListItem Value="dispatch">Dispatches</asp:ListItem>
                </asp:RadioButtonList>
                <asp:DropDownList Visible="false" ID="DropDownListPrinters" runat="server" CssClass="myDdlMedium" Height="40px"></asp:DropDownList>
                <asp:Button ID="PrintSelected" runat="server" Text="Print" OnClick="PrintSelected_Click" Font-Size="Medium" Height="40px" Width="102px" />
                <asp:Label ID="LabelPrintAllCopies" runat="server" Text="Copies:" style="margin-left: 5px; font-weight:bold;"></asp:Label>
                <asp:TextBox ID="TextBoxPrintAllCopies" runat="server" TextMode="SingleLine" step="1" Text="1" style="position:relative; width:39px;" CssClass="rgHeadPrintCopies"></asp:TextBox>
				<asp:Label ID="TextBoxPrintAllCopiesInfo" runat="server" Text="(Enter zero(0) to take the quantity individually from the Copies column)" style="margin-left: 5px; font-style:italic; font-size:9pt; color:red;"></asp:Label>
                <asp:CheckBox ID="CheckBoxSelectAllCurrentPageForPrint" runat="server" AutoPostBack="false" CssClass="checkbox" Text="Select All (current page)" Style="position:relative; left:16px; display:block;" />
            </div>
        </div>
        <div class="row">
            <div class="col-md-12">
				<%-- 2021-MAR-15: Comentado para modificación de la impresión masiva, por Aldo Reina: --%>
                <%--
                <tel:RadGrid ID="rgHead" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False"
                    AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="True" PageSize="15" CssClass="Panel"
                    OnNeedDataSource="rgHead_NeedDataSource" OnItemCommand="rgHead_ItemCommand">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <ExportSettings OpenInNewWindow="true" IgnorePaging="true" Excel-Format="Xlsx" FileName="ViewTransfers" UseItemStyles="true" />
                    <MasterTableView Width="100%" AllowNaturalSort="false" DataKeyNames="DocEntry" CommandItemDisplay="Top">
                        <CommandItemSettings ShowExportToExcelButton="true" ShowRefreshButton="false" ShowAddNewRecordButton="false" />
                        <Columns>
                            <tel:GridButtonColumn UniqueName="DocEntry" ButtonType="LinkButton" HeaderText="Details" DataTextField="DocEntry" CommandName="Details" />
                            <tel:GridBoundColumn SortExpression="DocDate" HeaderText="Date" HeaderButtonType="TextButton" DataField="DocDate" UniqueName="DocDate" DataFormatString="{0:d}" />
                            <tel:GridBoundColumn SortExpression="DocStatus" HeaderText="Status" HeaderButtonType="TextButton" DataField="DocStatus" UniqueName="DocStatus" />
                            <tel:GridBoundColumn SortExpression="FromLocName" HeaderText="From Loc" HeaderButtonType="TextButton" DataField="FromLocName" UniqueName="FromLocName" HeaderStyle-Width="170px" />
                            <tel:GridBoundColumn SortExpression="ToLocName" HeaderText="To Loc" HeaderButtonType="TextButton" DataField="ToLocName" UniqueName="ToLocName" HeaderStyle-Width="170px" />
                            <tel:GridBoundColumn SortExpression="Category" HeaderText="Category" HeaderButtonType="TextButton" DataField="Category" UniqueName="Category" />
                            <tel:GridBoundColumn SortExpression="TotalLines" HeaderText="Total Lines" HeaderButtonType="TextButton" DataField="TotalLines" UniqueName="TotalLines" DataFormatString="{0:N0}" />
                            <tel:GridBoundColumn SortExpression="TotalQty" HeaderText="Qty" HeaderButtonType="TextButton" DataField="TotalQty" UniqueName="TotalQty" DataFormatString="{0:N0}" />
                            <tel:GridBoundColumn SortExpression="dispatched" HeaderText="Dispatched" HeaderButtonType="TextButton" DataField="dispatched" UniqueName="dispatched" DataFormatString="{0:N0}" />
                            <tel:GridBoundColumn SortExpression="received" HeaderText="Received" HeaderButtonType="TextButton" DataField="received" UniqueName="received" DataFormatString="{0:N0}" />
                            <tel:GridBoundColumn SortExpression="DispCompleted" HeaderText="Dispatch Completed" HeaderButtonType="TextButton" DataField="DispCompleted" UniqueName="DispCompleted" DataFormatString="{0:N0}" />
                            <tel:GridBoundColumn SortExpression="ReceCompleted" HeaderText="Rcv. Completed" HeaderButtonType="TextButton" DataField="ReceCompleted" UniqueName="ReceCompleted" DataFormatString="{0:N0}" />
                            <tel:GridBoundColumn SortExpression="InputType" HeaderText="Input Type" HeaderButtonType="TextButton" DataField="InputType" UniqueName="InputType" DataFormatString="{0:N0}" />
                            <tel:GridBoundColumn SortExpression="ScanStatus" HeaderText="Scan Status" HeaderButtonType="TextButton" DataField="ScanStatus" UniqueName="ScanStatus" DataFormatString="{0:N0}" />
                            <tel:GridButtonColumn UniqueName="DocEntry" ButtonType="LinkButton" HeaderText="Dispatch" DataTextField="DocEntry" CommandName="Dispatch" />
                        </Columns>
                    </MasterTableView>
                    <ClientSettings>
                        <Resizing AllowColumnResize="true" />
                        <Selecting AllowRowSelect="true" />
                    </ClientSettings>
                </tel:RadGrid>
				--%>
				
				<%-- 2021-MAR-15: Modificado para la impresión masiva, por Aldo Reina: Añadido <tel:GridTemplateColumn UniqueName="ChkTemplatePrint"... --%>
                <tel:RadGrid ID="rgHead" runat="server" Width="100%" ShowStatusBar="true" AutoGenerateColumns="False"
                    AllowSorting="true" AllowMultiRowSelection="False" AllowPaging="True" PageSize="15" CssClass="Panel"
                    OnNeedDataSource="rgHead_NeedDataSource" OnItemCommand="rgHead_ItemCommand">
                    <PagerStyle Mode="Slider"></PagerStyle>
                    <SortingSettings EnableSkinSortStyles="false" />
                    <ExportSettings OpenInNewWindow="true" IgnorePaging="true" Excel-Format="Xlsx" FileName="ViewTransfers" UseItemStyles="true" />
                    <MasterTableView Width="100%" AllowNaturalSort="false" DataKeyNames="DocEntry" CommandItemDisplay="Top">
                        <CommandItemSettings ShowExportToExcelButton="true" ShowRefreshButton="false" ShowAddNewRecordButton="false" />
                        <Columns>
                            <tel:GridTemplateColumn UniqueName="ChkTemplatePrint" HeaderText="Print" HeaderStyle-Width="40px">
                                <ItemTemplate>
                                    <asp:CheckBox ID="CheckBoxPrintDetail" runat="server" AutoPostBack="false" />
                                </ItemTemplate>
                            </tel:GridTemplateColumn>
                            <tel:GridTemplateColumn UniqueName="TxtTemplatePrintCopies" HeaderText="Copies" HeaderStyle-Width="50px">
                                <ItemTemplate>
                                    <asp:TextBox ID="TxtPrintCopies" runat="server" TextMode="SingleLine" step="1" Text="1" style="position:relative; width:39px;" CssClass="rgHeadPrintCopies"></asp:TextBox>
                                </ItemTemplate>
                            </tel:GridTemplateColumn>
                            <tel:GridBoundColumn SortExpression="DocEntry" HeaderText="-" Display="false" DataField="DocEntry" UniqueName="DocEntryPrint" />
                            <tel:GridButtonColumn UniqueName="DocEntry" ButtonType="LinkButton" HeaderText="Details" DataTextField="DocEntry" CommandName="Details" />
                            <tel:GridBoundColumn SortExpression="DocDate" HeaderText="Date" HeaderButtonType="TextButton" DataField="DocDate" UniqueName="DocDate" DataFormatString="{0:d}" />
                            <tel:GridBoundColumn SortExpression="DocStatus" HeaderText="Status" HeaderButtonType="TextButton" DataField="DocStatus" UniqueName="DocStatus" />
                            <tel:GridBoundColumn SortExpression="FromLocName" HeaderText="From Loc" HeaderButtonType="TextButton" DataField="FromLocName" UniqueName="FromLocName" HeaderStyle-Width="170px" />
                            <tel:GridBoundColumn SortExpression="ToLocName" HeaderText="To Loc" HeaderButtonType="TextButton" DataField="ToLocName" UniqueName="ToLocName" HeaderStyle-Width="170px" />
                            <tel:GridBoundColumn SortExpression="Category" HeaderText="Category" HeaderButtonType="TextButton" DataField="Category" UniqueName="Category" />
                            <tel:GridBoundColumn SortExpression="TotalLines" HeaderText="Total Lines" HeaderButtonType="TextButton" DataField="TotalLines" UniqueName="TotalLines" DataFormatString="{0:N0}" />
                            <tel:GridBoundColumn SortExpression="TotalQty" HeaderText="Qty" HeaderButtonType="TextButton" DataField="TotalQty" UniqueName="TotalQty" DataFormatString="{0:N0}" />
                            <tel:GridBoundColumn SortExpression="dispatched" HeaderText="Dispatched" HeaderButtonType="TextButton" DataField="dispatched" UniqueName="dispatched" DataFormatString="{0:N0}" />
                            <tel:GridBoundColumn SortExpression="received" HeaderText="Received" HeaderButtonType="TextButton" DataField="received" UniqueName="received" DataFormatString="{0:N0}" />
                            <tel:GridBoundColumn SortExpression="DispCompleted" HeaderText="Dispatch Completed" HeaderButtonType="TextButton" DataField="DispCompleted" UniqueName="DispCompleted" DataFormatString="{0:N0}" />
                            <tel:GridBoundColumn SortExpression="ReceCompleted" HeaderText="Rcv. Completed" HeaderButtonType="TextButton" DataField="ReceCompleted" UniqueName="ReceCompleted" DataFormatString="{0:N0}" />
                            <tel:GridBoundColumn SortExpression="InputType" HeaderText="Input Type" HeaderButtonType="TextButton" DataField="InputType" UniqueName="InputType" DataFormatString="{0:N0}" />
                            <tel:GridBoundColumn SortExpression="ScanStatus" HeaderText="Scan Status" HeaderButtonType="TextButton" DataField="ScanStatus" UniqueName="ScanStatus" DataFormatString="{0:N0}" />
                            <tel:GridBoundColumn SortExpression="SapDocType" HeaderText="Type" HeaderButtonType="TextButton" DataField="SapDocType" UniqueName="SapDocType" HeaderStyle-Width="45px" ItemStyle-HorizontalAlign="Center" />
                            <tel:GridBoundColumn SortExpression="DocNumITR" HeaderText="DocSAP #" HeaderButtonType="TextButton" DataField="DocNumITR" UniqueName="DocNumITR" DataFormatString="{0:#;-;}" HeaderStyle-Width="65px" ItemStyle-HorizontalAlign="Center" />
                            <tel:GridButtonColumn UniqueName="DocEntry" ButtonType="LinkButton" HeaderText="Dispatch" DataTextField="DocEntry" CommandName="Dispatch" />
                        </Columns>
                    </MasterTableView>
                    <ClientSettings>
                        <Resizing AllowColumnResize="true" />
                        <Selecting AllowRowSelect="true" />
                    </ClientSettings>
                </tel:RadGrid>

                <%--<asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" CssClass="GridViewPanel" AllowSorting="True" DataSourceID="ObjectDataSource1" AllowPaging="True" PageSize="50" OnDataBound="GridView1_DataBound">

                    <RowStyle BackColor="#EEEEEE" ForeColor="Black" />
                    <FooterStyle BackColor="#CCCCCC" ForeColor="Black" />
                    <PagerStyle BackColor="#999999" ForeColor="Black" HorizontalAlign="Center" />
                    <SelectedRowStyle BackColor="#008A8C" Font-Bold="True" ForeColor="White" />
                    <HeaderStyle BackColor="#000084" Font-Bold="True" ForeColor="White" />
                    <AlternatingRowStyle BackColor="Gainsboro" />

                    <Columns>

                        <asp:TemplateField HeaderText="Doc #" SortExpression="DocEntry">
                            <ItemTemplate>
                                <%#"<a href=\"javascript:popUpReport3('TransferDetails.aspx?DocEntry=" + Eval("DocEntry").ToString().Trim() + "')\">" + Eval("DocEntry") + "</a>"%>
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Date" SortExpression="DocDate">
                            <ItemTemplate><%# String.Format("{0:MM/dd/yyyy}", Eval("DocDate"))%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Stat" SortExpression="DocStatus">
                            <ItemTemplate><%#Eval("DocStatus")%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="From" SortExpression="FromLocName">
                            <ItemTemplate><%#Eval("FromLocName")%></ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="To" SortExpression="ToLocName">
                            <ItemTemplate><%#Eval("ToLocName")%></ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Category" SortExpression="Category">
                            <ItemTemplate><%#Eval("Category")%></ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="TotalLines" SortExpression="TotalLines">
                            <ItemTemplate><%#Eval("TotalLines")%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="TotalQty" SortExpression="TotalQty">
                            <ItemTemplate><%#String.Format("{0:#,###}", Eval("TotalQty"))%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="D" SortExpression="dispatched">
                            <ItemTemplate><%#String.Format("{0:#,###}", Eval("dispatched"))%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="R" SortExpression="received">
                            <ItemTemplate><%#String.Format("{0:#,###}", Eval("received"))%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Dc" SortExpression="DispCompleted">
                            <ItemTemplate><%#String.Format("{0:#,###}", Eval("DispCompleted"))%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Rc" SortExpression="ReceCompleted">
                            <ItemTemplate><%#String.Format("{0:#,###}", Eval("ReceCompleted"))%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="It" SortExpression="InputType">
                            <ItemTemplate><%#String.Format("{0:#,###}", Eval("InputType"))%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Is" SortExpression="ScanStatus">
                            <ItemTemplate><%#String.Format("{0:#,###}", Eval("ScanStatus"))%></ItemTemplate>
                            <ItemStyle HorizontalAlign="Center" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Doc #" SortExpression="ReceCompleted">
                            <ItemTemplate>
                                <%#"<a href=\"javascript:openDiscrepWindow('TransferDiscreOrdf.aspx?DocEntry=" + Eval("DocEntry").ToString().Trim() + "')\">" + Eval("DocEntry") +  "</a>"%>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>

                <asp:ObjectDataSource ID="ObjectDataSource1" runat="server" SelectMethod="GetTransferDrafts" TypeName="Transfer">
                    <SelectParameters>
                        <asp:ControlParameter ControlID="StatusRadioButtonList" Name="statusDoc" PropertyName="SelectedValue" Type="String" />
                        <asp:ControlParameter ControlID="txtDocNum" Name="txtDocNum" PropertyName="Text" Type="String" />
                        <asp:ControlParameter ControlID="hfFromDate" Name="FromDateTxt" PropertyName="Value" Type="String" />
                        <asp:ControlParameter ControlID="hfToDate" Name="toDateTxt" PropertyName="Value" Type="String" />
                        <asp:ControlParameter ControlID="drpFromWhsCode" Name="fromLocTxt" PropertyName="Text" Type="String" />
                        <asp:ControlParameter ControlID="drpToWhsCode" Name="toLocTxt" PropertyName="Text" Type="String" />
                        <asp:ControlParameter ControlID="drpItemGroups" Name="categoryTxt" PropertyName="Text" Type="String" />
                        <asp:ControlParameter ControlID="andOrDropDownList1" Name="andOr1" PropertyName="Text" Type="String" />
                        <asp:ControlParameter ControlID="andOrDropDownList2" Name="andOr2" PropertyName="Text" Type="String" />
                        <asp:ControlParameter ControlID="andOrDropDownList3" Name="andOr3" PropertyName="Text" Type="String" />
                        <asp:ControlParameter ControlID="CompanyLabel" Name="companyId" PropertyName="Text" Type="String" />
                    </SelectParameters>
                </asp:ObjectDataSource>--%>
            </div>
        </div>
    </div>
    <div class="row">
        <tel:RadWindow ID="rwTransfers" runat="server" Modal="true" OpenerElementID=""
            Title="" Behaviors="Reload, Move, Resize" VisibleTitlebar="false" VisibleStatusbar="false"
            RegisterWithScriptManager="true" Style="display: inline; overflow: hidden;" ShowContentDuringLoad="false" Top="-10" Left="100"
            OnClientClose="onTransferWindowClose" />
    </div>

    <script type="text/javascript">
        var myHiddenWidth = document.getElementById('<%= hfScreenWidth.ClientID %>');
        var myHiddenHeight = document.getElementById('<%= hfScreenHeight.ClientID %>');

        //alert(screen.width);
        //alert(screen.height);
        //alert(myHiddenHeight.value);

        if (myHiddenWidth) {
            myHiddenWidth.value = window.innerWidth;//screen.width;
        }

        if (myHiddenHeight) {
            myHiddenHeight.value = window.innerHeight;//screen.height;
        }

        function onTransferWindowClose(sender, args) {
            var msg = sender.get_argument ? sender.get_argument() : null;
            if (msg) alert(msg);
            sender.set_argument(null);
            $find("<%= rgHead.ClientID %>").rebind();
        }

        function openDiscrepWindow(url) {
            var win = $find("<%= rwTransfers.ClientID %>");
            win.setWidth(Math.round(window.innerWidth * 0.98));
            win.setHeight(Math.round(window.innerHeight * 0.90));
            win.setUrl(url);
            win.show();
        }

        function displayTransfer() {
            var DocEntry = document.getElementById("<%=txtDocNum.ClientID%>").value;

            if (DocEntry != "") {
                var url = "TransferDetails.aspx?DocEntry=" + DocEntry;
                popUpReport(url);
                return false;
            }
            else {
                return true;
            }
        }
		
		//2021-MAR-25: Agregado para la impresión masiva, por Aldo Reina:
        var radGrid;
        var mt;
        var printSelectedBtn;
        var dropDownListPrinters1;
        var checkBoxPrintAll1;
        var labelPrintAllCopies1;
        var textBoxPrintAllCopies1;
        var rbtnList;
        var checkBoxSelectAllCurrentPageForPrint1;
        var textBoxPrintAllCopiesInfo1;
        window.onload = function () {
            printSelectedBtn = document.getElementById('<%=PrintSelected.ClientID%>');
            dropDownListPrinters1 = document.getElementById('<%=DropDownListPrinters.ClientID%>');
            labelPrintAllCopies1 = document.getElementById('<%=LabelPrintAllCopies.ClientID %>');
            textBoxPrintAllCopies1 = document.getElementById('<%=TextBoxPrintAllCopies.ClientID %>');
            rbtnList = document.getElementById('<%=RadioButtonListType.ClientID %>');
            checkBoxSelectAllCurrentPageForPrint1 = document.getElementById('<%=CheckBoxSelectAllCurrentPageForPrint.ClientID %>');
            textBoxPrintAllCopiesInfo1 = document.getElementById('<%=TextBoxPrintAllCopiesInfo.ClientID %>');

            radGrid = $find('<%=rgHead.ClientID %>');
            mt = radGrid.get_masterTableView();

            $(labelPrintAllCopies1).hide();
            $(textBoxPrintAllCopies1).hide();
            $(textBoxPrintAllCopiesInfo1).hide();
            mt.hideColumn(1);

            if ($("#" + rbtnList.id + " input:checked").val() === "dispatch") {
                mt.showColumn(1);
                if ($(checkBoxSelectAllCurrentPageForPrint1).prop("checked")) {
                    $(labelPrintAllCopies1).show();
                    $(textBoxPrintAllCopies1).show();
                    $(textBoxPrintAllCopiesInfo1).show();
                }
            }

            $(checkBoxSelectAllCurrentPageForPrint1).change(function (e) {
                if (e.target.checked) {
                    if ($("#" + rbtnList.id + " input:checked").val() === "detail") {
                        $(labelPrintAllCopies1).hide();
                        $(textBoxPrintAllCopies1).hide();
                        $(textBoxPrintAllCopiesInfo1).hide();
                    } else {
                        
                        $(labelPrintAllCopies1).show();
                        $(textBoxPrintAllCopies1).show();
                        $(textBoxPrintAllCopiesInfo1).show();
                    }
                } else {
                    $(labelPrintAllCopies1).hide();
                    $(textBoxPrintAllCopies1).hide();
                    $(textBoxPrintAllCopiesInfo1).hide();
                }
            });

            $(rbtnList).change(function (e) {
                if (e.target.value === "detail") {
                    mt.hideColumn(1);
                    $(labelPrintAllCopies1).hide();
                    $(textBoxPrintAllCopies1).hide();
                    $(textBoxPrintAllCopiesInfo1).hide();
                } else {
                    mt.showColumn(1);
                    if ($(checkBoxSelectAllCurrentPageForPrint1).prop("checked")) {
                        $(labelPrintAllCopies1).show();
                        $(textBoxPrintAllCopies1).show();
                        $(textBoxPrintAllCopiesInfo1).show();
                    } else {
                        $(labelPrintAllCopies1).hide();
                        $(textBoxPrintAllCopies1).hide();
                        $(textBoxPrintAllCopiesInfo1).hide();
                    }
                }
            });

            $(printSelectedBtn).css("cursor", "default");
            $(printSelectedBtn).click(function (e) {
                if ($(dropDownListPrinters1).val() === '-1') {
                    alert('Select a printer');
                    return false;
                }
                var dItems = mt.get_dataItems();

                var checkedQty = 0;
                var length = dItems.length;
                for (var i = 0; i < length; i++) {
                    if ($(dItems[i].findElement("CheckBoxPrintDetail")).prop("checked")) {
                        checkedQty++;
                    }
                }

                if (checkedQty === 0) {
                    alert(decodeHtml("Select one or more orders to print"));
                    return false;
                }
                 
                $(e.target).css("cursor", "not-allowed");
                $(e.target).click(function (e1) {
                    return false;
                });
            });

            $(checkBoxSelectAllCurrentPageForPrint1).change(function (e) {
                var dItems = mt.get_dataItems();
                for (var i = 0; i < dItems.length; i++) {
                    $(dItems[i].findElement("CheckBoxPrintDetail")).prop("checked", e.target.checked);
                }
            });
        };

        function decodeHtml(html) {
            var txt = document.createElement("textarea");
            txt.innerHTML = html;
            return txt.value;
        }
        //

    </script>

</asp:Content>

