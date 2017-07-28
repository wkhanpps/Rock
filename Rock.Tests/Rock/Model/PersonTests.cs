using System;
using System.Linq;

using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using RockM = Rock.Migrations;

using Xunit;

namespace Rock.Tests.Rock.Model
{
    public class PersonTests
    {
        //[Fact( Skip = "Need a mock for Global Attributes" )]
        [Fact]
        public void OffsetGraduatesToday()
        {
            var RockContext = new RockContext();
            var x = RockContext.Database.Connection.ConnectionString;
            InitGlobalAttributesCache();
            var person = new Person();
            person.GraduationYear = RockDateTime.Now.Year; // the "year" the person graduates.

            Assert.True( -1 == person.GradeOffset );
        }

        //[Fact( Skip = "Need a mock for Global Attributes" )]
        [Fact]
        public void OffsetGraduatesTomorrow()
        {
            InitGlobalAttributesCache();

            DateTime tomorrow = RockDateTime.Now.AddDays( 1 );
            SetGradeTransitionDateGlobalAttribute( tomorrow.Month, tomorrow.Day );

            var Person = new Person();
            Person.GraduationYear = RockDateTime.Now.Year; // the "year" the person graduates.

            Assert.True( 0 == Person.GradeOffset );
        }

        //[Fact( Skip = "Need a mock for Global Attributes" )]
        [Fact]
        public void GraduatesThisYear()
        {
            InitGlobalAttributesCache();
            var Person = new Person();
            Person.GradeOffset = 0;

            Assert.True( Person.GraduationYear == RockDateTime.Now.AddYears( 1 ).Year );
        }

        //[Fact( Skip = "Need a mock for Global Attributes" )]
        [Fact]
        public void GraduatesNextYear()
        {
            InitGlobalAttributesCache();

            DateTime tomorrow = RockDateTime.Now.AddDays( 1 );
            SetGradeTransitionDateGlobalAttribute( tomorrow.Month, tomorrow.Day );

            var Person = new Person();
            Person.GradeOffset = 1;

            Assert.True( Person.GraduationYear - RockDateTime.Now.Year == 1 );
        }

        private static void InitGlobalAttributesCache()
        {
            initDB();
            DateTime today = RockDateTime.Now;
            GlobalAttributesCache globalAttributes = GlobalAttributesCache.Read();
            globalAttributes.SetValue( "GradeTransitionDate", string.Format( "{0}/{1}", today.Month, today.Day ), false );
        }

        private static void SetGradeTransitionDateGlobalAttribute( int month, int day )
        {
            GlobalAttributesCache globalAttributes = GlobalAttributesCache.Read();
            globalAttributes.SetValue( "GradeTransitionDate", string.Format( "{0}/{1}", month, day ), false );
        }

        private static void initDB()
        {
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Clear all cache
                RockMemoryCache.Clear();

                // Get a db context
                using ( var rockContext = new RockContext() )
                {
                    //// Run any needed Rock and/or plugin migrations
                    //// NOTE: MigrateDatabase must be the first thing that touches the database to help prevent EF from creating empty tables for a new database
                    MigrateDatabase( rockContext );
                }
            }
            catch { }
        }

        /// <summary>
        /// Migrates the database.
        /// </summary>
        /// <returns>True if at least one migration was run</returns>
        public static bool MigrateDatabase( RockContext rockContext )
        {
            // get the pendingmigrations sorted by name (in the order that they run), then run to the latest migration
            var migrator = new System.Data.Entity.Migrations.DbMigrator( new RockM.Configuration() );
            var pendingMigrations = migrator.GetPendingMigrations().OrderBy( a => a );
            if ( pendingMigrations.Any() )
            {

                var lastMigration = pendingMigrations.Last();

                // NOTE: we need to specify the last migration vs null so it won't detect/complain about pending changes
                migrator.Update( lastMigration );
            }

            return true;
        }

    }
}