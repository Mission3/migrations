using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Migrations;
using System.Reflection;

namespace MigrationsTest
{
    [TestClass]
    public class Test1
    {
        private MigrationService runner;

        [TestInitialize]
        public void SetUp()
        {
            IVersionDataSource versionDataSource = new StubVersionDataSource();
            versionDataSource.SetVersionNumber(0);
            this.runner = new MigrationService(versionDataSource);
        }

        [TestMethod]
        public void TestLoadMigrationsFromAssembly()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            this.runner.LoadMigrationsFromAssembly(asm);
            List<IMigration> migrations = this.runner.Migrations;
            Assert.IsTrue(migrations.Count == 4);
        }

        [TestMethod]
        public void TestGetMigrationAttributes()
        {
            Migration1 foo = new Migration1();
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

            List<IMigration> migrations = new List<IMigration>{
                migration2,
                migration1
            };

            migrations.Sort(MigrationService.MigrationSorter);
            Assert.AreSame(migrations[0], migration1);

            migrations = new List<IMigration>{
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
            List<IMigration> migrations = new List<IMigration>() {
                migration1,
                migration2
            };

            try
            {
                this.runner.Migrations = migrations;
                this.runner.RunUpMigrations();
                Assert.IsTrue(true);
            }
            catch(Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestRunDownMigrations()
        {
            IMigration migration1 = new Migration1();
            IMigration migration2 = new Migration2();
            List<IMigration> migrations = new List<IMigration>() {
                migration1,
                migration2
            };

            try
            {
                this.runner.Migrations = migrations;
                this.runner.RunDownMigrations();
                Assert.IsTrue(true);
            }
            catch(Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        //[TestMethod]
        //public void TestRunMigrations()
        //{
        //    IMigration migration1 = new Migration1();
        //    IMigration migration2 = new Migration2();
        //    List<IMigration> migrations = new List<IMigration>() {
        //        migration1,
        //        migration2
        //    };

        //    try
        //    {
        //        this.runner.RunMigrations(migrations, m => m.Up());
        //        this.runner.RunMigrations(migrations, m => m.Down());
        //        Assert.IsTrue(true);
        //    }
        //    catch (Exception ex)
        //    {
        //        Assert.Fail(ex.Message);
        //    }
        //}
    }
}
