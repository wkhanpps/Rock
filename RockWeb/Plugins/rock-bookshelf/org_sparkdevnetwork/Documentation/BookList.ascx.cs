//
// Copyright (C) Spark Development Network - All Rights Reserved
//
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;

using org.sparkdevnetwork.Documentation.Model;

using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using org.sparkdevnetwork.Documentation.Data;
using Rock.Data;

namespace RockWeb.Plugins.org_sparkdevnetwork.Documentation
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName( "Book List" )]
    [Category( "Spark > Documentation" )]
    [Description( "Displays list of books." )]

    [LinkedPage( "Detail Page" )]
    public partial class BookList : RockBlock
    {
        #region Control Methods

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            BindFilter();

            gBooks.DataKeyNames = new string[] { "id" };
            gBooks.Actions.AddClick += Actions_AddClick;
            gBooks.GridReorder += gBooks_GridReorder;
            gBooks.GridRebind += gBooks_GridRebind;

            bool canEdit = IsUserAuthorized( "Edit" );
            gBooks.Actions.ShowAdd = canEdit;
            gBooks.IsDeleteEnabled = canEdit;

            SecurityField securityField = gBooks.Columns[4] as SecurityField;
            securityField.EntityTypeId = EntityTypeCache.Read( typeof( Book ) ).Id;
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
            int? categoryId = catpCategory.SelectedValueAsId();
            gfSettings.SaveUserPreference( "Category", categoryId.HasValue ? categoryId.Value.ToString() : string.Empty );

            gfSettings.SaveUserPreference( "Active", ddlIsActive.SelectedValue );

            BindGrid();
        }

        protected void gfSettings_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case "Category":
                    {
                        int categoryId = int.MinValue;
                        if ( int.TryParse( e.Value, out categoryId ) )
                        {
                            var service = new CategoryService( new RockContext() );
                            var category = service.Get( categoryId );
                            if ( category != null )
                            {
                                e.Value = category.Name;
                            }
                        }
                        break;
                    }

            }
        }

        #endregion

        #region Grid Events

        protected void gBooks_RowSelected( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "bookId", e.RowKeyId );
        }

        void Actions_AddClick( object sender, EventArgs e )
        {
            var parms = new Dictionary<string, string>();
            parms.Add( "bookId", "0" );
            parms.Add( "category", gfSettings.GetUserPreference( "Category" ) );
            NavigateToLinkedPage( "DetailPage", parms );
        }

        protected void gBooks_Delete( object sender, RowEventArgs e )
        {
            var docContext = new DocumentationContext();
            var bookService = new BookService( docContext );
            var book = bookService.Get( e.RowKeyId );
            if (book != null)
            {
                string errorMessage;
                if (!bookService.CanDelete(book, out errorMessage))
                {
                    maGridWarning.Show( errorMessage, ModalAlertType.Information );
                    return;
                }

                bookService.Delete( book );
                docContext.SaveChanges();
            }

            BindGrid();
        }

        void gBooks_GridReorder( object sender, GridReorderEventArgs e )
        {
            int categoryId = int.MinValue;
            if ( !int.TryParse( gfSettings.GetUserPreference( "Category" ), out categoryId ) )
            {
                categoryId = int.MinValue;
            }

            if ( categoryId == int.MinValue )
            {
                var docContext = new DocumentationContext();
                var bookService = new BookService( docContext );
                bookService.Reorder( GetBooks( bookService ).ToList(), e.OldIndex, e.NewIndex );
                docContext.SaveChanges();
            }
            else
            {
                maGridWarning.Show( "Reordering is not allowed when filtering for a specific category" , ModalAlertType.Information );
                return;
            }

            BindGrid();

        }

        void gBooks_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        #endregion

        #endregion

        #region Methods

        private void BindFilter()
        {
            int categoryId = int.MinValue;
            if (int.TryParse(gfSettings.GetUserPreference("Category"), out categoryId))
            {
                var category = new CategoryService( new RockContext() ).Get(categoryId);
                catpCategory.SetValue(category);
            }

            ddlIsActive.SelectedValue = gfSettings.GetUserPreference( "Active" );
        }

        private void BindGrid()
        {
            gBooks.DataSource = GetBooks( new BookService( new DocumentationContext() ) ).ToList();
            gBooks.DataBind();
        }
        
        private IQueryable<Book> GetBooks(BookService bookService)
        {
            var bookQuery = bookService.Queryable( "Category" );

            int categoryId = int.MinValue;
            if ( int.TryParse( gfSettings.GetUserPreference( "Category" ), out categoryId ) )
            {
                bookQuery = bookQuery.Where( b => b.CategoryId == categoryId );
            }

            var isActive = gfSettings.GetUserPreference( "Active" );
            if ( isActive == "Yes" )
            {
                bookQuery = bookQuery.Where( b => b.IsActive );
            }
            else if ( isActive == "No" )
            {
                bookQuery = bookQuery.Where( b => !b.IsActive );
            }

            return bookQuery.OrderBy( b => b.Order );
        }

        #endregion
    }
}