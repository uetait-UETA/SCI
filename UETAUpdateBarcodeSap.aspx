<%@ Page Title="" Language="C#" MasterPageFile="~/SiteMaster.master" AutoEventWireup="true" CodeFile="UETAUpdateBarcodeSap.aspx.cs" Inherits="UETAUpdateBarcodeSap" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">

    <telerik:RadCodeBlock ID="RadCodeBlock1" runat="server">
        <script type="text/javascript">
            function rowDblClick(sender, eventArgs) {
                sender.get_masterTableView().editItem(eventArgs.get_itemIndexHierarchical());
            }
        </script>
    </telerik:RadCodeBlock>

</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="cphMain" Runat="Server">

 

<%--     <telerik:RadAjaxManager runat="server" ID="RadAjaxManager1" DefaultLoadingPanelID="RadAjaxLoadingPanel1">
        <AjaxSettings>
            <telerik:AjaxSetting AjaxControlID="rgHead">
                <UpdatedControls>
                    <telerik:AjaxUpdatedControl ControlID="rgHead" />
                    <telerik:AjaxUpdatedControl ControlID="lblCodSAP"/>
                    <telerik:AjaxUpdatedControl ControlID="lblDesc"/>
                    <telerik:AjaxUpdatedControl ControlID="lblBarCode"/>
                </UpdatedControls>
            </telerik:AjaxSetting>
        </AjaxSettings>
    </telerik:RadAjaxManager>--%>

    <telerik:RadAjaxLoadingPanel runat="server" ID="RadAjaxLoadingPanel1" />
    <telerik:RadFormDecorator RenderMode="Lightweight" ID="RadFormDecorator1" runat="server" DecorationZoneID="demo" DecoratedControls="All" EnableRoundedCorners="false" />

    <div class=" container">
        <div class=" row">
            <div class ="col-md-2"></div>
            <div class="col-md-10" >
                <asp:Panel ID="pnlHeader" runat="server" CssClass="page-curl shadow-bottom">
                   <%-- <telerik:RadLabel ID="lblTitule"  runat="server"  CssClass ="PanelHeading">Actualizar Codigo de Barras por Articulos.</telerik:RadLabel>--%>
                    <label id="lblTitule" runat="server" class="PanelHeading">Update Barcode by Items.</label>
                    <div class="row">
                        <div class="col-md-12">
                            <ul class="myUL">
                                <li>
                                    <telerik:RadLabel CssClass ="myLabel" ID="RadLabel1" runat ="server" >Item Code</telerik:RadLabel>
                                    <telerik:RadTextBox runat="server" OnKeyPress="if (event.keyCode == 13) { <%= this.ClientScript.GetPostBackEventReference(btnSearch, string.Empty) %> }" ID="txtItemCode" RenderMode="Classic"  skin="Windows7"></telerik:RadTextBox>
                                    <telerik:RadButton RenderMode="Classic" ID="btnSearch" OnClick="btnSearck_Click"  runat="server" Text=" Search" Skin ="Windows7">
                                        <Icon PrimaryIconCssClass="rbSearch" PrimaryIconLeft="5" PrimaryIconBottom="11"></Icon>
                                    </telerik:RadButton>
                                    &nbsp;&nbsp;
                                </li>

                            </ul>
                            <ul class="myUL" runat="server" id="ulData" visible="false">
                                <li>
                                    <label class="myLabelXLarge">SAP Code</label>
                                    <label class="myLabelXXXXLarge" runat="server" id="lblCodSAP" style="color: red;"></label>
                                    <label class="myLabelXLarge" visible="false" runat="server" id="lblCodSAP2" style="color: red;"></label>
                                    <label class="myLabelXLarge" runat="server" visible="true" id="test22" style="color: red;"></label>
                                </li>
                                <li>
                                    <label class="myLabelXLarge">Description</label>
                                    <label class="myLabelXXXXLarge" runat="server" id="lblDesc" style="color: red;"></label>
                                </li>
                                <li>
                                    <label class="myLabelXLarge">Default Barcode</label>
                                    <label class="myLabelXXXXLarge" runat="server" id="lblBarCode" style="color: red;"></label>
                                </li>
                                <li>
                                    <label class="myLabelXLarge">Item Type</label>
                                    <label class="myLabelXXXXLarge" runat="server" id="lblItemType" style="color: red;"></label>
                                </li>
                            </ul>
                            <ul class="myUl" runat="server" id="ulGrid" visible ="True" style="margin-left:160px;">

                                <telerik:RadGrid ID="rgHead" runat="server"  Width="90%" ShowStatusBar="true" AutoGenerateColumns="False" AllowSorting="false" 
                                   AllowMultiRowSelection ="False" AllowPaging="true" PageSize="10" CssClass="Panel" Visible="false" Skin ="Silk" ShowFooter ="true" OnPreRender ="rgHead_PreRender"
                                   OnItemCreated="rgHead_ItemCreated"  OnItemDataBound="rgHead_ItemDataBound" OnInsertCommand="rgHead_InsertCommand" OnUpdateCommand ="rgHead_UpdateCommand"  OnDeleteCommand ="rgHead_DeleteCommand" OnNeedDataSource="rgHead_NeedDataSource" >
                                    <PagerStyle Mode="Slider"></PagerStyle>
                                    <SortingSettings EnableSkinSortStyles="false" />

                                    <MasterTableView Width="100%" DataKeyNames="BcdEntry"  AllowNaturalSort="false" CommandItemDisplay="Top"   EditMode="EditForms"  >
                                        <EditFormSettings PopUpSettings-Modal ="False" PopUpSettings-Width="350px"
                                                          EditColumn-CancelText ="Cancel" EditColumn-InsertText ="Save"  EditColumn-UpdateText="Save"   >                                          
                                        </EditFormSettings>

                                         <NoRecordsTemplate>
                                            <div style="font-weight: bold; font-size: 16px; color: Green; width: 100%;">
                                                No barcodes exist for this item.
                                            </div>
                                         </NoRecordsTemplate>

                                        <Columns>
                                            
                                            <telerik:GridEditCommandColumn UniqueName="EditCommandColumn" HeaderStyle-Width="50px" ButtonType="FontIconButton" >
                                            </telerik:GridEditCommandColumn>
                                            <telerik:GridTemplateColumn DataField="BcdEntry" HeaderText="" UniqueName="BcdEntry" Visible ="false"  >
                                                <ItemTemplate>
                                                    <asp:Label ID="idLabel" Visible ="false"  runat="server" Text=""></asp:Label>
                                                </ItemTemplate>
                                                <EditItemTemplate>
                                                    <asp:TextBox runat="server" Visible ="false"  Enabled="false" ID="orderTxt" Text='<%#Bind("BcdEntry") %>'></asp:TextBox>
                                                </EditItemTemplate>
                                            </telerik:GridTemplateColumn>
                                            <telerik:GridHyperLinkColumn DataNavigateUrlFields="url" DataNavigateUrlFormatString="{0}" DataTextField="BcdCode" HeaderText="Barcode" />
                                             <%--<telerik:GridHyperLinkColumn NavigateUrl="https://www.google.com/search?q="  + '<%#Bind("BcdCode") %>' UniqueName="hlBcdCodeTxt" DataNavigateUrlFormatString="{0}" DataTextField="BcdCode" HeaderText="Código de Barra" />--%>
                                              <telerik:GridTemplateColumn DataField="BcdCode" HeaderText="Barcode" UniqueName="BcdCode" Visible ="false" >
                                                 <%-- <ItemTemplate>
                                                    <asp:Label ID="idLabel1" Visible ="true"  runat="server" Text='<%#Eval("BcdCode") %>' CssClass ="text-center" ></asp:Label>
                                                </ItemTemplate>--%>
                                                <EditItemTemplate>
                                                    <asp:TextBox runat="server" Visible ="true"  Enabled="true" ID="BcdCodeTxt" Text='<%#Bind("BcdCode") %>'></asp:TextBox>
                                                </EditItemTemplate>
                                            </telerik:GridTemplateColumn>
                                              <telerik:GridTemplateColumn DataField="ItemCode" HeaderText="Item Code" UniqueName="ItemCode" Visible ="true" >
                                                <ItemTemplate>
                                                    <asp:Label ID="Label1" Visible ="true"  runat="server" Text='<%#Eval("ItemCode") %>' CssClass ="text-center" ></asp:Label>
                                                </ItemTemplate>
                                                <EditItemTemplate>
                                                    <asp:TextBox runat="server" Visible ="true"  Enabled="false" ID="ItemCodeTxt" Text='<%#Bind("ItemCode") %>'></asp:TextBox>
                                                </EditItemTemplate>
                                            </telerik:GridTemplateColumn>
                                            <telerik:GridButtonColumn UniqueName="DeleteColumn" Text="Delete" CommandName="Delete" HeaderStyle-Width="50px" ButtonType="FontIconButton">
                                            </telerik:GridButtonColumn>  
                                        </Columns>
                                        <EditFormSettings>
                                            <EditColumn ButtonType="PushButton"  InsertText ="Save" CancelText="Cancel" />
                                        </EditFormSettings>
                                    </MasterTableView>
                                    <ClientSettings>
                                        <Resizing AllowColumnResize="false" />
                                        <Selecting AllowRowSelect="true" />
                                    </ClientSettings>
                                </telerik:RadGrid>
                            
                            </ul>
                        </div>
                    </div>
                </asp:Panel>


            </div>

        </div>
        <div class="row">&nbsp;</div>

        <div class="row">
            <div class="col-md-12">
                
            </div>
        </div>

        <telerik:RadInputManager RenderMode="Classic" runat="server" ID="RadInputManager1" Enabled="true" Skin="Silk" >
            <telerik:TextBoxSetting BehaviorID="txtBarcodeIdeSettings">
                <TargetControls>
                    <telerik:TargetInput ControlID="rgHead" /> 
                </TargetControls>
            </telerik:TextBoxSetting>
            <telerik:TextBoxSetting BehaviorID="txtBarcodeSettings">
                 <TargetControls>
                    <telerik:TargetInput ControlID="rgHead" /> 
                </TargetControls>
            </telerik:TextBoxSetting>
        
        </telerik:RadInputManager>
        <telerik:RadWindowManager RenderMode="Lightweight" ID="RadWindowManager1" runat="server" />
     
    </div>
</asp:Content>

