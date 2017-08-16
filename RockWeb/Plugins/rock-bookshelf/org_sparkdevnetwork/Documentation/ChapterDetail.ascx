<%-- Copyright (C) Spark Development Network - All Rights Reserved --%>
<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ChapterDetail.ascx.cs" Inherits="RockWeb.Plugins.org_sparkdevnetwork.Documentation.ChapterDetail" %>

<asp:UpdatePanel ID="upnlDetail" runat="server">
    <ContentTemplate>

        <asp:HiddenField ID="hfVersionId" runat="server" />
        <asp:HiddenField ID="hfChapterId" runat="server" />

        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-book"></i> Chapter: <asp:Literal ID="lReadOnlyTitle" runat="server" /></h1>
                <div class="panel-labels">
                    <Rock:HighlightLabel ID="hlblInactive" runat="server" LabelType="Danger" Text="Inactive" />
                </div>
            </div>
            <div class="panel-body">

                <asp:ValidationSummary ID="valSummary" runat="server" HeaderText="Please Correct the Following" CssClass="alert alert-danger" />

                <div id="pnlEditDetails" runat="server">

                    <fieldset>

                        <div class="row">
                            <div class="col-md-6">
                                <Rock:RockTextBox ID="tbChapterName" runat="server" Label="Title"  />
                            </div>
                            <div class="col-md-6">
                                <Rock:RockCheckBox ID="cbIsActive" runat="server" Text="Active" />
                            </div>
                        </div>

                        <div class="row">
                            <div class="col-md-12">
                                <Rock:RockTextBox ID="tbDescription" runat="server" TextMode="MultiLine" Rows="3" Label="Summary" />
                        
                                <div class="documentation-markuptips" style="display: none;">
                            
                                    <Rock:CodeEditor ID="ceTips" runat="server" EditorHeight="300" EditorMode="Html" EditorTheme="Rock">&lt;!-- Sub Sections --&gt;

&lt;!-- First Level --&gt;
&lt;section data-type=&quot;sect1&quot;&gt;

    &lt;p&gt;Now is the time for all good men...&lt;/p&gt;

    &lt;!-- Second Level --&gt;
    &lt;section data-type=&quot;sect2&quot;&gt;
        &lt;h2&gt;Sub Heading&lt;/h2&gt;
        &lt;p&gt;I just had to have a sub-section..&lt;/p&gt;

        &lt;!-- Third Level --&gt;
        &lt;section data-type=&quot;sect3&quot;&gt;
            &lt;h3&gt;Sub Sub Heading&lt;/h3&gt;
            &lt;p&gt;Seriously, just because you can doesn&#39;t mean you should...&lt;/p&gt;
        &lt;/section&gt;

    &lt;/section&gt;
&lt;/section&gt;

&lt;!-- Named Anchors --&gt;
&lt;a name=&quot;link-name&quot; data-summary=&quot;summary of what the link is used for...&quot;&gt;&lt;/a&gt;

&lt;!-- Images / Figures --&gt;
&lt;figure&gt;
    &lt;figcaption&gt;Here Are Your Tools&lt;/figcaption&gt;
    &lt;img src=&quot;{ImagePath}/1/1.0.0/images/chart-of-tools.jpg&quot; alt=&quot;Chart of available tools&quot; /&gt;
    
    &lt;!-- optional call-out markup, note that the dd element is an optional description --&gt;
    &lt;dl class=&quot;callout-key&quot;&gt;
        &lt;dt&gt;&lt;span class=&quot;callout-number primary-color&quot;&gt;1&lt;/span&gt; Photo&lt;/dt&gt;
        &lt;dd&gt;Shows a photo of the current person. If no photo is availabled a gender appropriate   
            anonymous photo will be shown.&lt;/dd&gt;
            	
            &lt;dt&gt;&lt;span class=&quot;callout-number primary-color&quot;&gt;2&lt;/span&gt; Tags&lt;/dt&gt;
            &lt;dd&gt;Tag list for the current person.&lt;/dd&gt;
            	
            &lt;dt&gt;&lt;span class=&quot;callout-number primary-color&quot;&gt;3&lt;/span&gt; Contact Information&lt;/dt&gt;
            &lt;dd&gt;Phone numbers and email address.&lt;/dd&gt;
    &lt;/dl&gt;
                                                
    &lt;!-- link is:
            {ImagePath} - will be substituted by code to the base path to the images
            1 - the document id
            1.0.0 - the semantic version   --&gt;
&lt;/figure&gt;

&lt;!-- Action Labels --&gt;
&lt;!-- A native OS button press (used primarily for setting up Rock) --&gt;
Press &lt;span data-type=&quot;action&quot; data-action=&quot;action-os-button&quot;&gt;OK&lt;/span&gt; when complete...

&lt;!-- A Rock specific button press (used in the Rock webapp) --&gt;
Press the &lt;span data-type=&quot;action&quot; data-action=&quot;action-rock-button&quot;&gt;Done&lt;/span&gt; button to save your work...

&lt;!-- Navigating a user through down a menu structure path --&gt;
To add a person to a new security role go to &lt;span data-type=&quot;action&quot; data-action=&quot;action-navigation&quot;&gt;Administration &gt; General Settings &gt; Security Roles&lt;/span&gt; ...

&lt;!-- Highlighted References --&gt;
&lt;!-- Reference A Specific Page --&gt;
The &lt;em data-type=&quot;reference&quot; data-reference=&quot;reference-page&quot;&gt;Person Profil&lt;/em&gt; contains...

