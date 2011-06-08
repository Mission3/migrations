using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Migrations;

namespace MigrationsTest
{
    [Migration("CTor Migration", 3)]
    public class MigrationWithCTorParams : IMigration
    {
        private string _foo;

        public MigrationWithCTorParams(string foo)
        {
            this._foo = foo;
        }

        public void Up()
        {
            Console.WriteLine("4-20-2011 1.1 migration Up()");
        }

        public void Down()
        {
            Console.WriteLine("4-20-2011 1.1 migration Down()");
        }
    }
}