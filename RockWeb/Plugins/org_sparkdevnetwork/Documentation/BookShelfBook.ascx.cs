//
// THIS WORK IS LICENSED UNDER A CREATIVE COMMONS ATTRIBUTION-NONCOMMERCIAL-
// SHAREALIKE 3.0 UNPORTED LICENSE:
// http://creativecommons.org/licenses/by-nc-sa/3.0/
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using org.sparkdevnetwork.Documentation.Data;
using org.sparkdevnetwork.Documentation.Model;
using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI;

namespace RockWeb.Plugins.org_sparkdevnetwork.Documentation
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName( "Book Shelf Book" )]
    [Category( "Spark > Documentation" )]
    [Description( "Displays a public summary of a book." )]
    [LinkedPage("Reading Page")]

    [IntegerField("Cover Image Width", "Width in pixels to load the cover image as (default 710px.)", false, 710)]
    [IntegerField("Cover Image Height", "Height in pixels to load the cover image as (default 710px.)", false, 919)]
    public partial class BookShelfBook : Rock.Web.UI.RockBlock
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

            RockPage.AddCSSLink( "~/Plugins/org_sparkdevnetwork/Documentation/Styles/documentation.css" );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( _bookVersion != null )
            {
                DisplayBook();
            }
            else
            {
                pnlDetails.Visible = false;
                lMessage.Text = "<div class='alert alert-danger'><strong>Error</strong> No book was selected</div>";
            }
        }

        public override System.Collections.Generic.List<BreadCrumb> GetBreadCrumbs( Rock.Web.PageReference pageReference )
        {
            var breadcrumbs = base.GetBreadCrumbs( pageReference );

            _bookVersion = GetVersion(
                ( PageParameter( "book" ).AsInteger() ),
                ( PageParameter( "version" ).AsIntegerOrNull() ) );

            if (_bookVersion != null)
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
        protected void Block_BlockUpdated(object sender, EventArgs e)
        {
            
        }

        #endregion

        #region Methods

        private BookVersion GetVersion( int bookId, int? versionId )
        {
            var qry = new BookVersionService( new DocumentationContext() ).Queryable( "Book" );
            
            if ( versionId.HasValue )
            {
                
                var bookVersion = qry
                    .Where( v =>
                        v.BookId == bookId &&
                        v.Id == versionId.Value)
                    .FirstOrDefault();

                if (bookVersion != null)
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

                    RockPage.Title = book.Name;

                    imgCover.ImageUrl = String.Format( "/GetImage.ashx?id={0}&width={1}&height={2}", book.CoverBinaryFileId, GetAttributeValue( "CoverImageWidth" ), GetAttributeValue( "CoverImageHeight" ) ); // todo change this to imageurl
                    lSummary.Text = book.Description;

                    if ( (book.IsActive && _bookVersion.IsActive) || IsUserAuthorized( "Edit" ) )
                    {
                        Dictionary<string, string> pageParams = new Dictionary<string, string>();
                        pageParams.Add( "book", book.Id.ToString() );
                        pageParams.Add( "version", _bookVersion.Id.ToString() );
                        var detailPageUrl = LinkedPageUrl( "ReadingPage", pageParams );

                        // set links
                        hlCoverLink.NavigateUrl = detailPageUrl;
                        hlReadOnline.NavigateUrl = detailPageUrl;
                        hlReadOnline.Visible = true;

                        // set download content
                        if ( !string.IsNullOrWhiteSpace( _bookVersion.PdfUrl ) )
                        {
                            hlPdf.Visible = true;
                            hlPdf.NavigateUrl = _bookVersion.PdfUrl;
                        }

                        if ( !string.IsNullOrWhiteSpace( _bookVersion.EBookUrl ) )
                        {
                            hlEpub.Visible = true;
                            hlEpub.NavigateUrl = _bookVersion.EBookUrl;
                        }

                        if ( !string.IsNullOrWhiteSpace( _bookVersion.MobiUrl ) )
                        {
                            hlMobi.Visible = true;
                            hlMobi.NavigateUrl = _bookVersion.MobiUrl;
                        }
                    }
                    else
                    {
                        lSummary.Text += "<div class='alert alert-info'><h4>Coming Soon...</h4><p>We're hard at work putting the final touches on this manual. Check back soon.</p></div>";
                    }
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

        #endregion
    }
}