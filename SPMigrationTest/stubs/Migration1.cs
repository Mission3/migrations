using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Migrations;

namespace MigrationsTest
{
    [Migration("First Version (1)", 1)]
    public class Migration1 : IMigration
    {
        public void Up()
        {
            // TODO: Apply version 1.0 changes
            Console.WriteLine("4-18-2011 Migration Up()");
        }

        public void Down()
        {
            // TODO: Write code to downgrade version 1.0 changes.
            Console.WriteLine("4-18-2011 Migration Down()");
        }
    }
}