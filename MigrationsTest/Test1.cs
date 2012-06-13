//-----------------------------------------------------------------------
// <copyright file="Test1.cs" company="Mission3, Inc.">
//      Copyright (c) Mission3, Inc. All rights reserved.
//
//      The MIT License (MIT)
//
//      Permission is hereby granted, free of charge, to any person
//      obtaining a copy of this software and associated documentation
//      files (the "Software"), to deal in the Software without
//      restriction, including without limitation the rights to use,
//      copy, modify, merge, publish, distribute, sublicense, and/or sell
//      copies of the Software, and to permit persons to whom the
//      Software is furnished to do so, subject to the following
//      conditions:
//
//      The above copyright notice and this permission notice shall be
//      included in all copies or substantial portions of the Software.
//
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
//      OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//      NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
//      HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
//      WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
//      FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
//      OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Migrations;

namespace MigrationsTest
{
    [TestClass]
    public class Test1
    {
        private MigrationService runner;
        private IVersionDataSource versionDataSource;

        [TestInitialize]
        public void SetUp()
        {
            versionDataSource = new StubVersionDataSource();
            versionDataSource.SetVersionNumber(0);
            runner = new MigrationService(versionDataSource);
        }

        [TestMethod]
        public void TestLoadMigrationsFromAssembly()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            runner.LoadMigrationsFromAssembly(asm); // Only assemblies with default empty ctor will get loaded
            List<IMigration> migrations = runner.Migrations;
            Assert.IsTrue(migrations.Count == 3); // Migration1 and Migration2 should be only ones loaded
            foreach (IMigration m in migrations)
            {
                Type foo = m.GetType();
                Assert.IsTrue(foo.IsClass);
                Assert.IsNotNull(foo.GetInterface("IMigration"));
                Assert.IsNotNull(MigrationService.GetMigrationsAttributes(m));
            }
        }

