using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Migrations
{
    /// <summary>
    /// Interface for a version data source (where current version information is persisted).
    /// </summary>
    public interface IVersionDataSource
    {
        void SetVersionNumber(int version);
        int GetVersionNumber();
    }
}
