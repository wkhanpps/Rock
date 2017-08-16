// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using RestSharp;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI.Controls;
using Rock.Security;
using Rock.VersionInfo;
using System.Runtime.Caching;
using Rock.Web.Cache;
using System.Collections.Specialized;
using System.Web;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using com.minecartstudio.Bi;

namespace RockWeb.Plugins.com_mineCartStudio.Bi
{
    /// <summary>
    /// Block that syncs selected people to an exchange server.
    /// </summary>
    [DisplayName( "Power Bi Report Viewer" )]
    [Category( "Mine Cart Studio > BI" )]
    [Description( "This block displays a selected report from Power BI." )]

    [DefinedValueField( com.minecartstudio.Bi.SystemGuid.DefinedType.POWERBI_ACCOUNTS, "Power BI Account", "The Power BI account to use to retrieve the report.", true, false, "", "CustomSetting", 0, "PowerBiAccount")]
    [TextField("Report URL", "The URL of the report to display.", true, "", "CustomSetting", 1, "ReportUrl")]
    public partial class PowerBiReportViewer : Rock.Web.UI.RockBlockCustomSettings
    {

        #region Fields
        

        protected string _accessToken = string.Empty;
        protected string _embedUrl = string.Empty;
        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:Init" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                ShowView();
            }
        }

        #endregion

        #region Events

        protected override void ShowSettings()
        {
            pnlEditModal.Visible = true;
            //pnlView.Visible = false;

            var biAccounts = DefinedTypeCache.Read( com.minecartstudio.Bi.SystemGuid.DefinedType.POWERBI_ACCOUNTS.AsGuid() );

            ddlSettingAccountList.DataSource = biAccounts.DefinedValues;
            ddlSettingAccountList.DataTextField = "Value";
            ddlSettingAccountList.DataValueField = "Guid";
            ddlSettingAccountList.DataBind();
            ddlSettingAccountList.Items.Insert( 0, "" );

            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "PowerBiAccount" ) ) )
            {
                // check that the power bi account still exists
                var configuredAccount = DefinedValueCache.Read( GetAttributeValue( "PowerBiAccount" ) );

                if ( configuredAccount != null )
                {
                    ddlSettingAccountList.SelectedValue = GetAttributeValue( "PowerBiAccount" );

                    var reportList = GetReportList( GetAttributeValue( "PowerBiAccount" ) );

                    ddlSettingReportUrl.DataSource = reportList;
                    ddlSettingReportUrl.DataTextField = "name";
                    ddlSettingReportUrl.DataValueField = "embedurl";
                    ddlSettingReportUrl.DataBind();

                    ddlSettingReportUrl.Items.Insert( 0, "" );

                    ddlSettingReportUrl.SelectedValue = GetAttributeValue( "ReportUrl" );
                }
            }

            upnlContent.Update();
            mdEdit.Show();
        }

        protected void lbSave_Click( object sender, EventArgs e )
        {
            SetAttributeValue( "PowerBiAccount", ddlSettingAccountList.SelectedValue );
            SetAttributeValue( "ReportUrl", ddlSettingReportUrl.SelectedValue );
            SaveAttributeValues();

            mdEdit.Hide();
            pnlEditModal.Visible = false;
            upnlContent.Update();

            ShowView();
        }

        protected void ddlSettingAccountList_SelectedIndexChanged( object sender, EventArgs e )
        {
            var reportList = GetReportList( ddlSettingAccountList.SelectedValue );

            ddlSettingReportUrl.DataSource = reportList;
            ddlSettingReportUrl.DataTextField = "name";
            ddlSettingReportUrl.DataValueField = "embedurl";
            ddlSettingReportUrl.DataBind();

            ddlSettingReportUrl.Items.Insert( 0, "" );
        }

        protected void btnLogin_Click( object sender, EventArgs e )
        {
            // authenicate
            PowerBiUtilities.AuthenicateAccount( GetAttributeValue( "PowerBiAccount" ), Request.Url.AbsoluteUri );
        }

        #endregion


        #region Methods

        private List<PBIReport> GetReportList(string biAccountValueGuid)
        {
            var accessToken = PowerBiUtilities.GetAccessToken( biAccountValueGuid );
            return PowerBiUtilities.GetReports( accessToken );
        }

        private void ShowView()
        {
            pnlEditModal.Visible = false;
            pnlView.Visible = true;

            nbError.Text = string.Empty;

            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "ReportUrl" ) ) )
            {
                _embedUrl = GetAttributeValue( "ReportUrl" );
            }
            else
            {
                pnlView.Visible = false;
                nbError.NotificationBoxType = NotificationBoxType.Warning;
                nbError.Text = "No report has been configured.";
                return;
            }

            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "PowerBiAccount" ) ) )
            {
                // ensure that the account still exists as a defined value
                var accountValue = DefinedValueCache.Read( GetAttributeValue( "PowerBiAccount" ) );

                if ( accountValue != null )
                {
                    _accessToken = PowerBiUtilities.GetAccessToken( accountValue.Id );

                    if ( string.IsNullOrWhiteSpace( _accessToken ) )
                    {
                        pnlView.Visible = false;
                        pnlLogin.Visible = true;
                    }
                }
                else
                {
                    pnlView.Visible = false;
                    nbError.NotificationBoxType = NotificationBoxType.Warning;
                    nbError.Text = "The account configured for this block no longer exists.";
                    return;
                }
            }
        }

        #endregion

        
    }


}