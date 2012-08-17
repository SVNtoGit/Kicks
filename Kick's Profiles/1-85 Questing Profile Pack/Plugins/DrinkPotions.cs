// Plugin DrinkManager Originally contributed by Pasterke.  Redeveloped and Renamed by Kickazz006
// This Plugin drinks HP/Mana pots when low
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;

// HB Stuff
using Styx;
using Styx.Helpers;
using Styx.Logic;
using Styx.Logic.Combat;
using Styx.Logic.Inventory;
using Styx.Logic.Inventory.Frames.Gossip;
using Styx.Logic.Pathing;
using Styx.Logic.Profiles;
using Styx.Logic.BehaviorTree;
using Styx.Plugins.PluginClass;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;

namespace DrinkPotions
{
    public class DrinkPotions : HBPlugin
    {
        #region Globals

        public override string Name { get { return "DrinkPotions"; } }
        public override string Author { get { return "Kickazz006"; } }
        public override Version Version { get { return new Version(1,0,0,1); } }
        public override string ButtonText { get { return "Kick"; } }
        public override bool WantButton { get { return false; } }
        private static LocalPlayer Me { get { return ObjectManager.Me; } }

        public int HealPotPercent = 25; // Drink HP %
        public int ManaPotPercent = 10; // Drink Mana %

        #endregion

        public override void Initialize()
        {
            Logging.Write(Color.CornflowerBlue, "Loaded DrinkPotions v1.0.0.1 by Kickazz006");
        }

        public WoWItem HealingPotions()
        {
            List<WoWItem> Items = ObjectManager.GetObjectsOfType<WoWItem>(false);
            foreach (WoWItem Item in Items)
            {
                switch (Item.Entry)
                {
                    case 80040: return Item; // Endless Master Healing Potion - MoP Alch Potion
                    case 76097: return Item; // Master Healing Potion - MoP
                    case 63144: return Item; // Baradin's Wardens Healing Potion
                    case 64994: return Item; // Hellscream's Reach Healing Potion
                    case 57193: return Item; // Mighty Rejuvenation Potion
                    case 57191: return Item; // Mythical Healing Potion
                    case 63300: return Item; // Rogue's Draught
                    case 67145: return Item; // Draught of War
                    case 33447: return Item; // Runic Healing Potion
                    case 40077: return Item; // Crazy Alchemist's Potion
                    case 40081: return Item; // Potion of Nightmares
                    case 40087: return Item; // Powerful Rejuvenation Potion
                    case 41166: return Item; // Runic Healing Injector
                    case 43569: return Item; // Endless Healing Potion
                    case 22850: return Item; // Super Rejuvenation Potion
                    case 34440: return Item; // Mad Alchemist's Potion
                    case 39671: return Item; // Resurgent Healing Potion
                    case 31838: return Item; // Major Combat Healing Potion
                    case 31839: return Item; // Major Combat Healing Potion
                    case 31852: return Item; // Major Combat Healing Potion
                    case 31853: return Item; // Major Combat Healing Potion
                    case 32784: return Item; // Red Ogre Brew
                    case 32910: return Item; // Red Ogre Brew Special
                    case 31676: return Item; // Fel Regeneration Potion
                    case 23822: return Item; // Healing Potion Injector
                    case 33092: return Item; // Healing Potion Injector
                    case 22829: return Item; // Super Healing Potion
                    case 32763: return Item; // Rulkster's Secret Sauce
                    case 32904: return Item; // Cenarion Healing Salve
                    case 32905: return Item; // Bottled Nethergon Vapor
                    case 32947: return Item; // Auchenai Healing Potion
                    case 39327: return Item; // Noth's Special Brew - DK
                    case 43531: return Item; // Argent Healing Potion
                    case 18253: return Item; // Major Rejuvenation Potion
                    case 28100: return Item; // Volatile Healing Potion
                    case 33934: return Item; // Crystal Healing Potion
                    case 13446: return Item; // Major Healing Potion
                    case 17384: return Item; // Major Healing Draught
                    case 3928: return Item; // Superior Healing Potion
                    case 9144: return Item; // Wildvine Potion
                    case 12190: return Item; // Dreamless Sleep Potion
                    case 17349: return Item; // Superior Healing Draught
                    case 18839: return Item; // Combat Healing Potion
                    case 1710: return Item; // Greater Healing Potion
                    case 929: return Item; // Healing Potion
                    case 4596: return Item; // Discolored Healing Potion
                    case 858: return Item; // Lesser Healing Potion
                    case 118: return Item; // Minor Healing Potion
                }
            }
            return null;
        }

