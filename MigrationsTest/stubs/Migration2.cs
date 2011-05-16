using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Migrations;

namespace MigrationsTest
{
    [Migration("Second Migration (2)", 2)]
    public class Migration2 : IMigration
    {
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