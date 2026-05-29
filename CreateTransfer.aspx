<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="CreateTransfer.aspx.cs" Inherits="CreateTransfer" %>


<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" Runat="Server">

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

    <div class="container" style="margin-left: 20px;">
        <div class="row">
            <div class="col-md-12">
                <div id="divMessage" runat="server" class="alert-danger" />
                <br />
            </div>
        </div>
        <div class="row">
            <div class="col-md-6">
                <asp:Panel ID="pnlHeader" runat="server" CssClass="page-curl shadow-bottom">
                    <label id="labelForm" runat="server" class="PanelHeading">Create Min-Max Transfer</label>
                    <div class="row">
                        <div class="col-md-12">
                            <ul class="myUL">
                                <li>
                                    <label class="myLabel">From Location</label>
                                    <asp:DropDownList ID="drpFromWhsCode" runat="server" DataTextField="WHS" DataValueField="WhsCode" AutoPostBack="True" CssClass="myDdlLarge" OnSelectedIndexChanged="drpFromWhsCode_SelectedIndexChanged" />
                                </li>
                                <li>
                                    <label class="myLabel">To Location</label>
                                    <asp:DropDownList ID="drpToWhsCode" runat="server" DataTextField="WHS" DataValueField="WhsCode" AutoPostBack="False" CssClass="myDdlLarge" />
                                </li>
                                <li>
                                    <label class="myLabel">Item Groups</label>
                                    <asp:DropDownList ID="drpItemGroups" runat="server" CssClass="myDdlLarge" DataTextField="GroupName" DataValueField="GroupCode" AutoPostBack="True" OnSelectedIndexChanged="drpItemGroups_SelectedIndexChanged" />
                                </li>
                                <li>
                                    <label class="myLabel" style="vertical-align:top;">Brand</label>
                                    <asp:ListBox ID="lstItemGroups" runat="server" CssClass="myDdlLarge" DataTextField="Brand" DataValueField="Brand" Height="105px" /> 
                                </li>
                                <li>
                                    <label class="myLabel">&nbsp;</label>
                                    <asp:Button ID="btnCreateTransfer" CssClass="mybtnlarge" runat="server" Text="Create Transfer" onclick="btnCreateTransfer_Click"  OnClientClick="return confirm('WARNING!!\n\nAre you sure you want to Create this Transfer?\n\nClick OK to Create or Cancel to abort')" onload="btnCreateTransfer_Load"/>
                                </li>
                            </ul>
                        </div>
                    </div>
                    <div class="row">&nbsp;</div>
                </asp:Panel>
            </div>
        </div>
    </div>
</asp:Content>

