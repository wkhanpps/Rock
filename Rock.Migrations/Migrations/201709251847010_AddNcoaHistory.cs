// <copyright>
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
namespace Rock.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    /// <summary>
    ///
    /// </summary>
    public partial class AddNcoaHistory : Rock.Migrations.RockMigration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            CreateTable(
                "dbo.NcoaHistory",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        PersonAliasId = c.Int(nullable: false),
                        FamilyId = c.Int(nullable: false),
                        MoveType = c.Int(nullable: false),
                        NcoaType = c.Int(nullable: false),
                        AddressStatus = c.Int(nullable: false),
                        AddressInvalidReason = c.Int(nullable: false),
                        OriginalStreet1 = c.String(maxLength: 100),
                        OriginalStreet2 = c.String(maxLength: 100),
                        OriginalCity = c.String(maxLength: 50),
                        OriginalState = c.String(maxLength: 50),
                        OriginalPostalCode = c.String(maxLength: 50),
                        UpdatedStreet1 = c.String(maxLength: 100),
                        UpdatedStreet2 = c.String(maxLength: 100),
                        UpdatedCity = c.String(maxLength: 50),
                        UpdatedState = c.String(maxLength: 50),
                        UpdatedPostalCode = c.String(maxLength: 50),
                        UpdatedCountry = c.String(maxLength: 50),
                        UpdatedBarcode = c.String(maxLength: 40),
                        UpdatedAddressType = c.Int(nullable: false),
                        MoveDate = c.DateTime(),
                        MoveDistance = c.Decimal(precision: 6, scale: 2),
                        MatchFlag = c.Int(nullable: false),
                        Processed = c.Int(nullable: false),
                        NcoaRunDateTime = c.DateTime(nullable: false),
                        NcoaNote = c.String(),
                        CreatedDateTime = c.DateTime(),
                        ModifiedDateTime = c.DateTime(),
                        CreatedByPersonAliasId = c.Int(),
                        ModifiedByPersonAliasId = c.Int(),
                        Guid = c.Guid(nullable: false),
                        ForeignId = c.Int(),
                        ForeignGuid = c.Guid(),
                        ForeignKey = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.PersonAlias", t => t.CreatedByPersonAliasId)
                .ForeignKey("dbo.PersonAlias", t => t.ModifiedByPersonAliasId)
                .ForeignKey("dbo.PersonAlias", t => t.PersonAliasId)
                .Index(t => t.PersonAliasId)
                .Index(t => t.CreatedByPersonAliasId)
                .Index(t => t.ModifiedByPersonAliasId)
                .Index(t => t.Guid, unique: true);

            RockMigrationHelper.AddGlobalAttribute( Rock.SystemGuid.FieldType.DECIMAL, null, null, "Move Distance to Inactivate", "The distance that someone must move before they will be automatically inactivated by the NCOA process. Set this number to a very high number (9999999) to disable the Inactivation.", 0, "", "538CE5DD-10AE-4232-B655-EAC0FCB54A50", "MoveDistanceToInactivate" );
        }
        
        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            DropForeignKey("dbo.NcoaHistory", "PersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo.NcoaHistory", "ModifiedByPersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo.NcoaHistory", "CreatedByPersonAliasId", "dbo.PersonAlias");
            DropIndex("dbo.NcoaHistory", new[] { "Guid" });
            DropIndex("dbo.NcoaHistory", new[] { "ModifiedByPersonAliasId" });
            DropIndex("dbo.NcoaHistory", new[] { "CreatedByPersonAliasId" });
            DropIndex("dbo.NcoaHistory", new[] { "PersonAliasId" });
            DropTable("dbo.NcoaHistory");
        }
    }
}
