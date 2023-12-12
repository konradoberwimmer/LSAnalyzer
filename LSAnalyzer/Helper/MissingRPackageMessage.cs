using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSAnalyzer.Helper
{
    public class MissingRPackageMessage
    {
        public string PackageName;

        public MissingRPackageMessage(string packageName)
        {
            this.PackageName = packageName;
        }
    }
}
