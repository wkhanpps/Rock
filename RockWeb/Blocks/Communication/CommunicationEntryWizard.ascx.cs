﻿// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
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
using System.Linq.Expressions;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Communication
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName( "Communication Entry Wizard" )]
    [Category( "Communication" )]
    [Description( "Used for creating and sending a new communications such as email, SMS, etc. to recipients." )]

    [SecurityAction( Authorization.APPROVE, "The roles and/or users that have access to approve new communications." )]

    [BinaryFileTypeField( "Binary File Type", "The FileType to use for images that are added to the email using the image component", true, Rock.SystemGuid.BinaryFiletype.DEFAULT, order: 1 )]
    [IntegerField( "Character Limit", "Set this to show a character limit countdown for SMS communications. Set to 0 to disable", false, 160, order: 2 )]

    [LavaCommandsField( "Enabled Lava Commands", "The Lava commands that should be enabled for this HTML block.", false, order: 3 )]
    [CustomCheckboxListField( "Communication Types", "The communication types that should be available to user to send (If none are selected, all will be available).", "Recipient Preference,Email,SMS", false, order: 4 )]
    [IntegerField( "Maximum Recipients", "The maximum number of recipients allowed before communication will need to be approved.", false, 300, "", order: 5 )]
    [BooleanField( "Send When Approved", "Should communication be sent once it's approved (vs. just being queued for scheduled job to send)?", true, "", 6 )]
    public partial class CommunicationEntryWizard : RockBlock, IDetailBlock
    {
        #region Properties

        /// <summary>
        /// Gets or sets the individual recipient person ids.
        /// </summary>
        /// <value>
        /// The individual recipient person ids.
        /// </value>
        protected List<int> IndividualRecipientPersonIds
        {
            get
            {
                var recipients = ViewState["IndividualRecipientPersonIds"] as List<int>;
                if ( recipients == null )
                {
                    recipients = new List<int>();
                    ViewState["IndividualRecipientPersonIds"] = recipients;
                }

                return recipients;
            }

            set
            {
                ViewState["IndividualRecipientPersonIds"] = value;
            }
        }

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            componentImageUploader.BinaryFileTypeGuid = this.GetAttributeValue( "BinaryFileType" ).AsGuidOrNull() ?? Rock.SystemGuid.BinaryFiletype.DEFAULT.AsGuid();
            hfSMSCharLimit.Value = ( this.GetAttributeValue( "CharacterLimit" ).AsIntegerOrNull() ?? 160 ).ToString();

            gIndividualRecipients.DataKeyNames = new string[] { "Id" };
            gIndividualRecipients.GridRebind += gIndividualRecipients_GridRebind;
            gIndividualRecipients.Actions.ShowAdd = false;
            gIndividualRecipients.ShowActionRow = false;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                ShowDetail( PageParameter( "CommunicationId" ).AsInteger() );
            }

            // set the email preview visible = false on every load so that it doesn't stick around after previewing then navigating
            pnlEmailPreview.Visible = false;
        }

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="communicationId">The communication identifier.</param>
        public void ShowDetail( int communicationId )
        {
            Rock.Model.Communication communication = null;
            var rockContext = new RockContext();

            if ( communicationId != 0 )
            {
                communication = new CommunicationService( rockContext ).Get( communicationId );
            }

            var editingApproved = PageParameter( "Edit" ).AsBoolean() && IsUserAuthorized( "Approve" );

            if ( communication == null )
            {
                communication = new Rock.Model.Communication() { Status = CommunicationStatus.Transient };
                communication.SenderPersonAliasId = CurrentPersonAliasId;
                communication.EnabledLavaCommands = GetAttributeValue( "EnabledLavaCommands" );

                var allowedCommunicationTypes = GetAllowedCommunicationTypes();
                if ( !allowedCommunicationTypes.Contains( communication.CommunicationType ) )
                {
                    communication.CommunicationType = allowedCommunicationTypes.First();
                }
            }

            CommunicationStatus[] editableStatuses = new CommunicationStatus[] { CommunicationStatus.Transient, CommunicationStatus.Draft, CommunicationStatus.Denied };
            if ( editableStatuses.Contains( communication.Status ) || ( communication.Status == CommunicationStatus.PendingApproval && editingApproved ) )
            {
                // communication is either new or OK to edit
            }
            else
            {
                // Not an editable communication, so hide this block. If there is a CommunicationDetail block on this page, it'll be shown instead
                this.Visible = false;
                return;
            }

            LoadDropDowns();

            hfCommunicationId.Value = communication.Id.ToString();
            lTitle.Text = ( communication.Name ?? "New Communication" ).FormatAsHtmlTitle();

            tbCommunicationName.Text = communication.Name;
            tglBulkCommunication.Checked = communication.IsBulkCommunication;

            tglRecipientSelection.Checked = communication.Id == 0 || communication.ListGroupId.HasValue;
            ddlCommunicationGroupList.SetValue( communication.ListGroupId );

            var segmentDataviewGuids = communication.Segments.SplitDelimitedValues().AsGuidList();
            if ( segmentDataviewGuids.Any() )
            {
                var segmentDataviewIds = new DataViewService( rockContext ).GetByGuids( segmentDataviewGuids ).Select( a => a.Id ).ToList();
                cblCommunicationGroupSegments.SetValues( segmentDataviewIds );
            }

            this.IndividualRecipientPersonIds = new CommunicationRecipientService( rockContext ).Queryable().Where( r => r.CommunicationId == communication.Id ).Select( a => a.PersonAlias.PersonId ).ToList();
            UpdateIndividualRecipientsCountText();

            // If there aren't any Communication Groups, hide the option and only show the Individual Recipient selection
            if ( ddlCommunicationGroupList.Items.Count <= 1 )
            {
                tglRecipientSelection.Checked = false;
                tglRecipientSelection.Visible = false;
            }

            tglRecipientSelection_CheckedChanged( null, null );

            // Note: Javascript takes care of making sure the buttons are set up based on this
            hfMediumType.Value = communication.CommunicationType.ConvertToInt().ToString();

            tglSendDateTime.Checked = !communication.FutureSendDateTime.HasValue;
            dtpSendDateTime.SelectedDateTime = communication.FutureSendDateTime;
            tglSendDateTime_CheckedChanged( null, null );

            // TODO 
            // 1) when editing an existing communication, do we know which template they used?  
            // 2) when editing an existing communication, can they change which template to use? (what do we do on the Select Template page, skip it?)??
            hfSelectedCommunicationTemplateId.Value = communication.CommunicationTemplateId.ToString();

            // Email Summary fields
            tbFromName.Text = communication.FromName;
            tbFromAddress.Text = communication.FromEmail;

            // Email Summary fields: additional fields
            tbReplyToAddress.Text = communication.ReplyToEmail;
            tbCCList.Text = communication.CCEmails;
            tbBCCList.Text = communication.BCCEmails;

            hfShowAdditionalFields.Value = ( !string.IsNullOrEmpty( communication.ReplyToEmail ) || !string.IsNullOrEmpty( communication.CCEmails ) || !string.IsNullOrEmpty( communication.BCCEmails ) ).ToTrueFalse().ToLower();

            tbEmailSubject.Text = communication.Subject;

            //// NOTE: tbEmailPreview will be populated by parsing the Html of the Email/Template

            hfAttachedBinaryFileIds.Value = communication.Attachments.Select( a => a.BinaryFileId ).ToList().AsDelimited( "," );
            UpdateAttachedFiles( false );

            // Mobile Text Editor
            ddlSMSFrom.SetValue( communication.SMSFromDefinedValueId );
            tbSMSTextMessage.Text = communication.SMSMessage;

            // Email Editor
            hfEmailEditorHtml.Value = communication.Message;

            /** DEBUG **/
            if ( ddlCommunicationGroupList.Items.Count > 1 )
            {
                ddlCommunicationGroupList.SelectedValue = ddlCommunicationGroupList.Items[1].Value;
                ddlCommunicationGroupList_SelectedIndexChanged( null, null );
            }

            tbCommunicationName.Text = "DEBUG";
            tbFromName.Text = "DEBUG";
            tbEmailSubject.Text = "DEBUG";
            tbFromAddress.Text = "DEBUG";
            dtpSendDateTime.SelectedDateTime = RockDateTime.Now;
            /**********/
        }

        /// <summary>
        /// Loads the drop downs.
        /// </summary>
        private void LoadDropDowns()
        {
            var rockContext = new RockContext();

            // load communication group list
            var groupTypeCommunicationGroupId = GroupTypeCache.Read( Rock.SystemGuid.GroupType.GROUPTYPE_COMMUNICATIONLIST.AsGuid() ).Id;
            var groupService = new GroupService( rockContext );

            var communicationGroupList = groupService.Queryable().Where( a => a.GroupTypeId == groupTypeCommunicationGroupId ).OrderBy( a => a.Order ).ThenBy( a => a.Name ).ToList();
            var authorizedCommunicationGroupList = communicationGroupList.Where( g => g.IsAuthorized( Rock.Security.Authorization.VIEW, this.CurrentPerson ) ).ToList();

            ddlCommunicationGroupList.Items.Clear();
            ddlCommunicationGroupList.Items.Add( new ListItem() );
            foreach ( var communicationGroup in authorizedCommunicationGroupList )
            {
                ddlCommunicationGroupList.Items.Add( new ListItem( communicationGroup.Name, communicationGroup.Id.ToString() ) );
            }

            LoadCommunicationSegmentFilters();

            rblCommunicationGroupSegmentFilterType.Items.Clear();
            rblCommunicationGroupSegmentFilterType.Items.Add( new ListItem( "All segment filters", SegmentCriteria.All.ToString() ) { Selected = true } );
            rblCommunicationGroupSegmentFilterType.Items.Add( new ListItem( "Any segment filters", SegmentCriteria.Any.ToString() ) );

            UpdateRecipientFromListCount();

            btnMediumRecipientPreference.Attributes["data-val"] = Rock.Model.CommunicationType.RecipientPreference.ConvertToInt().ToString();
            btnMediumEmail.Attributes["data-val"] = Rock.Model.CommunicationType.Email.ConvertToInt().ToString();
            btnMediumSMS.Attributes["data-val"] = Rock.Model.CommunicationType.SMS.ConvertToInt().ToString();

            var allowedCommunicationTypes = GetAllowedCommunicationTypes();
            btnMediumRecipientPreference.Visible = allowedCommunicationTypes.Contains( CommunicationType.RecipientPreference );
            btnMediumEmail.Visible = allowedCommunicationTypes.Contains( CommunicationType.Email );
            btnMediumSMS.Visible = allowedCommunicationTypes.Contains( CommunicationType.SMS );

            // only show the medium type selection if there is a choice
            rcwMediumType.Visible = allowedCommunicationTypes.Count > 1;

            ddlSMSFrom.BindToDefinedType( DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.COMMUNICATION_SMS_FROM ) ), true, true );
        }

        /// <summary>
        /// Loads the communication types that are configured for this block
        /// </summary>
        private List<CommunicationType> GetAllowedCommunicationTypes()
        {
            var communicationTypes = this.GetAttributeValue( "CommunicationTypes" ).SplitDelimitedValues();
            var result = new List<CommunicationType>();
            if ( communicationTypes.Any() )
            {
                // Recipient Preference,Email,SMS
                if ( communicationTypes.Contains( "RecipientPreference" ) )
                {
                    result.Add( CommunicationType.RecipientPreference );
                }

                if ( communicationTypes.Contains( "Email" ) )
                {
                    result.Add( CommunicationType.Email );
                }

                if ( communicationTypes.Contains( "SMS" ) )
                {
                    result.Add( CommunicationType.SMS );
                }
            }
            else
            {
                result.Add( CommunicationType.RecipientPreference );
                result.Add( CommunicationType.Email );
                result.Add( CommunicationType.SMS );
            }

            return result;
        }

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            this.NavigateToCurrentPageReference( new Dictionary<string, string>() );
        }

        #endregion

        #region Recipient Selection

        /// <summary>
        /// Shows the recipient selection.
        /// </summary>
        private void ShowRecipientSelection()
        {
            pnlRecipientSelection.Visible = true;
        }

        /// <summary>
        /// Loads the common communication segment filters along with any additional filters that are defined for the selected communication list
        /// </summary>
        private void LoadCommunicationSegmentFilters()
        {
            var rockContext = new RockContext();

            // load common communication segments (each communication list may have additional segments)
            var dataviewService = new DataViewService( rockContext );
            var categoryIdCommunicationSegments = CategoryCache.Read( Rock.SystemGuid.Category.DATAVIEW_COMMUNICATION_SEGMENTS.AsGuid() ).Id;
            var commonSegmentDataViewList = dataviewService.Queryable().Where( a => a.CategoryId == categoryIdCommunicationSegments ).OrderBy( a => a.Name ).ToList();

            var selectedDataViewIds = cblCommunicationGroupSegments.Items.OfType<ListItem>().Where( a => a.Selected ).Select( a => a.Value ).AsIntegerList();

            cblCommunicationGroupSegments.Items.Clear();
            foreach ( var commonSegmentDataView in commonSegmentDataViewList )
            {
                if ( commonSegmentDataView.IsAuthorized( Rock.Security.Authorization.VIEW, this.CurrentPerson ) )
                {
                    cblCommunicationGroupSegments.Items.Add( new ListItem( commonSegmentDataView.Name, commonSegmentDataView.Id.ToString() ) );
                }
            }

            // reselect any segments that they previously select (if they exist)
            cblCommunicationGroupSegments.SetValues( selectedDataViewIds );

            int? communicationGroupId = ddlCommunicationGroupList.SelectedValue.AsIntegerOrNull();
            List<Guid> segmentDataViewGuids = null;
            if ( communicationGroupId.HasValue )
            {
                var communicationGroup = new GroupService( rockContext ).Get( communicationGroupId.Value );
                if ( communicationGroup != null )
                {
                    communicationGroup.LoadAttributes();
                    var segmentAttribute = AttributeCache.Read( Rock.SystemGuid.Attribute.GROUP_COMMUNICATION_SEGMENTS.AsGuid() );
                    segmentDataViewGuids = communicationGroup.GetAttributeValue( segmentAttribute.Key ).SplitDelimitedValues().AsGuidList();
                    var additionalSegmentDataViewList = dataviewService.GetByGuids( segmentDataViewGuids ).OrderBy( a => a.Name ).ToList();

                    foreach ( var additionalSegmentDataView in additionalSegmentDataViewList )
                    {
                        if ( additionalSegmentDataView.IsAuthorized( Rock.Security.Authorization.VIEW, this.CurrentPerson ) )
                        {
                            cblCommunicationGroupSegments.Items.Add( new ListItem( additionalSegmentDataView.Name, additionalSegmentDataView.Id.ToString() ) );
                        }
                    }
                }
            }

            pnlCommunicationGroupSegments.Visible = cblCommunicationGroupSegments.Items.Count > 0;
        }

        /// <summary>
        /// Handles the Click event of the btnRecipientSelectionNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnRecipientSelectionNext_Click( object sender, EventArgs e )
        {
            nbRecipientsAlert.Visible = false;
            if ( tglRecipientSelection.Checked )
            {
                if ( !GetRecipientFromListSelection().Any() )
                {
                    nbRecipientsAlert.Text = "At least one recipient is required.";
                    nbRecipientsAlert.Visible = true;
                    return;
                }
            }
            else
            {
                if ( !this.IndividualRecipientPersonIds.Any() )
                {
                    nbRecipientsAlert.Text = "At least one recipient is required.";
                    nbRecipientsAlert.Visible = true;
                    return;
                }
            }

            pnlRecipientSelection.Visible = false;
            ShowMediumSelection();
        }

        /// <summary>
        /// Handles the CheckedChanged event of the tglRecipientSelection control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void tglRecipientSelection_CheckedChanged( object sender, EventArgs e )
        {
            nbRecipientsAlert.Visible = false;
            pnlRecipientSelectionList.Visible = tglRecipientSelection.Checked;
            pnlRecipientSelectionIndividual.Visible = !tglRecipientSelection.Checked;
        }

        /// <summary>
        /// Handles the SelectPerson event of the ppAddPerson control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ppAddPerson_SelectPerson( object sender, EventArgs e )
        {
            if ( ppAddPerson.PersonId.HasValue )
            {
                if ( !IndividualRecipientPersonIds.Contains( ppAddPerson.PersonId.Value ) )
                {
                    IndividualRecipientPersonIds.Add( ppAddPerson.PersonId.Value );
                }

                // clear out the personpicker and have it say "Add Person" again since they are added to the list
                ppAddPerson.SetValue( null );
                ppAddPerson.PersonName = "Add Person";
            }

            UpdateIndividualRecipientsCountText();
        }

        /// <summary>
        /// Updates the individual recipients count text.
        /// </summary>
        private void UpdateIndividualRecipientsCountText()
        {
            var individualRecipientCount = this.IndividualRecipientPersonIds.Count();
            lIndividualRecipientCount.Text = string.Format( "{0} {1} selected", individualRecipientCount, "recipient".PluralizeIf( individualRecipientCount != 1 ) );
        }

        /// <summary>
        /// Handles the Click event of the btnViewIndividualRecipients control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnViewIndividualRecipients_Click( object sender, EventArgs e )
        {
            BindIndividualRecipientsGrid();
            mdIndividualRecipients.Show();
        }

        /// <summary>
        /// Handles the GridRebind event of the gIndividualRecipients control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridRebindEventArgs"/> instance containing the event data.</param>
        private void gIndividualRecipients_GridRebind( object sender, GridRebindEventArgs e )
        {
            BindIndividualRecipientsGrid();
        }

        /// <summary>
        /// Handles the RowDataBound event of the gIndividualRecipients control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        protected void gIndividualRecipients_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            var recipientPerson = e.Row.DataItem as Person;
            var lRecipientAlert = e.Row.FindControl( "lRecipientAlert" ) as Literal;
            var lRecipientAlertEmail = e.Row.FindControl( "lRecipientAlertEmail" ) as Literal;
            var lRecipientAlertSMS = e.Row.FindControl( "lRecipientAlertSMS" ) as Literal;
            if ( recipientPerson != null && lRecipientAlert != null )
            {
                string alertClass = string.Empty;
                string alertMessage = string.Empty;
                string alertClassEmail = string.Empty;
                string alertMessageEmail = recipientPerson.Email;
                string alertClassSMS = string.Empty;
                string alertMessageSMS = string.Format( "{0}", recipientPerson.PhoneNumbers.FirstOrDefault( a => a.IsMessagingEnabled ) );

                // General alert info about recipient
                if ( recipientPerson.IsDeceased )
                {
                    alertClass = "text-danger";
                    alertMessage = "Deceased";
                }

                // Email related
                if ( string.IsNullOrWhiteSpace( recipientPerson.Email ) )
                {
                    alertClassEmail = "text-danger";
                    alertMessageEmail = "No Email." + recipientPerson.EmailNote;
                }
                else if ( !recipientPerson.IsEmailActive )
                {
                    // if email is not active, show reason why as tooltip
                    alertClassEmail = "text-danger";
                    alertMessageEmail = "Email is Inactive. " + recipientPerson.EmailNote;
                }
                else
                {
                    // Email is active
                    if ( recipientPerson.EmailPreference != EmailPreference.EmailAllowed )
                    {
                        alertMessageEmail = string.Format( "{0} <span class='label label-warning'>{1}</span>", recipientPerson.Email, recipientPerson.EmailPreference.ConvertToString( true ) );
                        if ( recipientPerson.EmailPreference == EmailPreference.NoMassEmails )
                        {
                            alertClassEmail = "js-no-bulk-email";
                            if ( tglBulkCommunication.Checked )
                            {
                                // This is a bulk email and user does not want bulk emails
                                alertClassEmail += " text-danger";
                            }
                        }
                        else
                        {
                            // Email preference is 'Do Not Email'
                            alertClassEmail = "text-danger";
                        }
                    }
                }

                // SMS Related
                if ( !recipientPerson.PhoneNumbers.Any( a => a.IsMessagingEnabled ) )
                {
                    // No SMS Number
                    alertClassSMS = "text-danger";
                    alertMessageSMS = "No phone number with SMS enabled.";
                }

                lRecipientAlert.Text = string.Format( "<span class=\"{0}\">{1}</span>", alertClass, alertMessage );
                lRecipientAlertEmail.Text = string.Format( "<span class=\"{0}\">{1}</span>", alertClassEmail, alertMessageEmail );
                lRecipientAlertSMS.Text = string.Format( "<span class=\"{0}\">{1}</span>", alertClassSMS, alertMessageSMS );
            }
        }

        /// <summary>
        /// Binds the individual recipients grid.
        /// </summary>
        private void BindIndividualRecipientsGrid()
        {
            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );
            var qryPersons = personService.GetByIds( this.IndividualRecipientPersonIds ).Include( a => a.PhoneNumbers ).OrderBy( a => a.LastName ).ThenBy( a => a.NickName );

            gIndividualRecipients.SetLinqDataSource( qryPersons );
            gIndividualRecipients.DataBind();
        }

        /// <summary>
        /// Handles the DeleteClick event of the gIndividualRecipients control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gIndividualRecipients_DeleteClick( object sender, RowEventArgs e )
        {
            this.IndividualRecipientPersonIds.Remove( e.RowKeyId );
            BindIndividualRecipientsGrid();
            UpdateIndividualRecipientsCountText();

            // upnlContent has UpdateMode = Conditional and this is a modal, so we have to update manually
            upnlContent.Update();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the cblCommunicationGroupSegments control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cblCommunicationGroupSegments_SelectedIndexChanged( object sender, EventArgs e )
        {
            UpdateRecipientFromListCount();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlCommunicationGroupList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlCommunicationGroupList_SelectedIndexChanged( object sender, EventArgs e )
        {
            // reload segments in case the communication list has additional segments
            LoadCommunicationSegmentFilters();

            UpdateRecipientFromListCount();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the rblCommunicationGroupSegmentFilterType control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void rblCommunicationGroupSegmentFilterType_SelectedIndexChanged( object sender, EventArgs e )
        {
            UpdateRecipientFromListCount();
        }

        /// <summary>
        /// Updates the recipient from list count.
        /// </summary>
        private void UpdateRecipientFromListCount()
        {
            IQueryable<GroupMember> groupMemberQuery = GetRecipientFromListSelection();

            if ( groupMemberQuery != null )
            {
                int groupMemberCount = groupMemberQuery.Count();
                lRecipientFromListCount.Visible = true;

                lRecipientFromListCount.Text = string.Format( "{0} {1} selected", groupMemberCount, "recipient".PluralizeIf( groupMemberCount != 1 ) );
            }
            else
            {
                lRecipientFromListCount.Visible = false;
            }
        }

        /// <summary>
        /// Gets the GroupMember Query for the recipients selected on the 'Select From List' tab
        /// </summary>
        /// <param name="groupMemberQuery">The group member query.</param>
        /// <returns></returns>
        private IQueryable<GroupMember> GetRecipientFromListSelection()
        {
            IQueryable<GroupMember> groupMemberQuery = null;
            var rockContext = new RockContext();
            var groupMemberService = new GroupMemberService( rockContext );
            var personService = new PersonService( rockContext );
            var dataViewService = new DataViewService( rockContext );
            int? communicationGroupId = ddlCommunicationGroupList.SelectedValue.AsIntegerOrNull();
            if ( communicationGroupId.HasValue )
            {
                groupMemberQuery = groupMemberService.Queryable().Where( a => a.GroupId == communicationGroupId.Value && a.GroupMemberStatus == GroupMemberStatus.Active );

                var segmentFilterType = rblCommunicationGroupSegmentFilterType.SelectedValueAsEnum<SegmentCriteria>();
                var segmentDataViewIds = cblCommunicationGroupSegments.Items.OfType<ListItem>().Where( a => a.Selected ).Select( a => a.Value.AsInteger() ).ToList();

                Expression segmentExpression = null;
                ParameterExpression paramExpression = personService.ParameterExpression;
                var segmentDataViewList = dataViewService.GetByIds( segmentDataViewIds ).AsNoTracking().ToList();
                foreach ( var segmentDataView in segmentDataViewList )
                {
                    List<string> errorMessages;

                    var exp = segmentDataView.GetExpression( personService, paramExpression, out errorMessages );
                    if ( exp != null )
                    {
                        if ( segmentExpression == null )
                        {
                            segmentExpression = exp;
                        }
                        else
                        {
                            if ( segmentFilterType == SegmentCriteria.All )
                            {
                                segmentExpression = Expression.AndAlso( segmentExpression, exp );
                            }
                            else
                            {
                                segmentExpression = Expression.OrElse( segmentExpression, exp );
                            }
                        }
                    }
                }

                if ( segmentExpression != null )
                {
                    var personQry = personService.Get( paramExpression, segmentExpression );
                    groupMemberQuery = groupMemberQuery.Where( a => personQry.Any( p => p.Id == a.PersonId ) );
                }
            }

            return groupMemberQuery;
        }

        #endregion Recipient Selection

        #region Medium Selection

        /// <summary>
        /// Shows the medium selection.
        /// </summary>
        private void ShowMediumSelection()
        {
            pnlMediumSelection.Visible = true;
        }

        /// <summary>
        /// Handles the Click event of the btnMediumSelectionPrevious control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnMediumSelectionPrevious_Click( object sender, EventArgs e )
        {
            pnlMediumSelection.Visible = false;
            ShowRecipientSelection();
        }

        /// <summary>
        /// Handles the Click event of the btnMediumSelectionNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnMediumSelectionNext_Click( object sender, EventArgs e )
        {
            if ( dtpSendDateTime.Visible )
            {
                if ( !dtpSendDateTime.SelectedDateTime.HasValue )
                {
                    nbSendDateTimeWarning.NotificationBoxType = NotificationBoxType.Danger;
                    nbSendDateTimeWarning.Text = "Send Date Time is required";
                    nbSendDateTimeWarning.Visible = true;
                    return;
                }
                else if ( dtpSendDateTime.SelectedDateTime.Value < RockDateTime.Now )
                {
                    nbSendDateTimeWarning.NotificationBoxType = NotificationBoxType.Danger;
                    nbSendDateTimeWarning.Text = "Send Date Time must be immediate or a future date/time";
                    nbSendDateTimeWarning.Visible = true;
                }
            }

            // set the confirmation send datetime controls to what we pick here
            tglSendDateTimeConfirmation.Checked = tglSendDateTime.Checked;
            tglSendDateTimeConfirmation_CheckedChanged( null, null );
            dtpSendDateTimeConfirmation.SelectedDateTime = dtpSendDateTime.SelectedDateTime;

            nbSendDateTimeWarning.Visible = false;

            pnlMediumSelection.Visible = false;

            ShowTemplateSelection();
        }

        /// <summary>
        /// Handles the CheckedChanged event of the tglSendDateTime control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void tglSendDateTime_CheckedChanged( object sender, EventArgs e )
        {
            nbSendDateTimeWarning.Visible = false;
            dtpSendDateTime.Visible = !tglSendDateTime.Checked;
        }

        #endregion Medium Selection

        #region Template Selection

        /// <summary>
        /// Shows the template selection.
        /// </summary>
        private void ShowTemplateSelection()
        {
            pnlTemplateSelection.Visible = true;
            BindTemplatePicker();
        }

        /// <summary>
        /// Binds the template picker.
        /// </summary>
        private void BindTemplatePicker()
        {
            var rockContext = new RockContext();

            var templateQuery = new CommunicationTemplateService( rockContext ).Queryable().OrderBy( a => a.Name );

            // get list of templates that the current user is authorized to View
            IEnumerable<CommunicationTemplate> templateList = templateQuery.AsNoTracking().ToList().Where( a => a.IsAuthorized( Rock.Security.Authorization.VIEW, this.CurrentPerson ) ).ToList();

            // If this is an Email (or RecipientPreference) communication, limit to templates that support the email wizard
            Rock.Model.CommunicationType communicationType = ( Rock.Model.CommunicationType ) hfMediumType.Value.AsInteger();
            if ( communicationType == CommunicationType.Email || communicationType == CommunicationType.RecipientPreference )
            {
                templateList = templateList.Where( a => a.SupportsEmailWizard() );
            }
            else
            {
                templateList = templateList.Where( a => a.HasSMSTemplate() || a.Guid == Rock.SystemGuid.Communication.COMMUNICATION_TEMPLATE_BLANK.AsGuid() );
            }

            rptSelectTemplate.DataSource = templateList;
            rptSelectTemplate.DataBind();
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rptSelectTemplate control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rptSelectTemplate_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            CommunicationTemplate communicationTemplate = e.Item.DataItem as CommunicationTemplate;

            if ( communicationTemplate != null )
            {
                Literal lTemplateImagePreview = e.Item.FindControl( "lTemplateImagePreview" ) as Literal;
                Literal lTemplateName = e.Item.FindControl( "lTemplateName" ) as Literal;
                Literal lTemplateDescription = e.Item.FindControl( "lTemplateDescription" ) as Literal;
                LinkButton btnSelectTemplate = e.Item.FindControl( "btnSelectTemplate" ) as LinkButton;

                if ( communicationTemplate.ImageFileId.HasValue )
                {
                    var imageUrl = string.Format( "~/GetImage.ashx?id={0}", communicationTemplate.ImageFileId );
                    lTemplateImagePreview.Text = string.Format( "<img src='{0}' width='100%'/>", this.ResolveRockUrl( imageUrl ) );
                }
                else
                {
                    lTemplateImagePreview.Text = string.Format( "<img src='{0}'/>", this.ResolveRockUrl( "~/Assets/Images/communication-template-default.svg" ) );
                }

                lTemplateName.Text = communicationTemplate.Name;
                lTemplateDescription.Text = communicationTemplate.Description;
                btnSelectTemplate.CommandName = "CommunicationTemplateId";
                btnSelectTemplate.CommandArgument = communicationTemplate.Id.ToString();

                if ( hfSelectedCommunicationTemplateId.Value == communicationTemplate.Id.ToString() )
                {
                    btnSelectTemplate.AddCssClass( "template-selected" );
                }
                else
                {
                    btnSelectTemplate.RemoveCssClass( "template-selected" );
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btnSelectTemplate control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSelectTemplate_Click( object sender, EventArgs e )
        {
            hfSelectedCommunicationTemplateId.Value = ( sender as LinkButton ).CommandArgument;

            var communicationTemplate = new CommunicationTemplateService( new RockContext() ).Get( hfSelectedCommunicationTemplateId.Value.AsInteger() );

            tbFromName.Text = communicationTemplate.FromName;
            tbFromAddress.Text = communicationTemplate.FromEmail;

            tbReplyToAddress.Text = communicationTemplate.ReplyToEmail;
            tbCCList.Text = communicationTemplate.CCEmails;
            tbBCCList.Text = communicationTemplate.BCCEmails;

            hfShowAdditionalFields.Value = ( !string.IsNullOrEmpty( communicationTemplate.ReplyToEmail ) || !string.IsNullOrEmpty( communicationTemplate.CCEmails ) || !string.IsNullOrEmpty( communicationTemplate.BCCEmails ) ).ToTrueFalse().ToLower();

            tbEmailSubject.Text = communicationTemplate.Subject;

            hfEmailEditorHtml.Value = communicationTemplate.Message;

            hfAttachedBinaryFileIds.Value = communicationTemplate.Attachments.Select( a => a.BinaryFileId ).ToList().AsDelimited( "," );
            UpdateAttachedFiles( false );

            // SMS Fields
            if ( communicationTemplate.SMSFromDefinedValueId.HasValue )
            {
                ddlSMSFrom.SetValue( communicationTemplate.SMSFromDefinedValueId.Value );
            }
            
            tbSMSTextMessage.Text = communicationTemplate.SMSMessage;


            btnTemplateSelectionNext_Click( sender, e );
        }

        /// <summary>
        /// Handles the Click event of the btnTemplateSelectionPrevious control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnTemplateSelectionPrevious_Click( object sender, EventArgs e )
        {
            pnlTemplateSelection.Visible = false;

            ShowMediumSelection();
        }

        /// <summary>
        /// Handles the Click event of the btnTemplateSelectionNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnTemplateSelectionNext_Click( object sender, EventArgs e )
        {
            if ( !hfSelectedCommunicationTemplateId.Value.AsIntegerOrNull().HasValue )
            {
                nbTemplateSelectionWarning.Text = "Please select a template.";
                nbTemplateSelectionWarning.Visible = true;
                return;
            }

            nbTemplateSelectionWarning.Visible = false;

            pnlTemplateSelection.Visible = false;

            // The next page should be ShowEmailSummary since this is the Select Email Template Page, but just in case...
            Rock.Model.CommunicationType communicationType = ( Rock.Model.CommunicationType ) hfMediumType.Value.AsInteger();
            if ( communicationType == CommunicationType.Email || communicationType == CommunicationType.RecipientPreference )
            {
                ShowEmailSummary();
            }
            else if ( communicationType == CommunicationType.SMS )
            {
                ShowMobileTextEditor();
            }
        }

        #endregion Template Selection

        #region Email Editor

        /// <summary>
        /// Shows the email editor.
        /// </summary>
        private void ShowEmailEditor()
        {
            ifEmailDesigner.Attributes["srcdoc"] = hfEmailEditorHtml.Value;
            pnlEmailEditor.Visible = true;
        }

        /// <summary>
        /// Handles the Click event of the btnEmailEditorPrevious control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnEmailEditorPrevious_Click( object sender, EventArgs e )
        {
            pnlEmailEditor.Visible = false;
            Rock.Model.CommunicationType communicationType = ( Rock.Model.CommunicationType ) hfMediumType.Value.AsInteger();
            if ( communicationType == CommunicationType.SMS || communicationType == CommunicationType.RecipientPreference )
            {
                ShowMobileTextEditor();
            }
            else
            {
                ShowEmailSummary();
            }
        }

        /// <summary>
        /// Handles the Click event of the btnEmailEditorNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnEmailEditorNext_Click( object sender, EventArgs e )
        {
            ifEmailDesigner.Attributes["srcdoc"] = hfEmailEditorHtml.Value;
            pnlEmailEditor.Visible = false;

            ShowConfirmation();
        }

        /// <summary>
        /// Handles the Click event of the btnEmailSendTest control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnEmailSendTest_Click( object sender, EventArgs e )
        {
            // upnlContent.Update();
        }

        /// <summary>
        /// Handles the Click event of the btnEmailPreview control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnEmailPreview_Click( object sender, EventArgs e )
        {
            upnlEmailPreview.Update();

            ifEmailPreview.Attributes["srcdoc"] = hfEmailEditorHtml.Value;
            ifEmailDesigner.Attributes["srcdoc"] = hfEmailEditorHtml.Value;
            pnlEmailPreview.Visible = true;
            mdEmailPreview.Show();
        }

        #endregion Email Editor

        #region Email Summary

        /// <summary>
        /// Shows the email summary.
        /// </summary>
        private void ShowEmailSummary()
        {
            // See if the template supports preview-text
            HtmlAgilityPack.HtmlDocument templateDoc = new HtmlAgilityPack.HtmlDocument();
            templateDoc.LoadHtml( hfEmailEditorHtml.Value );
            var preheaderTextNode = templateDoc.GetElementbyId( "preheader-text" );
            tbEmailPreview.Visible = preheaderTextNode != null;
            tbEmailPreview.Text = preheaderTextNode != null ? preheaderTextNode.InnerHtml : string.Empty;

            pnlEmailSummary.Visible = true;
        }

        /// <summary>
        /// Handles the Click event of the btnEmailSummaryPrevious control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnEmailSummaryPrevious_Click( object sender, EventArgs e )
        {
            pnlTemplateSelection.Visible = true;
            BindTemplatePicker();

            pnlEmailSummary.Visible = false;
        }

        /// <summary>
        /// Handles the Click event of the btnEmailSummaryNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnEmailSummaryNext_Click( object sender, EventArgs e )
        {
            if ( tbEmailPreview.Visible )
            {
                // set the preheader-text of our email html
                HtmlAgilityPack.HtmlDocument templateDoc = new HtmlAgilityPack.HtmlDocument();
                templateDoc.LoadHtml( hfEmailEditorHtml.Value );
                var preheaderTextNode = templateDoc.GetElementbyId( "preheader-text" );
                if ( preheaderTextNode != null )
                {
                    preheaderTextNode.InnerHtml = tbEmailPreview.Text;
                    hfEmailEditorHtml.Value = templateDoc.DocumentNode.OuterHtml;
                }

                tbEmailPreview.Text = preheaderTextNode != null ? preheaderTextNode.InnerHtml : string.Empty;
            }

            pnlEmailSummary.Visible = false;

            Rock.Model.CommunicationType communicationType = ( Rock.Model.CommunicationType ) hfMediumType.Value.AsInteger();
            if ( communicationType == CommunicationType.SMS || communicationType == CommunicationType.RecipientPreference )
            {
                ShowMobileTextEditor();
            }
            else if ( communicationType == CommunicationType.Email )
            {
                ShowEmailEditor();
            }
        }

        /// <summary>
        /// Handles the FileUploaded event of the fupAttachments control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FileUploaderEventArgs"/> instance containing the event data.</param>
        protected void fupAttachments_FileUploaded( object sender, FileUploaderEventArgs e )
        {
            UpdateAttachedFiles( true );
        }

        /// <summary>
        /// Updates the HTML for the Attached Files and optionally adds the file that was just uploaded using the file uploader
        /// </summary>
        private void UpdateAttachedFiles( bool addUploadedFile )
        {
            List<int> attachmentList = hfAttachedBinaryFileIds.Value.SplitDelimitedValues().AsIntegerList();
            if ( addUploadedFile && fupAttachments.BinaryFileId.HasValue )
            {
                if ( !attachmentList.Contains( fupAttachments.BinaryFileId.Value ) )
                {
                    attachmentList.Add( fupAttachments.BinaryFileId.Value );
                }
            }

            hfAttachedBinaryFileIds.Value = attachmentList.AsDelimited( "," );
            fupAttachments.BinaryFileId = null;

            // pre-populate dictionary so that the attachments are listed in the order they were added
            Dictionary<int, string> binaryFileAttachments = attachmentList.ToDictionary( k => k, v => string.Empty );
            using ( var rockContext = new RockContext() )
            {
                var binaryFileInfoList = new BinaryFileService( rockContext ).Queryable()
                        .Where( f => attachmentList.Contains( f.Id ) )
                        .Select( f => new
                        {
                            f.Id,
                            f.FileName
                        } );

                foreach ( var binaryFileInfo in binaryFileInfoList )
                {
                    binaryFileAttachments[binaryFileInfo.Id] = binaryFileInfo.FileName;
                }
            }

            StringBuilder sbAttachmentsHtml = new StringBuilder();
            sbAttachmentsHtml.AppendLine( "<div class='attachment'>" );
            sbAttachmentsHtml.AppendLine( "  <ul class='attachment-content'>" );

            foreach ( var binaryFileAttachment in binaryFileAttachments )
            {
                var attachmentUrl = string.Format( "{0}GetFile.ashx?id={1}", System.Web.VirtualPathUtility.ToAbsolute( "~" ), binaryFileAttachment.Key );
                var removeAttachmentJS = string.Format( "removeAttachment( this, '{0}', '{1}' );", hfAttachedBinaryFileIds.ClientID, binaryFileAttachment.Key );
                sbAttachmentsHtml.AppendLine( string.Format( "    <li><a href='{0}' target='_blank'>{1}</a> <a><i class='fa fa-times' onclick=\"{2}\"></i></a></li>", attachmentUrl, binaryFileAttachment.Value, removeAttachmentJS ) );
            }

            sbAttachmentsHtml.AppendLine( "  </ul>" );
            sbAttachmentsHtml.AppendLine( "</div>" );

            lAttachmentListHtml.Text = sbAttachmentsHtml.ToString();
        }

        #endregion Email Summary

        #region Mobile Text Editor

        /// <summary>
        /// Shows the mobile text editor.
        /// </summary>
        private void ShowMobileTextEditor()
        {
            // TODO: is this the right person
            if ( !ddlSMSFrom.SelectedValue.AsIntegerOrNull().HasValue )
            {
                InitializeSMSFromSender( this.CurrentPerson );
            }

            mfpSMSMessage.MergeFields.Clear();
            mfpSMSMessage.MergeFields.Add( "GlobalAttribute" );
            mfpSMSMessage.MergeFields.Add( "Rock.Model.Person" );

            var currentPersonSMSNumber = this.CurrentPerson.PhoneNumbers.FirstOrDefault( a => a.IsMessagingEnabled );
            tbTestSMSNumber.Text = currentPersonSMSNumber != null ? currentPersonSMSNumber.NumberFormatted : string.Empty;

            nbSMSTestResult.Visible = false;
            pnlMobileTextEditor.Visible = true;
        }

        /// <summary>
        /// Handles the Click event of the btnMobileTextEditorPrevious control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnMobileTextEditorPrevious_Click( object sender, EventArgs e )
        {
            pnlMobileTextEditor.Visible = false;

            Rock.Model.CommunicationType communicationType = ( Rock.Model.CommunicationType ) hfMediumType.Value.AsInteger();
            if ( communicationType == CommunicationType.Email || communicationType == CommunicationType.RecipientPreference )
            {
                ShowEmailSummary();
            }
            else
            {
                ShowTemplateSelection();
            }
        }

        /// <summary>
        /// Handles the Click event of the btnMobileTextEditorNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnMobileTextEditorNext_Click( object sender, EventArgs e )
        {
            pnlMobileTextEditor.Visible = false;
            Rock.Model.CommunicationType communicationType = ( Rock.Model.CommunicationType ) hfMediumType.Value.AsInteger();
            if ( communicationType == CommunicationType.Email || communicationType == CommunicationType.RecipientPreference )
            {
                ShowEmailEditor();
            }
            else
            {
                ShowConfirmation();
            }
        }

        /// <summary>
        /// Initializes the SMS from sender.
        /// </summary>
        /// <param name="sender">The sender.</param>
        public void InitializeSMSFromSender( Person sender )
        {
            
            var numbers = DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.COMMUNICATION_SMS_FROM.AsGuid() );
            if ( numbers != null )
            {
                foreach ( var number in numbers.DefinedValues )
                {
                    var personAliasGuid = number.GetAttributeValue( "ResponseRecipient" ).AsGuidOrNull();
                    if ( personAliasGuid.HasValue && sender.Aliases.Any( a => a.Guid == personAliasGuid.Value ) )
                    {
                        ddlSMSFrom.SetValue( number.Id );
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Handles the SelectItem event of the mfpMessage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void mfpMessage_SelectItem( object sender, EventArgs e )
        {
            tbSMSTextMessage.Text += mfpSMSMessage.SelectedMergeField;
            mfpSMSMessage.SetValue( string.Empty );
        }

        /// <summary>
        /// Handles the Click event of the btnSMSSendTest control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSMSSendTest_Click( object sender, EventArgs e )
        {
            // TODO

            nbSMSTestResult.Text = string.Format( "Test SMS has been sent to {0}.", tbTestSMSNumber.Text );
            nbSMSTestResult.Dismissable = true;
            nbSMSTestResult.Visible = true;
        }

        #endregion Mobile Text Editor

        #region Confirmation

        /// <summary>
        /// Shows the confirmation.
        /// </summary>
        private void ShowConfirmation()
        {
            pnlConfirmation.Visible = true;
            string sendDateTimeText = string.Empty;
            if ( tglSendDateTimeConfirmation.Checked )
            {
                sendDateTimeText = "immediately";
            }
            else if ( dtpSendDateTimeConfirmation.SelectedDateTime.HasValue )
            {
                sendDateTimeText = dtpSendDateTimeConfirmation.SelectedDateTime.Value.ToString( "f" );
            }

            lConfirmationSendDateTimeHtml.Text = string.Format( "This communication has been configured to send {0}.", sendDateTimeText );

            int sendCount;
            string sendCountTerm = "individual";

            if ( tglRecipientSelection.Checked )
            {
                sendCount = this.GetRecipientFromListSelection().Count();
            }
            else
            {
                sendCount = this.IndividualRecipientPersonIds.Count();
            }

            lblConfirmationSendHtml.Text = string.Format(
@"<p>Now Is the Moment Of Truth</p>
<p>You are about to send this communication to <strong>{0}</strong> {1}</p>
",
sendCount,
sendCountTerm.PluralizeIf( sendCount != 1 ) );

            int maxRecipients = GetAttributeValue( "MaximumRecipients" ).AsIntegerOrNull() ?? int.MaxValue;
            bool userCanApprove = IsUserAuthorized( "Approve" );
            if ( sendCount > maxRecipients && !userCanApprove )
            {
                btnSend.Text = "Submit";
            }
            else
            {
                btnSend.Text = "Send";
            }
        }

        /// <summary>
        /// Handles the Click event of the btnConfirmationPrevious control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnConfirmationPrevious_Click( object sender, EventArgs e )
        {
            pnlConfirmation.Visible = false;

            Rock.Model.CommunicationType communicationType = ( Rock.Model.CommunicationType ) hfMediumType.Value.AsInteger();
            if ( communicationType == CommunicationType.Email || communicationType == CommunicationType.RecipientPreference )
            {
                ShowEmailEditor();
            }
            else if ( communicationType == CommunicationType.SMS )
            {
                ShowMobileTextEditor();
            }
        }

        /// <summary>
        /// Handles the Click event of the btnSend control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSend_Click( object sender, EventArgs e )
        {
            if ( !ValidateSendDateTime() )
            {
                return;
            }

            var rockContext = new RockContext();
            Rock.Model.Communication communication = UpdateCommunication( rockContext );

            int maxRecipients = GetAttributeValue( "MaximumRecipients" ).AsIntegerOrNull() ?? int.MaxValue;
            bool userCanApprove = IsUserAuthorized( "Approve" );
            var recipientCount = communication.GetRecipientCount( rockContext );
            string message = string.Empty;
            if ( recipientCount > maxRecipients && !userCanApprove )
            {
                communication.Status = CommunicationStatus.PendingApproval;
                message = "Communication has been submitted for approval.";
            }
            else
            {
                communication.Status = CommunicationStatus.Approved;
                communication.ReviewedDateTime = RockDateTime.Now;
                communication.ReviewerPersonAliasId = CurrentPersonAliasId;

                if ( communication.FutureSendDateTime.HasValue &&
                               communication.FutureSendDateTime > RockDateTime.Now )
                {
                    message = string.Format(
                        "Communication will be sent {0}.",
                        communication.FutureSendDateTime.Value.ToRelativeDateString( 0 ) );
                }
                else
                {
                    message = "Communication has been queued for sending.";
                }
            }

            rockContext.SaveChanges();

            // send approval email if needed (now that we have a communication id)
            if ( communication.Status == CommunicationStatus.PendingApproval )
            {
                var approvalTransaction = new Rock.Transactions.SendCommunicationApprovalEmail();
                approvalTransaction.CommunicationId = communication.Id;
                approvalTransaction.ApprovalPageUrl = HttpContext.Current.Request.Url.AbsoluteUri;
                Rock.Transactions.RockQueue.TransactionQueue.Enqueue( approvalTransaction );
            }

            if ( communication.Status == CommunicationStatus.Approved &&
                ( !communication.FutureSendDateTime.HasValue || communication.FutureSendDateTime.Value <= RockDateTime.Now ) )
            {
                if ( GetAttributeValue( "SendWhenApproved" ).AsBoolean() )
                {
                    var transaction = new Rock.Transactions.SendCommunicationTransaction();
                    transaction.CommunicationId = communication.Id;
                    transaction.PersonAlias = CurrentPersonAlias;
                    Rock.Transactions.RockQueue.TransactionQueue.Enqueue( transaction );
                }
            }

            nbSendCommunication.NotificationBoxType = NotificationBoxType.Success;
            nbSendCommunication.Text = string.Format( "(#TODO: Test actually sending the communication) {0}  CommunicationId={1}", message, communication.Id );
            nbSendCommunication.Dismissable = true;
            nbSendCommunication.Visible = true;
        }

        /// <summary>
        /// Handles the Click event of the btnSaveAsDraft control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSaveAsDraft_Click( object sender, EventArgs e )
        {
            if ( !ValidateSendDateTime() )
            {
                return;
            }

            var rockContext = new RockContext();
            Rock.Model.Communication communication = UpdateCommunication( rockContext );
            communication.Status = CommunicationStatus.Draft;
            rockContext.SaveChanges();
            string message = "The communication has been saved";

            nbSendCommunication.NotificationBoxType = NotificationBoxType.Success;
            nbSendCommunication.Text = string.Format( "(#TODO: Test actually sending the communication) {0}  CommunicationId={1}", message, communication.Id );
            nbSendCommunication.Dismissable = true;
            nbSendCommunication.Visible = true;
        }

        /// <summary>
        /// Displays a message if the send datetime is not valid, and returns true if send datetime is valid
        /// </summary>
        /// <returns></returns>
        private bool ValidateSendDateTime()
        {
            if ( dtpSendDateTimeConfirmation.Visible )
            {
                if ( !dtpSendDateTimeConfirmation.SelectedDateTime.HasValue )
                {
                    nbSendDateTimeWarningConfirmation.NotificationBoxType = NotificationBoxType.Danger;
                    nbSendDateTimeWarningConfirmation.Text = "Send Date Time is required";
                    nbSendDateTimeWarningConfirmation.Visible = true;
                    return false;
                }
                else if ( dtpSendDateTimeConfirmation.SelectedDateTime.Value < RockDateTime.Now )
                {
                    nbSendDateTimeWarningConfirmation.NotificationBoxType = NotificationBoxType.Danger;
                    nbSendDateTimeWarningConfirmation.Text = "Send Date Time must be immediate or a future date/time";
                    nbSendDateTimeWarningConfirmation.Visible = true;
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Updates the communication.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        private Rock.Model.Communication UpdateCommunication( RockContext rockContext )
        {
            var communicationService = new CommunicationService( rockContext );
            var communicationAttachmentService = new CommunicationAttachmentService( rockContext );
            var communicationRecipientService = new CommunicationRecipientService( rockContext );

            Rock.Model.Communication communication = null;
            IQueryable<CommunicationRecipient> qryRecipients = null;

            int communicationId = hfCommunicationId.Value.AsInteger();
            if ( communicationId > 0 )
            {
                communication = communicationService.Get( communicationId );
            }

            List<int> recipientPersonIds;
            if ( tglRecipientSelection.Checked )
            {
                recipientPersonIds = GetRecipientFromListSelection().Select( a => a.PersonId ).ToList();
            }
            else
            {
                recipientPersonIds = this.IndividualRecipientPersonIds;
            }

            if ( communication != null )
            {
                // Remove any deleted recipients
                HashSet<int> personIdHash = new HashSet<int>( recipientPersonIds );
                qryRecipients = communication.GetRecipientsQry( rockContext );

                foreach ( var item in qryRecipients.Select( a => new
                {
                    Id = a.Id,
                    PersonId = a.PersonAlias.PersonId
                } ) )
                {
                    if ( !personIdHash.Contains( item.PersonId ) )
                    {
                        var recipient = qryRecipients.Where( a => a.Id == item.Id ).FirstOrDefault();
                        communicationRecipientService.Delete( recipient );
                        communication.Recipients.Remove( recipient );
                    }
                }
            }

            if ( communication == null )
            {
                communication = new Rock.Model.Communication();
                communication.Status = CommunicationStatus.Transient;
                communication.SenderPersonAliasId = CurrentPersonAliasId;
                communicationService.Add( communication );
            }

            communication.EnabledLavaCommands = GetAttributeValue( "EnabledLavaCommands" );

            if ( qryRecipients == null )
            {
                qryRecipients = communication.GetRecipientsQry( rockContext );
            }

            // Add any new recipients
            HashSet<int> communicationPersonIdHash = new HashSet<int>( qryRecipients.Select( a => a.PersonAlias.PersonId ) );
            foreach ( var recipientPersonId in recipientPersonIds )
            {
                if ( !communicationPersonIdHash.Contains( recipientPersonId ) )
                {
                    var person = new PersonService( rockContext ).Get( recipientPersonId );
                    if ( person != null )
                    {
                        var communicationRecipient = new CommunicationRecipient();
                        communicationRecipient.PersonAlias = person.PrimaryAlias;
                        communication.Recipients.Add( communicationRecipient );
                    }
                }
            }

            communication.IsBulkCommunication = tglBulkCommunication.Checked;
            communication.CommunicationType = ( CommunicationType ) hfMediumType.Value.AsInteger();

            communication.ListGroupId = ddlCommunicationGroupList.SelectedValue.AsIntegerOrNull();
            var segmentDataViewIds = cblCommunicationGroupSegments.Items.OfType<ListItem>().Where( a => a.Selected ).Select( a => a.Value.AsInteger() ).ToList();
            var segmentDataViewGuids = new DataViewService( rockContext ).GetByIds( segmentDataViewIds ).Select( a => a.Guid ).ToList();

            communication.Segments = segmentDataViewGuids.AsDelimited( "," );
            communication.SegmentCriteria = rblCommunicationGroupSegmentFilterType.SelectedValueAsEnum<SegmentCriteria>();

            communication.CommunicationTemplateId = hfSelectedCommunicationTemplateId.Value.AsIntegerOrNull();

            communication.FromName = tbFromName.Text;
            communication.FromEmail = tbFromAddress.Text;
            communication.ReplyToEmail = tbReplyToAddress.Text;
            communication.CCEmails = tbCCList.Text;
            communication.BCCEmails = tbBCCList.Text;

            communication.AttachmentBinaryFileIds = hfAttachedBinaryFileIds.Value.SplitDelimitedValues().AsIntegerList();

            // delete any attachments that are no longer included
            foreach ( var attachment in communication.Attachments.Where( a => !communication.AttachmentBinaryFileIds.Contains( a.BinaryFileId ) ).ToList() )
            {
                communication.Attachments.Remove( attachment );
                communicationAttachmentService.Delete( attachment );
            }

            // add any new attachments that were added
            foreach ( var attachmentBinaryFileId in communication.AttachmentBinaryFileIds.Where( a => !communication.Attachments.Any( x => x.BinaryFileId == a ) ) )
            {
                communication.Attachments.Add( new CommunicationAttachment { BinaryFileId = attachmentBinaryFileId } );
            }

            communication.Subject = tbEmailSubject.Text;
            communication.Message = hfEmailEditorHtml.Value;

            communication.SMSFromDefinedValueId = ddlSMSFrom.SelectedValue.AsIntegerOrNull();
            communication.SMSMessage = tbSMSTextMessage.Text;

            if ( tglSendDateTimeConfirmation.Checked )
            {
                communication.FutureSendDateTime = null;
            }
            else
            {
                communication.FutureSendDateTime = dtpSendDateTimeConfirmation.SelectedDateTime;
            }

            return communication;
        }

        /// <summary>
        /// Handles the Click event of the btnConfirmationCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnConfirmationCancel_Click( object sender, EventArgs e )
        {
            // TODO. What should this do?
        }

        /// <summary>
        /// Handles the CheckedChanged event of the tglSendDateTimeConfirmation control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void tglSendDateTimeConfirmation_CheckedChanged( object sender, EventArgs e )
        {
            nbSendDateTimeWarningConfirmation.Visible = false;
            dtpSendDateTimeConfirmation.Visible = !tglSendDateTimeConfirmation.Checked;
        }

        #endregion

        #region Methods

        #endregion

        // remove before flight
        string sampleTemplate = @"<!DOCTYPE html>
<html>
<head>
<title>A Responsive Email Template</title>

<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />
<style type=""text/css"">
    /* CLIENT-SPECIFIC STYLES */
    body, table, td, a{-webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%;} /* Prevent WebKit and Windows mobile changing default text sizes */
    table, td{mso-table-lspace: 0pt; mso-table-rspace: 0pt;} /* Remove spacing between tables in Outlook 2007 and up */
    img{-ms-interpolation-mode: bicubic;} /* Allow smoother rendering of resized image in Internet Explorer */

    /* RESET STYLES */
    img{border: 0; height: auto; line-height: 100%; outline: none; text-decoration: none;}
    table{border-collapse: collapse !important;}
    body{height: 100% !important; margin: 0 !important; padding: 0 !important; width: 100% !important;}

    /* iOS BLUE LINKS */
    a[x-apple-data-detectors] {
        color: inherit !important;
        text-decoration: none !important;
        font-size: inherit !important;
        font-family: inherit !important;
        font-weight: inherit !important;
        line-height: inherit !important;
    }

    /* MOBILE STYLES */
    @media screen and (max-width: 525px) {

        /* ALLOWS FOR FLUID TABLES */
        .wrapper {
          width: 100% !important;
        	max-width: 100% !important;
        }

        /* ADJUSTS LAYOUT OF LOGO IMAGE */
        .logo img {
          margin: 0 auto !important;
        }

        /* USE THESE CLASSES TO HIDE CONTENT ON MOBILE */
        .mobile-hide {
          display: none !important;
        }

        .img-max {
          max-width: 100% !important;
          width: 100% !important;
          height: auto !important;
        }

        /* FULL-WIDTH TABLES */
        .responsive-table {
          width: 100% !important;
        }

        /* UTILITY CLASSES FOR ADJUSTING PADDING ON MOBILE */
        .padding {
          padding: 10px 5% 15px 5% !important;
        }

        .padding-meta {
          padding: 30px 5% 0px 5% !important;
          text-align: center;
        }

        .padding-copy {
     		padding: 10px 5% 10px 5% !important;
          text-align: center;
        }

        .no-padding {
          padding: 0 !important;
        }

        .section-padding {
          padding: 50px 15px 50px 15px !important;
        }

        /* ADJUST BUTTONS ON MOBILE */
        .mobile-button-container {
            margin: 0 auto;
            width: 100% !important;
        }

        .mobile-button {
            padding: 15px !important;
            border: 0 !important;
            font-size: 16px !important;
            display: block !important;
        }

    }

    /* ANDROID CENTER FIX */
    div[style*=""margin: 16px 0;""] { margin: 0 !important; }
</style>
<!--[if gte mso 12]>
<style type=""text/css"">
.mso-right {
	padding-left: 20px;
}
</style>
<![endif]-->
</head>
<body style=""margin: 0 !important; padding: 0 !important;"">

<!-- HIDDEN PREHEADER TEXT -->
<div id=""preheader-text"" style=""display: none; font-size: 1px; color: #fefefe; line-height: 1px; font-family: Helvetica, Arial, sans-serif; max-height: 0px; max-width: 0px; opacity: 0; overflow: hidden;"">
    Entice the open with some amazing preheader text. Use a little mystery and get those subscribers to read through...
</div>

<!-- HEADER -->
<table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
    <tr>
        <td bgcolor=""#333333"" align=""center"">
            <!--[if (gte mso 9)|(IE)]>
            <table align=""center"" border=""0"" cellspacing=""0"" cellpadding=""0"" width=""500"">
            <tr>
            <td align=""center"" valign=""top"" width=""500"">
            <![endif]-->
            <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 500px;"" class=""wrapper"">
                <tr>
                    <td align=""center"" valign=""top"" style=""padding: 15px 0;"" class=""logo"">
                        <a href=""http://litmus.com"" target=""_blank"">
                            <img alt=""Logo"" src=""http://www.minecartstudio.com/Content/Misc/logo-1.jpg"" width=""60"" height=""60"" style=""display: block; font-family: Helvetica, Arial, sans-serif; color: #ffffff; font-size: 16px;"" border=""0"">
                        </a>
                    </td>
                </tr>
            </table>
            <!--[if (gte mso 9)|(IE)]>
            </td>
            </tr>
            </table>
            <![endif]-->
        </td>
    </tr>
    <tr>
        <td bgcolor=""#D8F1FF"" align=""center"" style=""padding: 70px 15px 70px 15px;"" class=""section-padding"">
            <!--[if (gte mso 9)|(IE)]>
            <table align=""center"" border=""0"" cellspacing=""0"" cellpadding=""0"" width=""500"">
            <tr>
            <td align=""center"" valign=""top"" width=""500"">
            <![endif]-->
            <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 500px;"" class=""responsive-table"">
                <tr>
                    <td style=""font-size: 25px; font-family: Helvetica, Arial, sans-serif; color: #333333; padding-top: 30px;"">
					    <div class=""structure-dropzone"">
					
                            <div class=""dropzone"">

							    <div class=""component component-text"" data-content=""<h1>Hello There!</h1>"" data-state=""component"">
								    <h1>Hello There!</h1>
							    </div>

						    </div>
                        </div>
					
					</td>
                </tr>
            </table>
            <!--[if (gte mso 9)|(IE)]>
            </td>
            </tr>
            </table>
            <![endif]-->
        </td>
    </tr>
    <tr>
        <td bgcolor=""#ffffff"" align=""center"" style=""padding: 70px 15px 25px 15px;"" class=""section-padding"">
            <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""500"" style=""padding:0 0 20px 0;"" class=""responsive-table"">
                <tr>
                    <td align=""center"" height=""100%"" valign=""top"" width=""100%"" style=""padding-bottom: 35px;"">
                        <!--[if (gte mso 9)|(IE)]>
                        <table align=""center"" border=""0"" cellspacing=""0"" cellpadding=""0"" width=""500"">
                        <tr>
                        <td align=""center"" valign=""top"" width=""500"">
                        <![endif]-->
                        <table align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width:500;"">
                            <tr>
                                <td align=""center"" valign=""top"">
                                    <!--[if (gte mso 9)|(IE)]>
                                    <table align=""center"" border=""0"" cellspacing=""0"" cellpadding=""0"" width=""500"">
                                    <tr>
                                    <td align=""left"" valign=""top"" width=""150"">
                                    <![endif]-->
                                    
                                    <div class=""structure-dropzone"">
									
									<div class=""dropzone""></div>

                                    </div>

                                </td>
                            </tr>
                        </table>
                        <!--[if (gte mso 9)|(IE)]>
                        </td>
                        </tr>
                        </table>
                        <![endif]-->
                    </td>
                </tr>
                
            </table>
        </td>
    </tr>
    
        
    <tr>
        <td bgcolor=""#ffffff"" align=""center"" style=""padding: 20px 0px;"">
            <!--[if (gte mso 9)|(IE)]>
            <table align=""center"" border=""0"" cellspacing=""0"" cellpadding=""0"" width=""500"">
            <tr>
            <td align=""center"" valign=""top"" width=""500"">
            <![endif]-->
            <!-- UNSUBSCRIBE COPY -->
            <table width=""100%"" border=""0"" cellspacing=""0"" cellpadding=""0"" align=""center"" style=""max-width: 500px;"" class=""responsive-table"">
                <tr>
                    <td align=""center"" style=""font-size: 12px; line-height: 18px; font-family: Helvetica, Arial, sans-serif; color:#666666;"">
                        1234 Main Street, Anywhere, MA 01234, USA
                        <br>
                        <a href=""http://litmus.com"" target=""_blank"" style=""color: #666666; text-decoration: none;"">Unsubscribe</a>
                        <span style=""font-family: Arial, sans-serif; font-size: 12px; color: #444444;"">&nbsp;&nbsp;|&nbsp;&nbsp;</span>
                        <a href=""http://litmus.com"" target=""_blank"" style=""color: #666666; text-decoration: none;"">View this email in your browser</a>
                    </td>
                </tr>
            </table>
            <!--[if (gte mso 9)|(IE)]>
            </td>
            </tr>
            </table>
            <![endif]-->
        </td>
    </tr>
</table>
</body>
</html>
";


    }
}