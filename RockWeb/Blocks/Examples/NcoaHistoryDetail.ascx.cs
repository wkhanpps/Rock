// <copyright>uns   
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
using System.ComponentModel;
using System.Collections.Generic;
using System.Web.UI;

using Rock;
using Rock.Attribute;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Security;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using System.Linq;
using System.Globalization;

namespace RockWeb.Blocks.Examples
{
    [DisplayName( "Ncoa History Detail" )]
    [Category( "Examples" )]
    [Description( "Parse the Ncoa History CSV" )]

    public partial class NcoaHistoryDetail : RockBlock
    {

        #region Control Methods

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

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

            }
        }

        #endregion

        #region Internal Methods

        private void MapNcoaRow()
        {

        }


        #endregion

        #region Edit Events


        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
        }

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            BinaryFile binaryFile;
            var rockContext = new RockContext();
            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            NcoaHistoryService ncoaHistoryService = new NcoaHistoryService( rockContext );
            PersonAliasService personAliasService = new PersonAliasService( rockContext );

            if ( FileUploader1.BinaryFileId.HasValue )
            {
                binaryFile = binaryFileService.Get( FileUploader1.BinaryFileId.Value );
                TextReader tr = new StreamReader( binaryFile.ContentStream );
                var csv = new CsvReader( tr );
                csv.Configuration.RegisterClassMap<NcoaRowMap>();
                var records = csv.GetRecords<NcoaRow>()
                            .OrderBy( a => a.PersonAliasId )
                            .ThenBy( a => a.RecordType ).Take( 50000 )
                            .ToList();

                int previousPersonId = 0;
                bool ncoaIsValid = false;
                foreach ( var record in records )
                {
                    //We can be comment out this section to improve the performance if we are assure that personAliasId in csv will always be present.
                    //var person = personAliasService.Get( record.PersonAliasId );

                    //if ( person == null )
                    //{
                    //    continue;
                    //}

                    NcoaHistory ncoaHistory = new NcoaHistory()
                    {
                        PersonAliasId = record.PersonAliasId,
                        FamilyId = record.FamilyId,
                        NcoaRunDateTime = RockDateTime.Now,
                    };

                    if ( !string.IsNullOrEmpty( record.Vacant ) && record.Vacant.ToUpper() == "Y" )
                    {
                        ncoaHistory.AddressStatus = AddressStatus.InValid;
                        ncoaHistory.AddressInvalidReason = AddressInvalidReason.Vacant;
                        ncoaIsValid = true;
                    }
                    else if ( !string.IsNullOrEmpty( record.RecordType ) )
                    {
                        if ( record.RecordType.ToUpper() == "A" && !string.IsNullOrEmpty( record.AddressStatus ) && record.AddressStatus.ToUpper() == "N" )
                        {
                            ncoaHistory.NcoaType = NcoaType.NoMove;
                            ncoaHistory.AddressStatus = AddressStatus.InValid;
                            ncoaHistory.AddressInvalidReason = AddressInvalidReason.NotFound;
                            ncoaIsValid = true;
                        }
                        else if ( record.RecordType.ToUpper() == "C" && !IsAddressSame( record ) )
                        {
                            SetPreviousAddress( record, ncoaHistory );
                            SetNewAddress( record, ncoaHistory );
                            ncoaHistory.NcoaType = NcoaType.Move;
                            ncoaHistory.MoveDistance = record.MoveDistance;
                            ncoaHistory.AddressStatus = AddressStatus.Valid;
                            if ( !string.IsNullOrEmpty( record.MoveDate ) )
                            {
                                ncoaHistory.MoveDate = DateTime.ParseExact( record.MoveDate + "01",
                                    "yyyyMMdd",
                                   CultureInfo.InvariantCulture );
                            }
                            switch ( record.MoveType.ToUpper() )
                            {
                                case "B":
                                    ncoaHistory.MoveType = MoveType.Business;
                                    break;
                                case "F":
                                    ncoaHistory.MoveType = MoveType.Family;
                                    break;
                                case "I":
                                    ncoaHistory.MoveType = MoveType.Individual;
                                    break;
                                default:
                                    ncoaHistory.MoveType = MoveType.None;
                                    break;
                            }

                            switch ( record.MatchFlag.ToUpper() )
                            {
                                case "M":
                                    ncoaHistory.MatchFlag = MatchFlag.Moved;
                                    break;
                                case "G":
                                    ncoaHistory.MatchFlag = MatchFlag.POBoxClosed;
                                    break;
                                case "K":
                                    ncoaHistory.MatchFlag = MatchFlag.MovedNoForwarding;
                                    break;
                                case "F":
                                    ncoaHistory.MatchFlag = MatchFlag.MovedToForeignCountry;
                                    break;
                                default:
                                    ncoaHistory.MatchFlag = MatchFlag.None;
                                    break;
                            }
                            ncoaIsValid = true;
                        }
                        else if ( record.RecordType.ToUpper() == "H" && !string.IsNullOrEmpty( record.Ank ) && record.Ank.Trim() == "77" && previousPersonId != record.PersonAliasId )
                        {
                            ncoaHistory.NcoaType = NcoaType.Month48Move;
                            ncoaHistory.AddressStatus = AddressStatus.Valid;
                            SetPreviousAddress( record, ncoaHistory );
                            ncoaIsValid = true;
                        }

                    }

                    if ( ncoaIsValid )
                    {
                        ncoaHistoryService.Add( ncoaHistory );
                    }

                    ncoaIsValid = false;
                    previousPersonId = record.PersonAliasId;
                }
            }

            rockContext.WrapTransaction( () =>
            {
                rockContext.SaveChanges();

                var tempBinaryFile = binaryFileService.Get( FileUploader1.BinaryFileId.Value );
                if ( tempBinaryFile != null && tempBinaryFile.IsTemporary )
                {
                    binaryFileService.Delete( tempBinaryFile );
                }

                rockContext.SaveChanges();
                FileUploader1.BinaryFileId = null;
            } );
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the FileUploaded event of the fsFile control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void fsFile_FileUploaded( object sender, EventArgs e )
        {
            //var rockContext = new RockContext();
            //var binaryFileService = new BinaryFileService( rockContext );
            //BinaryFile binaryFile = null;
            //if ( fsFile.BinaryFileId.HasValue )
            //{
            //    binaryFile = binaryFileService.Get( fsFile.BinaryFileId.Value );
            //}

            //if ( binaryFile != null )
            //{
            //    if ( !string.IsNullOrWhiteSpace( tbName.Text ) )
            //    {
            //        binaryFile.FileName = tbName.Text;
            //    }

            //    // set binaryFile.Id to original id since the UploadedFile is a temporary binaryFile with a different id
            //    binaryFile.Id = hfBinaryFileId.ValueAsInt();
            //    binaryFile.Description = tbDescription.Text;
            //    binaryFile.BinaryFileTypeId = ddlBinaryFileType.SelectedValueAsInt();
            //    if ( binaryFile.BinaryFileTypeId.HasValue )
            //    {
            //        binaryFile.BinaryFileType = new BinaryFileTypeService( rockContext ).Get( binaryFile.BinaryFileTypeId.Value );
            //    }

            //    var tempList = OrphanedBinaryFileIdList;
            //    tempList.Add( fsFile.BinaryFileId.Value );
            //    OrphanedBinaryFileIdList = tempList;

            //}
        }

        #endregion

        #region Methods

        /// <summary>
        /// Set the Previous Address in reference to CSV Record
        /// </summary>
        private void SetPreviousAddress( NcoaRow record, NcoaHistory ncoaHistory )
        {
            ncoaHistory.OriginalStreet1 = FindAndReplaceNull( record.OriginalStreet1 );
            ncoaHistory.OriginalStreet2 = FindAndReplaceNull( record.OriginalStreet2 );
            ncoaHistory.OriginalCity = FindAndReplaceNull( record.OriginalCity );
            ncoaHistory.OriginalState = FindAndReplaceNull( record.OriginalState );
            ncoaHistory.OriginalPostalCode = FindAndReplaceNull( record.OriginalPostalCode );
        }


        /// <summary>
        /// Set the New Address in reference to CSV Record
        /// </summary>
        private void SetNewAddress( NcoaRow record, NcoaHistory ncoaHistory )
        {
            ncoaHistory.UpdatedStreet1 = FindAndReplaceNull( record.UpdatedStreet1 );
            ncoaHistory.UpdatedStreet2 = FindAndReplaceNull( record.UpdatedStreet2 );
            ncoaHistory.UpdatedCity = FindAndReplaceNull( record.UpdatedCity );
            ncoaHistory.UpdatedState = FindAndReplaceNull( record.UpdatedState );
            ncoaHistory.UpdatedPostalCode = FindAndReplaceNull( record.UpdatedPostalCode );
            ncoaHistory.UpdatedCountry = FindAndReplaceNull( record.UpdatedCountry );
            ncoaHistory.UpdatedBarcode = FindAndReplaceNull( record.UpdatedBarcode );
        }

        private bool IsAddressSame( NcoaRow record )
        {
            if ( FindAndReplaceNull( record.OriginalStreet1 ) == FindAndReplaceNull( record.UpdatedStreet1 ) &&
                    FindAndReplaceNull( record.OriginalStreet2 ) == FindAndReplaceNull( record.UpdatedStreet2 ) )
            {
                return true;
            }
            return false;
        }

        private string FindAndReplaceNull( string value )
        {
            return ( !string.IsNullOrEmpty( value ) && value.ToUpper() == "NULL" ) ? string.Empty : value;
        }

        #endregion

        #region nestedClass

        public sealed class NcoaRowMap : CsvClassMap<NcoaRow>
        {
            public NcoaRowMap()
            {

                Map( m => m.PersonAliasId ).Name( "input_PersonAliasId" );
                Map( m => m.OriginalStreet1 ).Name( "input_Street1" );
                Map( m => m.OriginalStreet2 ).Name( "input_Street2" );
                Map( m => m.OriginalCity ).Name( "input_City" );
                Map( m => m.OriginalState ).Name( "input_State" );
                Map( m => m.OriginalPostalCode ).Name( "input_Postal Code" );
                Map( m => m.UpdatedStreet1 ).Name( "address_line_1" );
                Map( m => m.UpdatedStreet2 ).Name( "input_Street2" );
                Map( m => m.UpdatedCity ).Name( "city_name" );
                Map( m => m.UpdatedState ).Name( "state_code" );
                Map( m => m.UpdatedPostalCode ).Name( "postal_code" );
                Map( m => m.UpdatedCountry ).Name( "country_code" );
                Map( m => m.AddressStatus ).Name( "address_status" );
                Map( m => m.UpdatedAddressType ).Name( "address_type" );
                Map( m => m.UpdatedBarcode ).Name( "barcode" );
                Map( m => m.MoveDate ).Name( "move_date" );
                Map( m => m.MoveDistance ).Name( "move_distance" );
                Map( m => m.MoveType ).Name( "move_type" );
                Map( m => m.MatchFlag ).Name( "match_flag" );
                Map( m => m.Vacant ).Name( "vacant" );
                Map( m => m.Ank ).Name( "ank" );
                Map( m => m.RecordType ).Name( "record_type" );
                Map( m => m.FamilyId ).Name( "input_FamilyId" );
            }
        }

        public class NcoaRow
        {
            public int PersonAliasId { get; set; }

            /// <summary>
            /// Gets or sets the family group identifier.
            /// </summary>
            /// <value>
            /// The family group identifier.
            /// </value>
            public int FamilyId { get; set; }

            /// <summary>
            /// Gets or sets the move type value identifier.
            /// </summary>
            /// <value>
            /// The move type value identifier.
            /// </value>
            public string MoveType { get; set; }

            /// <summary>
            /// Gets or sets the address status.
            /// </summary>
            /// <value>
            /// The address status.
            /// </value>
            public string AddressStatus { get; set; }

            /// <summary>
            /// Gets or sets the avacant.
            /// </summary>
            /// <value>
            /// The Vacant.
            /// </value>
            public string Vacant { get; set; }

            /// <summary>
            /// Gets or sets the Original street 1.
            /// </summary>
            /// <value>
            /// The original street 1.
            /// </value>
            public string OriginalStreet1 { get; set; }

            /// <summary>
            /// Gets or sets the Original street 2.
            /// </summary>
            /// <value>
            /// The original street 2.
            /// </value>
            public string OriginalStreet2 { get; set; }

            /// <summary>
            /// Gets or sets the Original city.
            /// </summary>
            /// <value>
            /// The original city.
            /// </value>
            public string OriginalCity { get; set; }

            /// <summary>
            /// Gets or sets the Original state.
            /// </summary>
            /// <value>
            /// The original state.
            /// </value>
            public string OriginalState { get; set; }

            /// <summary>
            /// Gets or sets the Original postal code.
            /// </summary>
            /// <value>
            /// The original postal code.
            /// </value>
            public string OriginalPostalCode { get; set; }

            /// <summary>
            /// Gets or sets the Updated street 1.
            /// </summary>
            /// <value>
            /// The updated street 1.
            /// </value>
            public string UpdatedStreet1 { get; set; }

            /// <summary>
            /// Gets or sets the Updated street 2.
            /// </summary>
            /// <value>
            /// The updated street 2.
            /// </value>
            public string UpdatedStreet2 { get; set; }

            /// <summary>
            /// Gets or sets the Updated city.
            /// </summary>
            /// <value>
            /// The updated city.
            /// </value>
            public string UpdatedCity { get; set; }

            /// <summary>
            /// Gets or sets the Updated state.
            /// </summary>
            /// <value>
            /// The updated state.
            /// </value>
            public string UpdatedState { get; set; }

            /// <summary>
            /// Gets or sets the Updated postal code.
            /// </summary>
            /// <value>
            /// The updated postal code.
            /// </value>
            public string UpdatedPostalCode { get; set; }

            /// <summary>
            /// Gets or sets the Updated country.
            /// </summary>
            /// <value>
            /// The updated country.
            /// </value>
            public string UpdatedCountry { get; set; }

            /// <summary>
            /// Gets or sets the Updated barcode.
            /// </summary>
            /// <value>
            /// The updated barcode.
            /// </value>
            public string UpdatedBarcode { get; set; }

            /// <summary>
            /// Gets or sets the Updated address type.
            /// </summary>
            /// <value>
            /// The updated address type.
            /// </value>
            public string UpdatedAddressType { get; set; }

            /// <summary>
            /// Gets or sets the date when moved.
            /// </summary>
            /// <value>
            /// The move date.
            /// </value>
            public string MoveDate { get; set; }

            /// <summary>
            /// Gets or sets the moving distance.
            /// </summary>
            /// <value>
            /// The moving distance.
            /// </value>
            public decimal? MoveDistance { get; set; }

            /// <summary>
            /// Gets or sets the match flag.
            /// </summary>
            /// <value>
            /// The match flag.
            /// </value>
            public string MatchFlag { get; set; }

            /// <summary>
            /// Gets or sets the ank.
            /// </summary>
            /// <value>
            /// The ank.
            /// </value>
            public string Ank { get; set; }

            /// <summary>
            /// Gets or sets the record type.
            /// </summary>
            /// <value>
            /// The record type.
            /// </value>
            public string RecordType { get; set; }
        }
        #endregion
    }
}