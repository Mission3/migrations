using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace Migrations
{
    public class MigrationService
    {
        private IVersionDataSource versionDataSource;
        public List<IMigration> Migrations {get;set;}

        public MigrationService(IVersionDataSource versionDataSource)
        {
            this.versionDataSource = versionDataSource;
            this.Migrations = new List<IMigration>();
        }

        public void RunUpMigrations()
        {
            this.RunMigrations(m => m.Up());
        }

        public void RunDownMigrations()
        {
            this.RunMigrations(m => m.Down());
        }

        private void RunMigrations(Action<IMigration> action)
        {
            this.Migrations.Sort(MigrationSorter);

            foreach (IMigration migration in this.Migrations)
            {
                MigrationAttribute attribute = GetMigrationsAttributes(migration);
                if (attribute != null)
                {
                    Debug.WriteLine("Running Up() Migration - Description: " + attribute.Description);
                    Debug.WriteLine("Running Up() Migration - Version: " + attribute.Version);
                }

                action.Invoke(migration);
                
                // TODO: Update version here
            }
        }

        public void LoadMigrationsFromAssembly(Assembly asm)
        {
            Module[] modules = asm.GetModules();
            var migrations = new List<IMigration>();

            foreach (Module module in modules)
            {
                Type[] types = module.GetTypes();
                foreach (Type t in types)
                {
                    if (t.IsClass && t.GetInterface("IMigration") != null)
                    {
                        Console.WriteLine("Found Type: " + t.Name);
                        IMigration instance = Activator.CreateInstance(t) as IMigration;
                        if (instance != null)
                        {
                            migrations.Add(instance);
                        }
                    }
                }
            }

            this.Migrations = migrations;
        }

        public static MigrationAttribute GetMigrationsAttributes(IMigration migration)
        {
            Attribute[] attrs = Attribute.GetCustomAttributes(migration.GetType());
            MigrationAttribute results = null;

            foreach (var attr in attrs)
            {
                if (attr is MigrationAttribute)
                {
                    MigrationAttribute atr = attr as MigrationAttribute;
                    results = atr;
                    break;
                }
            }

            return results;
        }

        public static int MigrationSorter(IMigration x, IMigration y)
        {
            // Sort list by migration version numbers (queried by attributes on that class)

            /*
             * return -1 = x < y
             * return 0 x = y
             * return > 0 = x is greater than y
             */
            int xversion = GetMigrationVersionNumber(x);
            int yversion = GetMigrationVersionNumber(y);

            if (xversion < yversion)
            {
                return -1;
            }
            else if (xversion == yversion)
            {
                return 0;
            }
            else
            {
                // xversion > yversion
                return 1;
            }
        }

        public static int GetMigrationVersionNumber(IMigration migration)
        {
            int results = -1;
            MigrationAttribute attr = GetMigrationsAttributes(migration);
            if (attr != null)
            {
                results = attr.Version;
            }

            return results;
        }
    }
}