//Mr.LootBind - Created by CodenameGamma - 4-12-11 - For WoW Version 4.0.3+
//www.honorbuddy.de
//this is a free plugin, and should not be sold, or repackaged.
//Donations Accepted. 
//Version 1.0


using System.Drawing;
using System.Linq;

namespace MrLootBind
{
    using Styx.Logic;
    using System;
    using Styx.Helpers;
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

    public class MrLootBind : HBPlugin
    {
        //Normal Stuff.
        public override string Name { get { return "Mr.LootBind"; } }
        public override string Author { get { return "CnG"; } }
        public override Version Version { get { return new Version(1, 0); } }
        public override bool WantButton { get { return true; } }
        public override string ButtonText { get { return "Mr.LootBind"; } }

        //Logging Class for your conviance
        public static void slog(string format, params object[] args)
        { Logging.Write(Color.LimeGreen, "[Mr.LootBind]:" + format, args); }
        private static readonly LocalPlayer Me = ObjectManager.Me;

        public override void Initialize()
        {
            slog("Loot Bind Confirm Attached!");
            Lua.Events.AttachEvent("LOOT_BIND_CONFIRM", BindItemConfirmPopup);

        }
        public override void  Dispose()
        {
            slog("Loot Bind Confirm Detached!");
            Lua.Events.DetachEvent("LOOT_BIND_CONFIRM", BindItemConfirmPopup);

        }
        //Uncomment if adding a UI for the plugin
        /*public override void OnButtonPress()
        {

        }*/
        private static void BindItemConfirmPopup(object sender, LuaEventArgs args)
        {

           slog("Clicking Yes to Comfirm An Item being SoulBound to you.");
           Lua.DoString("RunMacroText(\"/click StaticPopup1Button1\");");

        }
        public override void Pulse()
        {
         
        }
        




    }
}

