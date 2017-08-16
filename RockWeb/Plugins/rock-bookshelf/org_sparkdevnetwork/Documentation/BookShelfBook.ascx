<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BookShelfBook.ascx.cs" Inherits="RockWeb.Plugins.org_sparkdevnetwork.Documentation.BookShelfBook" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Literal ID="lMessage" runat="server"></asp:Literal>
        <asp:Panel ID="pnlDetails" runat="server">
            <div class="row">
                <div class="col-md-6 bookshelf-book">
                    <asp:HyperLink ID="hlCoverLink" runat="server">
                        <div class="img-content">
                            <asp:Image ID="imgCover" runat="server" />
                        </div>
                    </asp:HyperLink>
                </div>
                <div class="col-md-6">
                    <h3>What's Inside...</h3>
                    <p><asp:Literal ID="lSummary" runat="server"></asp:Literal></p>
                    <asp:HyperLink ID="hlReadOnline" Text="Read Online" runat="server" Visible="false" CssClass="btn btn-primary" />
                    <asp:HyperLink ID="hlPdf" Text="PDF" runat="server" CssClass="btn btn-link" Visible="false" />
                    <asp:HyperLink ID="hlEpub" Text="ePub" runat="server" CssClass="btn btn-link" Visible="false" />
                    <asp:HyperLink ID="hlMobi" Text="mobi" runat="server" CssClass="btn btn-link" Visible="false" />
                </div>
            </div>
        </asp:Panel>       

    </ContentTemplate>
</asp:UpdatePanel>
