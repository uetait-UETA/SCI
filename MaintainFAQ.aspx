<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="MaintainFAQ.aspx.cs" Inherits="MaintainFAQ" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="tel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">
    <div class="container-fluid">
        <div class="row">
            <div class="col-md-5">
                <asp:Panel ID="pnlAddApps" runat="server" CssClass="Panel" Height="210px">
                    <label class="PanelHeading">Add FAQs</label>
                    <div class="row">
                        <div class="col-md-12">
                            <ul class="myUL">
                                <li>
                                    <label class="myLabel">Cases</label>
                                    <tel:RadTextBox ID="rtbCasos" runat="server" Width="300px" />
                                </li>
                                <li>
                                    <label class="myLabel">Solutions</label>
                                    <tel:RadTextBox ID="rtbSoluciones" runat="server" Width="300px" />
                                </li>
                                <li>
                                    <label class="myLabel">Solution Link</label>
                                    <tel:RadTextBox ID="rtbSolucionLink" runat="server" Width="300px" />
                                </li>
                                <li>
                                    <label class="myLabel">Upload</label>
                                    <asp:FileUpload runat="server" ID="fuDocs" CssClass="btn btn-warning btn-sm" />
                                </li>
                                <li>&nbsp;
                                </li>
                                <li>
                                    <label class="myLabel">&nbsp;</label>
                                    <tel:RadButton ID="rbtnAddFaq" runat="server" Text="Add" OnClick="rbtnAddFaq_Click" ToolTip="Add">
                                        <Icon SecondaryIconCssClass="fa fa-plus icon-green" SecondaryIconRight="4" SecondaryIconTop="5" />
                                    </tel:RadButton>
                                </li>
                            </ul>
                        </div>
                    </div>
                </asp:Panel>
            </div>
            <div class="col-md-5">
                <asp:Panel ID="Panel1" runat="server" CssClass="Panel" Height="210px">
                    <label class="PanelHeading">FAQs About Updates</label>
                    <div class="row">
                        <div class="col-md-12">
                            <ul class="myUL">
                                <li>
                                    <label class="myLabel">Select Cases</label>
                                    <tel:RadComboBox ID="rcbCasos" runat="server" Height="120px" DropDownAutoWidth="Disabled" Width="300px"
                                        HighlightTemplatedItems="true"
                                        AppendDataBoundItems="true"
                                        EmptyMessage="Select Cases" AutoPostBack="true" OnSelectedIndexChanged="rcbCasos_SelectedIndexChanged"
                                        Font-Italic="false">
                                        <ExpandAnimation Type="OutQuart" Duration="500" />
                                        <CollapseAnimation Type="OutQuint" Duration="300" />
                                    </tel:RadComboBox>
                                </li>
                                <li>
                                    <label class="myLabel">Cases</label>
                                    <tel:RadTextBox ID="rtbUCasos" runat="server" Width="300px" />
                                </li>
                                <li>
                                    <label class="myLabel">Solutions</label>
                                    <tel:RadTextBox ID="rtbUSoluciones" runat="server" Width="300px" />
                                </li>
                                <li>
                                    <label class="myLabel">Solution Link</label>
                                    <tel:RadTextBox ID="rtbUSolucionLink" runat="server" Width="300px" />
                                </li>
                                <li>
                                    <label class="myLabel">Upload</label>
                                    <asp:FileUpload runat="server" ID="fuUDocs" CssClass="btn btn-warning btn-sm" />
                                </li>
                                <li>&nbsp;
                                </li>
                                <li>
                                    <label class="myLabel">&nbsp;</label>
                                    <tel:RadButton ID="rtbUpdate" runat="server" Text="Update" OnClick="rtbUpdate_Click" ToolTip="Update">
                                        <Icon SecondaryIconCssClass="fa fa-edit icon-green" SecondaryIconRight="4" SecondaryIconTop="5" />
                                    </tel:RadButton>
                                    <tel:RadButton ID="rtbDelete" runat="server" Text="Delete" OnClick="rtbDelete_Click" ToolTip="Delete">
                                        <Icon SecondaryIconCssClass="fa fa-trash-o icon-red" SecondaryIconRight="4" SecondaryIconTop="5" />
                                    </tel:RadButton>
                                </li>
                            </ul>
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </div>
    </div>
</asp:Content>

