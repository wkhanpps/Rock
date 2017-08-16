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
using Rock.Attribute;
using Rock.Constants;
using Rock.Model;
using Rock.Web;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_sparkdevnetwork.Documentation
{
    /// <summary>
    /// 
    /// </summary>
    /// 
    [DisplayName( "Chapter Detail" )]
    [Category( "Spark > Documentation" )]
    [Description( "Displays the details of a chapter in a specific book version." )]

    [CodeEditorField( "Starting Chapter Markup", "The markup that will be put into the editor when a chapter is first created.", CodeEditorMode.Html, CodeEditorTheme.Rock, 300, false, @"
<section data-type=""chapter"" id=""<!-- chapter-name -->"">

    <p>
        <!-- Chapter intro -->
    </p>

    <section data-type=""sect1"">

        <h1><!-- Section Title --></h1>
        <p>
            <!-- Section Content -->
        </p>
    </section>

</section>

" )]
    public partial class ChapterDetail : RockBlock, IDetailBlock
    {
        #region Properties

        /// <summary>
        /// Gets or sets the last modified time stamp for the chapter being edited.
        /// </summary>
        /// <value>
        /// The last modified time stamp.
        /// </value>
        protected DateTime? LastModifiedTimeStamp
        {
            get { return ViewState["LastModifiedTimeStamp"] as DateTime?; }
            set { ViewState["LastModifiedTimeStamp"] = value; }
        }

        #endregion

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        /// 
        
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                string chapterId = PageParameter( "chapterId" );
                if ( !string.IsNullOrWhiteSpace( chapterId ) )
                {
                    ShowDetail( chapterId.AsInteger(), PageParameter( "bookId" ).AsIntegerOrNull() );

                    // disable active tag if editing
                    hlblInactive.Visible = false;
                }
                else
                {
                    upnlDetail.Visible = false;
                    hlblInactive.Visible = true;
                }
            }
        }

        /// <summary>
        /// Returns breadcrumbs specific to the block that should be added to navigation
        /// based on the current page reference.  This function is called during the page's
        /// oninit to load any initial breadcrumbs
        /// </summary>
        /// <param name="pageReference">The page reference.</param>
        /// <returns></returns>
        public override List<BreadCrumb> GetBreadCrumbs( PageReference pageReference )
        {
            var breadCrumbs = new List<BreadCrumb>();

            int? chapterId = PageParameter( pageReference, "chapterId" ).AsInteger();
            if ( chapterId != null )
            {
                Chapter chapter = new ChapterService( new DocumentationContext() ).Get( chapterId.Value );
                if ( chapter != null )
                {
                    breadCrumbs.Add( new BreadCrumb( chapter.Name, pageReference ) );
                }
                else
                {
                    breadCrumbs.Add( new BreadCrumb( "New Chapter", pageReference ) );
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


        /// <summary>
        /// Handles the Click event of the lbSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void lbSave_Click( object sender, EventArgs e )
        {
            if ( Page.IsValid )
            {
                var docContext = new DocumentationContext();

                int versionId = hfVersionId.ValueAsInt();
                var bookVersion = new BookVersionService( docContext ).Get( versionId );
                if ( bookVersion != null && bookVersion.IsAuthorized( "Edit", CurrentPerson ) )
                {
                    ChapterService chapterService = new ChapterService( docContext );
                    Chapter chapter;

                    int chapterId = int.Parse( hfChapterId.Value );

                    // if adding a new chapter 
                    if ( chapterId.Equals( 0 ) )
                    {
                        chapter = new Chapter { Id = 0 };
                        chapter.BookVersionId = bookVersion.Id;

                        var orders = bookVersion.Chapters.Select( c => c.Order ).ToList();
                        chapter.Order = orders.Any() ? orders.Max() + 1 : 0;
                    }
                    else
                    {
                        //load existing group member
                        chapter = chapterService.Get( chapterId );
                    }

                    if ( LastModifiedTimeStamp.HasValue && !LastModifiedTimeStamp.Equals( chapter.ModifiedDateTime ) && tbOverwrite.Text != "OVERWRITE" )
                    {
                        if (chapter.ModifiedByPersonAlias != null && chapter.ModifiedByPersonAlias.Person != null )
                        {
                            lOverwriteUser.Text = chapter.ModifiedByPersonAlias.Person.FullName;
                        }
                        else
                        {
                            lOverwriteUser.Text = "another user";
                        }
                        pnlOverwriteNotice.Visible = true;
                    }
                    else
                    {
                        chapter.Name = tbChapterName.Text;
                        chapter.IsActive = cbIsActive.Checked;
                        chapter.Description = tbDescription.Text;
                        chapter.Content = ceContent.Text;

                        if ( !chapter.IsValid )
                        {
                            return;
                        }

                        if ( chapter.Id.Equals( 0 ) )
                        {
                            chapterService.Add( chapter );
                        }
                        docContext.SaveChanges();

                        Dictionary<string, string> qryString = new Dictionary<string, string>();
                        qryString["versionId"] = hfVersionId.Value;
                        NavigateToParentPage( qryString );
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the lbCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void lbCancel_Click( object sender, EventArgs e )
        {
            if ( hfChapterId.Value.Equals( "0" ) )
            {
                // Cancelling on Add
                Dictionary<string, string> qryString = new Dictionary<string, string>();
                qryString["versionId"] = hfVersionId.Value;
                NavigateToParentPage( qryString );
            }
            else
            {
                // Cancelling on Edit
                ChapterService chapterService = new ChapterService( new DocumentationContext() );
                Chapter chapter = chapterService.Get( int.Parse( hfChapterId.Value ) );

                Dictionary<string, string> qryString = new Dictionary<string, string>();
                qryString["versionId"] = chapter.BookVersionId.ToString();
                NavigateToParentPage( qryString );
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="chapterId">The chapter identifier.</param>
        public void ShowDetail( int chapterId )
        {
            ShowDetail( chapterId, null );
        }

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="chapterId">The chapter identifier.</param>
        /// <param name="versionId">The group id.</param>
        public void ShowDetail( int chapterId, int? versionId )
        {
            var docContext = new DocumentationContext();
            Chapter chapter = null;

            bool canEdit = false;

            if ( !chapterId.Equals( 0 ) )
            {
                chapter = new ChapterService( docContext ).Get( chapterId );
                canEdit = chapter.BookVersion.IsAuthorized( "Edit", CurrentPerson );
            }
            
            // only create a new one if parent was specified
            if ( chapter == null && versionId.HasValue )
            {
                var bookVersion = new BookVersionService( docContext ).Get( versionId.Value );
                if ( bookVersion != null )
                {
                    chapter = new Chapter { Id = 0 };
                    chapter.BookVersionId = versionId.Value;
                    canEdit = bookVersion.IsAuthorized( "Edit", CurrentPerson );
                }
            }

            if ( chapter == null )
            {
                return;
            }

            LastModifiedTimeStamp = chapter.ModifiedDateTime;
            tbOverwrite.Text = string.Empty;

            hfVersionId.Value = chapter.BookVersionId.ToString();
            hfChapterId.Value = chapter.Id.ToString();

            if ( chapter.Id.Equals( 0 ) )
            {
                lReadOnlyTitle.Text = ActionTitle.Add( Chapter.FriendlyTypeName ).FormatAsHtmlTitle();
            }
            else
            {
                lReadOnlyTitle.Text = chapter.Name.FormatAsHtmlTitle();
                hlblInactive.Visible = !chapter.IsActive;
            }

            tbChapterName.Text = chapter.Name;
            cbIsActive.Checked = chapter.IsActive;
            tbDescription.Text = chapter.Description;

            if ( chapter.BookVersion != null )
            {
                lVersionTip.Text = String.Format( @"data-update-tag=""{0}""", chapter.BookVersion.VersionDefinedValue.Value );
            }

            if (chapter.Id.Equals(0))
            {
                // set default chapter markup in code editor
                ceContent.Text = GetAttributeValue("StartingChapterMarkup");
            }
            else
            {
                ceContent.Text = chapter.Content;
            }

            tbChapterName.Enabled = canEdit;
            cbIsActive.Enabled = canEdit;
            tbDescription.Enabled = canEdit;
            ceContent.Enabled = canEdit;

            lbSave.Visible = canEdit;
        }

        #endregion
    }
}