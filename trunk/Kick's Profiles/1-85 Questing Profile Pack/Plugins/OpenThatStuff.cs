
namespace OpenThatStuff
{
    using Styx;
    using Styx.Combat.CombatRoutine;
    using Styx.Helpers;
    using Styx.Logic;
    using Styx.Logic.Combat;
    using Styx.Logic.Pathing;
    using Styx.Logic.Inventory;
    using Styx.Plugins.PluginClass;
    using Styx.WoWInternals;
    using Styx.WoWInternals.WoWObjects;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Windows.Forms;

    public class OpenThatStuff : HBPlugin
    {
        public override string Name { get { return "OpenThatStuff"; } }
        public override string Author { get { return "Kickazz n KaZ"; } }
        public override Version Version { get { return _version; } }
        private readonly Version _version = new Version(1, 0, 0, 0);

        private static uint[,] _data = new uint[,] { 
			{1, 35792}, // Mage Hunter Personal Effects for Quest(12000)
			{1, 20767}, // Scum Covered Bag
			{1, 61387}, // Hidden Stash
			{1, 62829}, // Magnetized Scrap Collector
			{1, 32724}, // Sludge Covered Object
        };

        private static Stopwatch sw = new Stopwatch();

        private void log(String fmt, params object[] args)
        {
            String s = String.Format(fmt, args);
            log(Color.DarkBlue, fmt, args);
        }

        private void log(Color color, String fmt, params object[] args)
        {
            String s = String.Format(fmt, args);
            Styx.Helpers.Logging.Write(color, String.Format("[{0}]: {1}", Name, s));
        }


        public override void Pulse()
        {
            if (!sw.IsRunning)
            {
                sw.Start();
                log("Active");
            }

            if (ObjectManager.Me.Combat)
            {

                // Dismount if you're still on a mount
                if (!ObjectManager.Me.IsMoving && ObjectManager.Me.Mounted)
                    Mount.Dismount();
            }

            // 10 seconds pulse
            if (sw.Elapsed.TotalSeconds < 10 || 
                Battlegrounds.IsInsideBattleground || 
                ObjectManager.Me.Mounted || 
                ObjectManager.Me.Combat ||
                ObjectManager.Me.Dead)
                return;

          
            // Unlock and open items
            CheckInventoryItems();

         
            // Reset timer so it will all start over again in 5 seconds.
            sw.Reset();
            sw.Start();

        }

        private void CheckInventoryItems()
        {
           foreach (WoWItem item in ObjectManager.GetObjectsOfType<WoWItem>())
            {
            for (int i = 0; i <= _data.GetUpperBound(0); i++)
            {
                if (_data[i, 1] == item.Entry)
                {
                    int cnt = Convert.ToInt32(Lua.GetReturnValues(String.Format("return GetItemCount(\"{0}\")", item.Name), Name + ".lua")[0]);
                    int max = (int)(cnt / _data[i, 0]);
                    for (int j = 0; j < max; j++)
                    {
                        String s = String.Format("UseItemByName(\"{0}\")", item.Name);
                        log("Using {0} we have {1}", item.Name, cnt);
                        Lua.DoString(s);
                        StyxWoW.SleepForLagDuration();
                    }
                    break;
                }
            }
        }
        }
    }
}
