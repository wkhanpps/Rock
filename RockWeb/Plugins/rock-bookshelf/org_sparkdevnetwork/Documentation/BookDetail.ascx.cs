//
// Copyright (C) Spark Development Network - All Rights Reserved
//
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;

using org.sparkdevnetwork.Documentation.Data;
using org.sparkdevnetwork.Documentation.Model;

using Rock;
using Rock.Constants;
using Rock.Model;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using Rock.Data;

namespace RockWeb.Plugins.org_sparkdevnetwork.Documentation
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName( "Book Detail" )]
    [Category( "Spark > Documentation" )]
    [Description( "Displays details of a book." )]

    public partial class BookDetail : RockBlock
    {
        #region Control Methods

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            lbDelete.Attributes["onclick"] = string.Format( "javascript: return Rock.dialogs.confirmDelete(event, '{0}');", Book.FriendlyTypeName );
            sbtnSecurity.EntityTypeId = EntityTypeCache.Read( typeof( Book ) ).Id;
            
            // TODO: After issue #274 is resolved, the file type can be set here instead of hard-coding the guid in markup
            //imgupCover.BinaryFileTypeGuid = org.sparkdevnetwork.Documentation.SystemGuid.BinaryFileType.DOCUMENTATION_BOOK_COVER.AsGuid();
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                string itemId = PageParameter( "bookId" );

                if ( !string.IsNullOrWhiteSpace( itemId ) )
                {
                    ShowDetail( int.Parse( itemId ) );

                    // hide active tag if in edit
                    hlblInactive.Visible = false;
                }
                else
                {
                    pnlDetails.Visible = false;
                    hlblInactive.Visible = true;
                }
            }
        }

        public override List<BreadCrumb> GetBreadCrumbs( PageReference pageReference )
        {
            var breadCrumbs = new List<BreadCrumb>();

            int? bookId = PageParameter( pageReference, "bookId" ).AsInteger();
            if ( bookId != null )
            {
                Book book = new BookService( new DocumentationContext() ).Get( bookId.Value );
                if ( book != null )
                {
                    breadCrumbs.Add( new BreadCrumb( book.Name, pageReference ) );
                }
                else
                {
                    breadCrumbs.Add( new BreadCrumb( "New Book", pageReference ) );
                }
            }
            else
            {
                // don't show a breadcrumb if we don't have a pageparam to work with
            }

            return breadCrumbs;
        }

        #endregion

        #region Edit Events

        protected void lbEdit_Click( object sender, EventArgs e )
        {
            var book = new BookService(new DocumentationContext() ).Get( int.Parse( hfBookId.Value ) );
            ShowEditDetails( book );
        }

        protected void lbDelete_Click( object sender, EventArgs e )
        {
            var docContext = new DocumentationContext();
            var bookService = new BookService( docContext );
            var book = bookService.Get( int.Parse( hfBookId.Value ) );

            if ( book != null )
            {
                string errorMessage;
                if ( !bookService.CanDelete( book, out errorMessage ) )
                {
                    maDeleteWarning.Show( errorMessage, ModalAlertType.Information );
                    return;
                }

                bookService.Delete( book );
                docContext.SaveChanges();
            }

            NavigateToParentPage();
        }

        protected void lbSave_Click( object sender, EventArgs e )
        {
            if ( !Page.IsValid )
            {
                return;
            }

            var docContext = new DocumentationContext();
            var bookService = new BookService( docContext );
            Book book;

            int bookId = int.Parse( hfBookId.Value );

            if ( bookId == 0 )
            {
                book = new Book();
                book.Name = string.Empty;

                if ( bookService.Queryable().Any() )
                {
                    var maxOrder = bookService.Queryable().Max( c => c.Order );
                    book.Order = maxOrder + 1;
                }
                else
                {
                    book.Order = 1;
                }
            }
            else
            {
                book = bookService.Get( bookId );
            }

            book.Name = tbName.Text;
            book.Description = tbDescription.Text;
            book.Subtitle = tbSubtitle.Text;
            book.IsActive = cbIsActive.Checked;
            book.CategoryId = catpCategory.SelectedValueAsId();

            int? orphanedPhotoId = null;
            if (book.CoverBinaryFileId != imgupCover.BinaryFileId)
            {
                orphanedPhotoId = book.CoverBinaryFileId;
                book.CoverBinaryFileId = imgupCover.BinaryFileId;
            }

            if ( !book.IsValid )
            {
                // Controls will render the error messages                    
                return;
            }

            if ( book.Id.Equals( 0 ) )
            {
                bookService.Add( book );
            }

            if ( docContext.SaveChanges() > 0 )
            {
                if (orphanedPhotoId.HasValue)
                {
                    var rockContext = new RockContext();
                    BinaryFileService binaryFileService = new BinaryFileService( rockContext );
                    var binaryFile = binaryFileService.Get( orphanedPhotoId.Value );
                    if ( binaryFile != null )
                    {
                        // marked the old images as IsTemporary so they will get cleaned up later
                        binaryFile.IsTemporary = true;
                        rockContext.SaveChanges();
                    }
                }
            }

            var qryParams = new Dictionary<string, string>();
            qryParams["bookId"] = book.Id.ToString();
            NavigateToPage( RockPage.Guid, qryParams );
        }

        protected void lbCancel_Click( object sender, EventArgs e )
        {
            if ( hfBookId.Value.Equals( "0" ) )
            {
                NavigateToParentPage();
            }
            else
            {
                ShowDetail( hfBookId.ValueAsInt() );
            }
        }

        #endregion

        #region Internal Methods

        public void ShowDetail( int bookId )
        {
            bool editAllowed = IsUserAuthorized( "Edit" );

            Book book = null;
            if ( !bookId.Equals( 0 ) )
            {
                book = new BookService( new DocumentationContext() ).Get( bookId );
                if ( !editAllowed )
                {
                    editAllowed = book.IsAuthorized( "Edit", CurrentPerson );
                }
            }

            if (book == null)
            {
                book = new Book { Id = 0, IsActive = true };

                int categoryId = int.MinValue;
                if ( int.TryParse( PageParameter( "CategoryId" ), out categoryId ) )
                {
                    book.CategoryId = categoryId;
                }

                lTitle.Text = ActionTitle.Add( Book.FriendlyTypeName ).FormatAsHtmlTitle();
            }
            else
            {
                lTitle.Text = book.Name.FormatAsHtmlTitle();
            }

            hlblInactive.Visible = !book.IsActive;

            hfBookId.Value = book.Id.ToString();

            // render UI based on Authorized and IsSystem
            bool readOnly = false;

            nbEditModeMessage.Text = string.Empty;
            if ( !editAllowed || !IsUserAuthorized( "Edit" ) )
            {
                readOnly = true;
                nbEditModeMessage.Text = EditModeMessage.ReadOnlyEditActionNotAllowed( Book.FriendlyTypeName );
            }

            sbtnSecurity.Visible = book.IsAuthorized( "Administrate", CurrentPerson );
            sbtnSecurity.Title = book.Name;
            sbtnSecurity.EntityId = book.Id;

            if ( readOnly )
            {
                lbEdit.Visible = false;
                lbDelete.Visible = false;
                ShowReadonlyDetails( book );
            }
            else
            {
                lbEdit.Visible = true;
                lbDelete.Visible = true;
                if ( book.Id > 0 )
                {
                    ShowReadonlyDetails( book );
                }
                else
                {
                    ShowEditDetails( book );
                }
            }

        }

        private void ShowEditDetails(Book book)
        {
            pnlEditDetails.Visible = true;
            fieldsetViewDetails.Visible = false;
            this.HideSecondaryBlocks( true );

            tbName.Text = book.Name;
            cbIsActive.Checked = book.IsActive;
            tbDescription.Text = book.Description;
            tbSubtitle.Text = book.Subtitle;
            catpCategory.SetValue( book.Category );
            imgupCover.BinaryFileId = book.CoverBinaryFileId;
        }

        private void ShowReadonlyDetails(Book book)
        {
            pnlEditDetails.Visible = false;
            fieldsetViewDetails.Visible = true;
            this.HideSecondaryBlocks( false );

            lBookDescription.Text = book.Description;
            lBookSubtitle.Text = book.Subtitle;

            if ( book.CoverBinaryFileId.HasValue )
            {
                var getImageUrl = ResolveRockUrl( "~/GetImage.ashx" );
                lBookCover.Text = string.Format( "<img src='{0}?id={1}&maxwidth=100&maxheight=150' />", getImageUrl, book.CoverBinaryFileId );
            }
            DescriptionList descriptionList = new DescriptionList();
            if (book.Category != null)
            {
                descriptionList.Add( "Category", book.Category.Name );
            }
            lMainDetails.Text = descriptionList.Html;
        }

        #endregion

    }
}