        public WoWItem ManaPotions()
        {
            List<WoWItem> Items = ObjectManager.GetObjectsOfType<WoWItem>(false);
            foreach (WoWItem Item in Items)
            {
                switch (Item.Entry)
                {
                    case 76098: return Item; // Master Mana Potion - MoP
                    case 63145: return Item; // Baradin's Wardens Mana Potion
                    case 64993: return Item; // Hellscream's Reach Mana Potion
                    case 57193: return Item; // Mighty Rejuvenation Potion
                    case 57192: return Item; // Mythical Mana Potion
                    case 67145: return Item; // Draught of War
                    case 33448: return Item; // Runic Mana Potion
                    case 40077: return Item; // Crazy Alchemist's Potion
                    case 40081: return Item; // Potion of Nightmares
                    case 40087: return Item; // Powerful Rejuvenation Potion
                    case 42545: return Item; // Runic Mana Injector
                    case 43570: return Item; // Endless Mana Potion
                    case 22850: return Item; // Super Rejuvenation Potion
                    case 34440: return Item; // Mad Alchemist's Potion	
                    case 40067: return Item; // Icy Mana Potion
                    case 31677: return Item; // Fel Mana Potion
                    case 31840: return Item; // Major Combat Mana Potion
                    case 31841: return Item; // Major Combat Mana Potion
                    case 31854: return Item; // Major Combat Mana Potion
                    case 31855: return Item; // Major Combat Mana Potion
                    case 32783: return Item; // Blue Ogre Brew
                    case 32909: return Item; // Blue Ogre Brew Special
                    case 23823: return Item; // Mana Potion Injector
                    case 33093: return Item; // Mana Potion Injector
                    case 22832: return Item; // Super Mana Potion
                    case 32762: return Item; // Rulkster's Brain Juice
                    case 32902: return Item; // Bottled Nethergon Energy
                    case 32903: return Item; // Cenarion Mana Salve
                    case 32948: return Item; // Auchenai Mana Potion
                    case 43530: return Item; // Argent Mana Potion
                    case 28101: return Item; // Unstable Mana Potion
                    case 33935: return Item; // Crystal Mana Potion
                    case 18253: return Item; // Major Rejuvenation Potion
                    case 13444: return Item; // Major Mana Potion
                    case 17351: return Item; // Major Mana Draught
                    case 13443: return Item; // Superior Mana Potion
                    case 18841: return Item; // Combat Mana Potion
                    case 12190: return Item; // Dreamless Sleep Potion
                    case 17352: return Item; // Superior Mana Draught
                    case 6149: return Item; // Greater Mana Potion
                    case 3827: return Item; // Mana Potion
                    case 3385: return Item; // Lesser Mana Potion	
                    case 2455: return Item; // Minor Mana Potion
                    case 2456: return Item; // Minor Rejuvenation Potion
                    case 3087: return Item; // Mug of Shimmer Stout
                }
            }
            return null;
        }
        
        public override void Pulse()
        {
            if (!Me.Combat || Me.Dead || Me.IsGhost) // Chillax (Yes, it's redundant)
            { return; }

            if (Me.Combat) // Pay Attn!
            {
                if (Me.HealthPercent < HealPotPercent) // HP
                {
                    WoWItem UseHealPot = HealingPotions(); // Reference our list of Healing Potions
                    if (UseHealPot == null)
                    { Logging.Write(Color.Red, "I have no Healing Pots"); }
                    else
                    {
                        useItem(UseHealPot);
                        Logging.Write(Color.Yellow, "Used " + UseHealPot.Name + "!");
                        Logging.Write(Color.Yellow, "Starting 1 minute Potion Timer!");
                    }
                }
                if (Me.ManaPercent < ManaPotPercent) // Mana
                {
                    WoWItem UseManaPot = ManaPotions(); // Reference our list of Mana Potions
                    if (UseManaPot == null)
                    { Logging.Write(Color.Red, "I have no Mana Pots"); }
                    else
                    {
                        useItem(UseManaPot);
                        Logging.Write(Color.Yellow, "Used " + UseManaPot.Name + "!");
                        Logging.Write(Color.Yellow, "Starting 1 minute Potion Timer!");
                    }
                }
            }

        }
		
        // If Potion has been used, start a 1 min CD timer
        private static readonly Stopwatch PotionTimer = new Stopwatch();
        public void useItem(WoWItem Item)
        {
            if (!PotionTimer.IsRunning || PotionTimer.ElapsedMilliseconds > (500 + (1000 * 60)))
            {
                Item.Use();
                PotionTimer.Reset();
                PotionTimer.Start();
            }
        }         
    }
     
}
