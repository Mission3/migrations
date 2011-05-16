using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Migrations;

namespace MigrationsTest
{
    public class StubVersionDataSource : IVersionDataSource
    {
        private int versionNumber;

        public void SetVersionNumber(int version)
        {
            // Real implementation would persist this to a data source
            this.versionNumber = version;
        }

        public int GetVersionNumber()
        {
            // Real version would load from data source 
            return this.versionNumber;
        }
    }
}
