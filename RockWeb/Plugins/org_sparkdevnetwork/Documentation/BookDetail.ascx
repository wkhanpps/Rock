<%-- Copyright (C) Spark Development Network - All Rights Reserved --%>
<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BookDetail.ascx.cs" Inherits="RockWeb.Plugins.org_sparkdevnetwork.Documentation.BookDetail" %>
<asp:UpdatePanel ID="upnlBooks" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlDetails" CssClass="panel panel-block" runat="server">
            <asp:HiddenField ID="hfBookId" runat="server" />

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
                        <div class="col-md-12">
                            <Rock:RockTextBox ID="tbSubtitle" runat="server" TextMode="MultiLine" Rows="4" Label="Subtitle" />
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-12">
                            <Rock:CodeEditor ID="tbDescription" runat="server" TextMode="MultiLine" Rows="4" Label="Summary" />
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-6">
                            <Rock:CategoryPicker ID="catpCategory" runat="server" Label="Category" EntityTypeName="org.sparkdevnetwork.Documentation.Model.Book"  />
                        </div>
                        <div class="col-md-6">
                            <Rock:ImageUploader ID="imgupCover" runat="server" Label="Cover" BinaryFileTypeGuid="49CB1110-FD4B-428E-A597-84C1659C13E0" />
                        </div>
                    </div>

                    <div class="actions">
                        <asp:LinkButton ID="lbSave" runat="server" Text="Save" CssClass="btn btn-primary" OnClick="lbSave_Click" />
                        <asp:LinkButton ID="lbCancel" runat="server" Text="Cancel" CssClass="btn btn-link" CausesValidation="false" OnClick="lbCancel_Click" />
                    </div>

                </div>

                <fieldset id="fieldsetViewDetails" runat="server">

                    <div class="row">
                        <div class="col-md-2">
                            <asp:Literal ID="lBookCover" runat="server" />
                        </div>
                        <div class="col-md-10">
                            <strong>Subtitle</strong>
                            <p class="description">
                                <asp:Literal ID="lBookSubtitle" runat="server"></asp:Literal>
                            </p>

                            <strong>Summary</strong>
                            <p class="description">
                                <asp:Literal ID="lBookDescription" runat="server"></asp:Literal>
                            </p>

                            <asp:Literal ID="lMainDetails" runat="server" />

                        </div>
                    </div>

                    <div class="actions">
                        <asp:LinkButton ID="lbEdit" runat="server" Text="Edit" CssClass="btn btn-primary" OnClick="lbEdit_Click" CausesValidation="false" />
                        <Rock:ModalAlert ID="maDeleteWarning" runat="server" />
                        <asp:LinkButton ID="lbDelete" runat="server" Text="Delete" CssClass="btn btn-link" OnClick="lbDelete_Click" CausesValidation="false" />
                        <Rock:SecurityButton ID="sbtnSecurity" runat="server" class="btn btn-security btn-sm pull-right" />
                    </div>

                </fieldset>

            </div>
            
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>