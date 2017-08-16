<%-- Copyright (C) Spark Development Network - All Rights Reserved --%>
<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BookList.ascx.cs" Inherits="RockWeb.Plugins.org_sparkdevnetwork.Documentation.BookList" %>
<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <Rock:ModalAlert ID="maGridWarning" runat="server" />

        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-book"></i> Book List</h1>
            </div>
            <div class="panel-body">

                <div class="grid grid-panel">
                    <Rock:GridFilter ID="gfSettings" runat="server" OnApplyFilterClick="gfSettings_ApplyFilterClick" OnDisplayFilterValue="gfSettings_DisplayFilterValue">
                        <Rock:CategoryPicker ID="catpCategory" runat="server" Label="Category" EntityTypeName="org.sparkdevnetwork.Documentation.Model.Book" />
                        <Rock:RockDropDownList ID="ddlIsActive" runat="server" Label="Active">
                            <asp:ListItem Value="" Text=" " />
                            <asp:ListItem Value="Yes" Text="Yes" />
                            <asp:ListItem Value="No" Text="No" />
                        </Rock:RockDropDownList>
                    </Rock:GridFilter>

                    <Rock:Grid ID="gBooks" runat="server" AllowSorting="false" RowItemText="Book" OnRowSelected="gBooks_RowSelected" TooltipField="Subtitle">
                        <Columns>
                            <Rock:ReorderField />
                            <asp:BoundField DataField="Name" HeaderText="Title" />
                            <asp:BoundField DataField="Category.Name" HeaderText="Category" />
                            <Rock:BoolField DataField="IsActive" HeaderText="Active" />
                            <Rock:SecurityField TitleField="Name" />
                            <Rock:DeleteField OnClick="gBooks_Delete" />
                        </Columns>
                    </Rock:Grid>
                </div>

            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>