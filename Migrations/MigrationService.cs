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

        private const string TRACE_SWITCH_NAME = "Migrations";
        private static TraceSwitch ts = new TraceSwitch(TRACE_SWITCH_NAME, String.Empty);

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

        public void RunUpMigrations()
        {
            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - RunUpMigrations() - Start");

            // Sort in ascending order
            this.Migrations.Sort(MigrationSorter);

            int schemaVersion = this.versionDataSource.GetVersionNumber();
            this.RunMigrations(m => m.Up(), delegate(IMigration migration){
                MigrationAttribute atr = GetMigrationsAttributes(migration);
                if (atr != null)
                {
                    // Run migrations that are greater than the schema version
                    // TODO: Additional check here if we wanted to run upgrade to specific version
                    return atr.Version > schemaVersion;
                }

                return false;
            });

            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - RunUpMigrations() - End");
        }

        public void RunDownMigrations()
        {
            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - RunDownMigrations() - Start");

            // Sort in ascending order
            this.Migrations.Sort(MigrationSorter);
            // Reverse sort to descending
            this.Migrations.Reverse(); // TODO: Create a descending sorter instead of doing two sorts.

            int schemaVersion = this.versionDataSource.GetVersionNumber();
            this.RunMigrations(m => m.Down(), delegate(IMigration migration){
                MigrationAttribute atr = GetMigrationsAttributes(migration);
                if (atr != null)
                {
                    // Run down migrations that are less or equal to than the schema version
                    // TODO: Additional check here if we wanted to run downgrade to specific version
                    return atr.Version <= schemaVersion;
                }

                return false;
            });

            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - RunDownMigrations() - Start");
        }

        private void RunMigrations(Action<IMigration> action, Predicate<IMigration> predicate)
        {
            Trace.Indent();
            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - RunMigrations() - Start");

            Trace.Indent();
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
                    this.versionDataSource.SetVersionNumber(attribute.Version);
                }
            }

            Trace.Unindent();
            Trace.WriteLineIf(ts.TraceInfo, "MigrationService - RunMigrations() - End");
            Trace.Unindent();
        }

        public void LoadMigrationsFromAssembly(Assembly asm, params object [] args)
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