//
// THIS WORK IS LICENSED UNDER A CREATIVE COMMONS ATTRIBUTION-NONCOMMERCIAL-
// SHAREALIKE 3.0 UNPORTED LICENSE:
// http://creativecommons.org/licenses/by-nc-sa/3.0/
//
using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using org.sparkdevnetwork.Documentation.Data;
using org.sparkdevnetwork.Documentation.Model;
using Rock;
using Rock.Web;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.UI;
using ColorCode;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock.Security;

namespace RockWeb.Plugins.org_sparkdevnetwork.Documentation
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName( "Book Shelf Book Contents" )]
    [Category( "Spark > Documentation" )]
    [Description( "Displays the contents of a book to the public." )]

    [TextField( "Image Base URL", "The base URL where images are stored. This value will replace {ImagePath} in the HTML. Example: http://storage.rockrms.com/documentation/Books", false )]
    [IntegerField( "Cover Image Width", "Width in pixels to load the cover image as (default 710px.)", false, 710 )]
    [IntegerField( "Cover Image Height", "Height in pixels to load the cover image as (default 710px.)", false, 919 )]

    [LinkedPage( "Issue Page", "Page to link to report issues", false )]
    [WorkflowTypeField("Issue Workflow Type", "The workflow type to launch to report the issue.")]
    public partial class BookShelfBookContents : Rock.Web.UI.RockBlock
    {
        #region Fields

        private BookVersion _bookVersion = null;

        #endregion

        #region Properties

        // used for public / protected properties

        #endregion

        #region Base Control Methods

        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            RockPage.AddCSSLink( "~/Plugins/org_sparkdevnetwork/Documentation/Styles/documentation.css" );
            RockPage.AddScriptLink( "~/Plugins/org_sparkdevnetwork/Documentation/Scripts/jquery.sticky.js" );

            RockPage.AddCSSLink( ResolveRockUrl( "~/Styles/fluidbox.css" ) );
            RockPage.AddScriptLink( ResolveRockUrl( "~/Scripts/imagesloaded.min.js" ) );
            RockPage.AddScriptLink( ResolveRockUrl( "~/Scripts/jquery.fluidbox.min.js" ) );
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !IsPostBack )
            {
                if ( _bookVersion != null )
                {
                    DisplayBook();
                }
                else
                {
                    pnlDetails.Visible = false;
                    lMessage.Text = "<div class='alert alert-danger'><strong>Error</strong> No valid book was selected</div>";
                }
            }
        }

        public override System.Collections.Generic.List<BreadCrumb> GetBreadCrumbs( Rock.Web.PageReference pageReference )
        {
            var breadcrumbs = base.GetBreadCrumbs( pageReference );

            _bookVersion = GetVersion(
                ( PageParameter( "book" ).AsInteger() ),
                ( PageParameter( "version" ).AsIntegerOrNull() ) );

            if ( _bookVersion != null )
            {
                string bookName = _bookVersion.Book.Name;
                RockPage.Title = bookName;
                RockPage.BrowserTitle = bookName;
                breadcrumbs.Add( new BreadCrumb( bookName, pageReference ) );
            }

            return breadcrumbs;
        }

        #endregion

        #region Events

        // handlers called by the controls on your block

        /// <summary>
        /// Handles the BlockUpdated event of the PageLiquid control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            //CurrentPageReference.R
            //this.ddlBookVersions.SelectedValue;
        }

        protected void ddlBookVersions_SelectedIndexChanged( object sender, EventArgs e )
        {
            // redirect to the new version
            var parms = new Dictionary<string, string>();
            parms.Add( "Book", _bookVersion.Book.Id.ToString() );
            parms.Add( "Version", ddlBookVersions.SelectedValue );
            Response.Redirect( new PageReference( CurrentPageReference.PageId, CurrentPageReference.RouteId, parms ).BuildUrl(), false );
        }

        #endregion

        #region Methods

        private BookVersion GetVersion( int bookId, int? versionId )
        {
            var qry = new BookVersionService( new DocumentationContext() ).Queryable( "Book,Chapters" );

            if ( versionId.HasValue )
            {

                var bookVersion = qry
                    .Where( v =>
                        v.BookId == bookId &&
                        v.Id == versionId.Value )
                    .FirstOrDefault();

                if ( bookVersion != null )
                {
                    return bookVersion;
                }

            }

            return qry
                .Where( v =>
                    v.BookId == bookId )
                .OrderByDescending( v => v.IsActive )
                .ThenByDescending( v => v.VersionDefinedValue.Order )
                .ThenBy( v => v.Order )
                .FirstOrDefault();
        }

        private void DisplayBook()
        {
            if ( _bookVersion != null )
            {
                var book = _bookVersion.Book;

                bool viewAllowed = _bookVersion.Book.IsAuthorized( Authorization.VIEW, CurrentPerson );

                if ( viewAllowed )
                {

                    lTitle.Text = book.Name;
                    lSubtitle.Text = book.Subtitle;
                    imgCover.ImageUrl = String.Format( "/GetImage.ashx?id={0}&width={1}&height={2}", book.CoverBinaryFileId, GetAttributeValue( "CoverImageWidth" ), GetAttributeValue( "CoverImageHeight" ) ); // todo change to use url

                    // setup version controls
                    lBookVersion.Text = _bookVersion.VersionDefinedValue.Description;

                    ddlBookVersions.DataSource = book.Versions.Where( v => v.IsActive == true )
                                                    .OrderByDescending( v => v.VersionDefinedValue.Order )
                                                    .Select( v => new { Id = v.Id, Name = v.VersionDefinedValue.Description } );
                    ddlBookVersions.DataTextField = "Name";
                    ddlBookVersions.DataValueField = "Id";
                    ddlBookVersions.DataBind();
                    ddlBookVersions.SelectedValue = _bookVersion.Id.ToString();

                    // hide the version selector if there is only one version to show
                    if ( ddlBookVersions.Items.Count == 1 )
                    {
                        divBookVersionSelector.Visible = false;
                    }

                    // create a javascript array of versions in order
                    StringBuilder jsVersions = new StringBuilder();

                    foreach ( var version in book.Versions.Where( v => v.VersionDefinedValue.Order <= _bookVersion.VersionDefinedValue.Order && v.IsActive == true )
                                                                .OrderByDescending( v => v.VersionDefinedValue.Order ) )
                    {
                        jsVersions.Append( String.Format( "'{0}',", version.VersionDefinedValue.Value ) );
                    }

                    lVersionScript.Text = String.Format( "<script>var bookVersionOrder = [ {0} ];</script>", jsVersions.ToString().TrimEnd( ',' ) );

                    ddlVersionsSince.DataSource = book.Versions.Where( v => v.VersionDefinedValue.Order < _bookVersion.VersionDefinedValue.Order && v.IsActive == true )
                                                                .OrderByDescending( v => v.VersionDefinedValue.Order )
                                                                .Select( v => new { Id = v.VersionDefinedValue.Value, Name = v.VersionDefinedValue.Description } );
                    ddlVersionsSince.DataTextField = "Name";
                    ddlVersionsSince.DataValueField = "Id";
                    ddlVersionsSince.DataBind();
                    ddlVersionsSince.Items.Insert( 0, new ListItem( "", "" ) );
                    ddlVersionsSince.SelectedIndex = 0;

                    if ( ddlVersionsSince.Items.Count == 1 )
                    {
                        ddlVersionsSince.Visible = false;
                    }

                    // add update summaries
                    StringBuilder sbUpdateSummaries = new StringBuilder();
                    foreach ( var bkVersion in book.Versions.Where( v => v.IsActive == true ) )
                    {
                        if ( !string.IsNullOrWhiteSpace( bkVersion.UpdateSummary ) )
                        {
                            sbUpdateSummaries.Append( String.Format( "<div data-type='update-summary' data-update-tag='{0}' class='bookversion-updatesummary alert alert-info'><h4>Updates for {1}</h4>{2}</div>", bkVersion.VersionDefinedValue.Value, bkVersion.VersionDefinedValue.Description, bkVersion.UpdateSummary ) );
                        }
                        else
                        {
                            sbUpdateSummaries.Append( String.Format( "<div data-type='update-summary' data-update-tag='{0}' class='bookversion-updatesummary alert alert-info'><h4>Updates for {1}</h4>No updates made.</div>", bkVersion.VersionDefinedValue.Value, bkVersion.VersionDefinedValue.Description ) );
                        }
                    }
                    lUpdateSummaries.Text = sbUpdateSummaries.ToString();

                    // setup reporting tool
                    if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "IssuePage" ) ) && !string.IsNullOrWhiteSpace( GetAttributeValue( "IssueWorkflowType" ) ) )
                    {
                        var issuePageParams = new Dictionary<string, string>();
                        var issuePage = LinkedPageUrl( "IssuePage", issuePageParams );

                        int workflowId = new WorkflowTypeService( new Rock.Data.RockContext() ).Get( new Guid( GetAttributeValue( "IssueWorkflowType" ) ) ).Id;


                        string issueLinkScriptFormat = @" <script> 
                                                Sys.Application.add_load(function () {{
                                                    $('.report-issue').click(function() {{
                                                            var s = $(window).scrollTop(),
                                                                    d = $(document).height(),
                                                                    c = $(window).height();

                                                            scrollPercent = (s / (d - c)) * 100;

                                                            var windowWidth = $(window).width();
                                                            window.location.assign('{0}?WorkflowTypeId={1}&BookId={2}&BookVersionId={3}&ScrollPercentage=' + scrollPercent + '&WindowWidth=' + windowWidth );
                                                        }});
                                                 }});
                                            </script>";
                        string issueLinkScript = string.Format(
                                                    issueLinkScriptFormat,
                                                    issuePage,
                                                    workflowId.ToString(),
                                                    book.Id.ToString(),
                                                    _bookVersion.Id.ToString() );

                        ScriptManager.RegisterStartupScript( pnlDetails, pnlDetails.GetType(), "issue-script", issueLinkScript, false );

                    }
                    else
                    {
                        divReportIssue.Visible = false;
                    }

                    // add content to page
                    StringBuilder bookContent = new StringBuilder();

                    foreach ( Chapter chapter in _bookVersion.Chapters.Where( c => c.IsActive == true ).OrderBy( c => c.Order ) )
                    {
                        lToc.Text += String.Format( "<li><a href='#{0}'>{1}</a></li>", RemoveSpecialCharacters( chapter.Name.Replace( ' ', '-' ).ToLower() ), chapter.Name );

                        // add chapter anchors
                        HtmlDocument chapterHtml = new HtmlDocument();
                        chapterHtml.LoadHtml( chapter.Content );

                        HtmlNode chapterContent = chapterHtml.DocumentNode.SelectSingleNode( "/section[@data-type='chapter']" );
                        if ( chapterContent != null )
                        {
                            chapterContent.InnerHtml = String.Format( "<h1><a name='{0}' href='#{0}' class='book-link'><i class='fa fa-link'></i></a>{1}</h1>", RemoveSpecialCharacters( chapter.Name.Replace( ' ', '-' ).ToLower() ), chapter.Name ) + chapterContent.InnerHtml;
                            //chapterTitle.SetAttributeValue("class", "title");
                        }

                        // add section anchors
                        var nodeTitles = chapterHtml.DocumentNode.SelectNodes( "//section[starts-with(@data-type, 'sect')]/h1" );
                        if ( nodeTitles != null )
                        {
                            foreach ( HtmlNode sectionNodeTitle in nodeTitles )
                            {
                                string anchorText = String.Format( "<a name='{0}' href='#{0}' class='book-link'><i class='fa fa-link'></i></a>", RemoveSpecialCharacters( sectionNodeTitle.InnerHtml.Replace( ' ', '-' ).ToLower() ) );
                                sectionNodeTitle.InnerHtml = anchorText + sectionNodeTitle.InnerHtml;
                            }
                        }

                        // add anchors for fluidbox
                        var nodeImages = chapterHtml.DocumentNode.SelectNodes( "//img" );
                        if ( nodeImages != null )
                        {
                            foreach ( HtmlNode image in nodeImages )
                            {
                                HtmlNode newImage = HtmlNode.CreateNode( "<a href='' class='book-image'>" + image.OuterHtml + "</a>" );
                                image.ParentNode.ReplaceChild( newImage, image );
                            }
                        }

                        // syntax color any code blocks
                        var nodeProgramListings = chapterHtml.DocumentNode.SelectNodes( "//pre[starts-with(@data-type, 'programlisting')]" );
                        if ( nodeProgramListings != null )
                        {
                            foreach ( HtmlNode programListing in nodeProgramListings )
                            {
                                var language = Languages.Html;
                                if ( programListing.Attributes["data-code-language"] != null )
                                {
                                    switch ( programListing.Attributes["data-code-language"].Value )
                                    {
                                        case "csharp":
                                        case "c#":
                                            language = Languages.CSharp;
                                            break;
                                        case "aspx":
                                            language = Languages.Aspx;
                                            break;
                                        case "xml":
                                            language = Languages.Xml;
                                            break;
                                        case "css":
                                            language = Languages.Css;
                                            break;
                                        case "sql":
                                            language = Languages.Sql;
                                            break;
                                        case "js":
                                        case "javascript":
                                            language = Languages.JavaScript;
                                            break;
                                        default:
                                            language = Languages.Html;
                                            break;
                                    }
                                }
                                HtmlNode newProgramListing = HtmlNode.CreateNode( new CodeColorizer().Colorize( Server.HtmlDecode( programListing.InnerHtml ), language ) );
                                programListing.ParentNode.ReplaceChild( newProgramListing, programListing );
                            }
                        }

                        string chapterContents = chapterHtml.DocumentNode.OuterHtml;

                        // replace base url for images
                        string baseImageUrl = GetAttributeValue( "ImageBaseURL" );
                        if ( !string.IsNullOrWhiteSpace( baseImageUrl ) )
                        {
                            chapterContents = chapterContents.Replace( "{ImagePath}", baseImageUrl );
                        }
                        else
                        {
                            if ( chapterContents.Contains( "{ImagePath}" ) )
                            {
                                lMessage.Text = "<div class='alert alert-warning'>The Base Image URL block setting needs to be set.</div>";
                            }
                        }

                        bookContent.Append( chapterContents );
                    }

                    lContent.Text = bookContent.ToString();
                }
                else
                {
                    pnlDetails.Visible = false;
                    lMessage.Text = "<div class='alert alert-warning'><strong>Unauthorized</strong> This book is not available for viewing.</div>";
                }
            }
            else
            {
                pnlDetails.Visible = false;
                lMessage.Text = "<div class='alert alert-danger'><strong>Error</strong> No book was selected with that id and version.</div>";
            }
        }

        private string RemoveSpecialCharacters( string str )
        {
            StringBuilder sb = new StringBuilder();
            foreach ( char c in str )
            {
                if ( ( c >= '0' && c <= '9' ) || ( c >= 'A' && c <= 'Z' ) || ( c >= 'a' && c <= 'z' ) || c == '.' || c == '_' )
                {
                    sb.Append( c );
                }
            }
            return sb.ToString();
        }

        #endregion
        
}
}