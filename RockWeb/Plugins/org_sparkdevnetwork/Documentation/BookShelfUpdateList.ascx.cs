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
using Rock.Web.UI;

namespace RockWeb.Plugins.org_sparkdevnetwork.Documentation
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName( "Book Shelf Update List" )]
    [Category( "Spark > Documentation" )]
    [Description( "Lists all changes made to books for each version of Rock." )]
    [LinkedPage("Book Detail")]
    [CategoryField( "Categories", "Categories to display on the shelf.", true, "org.sparkdevnetwork.Documentation.Model.Book" )]
    public partial class BookShelfUpdateList : Rock.Web.UI.RockBlock
    {
        #region Fields

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

            DisplayUpdates();
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
            DisplayUpdates();
        }

        #endregion

        #region Methods

        private void DisplayUpdates()
        {
            // get a list of books that have been updated by rock version
            DocumentationContext docContext = new DocumentationContext();

            List<string> sCategories = GetAttributeValues( "Categories" );
            List<Guid> gCategories = new List<Guid>();

            // convert list of guids as strings to list of guids
            foreach ( string category in sCategories )
            {
                gCategories.Add( new Guid( category ) );
            }

            var bookList = new BookVersionService( docContext ).Queryable( "Book, Book.Category, VersionDefinedValue" )
                                .Where( 
                                    v => v.UpdateSummary != string.Empty 
                                    && v.IsActive == true
                                    && gCategories.Contains( v.Book.Category.Guid )
                                 )
                                .OrderByDescending( v => v.VersionDefinedValueId)
                                .ThenBy( v => v.Book.CategoryId)
                                .ToList();

            StringBuilder output = new StringBuilder();

            string currentRockVersion = "";

            foreach ( var book in bookList )
            {
                if ( book.VersionDefinedValue.Value != currentRockVersion )
                {
                    output.Append( string.Format( "<h2>New in {0}</h2>", book.VersionDefinedValue.Description ) );
                    currentRockVersion = book.VersionDefinedValue.Value;
                }

                // book link
                Dictionary<string, string> pageParams = new Dictionary<string, string>();
                pageParams.Add( "book", book.Book.Id.ToString() );
                pageParams.Add( "version", book.Id.ToString() );
                var detailPageUrl = LinkedPageUrl( "BookDetail", pageParams );

                output.Append( "<div class='row margin-b-lg'>" );
                output.Append( "<div class='col-sm-2 hidden-xs'>" );
                output.Append( "<div class='img-content'>" );
                output.Append( string.Format("<a href='{0}'>", detailPageUrl) );
                output.Append( string.Format( "<img src='{0}' style='width: 100%' />", ResolveUrl(book.Book.CoverBinaryFile.Url) ) );
                output.Append( "</a>" );
                output.Append( "</div>" );
                output.Append( "</div>" );

                output.Append( "<div class='col-sm-10'>" );
                output.Append( string.Format("<strong>{0}</strong> <span class='label label-info'>{1}</span><br>{2}", book.Book.Name, book.Book.Category.Name, book.UpdateSummary) );
                output.Append( string.Format( "<p><a class='btn btn-default btn-xs' href='{0}'>Read <i class='fa fa-chevron-right'></i></a></p>", detailPageUrl ) );
                output.Append( "</div>" );
                output.Append( "</div>" );
            }

            lOutput.Text = output.ToString();
            
        }

        #endregion
    }
}