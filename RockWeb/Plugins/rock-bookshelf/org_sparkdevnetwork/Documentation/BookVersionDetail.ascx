<%-- Copyright (C) Spark Development Network - All Rights Reserved --%>
<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BookVersionDetail.ascx.cs" Inherits="RockWeb.Plugins.org_sparkdevnetwork.Documentation.BookVersionDetail" %>
<asp:UpdatePanel ID="upnlBooks" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlDetails" CssClass="panel panel-block" runat="server">

            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-book"></i> <asp:Literal ID="lTitle" runat="server" /></h1>
                <div class="panel-labels">
                    <Rock:HighlightLabel ID="hlblInactive" runat="server" LabelType="Danger" Text="Inactive" />
                </div>
            </div>
            <div class="panel-body">

                <Rock:NotificationBox ID="nbEditModeMessage" runat="server" NotificationBoxType="Info" />
                <asp:ValidationSummary ID="valSummary" runat="server" HeaderText="Please Correct the Following" CssClass="alert alert-danger" />

                <div id="pnlEditDetails" runat="server">

                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockTextBox ID="tbName" runat="server" Label="Title" />
                        </div>
                        <div class="col-md-6">
                            <Rock:RockCheckBox ID="cbIsActive" runat="server" Text="Active" />
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockDropDownList ID="ddlVersion" runat="server" Label="Rock Version" />
                        </div>
                        <div class="col-md-6">
                            <Rock:RockTextBox ID="tbPdfUrl" runat="server" Label="PDF Url" />
                            <Rock:RockTextBox ID="tbEBookUrl" runat="server" Label="eBook Url" />
                            <Rock:RockTextBox ID="tbMobiUrl" runat="server" Label="Mobi Url" />
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <Rock:CodeEditor ID="ceUpdateSummary" Label="Update Summary" runat="server" EditorMode="Html" EditorTheme="Rock" EditorHeight="400"></Rock:CodeEditor>
                        </div>
                        <div class="col-md-6 margin-t-md">
                            <pre>&lt;p&gt;Below is a summary of the updates for this version.&lt;/p&gt;
&lt;ul&gt;
    &lt;li&gt;Update 1&lt;/li&gt;
    &lt;li&gt;Update 2&lt;/li&gt;
    &lt;li&gt;Update 3&lt;/li&gt;
&lt;/ul&gt;</pre>
                        </div>
                    </div>

                    <div class="actions">
                        <asp:LinkButton ID="lbSave" runat="server" Text="Save" CssClass="btn btn-primary" OnClick="lbSave_Click" />
                        <asp:LinkButton ID="lbCancel" runat="server" Text="Cancel" CssClass="btn btn-link" CausesValidation="false" OnClick="lbCancel_Click" />
                    </div>

                </div>

                <fieldset id="fieldsetViewDetails" runat="server">

                    <asp:Literal ID="lMainDetails" runat="server" />

                    <div class="actions">
                        <asp:LinkButton ID="lbEdit" runat="server" Text="Edit" CssClass="btn btn-primary" OnClick="lbEdit_Click" CausesValidation="false" />
                        <Rock:ModalAlert ID="maDeleteWarning" runat="server" />
                        <asp:LinkButton ID="lbDelete" runat="server" Text="Delete" CssClass="btn btn-link" OnClick="lbDelete_Click" CausesValidation="false" />
                        <asp:LinkButton ID="lbCopy" runat="server" Text="Copy" CssClass="btn btn-link btn-sm" OnClick="lbCopy_Click" CausesValidation="false" />
                    </div>

                </fieldset>

            </div>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>