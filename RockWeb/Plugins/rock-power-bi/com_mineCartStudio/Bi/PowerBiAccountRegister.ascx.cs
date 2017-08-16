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
using com.minecartstudio.Bi;

namespace RockWeb.Plugins.com_mineCartStudio.Bi
{
    /// <summary>
    /// Block that syncs selected people to an exchange server.
    /// </summary>
    [DisplayName( "Power Bi Account Register" )]
    [Category( "Mine Cart Studio > BI" )]
    [Description( "This block registers a Power BI account for Rock to use." )]

    
    public partial class PowerBiAccountRegister : Rock.Web.UI.RockBlock
    {

        #region Fields

        private readonly string _authorityUri = "https://login.windows.net/common/oauth2/authorize/";
        private readonly string _resourceUri = "https://analysis.windows.net/powerbi/api";

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:Init" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
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
                // check if PowerBI is making a call back
                if ( Request["Authenticated"] != null )
                {
                    pnlEntry.Visible = false;
                    pnlResponse.Visible = true;

                    if ( Request["Authenticated"] == "True" )
                    {

                        nbResponse.NotificationBoxType = NotificationBoxType.Success;
                        nbResponse.Text = "The Power BI account has been successfully created. You can manage all Power BI accounts under 'Admin Tools > General Settings > Defined Types > Power BI Accounts'.";
                    }
                    else
                    {
                        nbResponse.NotificationBoxType = NotificationBoxType.Danger;
                        nbResponse.Text = "Authenication Failed.";
                    }
                }
                else
                {
                    pnlEntry.Visible = true;
                    pnlResponse.Visible = false;

                    var globalAttributes = GlobalAttributesCache.Read();
                    var externalUrl = globalAttributes.GetValue( "PublicApplicationRoot" );

                    if ( !externalUrl.EndsWith( @"\" ) )
                    {
                        externalUrl += @"\";
                    }

                    var redirectUrl = externalUrl + "Webhooks/PowerBiAuth.ashx";

                    lRedirectUrl.Text = redirectUrl;
                    lHomepage.Text = externalUrl;
                    txtRedirectUrl.Text = redirectUrl;
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the Click event of the btnRegister control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnRegister_Click( object sender, EventArgs e )
        {
            // authenicate
            PowerBiUtilities.CreateAccount( txtAccountName.Text, txtAccountDescription.Text, txtClientId.Text, txtClientSecret.Text, txtRedirectUrl.Text, Request.Url.AbsoluteUri );
        }

        #endregion


        #region Methods



        #endregion



    }
}