using Styx.Logic;
using System;
using Styx.Helpers;
using Styx.Logic.Combat;
using Styx.Logic.Pathing;
using System.Threading;
using System.Diagnostics;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.Net;
using Styx.Plugins.PluginClass;
using Styx;
using System.Drawing;
using System.Windows.Forms;

namespace ABHelper
{
    public class AB_Stuck_Helper : HBPlugin
    {
        public override string Name { get { return "ArchBuddy Stuck Helper"; } }
        public override string Author { get { return "Helper"; } }
        public override Version Version { get { return _version; } }
        private readonly Version _version = new Version(1, 0, 0, 0);

		private static Stopwatch swStuckTimer = new Stopwatch();
		private WoWPoint wpLast;
		
        public override void Initialize()
        {
			wpLast = Styx.WoWInternals.ObjectManager.Me.Location;
			swStuckTimer.Reset();
            Logging.Write(Color.Yellow,"[ArchBuddy Stuck Helper]: Hello.");
        }

        public override void Pulse()
        {
			// update game objects -- needed?
            ObjectManager.Update();
		
			// Only check for stuck if we're flying
			if (Styx.WoWInternals.ObjectManager.Me.IsFlying)
			{
				// Ok, we'er flying, make sure timer is running
				if(!swStuckTimer.IsRunning)
				{
					swStuckTimer.Start();
				}
				// We're flying, timer is running, check if we're stuck about every 9 seconds
				else if (swStuckTimer.Elapsed.TotalSeconds > 9.0)
				{
					// If we're flying, we should have moved 30 or more, if not, we're stuck!
					if(Styx.WoWInternals.ObjectManager.Me.Location.Distance(wpLast) < 30)
					{
						Logging.Write(Color.Yellow,"[ArchBuddy Stuck Helper]: We're stuck!  Dismounting!");
						// Just do something here to dismount (i.e. cast spell).  You may fall to your death but atleast you won't be stuck!
						// Could improve this to try and move down to ground or something.
						SpellManager.Cast("Rejuvenation");
					}
					else
					{
						// We flying but we're stuck, so reset timer and update location
						swStuckTimer.Reset();
						wpLast = Styx.WoWInternals.ObjectManager.Me.Location;
					}
				}
			}
			else
			{
				// We're not flying so just make sure timer is stopped/reset
				if(swStuckTimer.IsRunning)
				{
					swStuckTimer.Stop();
					swStuckTimer.Reset();
				}
			}
        }

    }
}