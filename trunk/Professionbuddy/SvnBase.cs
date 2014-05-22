//!CompilerOption:Optimize:On
//!CompilerOption:AddRef:WindowsBase.dll
// Professionbuddy plugin by HighVoltz

using System.Linq;
using Styx;
using Styx.Patchables;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace HighVoltz
{
    public class SvnBase
    {
        private int _rev = -1;

        protected virtual string RevString
        {
            get { return "0"; }
        }

        public int Revision
        {
            get
            {
                if (_rev == -1)
                    int.TryParse(RevString, out _rev);
                return _rev + 1;
            }
        }
    }

    public partial class Svn : SvnBase
    {
    }
}