&lt;!-- Generic Highlighted Reference --&gt;
A person&#39;s &lt;em data-type=&quot;reference&quot; data-reference=&quot;reference-highlight&quot;&gt;Baptism Date&lt;/em&gt; is listed on...

&lt;!-- A reference to another Rock manual --&gt;
For more information see the &lt;span data-type=&quot;action&quot; data-action=&quot;action-document-ref&quot;&gt;Admin Hero Guide&lt;/span&gt; .

&lt;!-- Tables --&gt;
&lt;table class=&quot;table table-bordered&quot;&gt;
    &lt;caption&gt;Table Caption&lt;/caption&gt;
    &lt;thead&gt;
        &lt;tr&gt;
            &lt;th&gt;First&lt;/th&gt;
            &lt;th&gt;Middle Initial&lt;/th&gt;
            &lt;th&gt;Last&lt;/th&gt;
        &lt;/tr&gt;
    &lt;/thead&gt;
    &lt;tbody&gt;
        &lt;tr&gt;
                &lt;td&gt;Alfred&lt;/td&gt;
                &lt;td&gt;E.&lt;/td&gt;
                &lt;td&gt;Newman&lt;/td&gt;
        &lt;/tr&gt;
    &lt;/tbody&gt;
&lt;/table&gt;

&lt;!-- Notes / Callouts --&gt;
&lt;div data-type=&quot;note&quot; data-note-type=&quot;tip, warning, note, remember, example, code, learn-more&quot;&gt;
    &lt;h1&gt;Helpful Info&lt;/h1&gt;
    &lt;p&gt;Just a note...&lt;/p&gt;
&lt;/div&gt;

&lt;!-- Data List --&gt;
&lt;dl&gt;
    &lt;dt&gt;Definition List Title&lt;/dt&gt;
    &lt;dd&gt;Definition List Data&lt;/dd&gt;
&lt;/dl&gt;

&lt;!-- Block Quote --&gt;
&lt;blockquote data-type=&quot;epigraph&quot;&gt;
        &lt;p&gt;When in the course of human events...&lt;/p&gt;
        &lt;p data-type=&quot;attribution&quot;&gt;U.S. Declaration of Independence&lt;/p&gt;
&lt;/blockquote&gt;

&lt;!-- Code Example --&gt;
&lt;div data-type=&quot;example&quot;&gt;
    &lt;h5&gt;Hello World in Python&lt;/h5&gt;
    &lt;pre data-type=&quot;programlisting&quot; data-code-language=&quot;&lt;python, csharp, html, aspx&gt;&quot;&gt;print &quot;Hello World&quot;&lt;/pre&gt;
&lt;/div&gt;

&lt;!-- Video --&gt;
&lt;video id=&quot;asteroids_video&quot; width=&quot;480&quot; height=&quot;270&quot; controls=&quot;controls&quot; poster=&quot;&gt;&gt;/1/0/7/images/fallback_image.png&quot;&gt;
    &lt;source src=&quot;video/html5_asteroids.mp4&quot; type=&quot;video/mp4&quot; /&gt;
    &lt;source src=&quot;video/html5_asteroids.ogg&quot; type=&quot;video/ogg&quot; /&gt;
    &lt;em&gt;
        Sorry, this video element is not supported in your
        reading system. View the video online at http://example.com.
    &lt;/em&gt;
&lt;/video&gt;

                                    </Rock:CodeEditor>
                                </div>
                        
                                <div class="clearfix">
                                    <div class="pull-left">
                                        <span class="label label-default"><asp:Literal ID="lVersionTip" runat="server"></asp:Literal></span>
                                    </div>
                                    <a class="btn btn-action btn-xs pull-right documentation-markupshow" >Show Tips</a><br />
                                </div>
                                
                                
                                <Rock:CodeEditor ID="ceContent" runat="server" EditorMode="Html" Label="Content" EditorHeight="800" />
                            </div>
                        </div>
                
                    </fieldset>

                    <asp:Panel ID="pnlOverwriteNotice" runat="server" Visible="false" CssClass="alert alert-danger">
                        <h4>Warning, Potential Data Loss!</h4>
                        <p>While you were editing this chapter, <asp:Literal ID="lOverwriteUser" runat="server" /> made changes to the same chapter. 
                        If you continue with your save, you will overwrite the changes they made. We recommend instead that you make a copy of your changes, cancel your save, 
                        edit the chapter again (which will then include their changes) and then re-apply your changes.  However, if you are determined to overwrite their changes, 
                        you can type 'OVERWRITE' in the textbox below and click 'Save' again to save your changes.</p>
                        <br />
                        <div class="row">
                            <div class="col-md-6">
                                <Rock:RockTextBox ID="tbOverwrite" runat="server" Label="Overwrite?" />
                            </div>
                            <div class="col-md-6">
                            </div>
                        </div>
                    </asp:Panel>

                    <div class="actions">
                        <asp:LinkButton ID="lbSave" runat="server" Text="Save" CssClass="btn btn-primary" OnClick="lbSave_Click" ></asp:LinkButton>
                        <asp:LinkButton ID="lbCancel" runat="server" Text="Cancel" CssClass="btn btn-link" OnClick="lbCancel_Click" CausesValidation="false"></asp:LinkButton>
                    </div>

                </div>

            </div>
        </div>

        

        <script>
            function pageLoad() {
                $(".documentation-markupshow").click(function (e) {
                    e.preventDefault();
                    $('.documentation-markuptips').slideToggle();

                    if ($(this).text() == 'Show Tips') {
                        $(this).text('Hide Tips');
                    } else {
                        $(this).text('Show Tips');
                    }
                });
            }

            
        </script>

    </ContentTemplate>
</asp:UpdatePanel>
