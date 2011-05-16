using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Migrations;

namespace MigrationsTest
{
    [Serializable]
    public class MigrationWrongAttributes : IMigration
    {
        public void Up()
        {
        }

        public void Down()
        {
        }
    }
}
