//
// Copyright (C) Spark Development Network - All Rights Reserved
//
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using org.sparkdevnetwork.Documentation.Model;

using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using org.sparkdevnetwork.Documentation.Data;

namespace RockWeb.Plugins.org_sparkdevnetwork.Documentation
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName( "Book Version List" )]
    [Category( "Spark > Documentation" )]
    [Description( "Displays a list of versions for a specific book." )]

    [LinkedPage( "Detail Page" )]
    public partial class BookVersionList : RockBlock, ISecondaryBlock
    {

        #region Fields

        private int? _bookId = null;

        #endregion

        #region Control Methods

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            BindFilter();

            gVersions.DataKeyNames = new string[] { "id" };
            gVersions.Actions.AddClick += Actions_AddClick;
            gVersions.GridReorder += gVersions_GridReorder;
            gVersions.GridRebind += gVersions_GridRebind;

            bool canEdit = IsUserAuthorized( "Edit" );
            gVersions.Actions.ShowAdd = canEdit;
            gVersions.IsDeleteEnabled = canEdit;

            _bookId = PageParameter( "bookId" ).AsInteger();
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
            gfSettings.SaveUserPreference( "Version", ddlVersion.SelectedValue );

            gfSettings.SaveUserPreference( "Active", ddlIsActive.SelectedValue );

            BindGrid();
        }

        protected void gfSettings_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case "Version":
                    {
                        int valueId = int.MinValue;
                        if ( int.TryParse( e.Value, out valueId ) )
                        {
                            var value = DefinedValueCache.Read( valueId );
                            if ( value != null )
                            {
                                e.Value = value.Value;
                            }
                        }
                        break;
                    }
            }
        }

        #endregion

        #region Grid Events

        protected void gVersions_RowSelected( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "versionId", e.RowKeyId );
        }

        void Actions_AddClick( object sender, EventArgs e )
        {
            var parms = new Dictionary<string, string>();
            parms.Add( "bookId", (_bookId ?? 0).ToString() );
            parms.Add( "versionId", "0" );
            parms.Add( "version", gfSettings.GetUserPreference( "Version" ) );
            NavigateToLinkedPage( "DetailPage", parms );
        }

        protected void gVersions_Delete( object sender, RowEventArgs e )
        {
            var docContext = new DocumentationContext();
            var bookVersionService = new BookVersionService( docContext );
            var version = bookVersionService.Get( e.RowKeyId );
            if ( version != null )
            {
                string errorMessage;
                if ( !bookVersionService.CanDelete( version, out errorMessage ) )
                {
                    maGridWarning.Show( errorMessage, ModalAlertType.Information );
                    return;
                }

                bookVersionService.Delete( version );
                docContext.SaveChanges();
            }

            BindGrid();
        }

        void gVersions_GridReorder( object sender, GridReorderEventArgs e )
        {
            if ( gfSettings.GetUserPreference("Version").AsInteger() > 0 )
            {
                maGridWarning.Show( "Reordering is not allowed when filtering for a specific version" , ModalAlertType.Information );
                return;
            }
            else
            {
                var docContext = new DocumentationContext();
                var bookVersionService = new BookVersionService( docContext );
                bookVersionService.Reorder( GetBookVersions( bookVersionService ).ToList(), e.OldIndex, e.NewIndex );
                docContext.SaveChanges();
            }

            BindGrid();

        }

        void gVersions_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        #endregion

        #endregion

        #region Methods

        private void BindFilter()
        {
            ddlVersion.BindToDefinedType( DefinedTypeCache.Read( org.sparkdevnetwork.Documentation.SystemGuid.DefinedType.ROCK_VERSION.AsGuid() ), true );
            int valueId = int.MinValue;
            if ( int.TryParse( gfSettings.GetUserPreference( "Version" ), out valueId ) )
            {
                ddlVersion.SetValue( valueId );
            }

            ddlIsActive.SelectedValue = gfSettings.GetUserPreference( "Active" );
        }

        private void BindGrid()
        {
            gVersions.DataSource = GetBookVersions( new BookVersionService( new DocumentationContext() ) ).ToList();
            gVersions.DataBind();
        }

        private IQueryable<BookVersion> GetBookVersions( BookVersionService bookVersionService )
        {
            var bookVersionQuery = bookVersionService.GetByBookId( _bookId ?? 0 );

            int valueId = int.MinValue;
            if ( int.TryParse( gfSettings.GetUserPreference( "Version" ), out valueId ) )
            {
                bookVersionQuery = bookVersionQuery.Where( b => b.VersionDefinedValueId == valueId );
            }

            var isActive = gfSettings.GetUserPreference( "Active" );
            if ( isActive == "Yes" )
            {
                bookVersionQuery = bookVersionQuery.Where( b => b.IsActive );
            }
            else if ( isActive == "No" )
            {
                bookVersionQuery = bookVersionQuery.Where( b => !b.IsActive );
            }

            return bookVersionQuery.OrderBy( b => b.Order );
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