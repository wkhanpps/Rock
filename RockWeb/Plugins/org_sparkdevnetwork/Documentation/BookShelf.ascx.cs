//
// THIS WORK IS LICENSED UNDER A CREATIVE COMMONS ATTRIBUTION-NONCOMMERCIAL-
// SHAREALIKE 3.0 UNPORTED LICENSE:
// http://creativecommons.org/licenses/by-nc-sa/3.0/
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using org.sparkdevnetwork.Documentation.Data;
using org.sparkdevnetwork.Documentation.Model;
using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_sparkdevnetwork.Documentation
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName( "Book Shelf" )]
    [Category( "Spark > Documentation" )]
    [Description( "Displays a list of books for the public site." )]
    [LinkedPage("Detail Page")]

    [DefinedValueField( org.sparkdevnetwork.Documentation.SystemGuid.DefinedType.ROCK_VERSION, "Default Version", "The default version that will be displayed." )]
    [IntegerField("Cover Image Width", "Width in pixels to load the cover image as (default 710px.)", false, 710)]
    [IntegerField("Cover Image Height", "Height in pixels to load the cover image as (default 710px.)", false, 919)]
    [CategoryField("Categories", "Categories to display on the shelf.", true, "org.sparkdevnetwork.Documentation.Model.Book")]
    [BooleanField("Show Inactive", "Show books marked inactive as coming soon with no link.", false)]
    public partial class BookShelf : Rock.Web.UI.RockBlock
    {
        #region Fields

        // used for private variables

        #endregion

        #region Properties

        // used for public / protected properties

        #endregion

        #region Base Control Methods
        
        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        protected override void OnInit( EventArgs e )
        {
            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            base.OnInit( e );
        }

        protected override void OnLoad( EventArgs e )
        {
            string version = GetAttributeValue( "DefaultVersion" );
            if (!string.IsNullOrWhiteSpace(version))
            {
                DisplayBooks(new Guid(version));
            }

            base.OnLoad( e );

            RockPage.AddCSSLink("~/Plugins/org_sparkdevnetwork/Documentation/Styles/documentation.css");
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

        private void DisplayBooks(Guid versionGuid)
        {
            List<string> sCategories = GetAttributeValues("Categories");
            List<Guid> gCategories = new List<Guid>();

            // convert list of guids as strings to list of guids
            foreach (string category in sCategories) {
                gCategories.Add(new Guid(category));
            }

            var versionValue = DefinedValueCache.Read( versionGuid );
            int maxVersionOrder = versionValue != null ? versionValue.Order : int.MaxValue;

            var versions = new BookVersionService( new DocumentationContext() )
                .Queryable("Book.Category,VersionDefinedValue")
                .Where(v => 
                    v.VersionDefinedValue.Order <= maxVersionOrder && 
                    gCategories.Contains(v.Book.Category.Guid) )
                .OrderBy(v => v.Book.Category.Order)
                .ThenBy(v => v.Book.Order)
                .ThenByDescending(v => v.IsActive)
                .ThenByDescending(v => v.VersionDefinedValue.Order)
                .ThenBy(v => v.Order)
                .ToList();
            //todo add , CoverBinaryFile to the QueryAble


            StringBuilder output = new StringBuilder();
            string currentCategory = string.Empty;

            var booksAdded = new List<int>();

            bool showInactive = GetAttributeValue("ShowInactive").AsBoolean();

            foreach ( var version in versions )
            {
                var book = version.Book;

                if ( !booksAdded.Contains( book.Id ) &&
                    ( ( book.IsActive && version.IsActive ) || showInactive ) &&
                    ( book.IsAuthorized( "View", CurrentPerson ) ) )
                {

                    booksAdded.Add( book.Id );

                    // category titles
                    if ( currentCategory != book.Category.Name )
                    {
                        if ( output.Length > 0 )
                        {
                            output.Append( "</div>" );
                        }

                        output.Append( String.Format( "<h3 class=\"title\">{0}</h3>", book.Category.Name ) );
                        output.Append( String.Format( "<p>{0}</p>", book.Category.Description ) );
                        output.Append( "<div class=\"row bookshelf\">" );
                        currentCategory = book.Category.Name;
                    }

                    output.Append( "<div class=\"col-md-3 col-xs-6 book margin-b-lg\">" );

                    if ( book.IsActive && version.IsActive )
                    {
                        Dictionary<string, string> pageParams = new Dictionary<string, string>();
                        pageParams.Add( "book", book.Id.ToString() );
                        pageParams.Add( "version", version.Id.ToString() );
                        var detailPageUrl = LinkedPageUrl( "DetailPage", pageParams );

                        output.Append( String.Format( "<div class='img-content'><a href='{0}'><img src='/GetImage.ashx?id={1}&width={2}&height={3}' /></a>", detailPageUrl, book.CoverBinaryFileId, GetAttributeValue( "CoverImageWidth" ), GetAttributeValue( "CoverImageHeight" ) ) );
                    }
                    else
                    {
                        output.Append( String.Format( "<div class='img-content inactive'><img src='/GetImage.ashx?id={0}&width={1}&height={2}' /></a>", book.CoverBinaryFileId, GetAttributeValue( "CoverImageWidth" ), GetAttributeValue( "CoverImageHeight" ) ) );
                    }

                    output.Append( "</div></div>" );
                }

                output.Append( "</div>" );
            }

            

            lOutput.Text = output.ToString();
        }

        #endregion
    }
}