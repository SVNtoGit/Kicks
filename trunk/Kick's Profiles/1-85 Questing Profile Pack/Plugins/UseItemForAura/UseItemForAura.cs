using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Xml.Linq;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Styx;
using Styx.Logic.Combat;
using Styx.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.Plugins.PluginClass;
using Styx.Logic.BehaviorTree;

using Styx.Logic.Pathing;
using Styx.Combat.CombatRoutine;
using Styx.Logic.Inventory.Frames.Quest;
using Styx.Logic.Questing;
using Styx.Plugins;
using Styx.Logic.Inventory.Frames.Gossip;
using Styx.Logic.Common;
using Styx.Logic.Inventory.Frames.Merchant;
using Styx.Logic;
using Styx.Logic.Profiles;
using Styx.Logic.Inventory.Frames.LootFrame;

namespace UseItemForAura
{
	class UseItemForAura: HBPlugin
	{
        public override string Name { get { return "Questhelper - UseItemforAura"; } }
        public override string Author { get { return "Kickazz n KaZ"; } }
        private readonly Version _version = new Version(1, 0);
        public override Version Version { get { return _version; } }
        public override string ButtonText { get { return "Settings"; } }
        public override bool WantButton { get { return true; } }

        public static Settings Settings = new Settings();
        public static LocalPlayer Me = ObjectManager.Me;
        bool hasItBeenInitialized = false;
        static Stopwatch pulseThrottleTimer = new Stopwatch();
        string tmp;

        public UseItemForAura()
        {
            Logging.Write("UseItemForAura - Questhelper - Version 1.0 Loaded.");
            Settings.Load();
        }

        public override void OnButtonPress()
        {
            Settings.Load(); 
            ConfigForm.ShowDialog();
        }


        private Form MyForm;
        public Form ConfigForm
        {
             get
             {
                 if (MyForm == null)
                     MyForm = new ConfigWindow();
                 return MyForm;
              }
        }

        public override void Pulse()
        {
            if (!hasItBeenInitialized)
            {
                hasItBeenInitialized = true;
                if (Settings.QuestID != "0" && IsQuestCompleted(Convert.ToInt16(Settings.QuestID)))
                    Logging.Write("UseItemForAura: Don't have the Quest {0}, deactivate Plugin", Settings.QuestID);
                if (Settings.QuestID != "0" && !HasQuest(Convert.ToInt16(Settings.QuestID)))
                    Logging.Write("UseItemForAura: Quest {0} is completed, deactivate Plugin", Settings.QuestID);
                if (Settings.AuraID == "0")
                    Logging.Write("UseItemForAura: Need Aura ID, deactivate Plugin");
                if (Settings.ItemID == "0")
                    Logging.Write("UseItemForAura: Need Item ID, deactivate Plugin");
				if(Settings.AuraID != "0")
					tmp = WoWSpell.FromId(Convert.ToInt32(Settings.AuraID)).Name;
            }
            if (Settings.AuraID == "0")
                return;
            if (Settings.ItemID == "0")
                return;

            if (Settings.QuestID != "0" && IsQuestCompleted(Convert.ToInt64(Settings.QuestID)))
                return;
            if (Settings.QuestID != "0" && !HasQuest(Convert.ToInt64(Settings.QuestID)))
                return;

            if (!pulseThrottleTimer.IsRunning || pulseThrottleTimer.ElapsedMilliseconds >= 1000)
			{
                pulseThrottleTimer.Reset();
                pulseThrottleTimer.Start();
                if (Me.IsCasting || Me.IsInInstance || Me.IsOnTransport || Battlegrounds.IsInsideBattleground
                   || Me.Dead || Me.IsGhost)
                    return;
                if (!Settings.Combat && Me.Combat)
                    return;
                if (!Me.HasAura(tmp))
                {
                    ObjectManager.Update();
                    List<WoWItem> objList = ObjectManager.GetObjectsOfType<WoWItem>()
                        .Where(o => ((o.Entry == Convert.ToInt64(Settings.ItemID))))
                        .OrderBy(o => o.Distance).ToList();
                    foreach (WoWItem o in objList)
                    {
                        o.Use();
                        Thread.Sleep(500);
                    }
                    while (Me.IsCasting)
                    {
                        Thread.Sleep(100);
                    }
                }
            }
        }

        private static bool IsQuestCompleted(Int64 ID)
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

        private static bool HasQuest(Int64 ID)
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
    }
}
