using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;

namespace GTMigrations.Helpers
{
    public static class SPListHelper
    {
        public static bool CreateList(string listTitle, SPListTemplateType listType, SPWeb web)
        {
            SPList list = web.Lists.TryGetList(listTitle);
            if (list == null)
            {
                web.Lists.Add(listTitle, "", listType);
                return true;
            }

            return false;
        }
    }
}
