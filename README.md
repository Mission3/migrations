Data Agnostic Migrations for .NET or DAMN
=========================================

Description
-----------
Ruby on Rails inspired migrations framework for C#. 

Why?
----
The current C# migration frameworks available only target specific databases. We wanted something that was database and datas tore agnostic and could target SharePoint lists. The downside to being data agnostic however, is you will need some helper libraries for your schema changes (to create tables, columns, etc).


Getting Started
---------------

1. Create a class that implements the IVersionDataSource interface.

    * IVersionDataSource is used to store the "version" integer that your data store is currently on.
    * When a migration runs, it calls SetVersionNumber() on this data store to update the current version.
    * See the MigrationsTest project for a really simple in memory implementation of this in action.

2. Create your first migration, implement the IMigration interface, and decorate the class with the Migration attribute.

        [Migration("First Version (1)", 1)] // Migration attribute takes a description and a version number.
        public class Migration1 : IMigration
        {
            public void Up()
            {
                // TODO: Apply version 1.0 changes
            }

            public void Down()
            {
                // TODO: Write code to downgrade version 1.0 changes.
            }
        }

3. Use the MigrationService class to run the migrations. Migrations must be loaded into the Migrations property of this class, or through the LoadMigrationsFromAssembly() call on a MigrationService instance.

        var versionDataSource = new StubVersionDataSource();
        var runner = new MigrationService(versionDataSource);
        IMigration migration1 = new Migration1();
        IMigration migration2 = new Migration2();
        List<IMigration> migrations = new List<IMigration>() {
                migration1,
                migration2
        };

        // Set migrations to run on the MigrationService class
        runner.Migrations = migrations;

        // Run Migration1.Up() and Migration2.Up() methods, the version should be at 2 after this completes.
        runner.RunUpMigrations();

4. See the MigrationsTest project for all of the Up/Down possibilities.
5. MigrationService uses a TraceSwitch called "Migrations". Configure your app.config/web.config if you wish to use this.
6. 
    TODOS:
    * More trace statements.
    * Bundle helper libraries for common databases/data stores for easier Up() and Down() creation.
    * Create a console application to actually run the migrations, currently it is up to the user of the library to create this.