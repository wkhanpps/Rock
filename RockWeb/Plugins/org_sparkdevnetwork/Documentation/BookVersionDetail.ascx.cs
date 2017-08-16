//
// Copyright (C) Spark Development Network - All Rights Reserved
//
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;

using org.sparkdevnetwork.Documentation.Model;

using Rock;
using Rock.Constants;
using Rock.Model;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using org.sparkdevnetwork.Documentation.Data;

namespace RockWeb.Plugins.org_sparkdevnetwork.Documentation
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName( "Book Version Detail" )]
    [Category( "Spark > Documentation" )]
    [Description( "Displays details of a particular version of a book." )]

    public partial class BookVersionDetail : RockBlock
    {
        #region Fields

        private BookVersion _bookVersion = null;
        private int? _bookId = null;

        #endregion

        #region Control Methods

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            lbDelete.Attributes["onclick"] = string.Format( "javascript: return Rock.dialogs.confirmDelete(event, '{0}');", Book.FriendlyTypeName );
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                ShowDetail();
            }
        }

        public override List<BreadCrumb> GetBreadCrumbs( PageReference pageReference )
        {
            var breadCrumbs = new List<BreadCrumb>();

            int? versionId = PageParameter( "versionId" ).AsInteger();
            _bookId = PageParameter( "bookId" ).AsInteger();

            if ( versionId != null )
            {
                _bookVersion = new BookVersionService( new DocumentationContext() ).Get( versionId.Value );
                if ( _bookVersion != null )
                {
                    _bookId = _bookVersion.BookId;
                    breadCrumbs.Add( new BreadCrumb( _bookVersion.Name, pageReference ) );
                }
                else
                {
                    breadCrumbs.Add( new BreadCrumb( "New Version", pageReference ) );
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
            ShowEditDetails();
        }

        protected void lbCopy_Click( object sender, EventArgs e )
        {
            if ( _bookVersion != null )
            {
                var docContext = new DocumentationContext();
                var bookVersionService = new BookVersionService( docContext );

                var newVersion = new BookVersion();
                newVersion.BookId = _bookId.Value;

                var otherVersions = bookVersionService.GetByBookId( _bookId.Value ).Select( v => v.Order ).ToList();
                newVersion.Order = otherVersions.Any() ? otherVersions.Max() + 1 : 0;

                newVersion.Name = "New Version";
                newVersion.IsActive = false;
                newVersion.VersionDefinedValueId = _bookVersion.VersionDefinedValueId;
                newVersion.PdfUrl = _bookVersion.PdfUrl;
                newVersion.EBookUrl = _bookVersion.EBookUrl;
                newVersion.MobiUrl = _bookVersion.MobiUrl;

                foreach(var chapter in _bookVersion.Chapters)
                {
                    var newChapter = new Chapter();
                    newChapter.Name = chapter.Name;
                    newChapter.Description = chapter.Description;
                    newChapter.Content = chapter.Content;
                    newChapter.Order = chapter.Order;
                    newChapter.IsActive = chapter.IsActive;
                    newVersion.Chapters.Add( newChapter );
                }

                bookVersionService.Add( newVersion );
                docContext.SaveChanges();

                var qryParams = new Dictionary<string, string>();
                qryParams["versionId"] = newVersion.Id.ToString();
                qryParams["mode"] = "edit";
                NavigateToPage( RockPage.Guid, qryParams );
            }
        }

        protected void lbDelete_Click( object sender, EventArgs e )
        {
            if ( _bookVersion != null )
            {
                var docContext = new DocumentationContext();
                var bookVersionService = new BookVersionService( docContext );
                var version = bookVersionService.Get( _bookVersion.Id );

                if ( version != null )
                {
                    string errorMessage;
                    if ( !bookVersionService.CanDelete( version, out errorMessage ) )
                    {
                        maDeleteWarning.Show( errorMessage, ModalAlertType.Information );
                        return;
                    }

                    bookVersionService.Delete( version );
                    docContext.SaveChanges();
                }
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
            var bookVersionService = new BookVersionService( docContext );
            BookVersion version = null;

            int bookVersionId = ( _bookVersion != null ? _bookVersion.Id : 0 );
            int versionId = ddlVersion.SelectedValueAsId().Value;

            if ( bookVersionId != 0 )
            {
                version = bookVersionService.Get( bookVersionId );
            }

            if (version == null)
            {
                version = new BookVersion();
                version.BookId = _bookId.Value;

                var otherVersions = bookVersionService.GetByBookId( _bookId.Value ).Select( v => v.Order ).ToList();
                version.Order = otherVersions.Any() ? otherVersions.Max() + 1 : 0;
            }

            version.Name = tbName.Text;
            version.IsActive = cbIsActive.Checked;
            version.VersionDefinedValueId = versionId;
            version.PdfUrl = tbPdfUrl.Text;
            version.EBookUrl = tbEBookUrl.Text;
            version.MobiUrl = tbMobiUrl.Text;
            version.UpdateSummary = ceUpdateSummary.Text;

            if ( !version.IsValid )
            {
                // Controls will render the error messages                    
                return;
            }

            if ( version.Id.Equals( 0 ) )
            {
                bookVersionService.Add( version );
            }

            docContext.SaveChanges();

            var qryParams = new Dictionary<string, string>();
            qryParams["versionId"] = version.Id.ToString();
            NavigateToPage( RockPage.Guid, qryParams );
        }

        protected void lbCancel_Click( object sender, EventArgs e )
        {
            if ( _bookVersion != null )
            {
                ShowDetail();
            }
            else
            {
                NavigateToParentPage();
            }
        }

        #endregion

        #region Internal Methods

        public void ShowDetail()
        {
            bool editAllowed = IsUserAuthorized( "Edit" );

            if (_bookVersion == null)
            {
                _bookVersion = new BookVersion { Id = 0, IsActive = true };
                _bookVersion.BookId = _bookId.Value;

                int versionId = int.MinValue;
                if ( int.TryParse( PageParameter( "VersionId" ), out versionId ) )
                {
                    _bookVersion.VersionDefinedValueId = versionId;
                }

                lTitle.Text = ActionTitle.Add( BookVersion.FriendlyTypeName ).FormatAsHtmlTitle();
            }
            else
            {
                lTitle.Text = (_bookVersion.Book.Name + ": " + _bookVersion.Name).FormatAsHtmlTitle();
            }

            hlblInactive.Visible = !_bookVersion.IsActive;

            // render UI based on Authorized and IsSystem
            bool readOnly = false;

            nbEditModeMessage.Text = string.Empty;
            if ( !editAllowed || !IsUserAuthorized( "Edit" ) )
            {
                readOnly = true;
                nbEditModeMessage.Text = EditModeMessage.ReadOnlyEditActionNotAllowed( Book.FriendlyTypeName );
            }

            if ( readOnly )
            {
                lbEdit.Visible = false;
                lbDelete.Visible = false;
                ShowReadonlyDetails();
            }
            else
            {
                lbEdit.Visible = true;
                lbDelete.Visible = true;
                if ( _bookVersion.Id == 0 || PageParameter("mode").ToLower() == "edit")
                {
                    ShowEditDetails();
                }
                else
                {
                    ShowReadonlyDetails();
                }
            }

        }

        private void ShowEditDetails()
        {
            pnlEditDetails.Visible = true;
            fieldsetViewDetails.Visible = false;
            this.HideSecondaryBlocks( true );

            ddlVersion.BindToDefinedType( DefinedTypeCache.Read( org.sparkdevnetwork.Documentation.SystemGuid.DefinedType.ROCK_VERSION.AsGuid() ) );

            tbName.Text = _bookVersion.Name;
            cbIsActive.Checked = _bookVersion.IsActive;
            ddlVersion.SetValue( _bookVersion.VersionDefinedValueId );
            tbPdfUrl.Text = _bookVersion.PdfUrl;
            tbEBookUrl.Text = _bookVersion.EBookUrl;
            tbMobiUrl.Text = _bookVersion.MobiUrl;

            if ( !string.IsNullOrWhiteSpace( _bookVersion.UpdateSummary ) )
            {
                ceUpdateSummary.Text = _bookVersion.UpdateSummary;
            }
        }

        private void ShowReadonlyDetails()
        {
            pnlEditDetails.Visible = false;
            fieldsetViewDetails.Visible = true;
            this.HideSecondaryBlocks( false );

            DescriptionList descriptionList = new DescriptionList();
            if ( _bookVersion.VersionDefinedValue != null )
            {
                descriptionList.Add( "Rock Version", _bookVersion.VersionDefinedValue.Value );
            }

            descriptionList.Add( "Update Summary", _bookVersion.UpdateSummary );

            lMainDetails.Text = descriptionList.Html;
        }

        #endregion

    }
}