using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Xml.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;


using Styx;
using Styx.Plugins.PluginClass;
using Styx.Logic.BehaviorTree;
using Styx.Helpers;
using Styx.WoWInternals;
using Styx.Logic.Pathing;
using Styx.Logic.Combat;
using Styx.WoWInternals.WoWObjects;
using Styx.Logic.Inventory.Frames.Quest;
using Styx.Logic.Questing;
using Styx.Plugins;
using Styx.Logic.Inventory.Frames.Gossip;
using Styx.Logic.Common;
using Styx.Logic.Inventory.Frames.Merchant;
using Styx.Logic;
using Styx.Logic.Profiles;
using Styx.Logic.Inventory.Frames.LootFrame;


namespace katzerle
{
	class Magnets: HBPlugin
	{       
        // ***** anything below here isn't meant to be modified *************
		public static string name { get { return "Magnets Collector " + _version.ToString(); } }
		public override string Name { get { return name; } }
		public override string Author { get { return "katzerle"; } }
		private readonly static Version _version = new Version(0, 1);
		public override Version Version { get { return _version; } }
		public override string ButtonText { get { return "No Settings"; } }
		public override bool WantButton { get { return false; } }
		public static LocalPlayer Me = ObjectManager.Me;
        List<uint> SSCompQuests = new List<uint>();
        List<uint> SSQuestIDList = new List<uint>();
		WoWPoint Area = new WoWPoint(-416.3247, 1627.922, 18.6334);

		public override void Pulse()
		{
			
				try
				{
					if (Me.Location.Distance(Area) < 100)
					{
						if (!inCombat && ((HasQuest(27992) && !IsQuestCompleted(27992)) || (HasQuest(28692) && !IsQuestCompleted(28692))))
							PickupPieces();
					}
				}
				catch (ThreadAbortException) { }
				catch (Exception e)
				{
					Log("Exception in Pulse:{0}", e);
				}

		}

		public static void movetoLoc(WoWPoint loc)
		{
			while (loc.Distance(Me.Location) > 10)
			{
				Navigator.MoveTo(loc);
				Thread.Sleep(100);
				if (inCombat) return;
			}
			Thread.Sleep(2000);
		}
		
		static public void PickupPieces()
		{
			ObjectManager.Update();
			List<WoWItem> objList1 = ObjectManager.GetObjectsOfType<WoWItem>()
                .Where(u => (u.Entry == 62829))				// Magnetized Scrap Collector
				.OrderByDescending(u => u.Entry).ToList();
			foreach (WoWItem u in objList1)
			{
				if(u.Cooldown == 0)
				{
					if (inCombat) return;
					WoWMovement.MoveStop();
					if (Me.Mounted)
						Mount.Dismount();
					u.Use();
				}
			}


			ObjectManager.Update();
			List<WoWGameObject> objList2 = ObjectManager.GetObjectsOfType<WoWGameObject>()
                .Where(o => ((o.Entry == 206644)	// Siege Scrap
					|| (o.Entry == 206652)
					|| (o.Entry == 206651)))
				.OrderByDescending(o => o.Entry).ToList();
			foreach (WoWGameObject o in objList2)
			{
				if (o.Location.Distance(Me.Location) < 40)
				{
					movetoLoc(o.Location);
					if (inCombat) return;
					WoWMovement.MoveStop();
					if (Me.Mounted)
						Mount.Dismount();
					o.Interact();
					Thread.Sleep(3000);
				}
			}
		}
		
		private static bool IsQuestCompleted(uint ID)
        {
            //to make sure every header is expanded in quest log
            Lua.DoString("ExpandQuestHeader(0)");
            //number of values in quest log (includes headers like "Durator")
            int QuestCount = Lua.GetReturnVal<int>("return select(1, GetNumQuestLogEntries())", 0);
            for (int i = 1; i <= QuestCount; i++)
            {
                List<string> QuestInfo = Lua.LuaGetReturnValue("return GetQuestLogTitle(" + i + ")", "raphus.lua");

                //pass if the index isHeader or isCollapsed
                if (QuestInfo[4] == "1" || QuestInfo[5] == "1")
                    continue;

                string QuestStatus = null;
                if (QuestInfo[6] == "1")
                    QuestStatus = "completed";
                else if (QuestInfo[6] == "-1")
                    QuestStatus = "failed";
                else
                    QuestStatus = "in progress";
                if (QuestInfo[8] == Convert.ToString(ID) && QuestStatus == "completed")
                {
                    return true;
                }
            }
            return false;
        }
		
		private static bool HasQuest(uint ID)
        {
            //to make sure every header is expanded in quest log
            Lua.DoString("ExpandQuestHeader(0)");
            //number of values in quest log (includes headers like "Durator")
            int QuestCount = Lua.GetReturnVal<int>("return select(1, GetNumQuestLogEntries())", 0);
            for (int i = 1; i <= QuestCount; i++)
            {
                List<string> QuestInfo = Lua.LuaGetReturnValue("return GetQuestLogTitle(" + i + ")", "raphus.lua");

                //pass if the index isHeader or isCollapsed
                if (QuestInfo[4] == "1" || QuestInfo[5] == "1")
                    continue;

                string QuestStatus = null;
                if (QuestInfo[8] == Convert.ToString(ID))
                {
                    return true;
                }
            }
            return false;
        }

        static public bool inCombat
        {
            get
            {
                if (Me.Dead || Me.IsGhost) return true;
                return false;
            }
        }

		public static int GetPing
		{
			get
			{
				return Lua.GetReturnVal<int>("return GetNetStats()", 2);
			}
		}
		
				public override void OnButtonPress()
		{
		}
		
		static public void Log(string msg, params object[] args) { Logging.Write(msg, args); }
		static public void Log(System.Drawing.Color c, string msg, params object[] args) { Logging.Write(c, msg, args); }		
		
	}
}

