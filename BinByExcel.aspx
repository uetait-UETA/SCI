<%@ Page Title="Upload Bins by Excel" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="BinByExcel.aspx.cs" Inherits="BinByExcel" %>


<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" runat="Server">

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
                <div id="divMessage" runat="server" />
                <br />
            </div>
        </div>
        <div class="row">
            <div class="col-md-8">
                <asp:Panel ID="pnlAddApps" runat="server" CssClass="page-curl shadow-bottom">
                    <label ID="labelForm" runat="server" class="PanelHeading">Upload Bins by Excel</label>
                    <div class="row">
                        <div class="col-md-8">
                            <ul class="myUL">
                                <li>
                                    <label class="myLabel">Operation</label>
                                    <asp:DropDownList ID="drpFromWhsCode" runat="server" DataTextField="WHS" DataValueField="WhsCode" AutoPostBack="True" CssClass="myDdlExtraLarge" />
                                </li>
                                <li>
                                    <label class="myLabel">Excel File</label>
                                    <asp:FileUpload ID="FileUpload1" runat="server" CssClass="mybtnXlarge" />
                                </li>
                                <li>
                                    <label class="myLabel">&nbsp;</label>
                                    <asp:Button ID="btnCreateDraft" runat="server" Text="Create Bins" OnClick="btnCreateDraft_Click" OnClientClick="return confirm('WARNING!!\n\nAre you sure you want to Create/Update Bins with the selected Excel file?\n\nClick OK to Create or Cancel to abort')" CssClass="mybtnmeduim" />
                                </li>
                            </ul>
                        </div>
                        <div class="col-md-4">
                            <asp:Image runat="server" ID="imgSample" ImageUrl="~/Images/BinesFormat.JPG" Width="200px" Height="100px" />
                        </div>
                    </div>
                    <div class="row">&nbsp;</div>
                </asp:Panel>
            </div>
        </div>
        <div class="row">&nbsp;</div>
        <div class="row">
            <div class="col-md-12">
                <asp:Label ID="Label2" runat="server" Font-Bold="True" Font-Size="Medium" ForeColor="#CC0000"></asp:Label>
            </div>
        </div>
        <div class="row">&nbsp;</div>
        <div class="row">
            <div class="col-md-4">
                <asp:GridView ID="GridView2" runat="server" CssClass="GridViewPanel"
                    EnableModelValidation="False" ForeColor="#333333" EnableTheming="False"
                    EnableViewState="False" HorizontalAlign="Left">
                    <AlternatingRowStyle BackColor="White" ForeColor="#284775" />
                    <EditRowStyle BackColor="#999999" />
                    <FooterStyle BackColor="#CCCCCC" Font-Bold="True" ForeColor="White" />
                    <HeaderStyle BackColor="#000084" Font-Bold="True" ForeColor="White" />
                    <PagerStyle BackColor="#999999" ForeColor="White" HorizontalAlign="Left" />
                    <RowStyle BackColor="#EEEEEE" ForeColor="#333333" HorizontalAlign="Left" />
                    <SelectedRowStyle BackColor="#008A8C" Font-Bold="True" ForeColor="#333333" />

                </asp:GridView>
            </div>
        </div>
    </div>
</asp:Content>

