using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Migrations
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class MigrationAttribute : Attribute
    {
        public string Description { get; private set; }
        public int Version { get; private set; }

        public MigrationAttribute(string description, int version)
        {
            this.Version = version;
            this.Description = description;
        }
    }
}
