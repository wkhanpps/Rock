<%-- Copyright (C) Spark Development Network - All Rights Reserved --%>
<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ChapterList.ascx.cs" Inherits="RockWeb.Plugins.org_sparkdevnetwork.Documentation.ChapterList" %>
<asp:UpdatePanel ID="upnlChapters" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlContent" CssClass="panel panel-block" runat="server">
            <div id="pnlChapters" runat="server">
                <asp:HiddenField ID="hfBookId" runat="server" />
                <Rock:ModalAlert ID="maGridWarning" runat="server" />

                <div class="panel-heading">
                    <h1 class="panel-title"><i class="fa fa-book"></i> Chapter List</h1>
                </div>
                <div class="panel-body">

                    <div class="grid grid-panel">
                        <Rock:GridFilter ID="gfSettings" runat="server" OnApplyFilterClick="gfSettings_ApplyFilterClick">
                            <Rock:RockDropDownList ID="ddlIsActive" runat="server" Label="Active">
                                <asp:ListItem Value="" Text=" " />
                                <asp:ListItem Value="Yes" Text="Yes" />
                                <asp:ListItem Value="No" Text="No" />
                            </Rock:RockDropDownList>
                        </Rock:GridFilter>

                        <Rock:Grid ID="gChapters" runat="server" AllowSorting="false" RowItemText="Chapter" OnRowSelected="gChapters_RowSelected" TooltipField="Description">
                            <Columns>
                                <Rock:ReorderField />
                                <asp:BoundField DataField="Name" HeaderText="Title" />
                                <Rock:BoolField DataField="IsActive" HeaderText="Active" />
                                <Rock:DeleteField OnClick="gChapters_Delete" />
                            </Columns>
                        </Rock:Grid>
                    </div>

                </div>
            </div>
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
