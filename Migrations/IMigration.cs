using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Migrations
{
    public interface IMigration
    {
        void Up();
        void Down();
    }
}
