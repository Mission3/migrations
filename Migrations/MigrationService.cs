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
        public List<IMigration> Migrations { get; set; }

        private const string TRACE_SWITCH_NAME = "Migrations";
        private static TraceSwitch ts = new TraceSwitch(TRACE_SWITCH_NAME, String.Empty);

        private const int RUN_ALL_MIGRATIONS = -1;

        public MigrationService(IVersionDataSource versionDataSource)
        {
            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - CTOR - Start");
            this.versionDataSource = versionDataSource;
            this.Migrations = new List<IMigration>();
            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - CTOR - End");
        }

        public IMigration this[int index]
        {
            get { return this.Migrations[index]; }
            set { this.Migrations[index] = value; }
        }

        private Predicate<IMigration> GetDownMigrationPredicate()
        {
            int schemaVersion = this.versionDataSource.GetVersionNumber();
            Predicate<IMigration> results = delegate(IMigration migration)
            {
                MigrationAttribute atr = GetMigrationsAttributes(migration);
                if (atr != null)
                {
                    // Run down migrations that are less or equal to than the schema version
                    return atr.Version <= schemaVersion;
                }

                return false;
            };

            return results;
        }

        private Predicate<IMigration> GetDownMigrationPredicate(int versionTo)
        {
            int schemaVersion = this.versionDataSource.GetVersionNumber();
            Predicate<IMigration> results = delegate(IMigration migration)
            {
                MigrationAttribute atr = GetMigrationsAttributes(migration);
                if (atr != null)
                {
                    // Run down migrations that are less than or equal to than the schema version
                    // and greater than the versionTo
                    // Example:
                    // 1 2 3 4 <- 4 is the current schemaVersion
                    // Running down migrations to version 2 should execute Down() on 4 and 3
                    return atr.Version > versionTo && atr.Version <= schemaVersion;
                }

                return false;
            };

            return results;
        }

        private Predicate<IMigration> GetUpMigrationPredicate()
        {
            int schemaVersion = this.versionDataSource.GetVersionNumber();
            Predicate<IMigration> results = delegate(IMigration migration)
            {
                MigrationAttribute atr = GetMigrationsAttributes(migration);
                if (atr != null)
                {
                    // Run migrations that are greater than the schema version
                    // TODO: Additional check here if we wanted to run upgrade to specific version
                    return atr.Version > schemaVersion;
                }

                return false;
            };

            return results;
        }

        private Predicate<IMigration> GetUpMigrationPredicate(int versionTo)
        {
            int schemaVersion = this.versionDataSource.GetVersionNumber();
            Predicate<IMigration> results = delegate(IMigration migration)
            {
                MigrationAttribute atr = GetMigrationsAttributes(migration);
                if (atr != null)
                {
                    // Run migrations that are greater than the schema version and less than/equal to the "versionTo"
                    return atr.Version > schemaVersion && atr.Version <= versionTo;
                }

                return false;
            };

            return results;
        }

        public void RunUpMigrations(int versionTo)
        {
            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - RunUpMigrations(int versionTo) - Start");

            this.RunAllUpMigrationsOrToVersion(versionTo);

            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - RunUpMigrations() - End");
        }

        public void RunUpMigrations()
        {
            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - RunUpMigrations() - Start");

            this.RunAllUpMigrationsOrToVersion(RUN_ALL_MIGRATIONS);

            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - RunUpMigrations() - End");
        }

        private void RunAllUpMigrationsOrToVersion(int versionTo)
        {
            // Sort in ascending order
            this.Migrations.Sort(MigrationSorter);
            Predicate<IMigration> predicate = null;

            if (versionTo > 0)
            {
                // Only run from current version -> versionTo
                predicate = this.GetUpMigrationPredicate(versionTo);
            }
            else
            {
                // Run all migrations upgrades
                predicate = this.GetUpMigrationPredicate();
            }

            this.RunMigrations(delegate(IMigration migration)
            {
                MigrationAttribute attribute = GetMigrationsAttributes(migration);
                migration.Up();
                this.versionDataSource.SetVersionNumber(attribute.Version);
            }, predicate);
        }

        public void MigrateToVersion(int versionTo)
        {
            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - MigrateToVersion() - Start");

            // Helper function to call Up/Down based on the current version.
            int schemaVersion = this.versionDataSource.GetVersionNumber();

            if (versionTo > schemaVersion)
            {
                this.RunUpMigrations(versionTo);
            }
            else if (versionTo < schemaVersion)
            {
                this.RunDownMigrations(versionTo);
            }

            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - MigrateToVersion() - End");
        }

        public void RunDownMigrations()
        {
            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - RunDownMigrations() - Start");

            this.RunAllDownMigrationsOrToVersion(RUN_ALL_MIGRATIONS);

            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - RunDownMigrations() - End");
        }

        public void RunDownMigrations(int versionTo)
        {
            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - RunDownMigrations(versionTo) - Start");

            this.RunAllDownMigrationsOrToVersion(versionTo);

            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - RunDownMigrations(versionTo) - Start");
        }

        private void RunAllDownMigrationsOrToVersion(int versionTo)
        {
            // Sort in ascending order
            this.Migrations.Sort(MigrationSorter);
            this.Migrations.Reverse();

            Predicate<IMigration> predicate = null;

            if (versionTo > 0)
            {
                // Only run from current version -> versionTo
                predicate = this.GetDownMigrationPredicate(versionTo);
            }
            else
            {
                // Run all migrations upgrades
                predicate = this.GetDownMigrationPredicate();
            }

            this.RunMigrations(delegate(IMigration migration)
            {
                MigrationAttribute attribute = GetMigrationsAttributes(migration);
                migration.Down();

                // The version we are downgrading to does not run it's down() method, therefore we will be off by 1 here
                this.versionDataSource.SetVersionNumber(attribute.Version - 1);
            }, predicate);
        }

        private void RunMigrations(Action<IMigration> action, Predicate<IMigration> predicate)
        {
            Trace.Indent();
            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - RunMigrations() - Start");

            Trace.Indent();
            MigrationAttribute prev = null;
            foreach (IMigration migration in this.Migrations)
            {
                MigrationAttribute attribute = GetMigrationsAttributes(migration);
                if (attribute != null)
                {
                    Trace.WriteLineIf(ts.TraceInfo, "Running Migration - Description: " + attribute.Description);
                    Trace.WriteLineIf(ts.TraceInfo, "Running Migration - Version: " + attribute.Version);
                }

                if (predicate.Invoke(migration))
                {
                    action.Invoke(migration); // Action to invoke on migration

                    if (prev != null)
                    {
                        // difference should be one, if not they loaded migrations with gaps in between them, or perhaps didn't decorate one with an attribute
                        int difference = Math.Abs(attribute.Version - prev.Version);
                        Trace.WriteLineIf(ts.TraceWarning, "Migrations executed in order but with a gap in between versions greater than 1. Make sure you didn't miss a migration attribute or use the wrong version number in the attribute.");
                    }

                    // Set current migration attribute to the previous for next iteration
                    prev = attribute;
                }
            }

            Trace.Unindent();
            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - RunMigrations() - End");
            Trace.Unindent();
        }

        public void LoadMigrationsFromAssembly(Assembly asm, params object[] args)
        {
            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - LoadMigrationsFromAssembly() - Start");
            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - LoadMigrationsFromAssembly() - Assembly: " + asm.FullName);
            Module[] modules = asm.GetModules();
            this.Migrations.Clear();

            Trace.Indent();

            foreach (Module module in modules)
            {
                Type[] types = module.GetTypes();
                foreach (Type t in types)
                {
                    if (t.GetInterface("IMigration") == null)
                    {
                        continue;
                    }

                    // Create instance of the migration, pass in args to the CTor if needed
                    ConstructorInfo[] ctors = t.GetConstructors();
                    IMigration instance = null;

                    if (ctors.Length > 0)
                    {
                        ConstructorInfo ctorInfo = ctors[0];
                        ParameterInfo[] paramInfo = ctorInfo.GetParameters();

                        // Check that the ctor has params, is public, and that we passed in args
                        if (paramInfo.Length > 0 && ctorInfo.IsPublic && args.Length > 0)
                        {
                            instance = Activator.CreateInstance(t, args) as IMigration;
                        }
                        else if (paramInfo.Length == 0 && ctorInfo.IsPublic) // Default empty CTor
                        {
                            instance = Activator.CreateInstance(t) as IMigration;
                        }
                        // instance will be null if a ctor requires params and none were passed in
                    }

                    if (instance != null && GetMigrationsAttributes(instance) != null)
                    {
                        this.Migrations.Add(instance);
                    }
                }
            }

            Trace.Unindent();
            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - LoadMigrationsFromAssembly() - End");
        }

        public static MigrationAttribute GetMigrationsAttributes(IMigration migration)
        {
            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - GetMigrationsAttributes() - Start");
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

            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - GetMigrationsAttributes() - End");
            return results;
        }

        public static int MigrationSorter(IMigration x, IMigration y)
        {
            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - MigrationSorter() - Called");
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
            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - GetMigrationVersionNumber() - Start");
            int results = -1;
            MigrationAttribute attr = GetMigrationsAttributes(migration);
            if (attr != null)
            {
                results = attr.Version;
            }

            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - GetMigrationVersionNumber() - End");
            return results;
        }
    }
}