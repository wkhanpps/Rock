//
// Copyright (C) Spark Development Network - All Rights Reserved
//
using System;
using System.ComponentModel;
using System.Linq;
using org.sparkdevnetwork.Documentation.Data;
using org.sparkdevnetwork.Documentation.Model;
using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_sparkdevnetwork.Documentation
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName( "Chapter List" )]
    [Category( "Spark > Documentation" )]
    [Description( "Displays list of chapters for a specific version of a book." )]

    [LinkedPage( "Detail Page" )]
    public partial class ChapterList : RockBlock, ISecondaryBlock
    {
        #region Control Methods

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            BindFilter();

            gChapters.DataKeyNames = new string[] { "id" };
            gChapters.Actions.AddClick += Actions_AddClick;
            gChapters.GridReorder += gChapters_GridReorder;
            gChapters.GridRebind += gChapters_GridRebind;

            bool canEdit = IsUserAuthorized( "Edit" );
            gChapters.Actions.ShowAdd = canEdit;
            gChapters.IsDeleteEnabled = canEdit;

        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if (!Page.IsPostBack)
            {
                BindGrid();
            }
        }

        #endregion

        #region Events

        #region Filter Events

        protected void gfSettings_ApplyFilterClick( object sender, EventArgs e )
        {
            gfSettings.SaveUserPreference( "Active", ddlIsActive.SelectedValue );
            BindGrid();
        }

        #endregion

        #region Grid Events

        protected void gChapters_RowSelected( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "chapterId", e.RowKeyId );
        }

        void Actions_AddClick( object sender, EventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "chapterId", 0, "bookId", hfBookId.ValueAsInt() );
        }

        protected void gChapters_Delete( object sender, RowEventArgs e )
        {
            var docContext = new DocumentationContext();
            var chapterService = new ChapterService( docContext );
            var chapter = chapterService.Get( e.RowKeyId );
            if (chapter != null)
            {
                string errorMessage;
                if (!chapterService.CanDelete(chapter, out errorMessage))
                {
                    maGridWarning.Show( errorMessage, ModalAlertType.Information );
                    return;
                }

                chapterService.Delete( chapter );
                docContext.SaveChanges();
            }

            BindGrid();
        }

        void gChapters_GridReorder( object sender, GridReorderEventArgs e )
        {
            var docContext = new DocumentationContext();
            var chapterService = new ChapterService( docContext );
            var chapters = GetChapters( chapterService );
            if ( chapters != null )
            {
                chapterService.Reorder( chapters.ToList(), e.OldIndex, e.NewIndex );
                docContext.SaveChanges();
                BindGrid();
            }
        }

        void gChapters_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        #endregion

        #endregion

        #region Methods

        private void BindFilter()
        {
            ddlIsActive.SelectedValue = gfSettings.GetUserPreference( "Active" );
        }

        private void BindGrid()
        {
            var chapters = GetChapters( new ChapterService( new DocumentationContext() ) );
            if ( chapters != null )
            {
                pnlChapters.Visible = true;
                gChapters.DataSource = chapters.ToList();
                gChapters.DataBind();
            }
            else
            {
                pnlChapters.Visible = false;
            }
        }
        
        private IQueryable<Chapter> GetChapters(ChapterService chapterService)
        {
            int versionId = PageParameter( "versionId" ).AsInteger();
            if (versionId == 0)
            {
                return null;
            }

            hfBookId.SetValue( versionId );

            var chapterQuery = chapterService.GetByBookVersionId( versionId );

            var isActive = gfSettings.GetUserPreference( "Active" );
            if ( isActive == "Yes" )
            {
                chapterQuery = chapterQuery.Where( b => b.IsActive );
            }
            else if ( isActive == "No" )
            {
                chapterQuery = chapterQuery.Where( b => !b.IsActive );
            }

            return chapterQuery.OrderBy( b => b.Order );
        }

        #endregion
 
        #region ISecondaryBlock

        /// <summary>
        /// Sets the visible.
        /// </summary>
        /// <param name="visible">if set to <c>true</c> [visible].</param>
        public void SetVisible( bool visible )
        {
            pnlContent.Visible = visible;
        }

        #endregion   
    }
}