<%-- Copyright (C) Spark Development Network - All Rights Reserved --%>
<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BookVersionList.ascx.cs" Inherits="RockWeb.Plugins.org_sparkdevnetwork.Documentation.BookVersionList" %>
<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlContent" CssClass="panel panel-block" runat="server" >

            <Rock:ModalAlert ID="maGridWarning" runat="server" />
            
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-book"></i> Book Versions</h1>
            </div>
            <div class="panel-body">

                <div class="grid grid-panel">
                    <Rock:GridFilter ID="gfSettings" runat="server" OnApplyFilterClick="gfSettings_ApplyFilterClick" OnDisplayFilterValue="gfSettings_DisplayFilterValue">
                        <Rock:RockDropDownList ID="ddlVersion" runat="server" Label="Version" />
                        <Rock:RockDropDownList ID="ddlIsActive" runat="server" Label="Active">
                            <asp:ListItem Value="" Text=" " />
                            <asp:ListItem Value="Yes" Text="Yes" />
                            <asp:ListItem Value="No" Text="No" />
                        </Rock:RockDropDownList>
                    </Rock:GridFilter>

                    <Rock:Grid ID="gVersions" runat="server" AllowSorting="false" RowItemText="Version" OnRowSelected="gVersions_RowSelected" TooltipField="Subtitle">
                        <Columns>
                            <Rock:ReorderField />
                            <asp:BoundField DataField="Name" HeaderText="Versions" />
                            <asp:BoundField DataField="VersionDefinedValue.Value" HeaderText="Rock Version" />
                            <Rock:BoolField DataField="IsActive" HeaderText="Active" />
                            <Rock:DeleteField OnClick="gVersions_Delete" />
                        </Columns>
                    </Rock:Grid>
                </div>

            </div>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>