        [TestMethod]
        public void TestLoadMigrationsFromAssemblyWithParams()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            runner.LoadMigrationsFromAssembly(asm, "");
                // Load migrations with default ctor, and optionally if ctor takes a string
            List<IMigration> migrations = runner.Migrations;
            Assert.IsTrue(migrations.Count == 4);
                // Migration1, Migration2, and MigrationWithCTorParams should be loaded
            foreach (IMigration m in migrations)
            {
                Type foo = m.GetType();
                Assert.IsTrue(foo.IsClass);
                Assert.IsNotNull(foo.GetInterface("IMigration"));
                Assert.IsNotNull(MigrationService.GetMigrationsAttributes(m));
            }
        }

        [TestMethod]
        public void TestGetMigrationAttributes()
        {
            var foo = new Migration1();
            MigrationAttribute attr = MigrationService.GetMigrationsAttributes(foo);
            Assert.AreEqual(attr.Version, 1.0);
        }

        [TestMethod]
        public void TestGetMigrationNoAttributes()
        {
            IMigration foo = new MigrationNoAttributes();
            MigrationAttribute attr = MigrationService.GetMigrationsAttributes(foo);
            Assert.IsNull(attr);
        }

        [TestMethod]
        public void TestGetMigrationWrongAttributes()
        {
            IMigration foo = new MigrationWrongAttributes();
            MigrationAttribute attr = MigrationService.GetMigrationsAttributes(foo);
            Assert.IsNull(attr);
        }

        [TestMethod]
        public void TestMigrationSort()
        {
            IMigration migration1 = new Migration1();
            IMigration migration2 = new Migration2();

            var migrations = new List<IMigration>
                                 {
                                     migration2,
                                     migration1
                                 };

            migrations.Sort(MigrationService.MigrationSorter);
            Assert.AreSame(migrations[0], migration1);

            migrations = new List<IMigration>
                             {
                                 migration1,
                                 migration2
                             };

            migrations.Sort(MigrationService.MigrationSorter);
            Assert.AreSame(migrations[0], migration1);
        }

        [TestMethod]
        public void TestGetMigrationVersionNumber()
        {
            IMigration migration1 = new Migration1();
            Assert.AreEqual(1.0, MigrationService.GetMigrationVersionNumber(migration1));
        }

        [TestMethod]
        public void TestGetMigrationVersionNumberNoAttributes()
        {
            IMigration migration = new MigrationNoAttributes();
            Assert.AreEqual(-1, MigrationService.GetMigrationVersionNumber(migration));
        }

        [TestMethod]
        public void TestGetMigrationVersionNumberWrongAttributes()
        {
            IMigration migration = new MigrationWrongAttributes();
            Assert.AreEqual(-1, MigrationService.GetMigrationVersionNumber(migration));
        }

        [TestMethod]
        public void TestRunUpMigrations()
        {
            IMigration migration1 = new Migration1();
            IMigration migration2 = new Migration2();
            var migrations = new List<IMigration>
                                 {
                                     migration1,
                                     migration2
                                 };

            try
            {
                runner.Migrations = migrations;
                runner.RunUpMigrations();
                // Assert that we ran two upgrade migrations, are at version 2
                Assert.IsTrue(versionDataSource.GetVersionNumber() == 2);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestRunUpMigrationsTo()
        {
            IMigration migration1 = new Migration1();
            IMigration migration2 = new Migration2();
            var migrations = new List<IMigration>
                                 {
                                     migration1,
                                     migration2
                                 };

            try
            {
                const int UPGRADE_TO_VERSION = 2;
                runner.Migrations = migrations;
                runner.RunUpMigrations(UPGRADE_TO_VERSION);
                // Assert that we ran two upgrade migrations, are at version UGRADE_TO_VERSION
                Assert.IsTrue(versionDataSource.GetVersionNumber() == UPGRADE_TO_VERSION);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestRunUpMigrationsToBogus()
        {
            IMigration migration1 = new Migration1();
            IMigration migration2 = new Migration2();
            var migrations = new List<IMigration>
                                 {
                                     migration1,
                                     migration2
                                 };

            try
            {
                const int SCHEMA_VERSION = 2;
                versionDataSource.SetVersionNumber(SCHEMA_VERSION);

                const int UPGRADE_TO_VERSION = 1;
                runner.Migrations = migrations;
                runner.RunUpMigrations(UPGRADE_TO_VERSION);

                // Assert that we did not run the bogus upgrade and that the schema version did not change
                Assert.IsTrue(versionDataSource.GetVersionNumber() == SCHEMA_VERSION);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestRunUpMigrationsBogus()
        {
            IMigration migration1 = new MigrationNoAttributes();
            IMigration migration2 = new MigrationWrongAttributes();
            var migrations = new List<IMigration>
                                 {
                                     migration1,
                                     migration2
                                 };

            try
            {
                const int SCHEMA_VERSION = 10;
                versionDataSource.SetVersionNumber(SCHEMA_VERSION);
                runner.Migrations = migrations;
                runner.RunUpMigrations();
                // Assert that we didn't run the bogus migrations
                Assert.IsTrue(versionDataSource.GetVersionNumber() == SCHEMA_VERSION);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestRunUpToMigrationsBogus()
        {
            IMigration migration1 = new MigrationNoAttributes();
            IMigration migration2 = new MigrationWrongAttributes();
            var migrations = new List<IMigration>
                                 {
                                     migration1,
                                     migration2
                                 };

            try
            {
                const int SCHEMA_VERSION = 0;
                const int UPGRADE_TO_VERSION = 2;
                versionDataSource.SetVersionNumber(SCHEMA_VERSION);
                runner.Migrations = migrations;
                runner.RunUpMigrations(UPGRADE_TO_VERSION);
                // Assert that we didn't run the bogus migrations
                Assert.IsTrue(versionDataSource.GetVersionNumber() == SCHEMA_VERSION);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestRunDownMigrations()
        {
            IMigration migration1 = new Migration1();
            IMigration migration2 = new Migration2();
            var migrations = new List<IMigration>
                                 {
                                     migration1,
                                     migration2
                                 };

            try
            {
                versionDataSource.SetVersionNumber(2);
                runner.Migrations = migrations;
                runner.RunDownMigrations();

                // Assert that we end up downgraded to version 0
                Assert.IsTrue(versionDataSource.GetVersionNumber() == 0);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestRunDownMigrationsToBogus()
        {
            IMigration migration1 = new MigrationWrongAttributes();
            IMigration migration2 = new MigrationNoAttributes();
            var migrations = new List<IMigration>
                                 {
                                     migration1,
                                     migration2
                                 };

            try
            {
                const int SCHEMA_VERSION = 10;
                versionDataSource.SetVersionNumber(SCHEMA_VERSION);

                const int DOWNGRADE_TO_VERSION = 1;
                runner.Migrations = migrations;
                runner.RunDownMigrations(DOWNGRADE_TO_VERSION);

                // Assert that we did not run the bogus downgrade and that the schema version did not change
                Assert.IsTrue(versionDataSource.GetVersionNumber() == SCHEMA_VERSION);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestRunDownMigrationsToBogusVersion()
        {
            IMigration migration1 = new Migration1();
            IMigration migration2 = new Migration2();
            var migrations = new List<IMigration>
                                 {
                                     migration1,
                                     migration2
                                 };

            try
            {
                const int SCHEMA_VERSION = 2;
                versionDataSource.SetVersionNumber(SCHEMA_VERSION);

                const int DOWNGRADE_TO_VERSION = 5;
                runner.Migrations = migrations;
                runner.RunDownMigrations(DOWNGRADE_TO_VERSION);
                runner.RunDownMigrations(SCHEMA_VERSION);

                // Assert that we did not run the bogus downgrade and that the schema version did not change
                Assert.IsTrue(versionDataSource.GetVersionNumber() == SCHEMA_VERSION);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestRunDownMigrationsTo()
        {
            IMigration migration1 = new Migration1();
            IMigration migration2 = new Migration2();
            var migrations = new List<IMigration>
                                 {
                                     migration1,
                                     migration2
                                 };

            try
            {
                const int DOWNGRADE_TO_VERSION = 1;
                const int SCHEMA_VERSION = 2;

                versionDataSource.SetVersionNumber(SCHEMA_VERSION);

                runner.Migrations = migrations;
                runner.RunDownMigrations(DOWNGRADE_TO_VERSION);
                // Assert that we ran two downgrade migrations, are at version DOWNGRADE_TO_VERSION
                Assert.IsTrue(versionDataSource.GetVersionNumber() == DOWNGRADE_TO_VERSION);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestRunDownMigrationsBogus()
        {
            IMigration migration1 = new MigrationNoAttributes();
            IMigration migration2 = new MigrationWrongAttributes();
            var migrations = new List<IMigration>
                                 {
                                     migration1,
                                     migration2
                                 };

            try
            {
                const int SCHEMA_VERSION = 10;
                versionDataSource.SetVersionNumber(SCHEMA_VERSION);

                runner.Migrations = migrations;
                runner.RunDownMigrations();

                // Assert that we didn't run the bogus migrations
                Assert.IsTrue(versionDataSource.GetVersionNumber() == SCHEMA_VERSION);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestRunMigrateTo()
        {
            IMigration migration1 = new Migration1();
            IMigration migration2 = new Migration2();
            var migrations = new List<IMigration>
                                 {
                                     migration1,
                                     migration2
                                 };

            try
            {
                int SCHEMA_VERSION = 2;
                int MIGRATE_TO_VERSION = 1;

                runner.Migrations = migrations;

                // Migrate down 
                versionDataSource.SetVersionNumber(SCHEMA_VERSION);
                runner.MigrateToVersion(MIGRATE_TO_VERSION);
                Assert.IsTrue(versionDataSource.GetVersionNumber() == MIGRATE_TO_VERSION);

                // Migrate up
                SCHEMA_VERSION = 1;
                MIGRATE_TO_VERSION = 2;
                versionDataSource.SetVersionNumber(SCHEMA_VERSION);
                runner.MigrateToVersion(MIGRATE_TO_VERSION);
                Assert.IsTrue(versionDataSource.GetVersionNumber() == MIGRATE_TO_VERSION);

                // no change
                SCHEMA_VERSION = 1;
                MIGRATE_TO_VERSION = 1;
                versionDataSource.SetVersionNumber(SCHEMA_VERSION);
                runner.MigrateToVersion(MIGRATE_TO_VERSION);
                Assert.IsTrue(versionDataSource.GetVersionNumber() == SCHEMA_VERSION);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestIndexerGet()
        {
            var m1 = new Migration1();
            runner.Migrations.Add(m1);
            Assert.AreSame(m1, runner[0]);
        }

        [TestMethod]
        public void TestIndexSet()
        {
            var m1 = new Migration1();
            runner.Migrations.Add(m1);

            var m2 = new Migration2();
            runner[0] = m2;

            Assert.AreEqual(m2, runner.Migrations[0]);
        }

        [TestMethod]
        public void TestRunUpMigrationsWithVersionsSkipped()
        {
            IMigration migration1 = new Migration1();
            IMigration migration3 = new Migration3();
            var migrations = new List<IMigration>
                                 {
                                     migration1,
                                     migration3
                                 };

            try
            {
                runner.Migrations = migrations;
                runner.RunUpMigrations();
                // Assert that we ran two upgrade migrations, are at version UGRADE_TO_VERSION
                Assert.IsTrue(versionDataSource.GetVersionNumber() == 3);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }
    }
}