#region using
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Styx.Combat.CombatRoutine;
using Styx.Helpers;
using Styx.Logic;
using Styx.Logic.Pathing;
using Styx.Logic.POI;
using Styx.Logic.BehaviorTree;
using Styx.Logic.Inventory;
using Styx.Plugins.PluginClass;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
#endregion

namespace Styx.Bot.Plugins.FieldRepair
{
    public class FieldRepair : HBPlugin
    {
        #region Settings and overrides
        public override string Name { get { return "FieldRepair"; } }
        public override string Author { get { return "Craftiestelk"; } }
        public override Version Version { get { return new Version(0, 1, 0); } }
        
        private static bool _hasMammoth = false;
        private static bool _hasJeeves = false;
        private static bool _hasMOLLE = false;
        private static bool _hasGuildChest = false;
        private static uint mammothAlliance = 61425; //spellID of Traveler's Mammoth
        private static uint mammothHorde = 61447; //same as above
        private static uint mammoth = 0; //is set to the mountID of the mammoth
        private static uint repair = 0; //is set to the NPC-ID of the repair vendor, depending on horde/alliance
        private static uint jeeves = 49040; //item-ID of Jeeves
        private static uint molle = 40768; //item-ID of MOLL-E
        private static uint guildChest = 83958; //spellID of Guild Chest/Mobile Banking
        private static readonly LocalPlayer _me = StyxWoW.Me;
        private static BotPoi _currentPOI;
        private static WoWPoint _oldLoc;
        private static bool _iGotThis = false;
        private static float _height;
        private static bool _ForceFly = false;
        #endregion

        #region Initialization, searching for Mammoth, Jeeves, MOLL-E and Mobile Banking
        public override void Initialize()
        {
            Log("THIS IS VERY MUCH A \"WORK IN PROGRESS\", IF YOU ENCOUNTER ANY ERRORS PLEASE POST THEM IN THE THREAD!");
            using (new FrameLock())
            {   //check if we have the Travelers Mammoth
                var numMounts = Lua.GetReturnVal<uint>("return GetNumCompanions(\"MOUNT\")",0);
                for (uint i = 1; i <= numMounts; i++)
                {
                    var mountInfo = Lua.GetReturnValues("return GetCompanionInfo(\"MOUNT\","+i+")");
                    if (mountInfo.Count < 6 || Int32.Parse(mountInfo[2]) == 0)
                    {
                        Log(true, "Error parsing mounts, cancelling");
                        return;
                    }
                    if (_me.IsAlliance)
                    {
                        if (Int32.Parse(mountInfo[2]) == mammothAlliance)
                        {
                            Log("Mammoth found, using for repairs and vendoring");
                            _hasMammoth = true;
                            mammoth = i;
                            repair = 32639;
                            break;
                        }
                    }
                    else
                    {
                        if (Int32.Parse(mountInfo[2]) == mammothHorde)
                        {
                            Log("Mammoth found, using for repairs and vendoring");
                            _hasMammoth = true;
                            mammoth = i;
                            repair = 32641;
                            break;
                        }
                    }
                }
                
                //check if we have the remote GBank spell
                if (Lua.GetReturnVal<uint>("return IsInGuild()",0) == 1)
                {
                    if (Lua.GetReturnVal<uint>("return GetGuildLevel()",0) >= 11)
                    {
                        Log("Guildlevel 11+, using Mobile Banking for guildbank purposes");
                        _hasGuildChest = true;
                    }
                }
                
                //check if we have Jeeves and/or MOLL-E with us
                foreach (WoWItem item in ObjectManager.GetObjectsOfType<WoWItem>())
                {
                    if (_hasJeeves == false && item.Entry == jeeves)
                    {
                        Log("Jeeves found, using for personal banking and repair/vendor");
                        _hasJeeves = true;
                    }
                    if (_hasMOLLE == false && item.Entry == molle)
                    {
                        Log("MOLL-E found, using for mailing (once every hour)");
                        _hasMOLLE = true;
                    }
                    if (_hasJeeves && _hasMOLLE)
                    {
                        break;
                    }
                }                
            }
            Log("Found: Mammoth [{0}], Jeeves [{1}], MOLL-E [{2}], MobileBank [{3}]", _hasMammoth, _hasJeeves, _hasMOLLE, _hasGuildChest);
        }
        #endregion

        public override void Pulse()
        {
            var _currentBot = Styx.BotManager.Current.Name;
            switch (_currentBot)
            {
                case "Gatherbuddy2":
                    _ForceFly = false;
                    break;
                case "ArchaeologyBuddy":
                    _ForceFly = false;
                    break;
                default:
                    _ForceFly = true;
                    break;
            }
            var _POI = getPoi();
            switch (_POI)
            {
                case "Repair":
                    RepairAndVendor();
                    break;
                case "Sell":
                    RepairAndVendor();
                    break;
                case "Mail":
                    Mail();
                    break;
                case "GuildVault":
                    //Bank(true);
                    break;
                default:
                    break;
            }

        }

        #region get POI, or simulate POI
        private static string getPoi()
        {
            var _curPOI = BotPoi.Current;
            if (_curPOI.Type.Equals(PoiType.Buy)) { return _curPOI.Type.ToString(); }
            if (_curPOI.Type.Equals(PoiType.Mail)) { return _curPOI.Type.ToString(); }
            if (_curPOI.Type.Equals(PoiType.Repair)) { return _curPOI.Type.ToString(); }
            if (_curPOI.Type.Equals(PoiType.Sell)) { return _curPOI.Type.ToString(); }
            else
            {
                if (Styx.BotManager.Current.Name == "Gatherbuddy2")
                {
                    var _gb = Bots.Gatherbuddy.GatherbuddySettings.Instance;
                    if (_me.FreeNormalBagSlots < 2 && _gb.MailToAlt) { return "Mail"; }
                    if (_me.FreeNormalBagSlots < 2 && _gb.UseGuildVault) { return "GuildVault"; }
                }
                else if (Styx.BotManager.Current.Name == "ArchaeologyBuddy")
                {
                    if (_me.FreeNormalBagSlots < 2) { return "Repair"; }
                }
            }
            return "";
        }
        #endregion

        #region Repairing and vendoring, Mammoth or Jeeves
        public static void RepairAndVendor()
        {
            //Repair/Vendor called
            if (_me.IsGhost || _me.Dead)
            {
                return;
            }
            //If we have mammoth and can mount here, do so
            if (Mount.CanMount() && _hasMammoth)
            {
                Log("Mammoth found and useable!");
                Lua.DoString("CallCompanion(\"MOUNT\", "+mammoth+")");
                Thread.Sleep(3000);
                ObjectManager.Update();
                var unitList = ObjectManager.GetObjectsOfType<WoWUnit>();
                foreach (WoWObject unit in unitList)
                {
                    if (unit.Entry == repair)
                    {
                        unit.Interact();
                        var _guildFunds = false;
                        Thread.Sleep(2500);             
                        if (Styx.BotManager.Current.Name == "Gatherbuddy2")
                        {
                            var GBSettings = Bots.Gatherbuddy.GatherbuddySettings.Instance;
                            _guildFunds = GBSettings.RepairFromGuildBank;
                        }
                        var vf = Logic.Inventory.Frames.Merchant.MerchantFrame.Instance;
                        StyxWoW.SleepForLagDuration();
                        Vendors.SellAllItems();
                        Thread.Sleep(1000);
                        vf.RepairAllItems(_guildFunds);
                        Thread.Sleep(1000);
                        break;
                    }
                }
                return;
            }
            //if we can't mount, but have jeeves, try him
            if (!Mount.CanMount() && _hasJeeves)
            {
                if (UInt32.Parse(Lua.GetReturnValues("return GetItemCooldown(" + jeeves + ")")[0]) == 0)
                {
                    //he is off cooldown, using
                    Log("We have Jeeves and he's ready to serve! Summoning.");
                    Lua.DoString("UseItemByName(" + jeeves + ")");
                    Thread.Sleep(2500);
                    ObjectManager.Update();
                    var unitList = ObjectManager.GetObjectsOfType<WoWUnit>();
                    foreach (WoWObject unit in unitList)
                    {
                        if (unit.Entry == 35642)
                        {
                            unit.Interact();
                            var _guildFunds = false;
                            Thread.Sleep(2500);
                            Logic.Inventory.Frames.Gossip.GossipFrame gf = Logic.Inventory.Frames.Gossip.GossipFrame.Instance;
                            if (gf.GossipOptionEntries != null)
                            {
                                gf.SelectGossipOption(1);//MUST CHECK THAT THIS IS CORRECT!
                                if (Styx.BotManager.Current.Name == "Gatherbuddy2")
                                {
                                    var GBSettings = Bots.Gatherbuddy.GatherbuddySettings.Instance;
                                    _guildFunds = GBSettings.RepairFromGuildBank;
                                }
                                var vf = Logic.Inventory.Frames.Merchant.MerchantFrame.Instance;
                                StyxWoW.SleepForLagDuration();
                                Vendors.SellAllItems();
                                Thread.Sleep(1000);
                                vf.RepairAllItems(_guildFunds);
                                Thread.Sleep(1000);
                            }
                            gf.Close();
                            break;
                        }
                    }
                    return;
                }
                if (_ForceFly)
                {
                    Log("No mammoth, and Jeeves is on cooldown. Will force flying to vendor.");
                    FieldRepair.RepairFly();
                }
            }
        }
        #endregion

        #region No mammoth/jeeves (or on CD)
        public static void RepairFly()
        {
            if (Flightor.MountHelper.CanMount && !_iGotThis)
            {
                _iGotThis = true;
                _currentPOI = BotPoi.Current;
                BotPoi.Clear();
                _oldLoc = _me.Location;
                Log("Vendor/repair run discovered, attempting to fly");
                var _safeSpot = findSafeFlightPoint(_currentPOI.Location);
                if (_safeSpot.Equals(_currentPOI.Location))
                {
                    flyTo(_currentPOI.Location);
                }
                else { performSafeFlight(_safeSpot, _currentPOI.Location); }
                _currentPOI.AsObject.Interact();
                StyxWoW.SleepForLagDuration();
                Thread.Sleep(1000);
                Vendors.RepairAllItems();
                Thread.Sleep(1000);
                Vendors.SellAllItems();
                Thread.Sleep(1000);
                Log("Vendor visited, flying back!");
                var _safeSpotBack = findSafeFlightPoint(_oldLoc);
                if (_safeSpotBack.Equals(_oldLoc))
                {
                    flyTo(_oldLoc);
                }
                else { performSafeFlight(_safeSpotBack, _oldLoc); }
                Log("Back where we started (hopefully), thank you for flying with Crafty Airlines!");
                _iGotThis = false;
            }
        }
        #endregion

        #region Fly to location loc
        private static void flyTo(WoWPoint loc)
        {
            while (_me.Location.Distance(loc) > 2)
            {
                if (Flightor.MountHelper.CanMount && !Flightor.MountHelper.Mounted)
                {
                    Flightor.MountHelper.MountUp();
                    StyxWoW.SleepForLagDuration();
                    Thread.Sleep(1000);
                    while (_me.IsCasting) { Thread.Sleep(150); }
                }
                Flightor.MoveTo(loc);
            }
            Styx.Logic.Pathing.Navigator.FindHeight(loc.X, loc.Y, out _height);
            while (Math.Abs(_me.Location.Z - _height) > 1)
            {
                WoWMovement.Move(WoWMovement.MovementDirection.Descend);
                Thread.Sleep(100);
            }
            Flightor.MountHelper.Dismount();
            while (_me.Location.Distance2D(loc) > 1)
            {
                Navigator.MoveTo(loc);
            }
        }
        #endregion

        #region Method for finding a safe landing point
        private static WoWPoint findSafeFlightPoint(WoWPoint loc)
        {
            #region If multiple layers (heights), attempt to land somewhere nearby
            var _heights = Logic.Pathing.Navigator.FindHeights(loc.X, loc.Y);
            _heights.Sort();
            if (_heights.Count() > 1)
            {
                Random rand = new Random();
                var i = 1;
                var _newSpot = new WoWPoint(0, 0, 0);
                while (i < 100)
                {
                    _newSpot = new WoWPoint((loc.X + rand.Next(-i, i)), (loc.Y + rand.Next(-i, i)), 0);
                    while (Logic.Pathing.Navigator.FindHeights(_newSpot.X, _newSpot.Y).Count() > 1)
                    {
                        _newSpot = new WoWPoint((loc.X + rand.Next(-i, i)), (loc.Y + rand.Next(-i, i)), 0);
                        i = i + 1;
                    }
                    Logic.Pathing.Navigator.FindHeight(_newSpot.X, _newSpot.Y, out _newSpot.Z);
                    if (Navigator.CanNavigateFully(_newSpot, loc) && clearSpot(_newSpot))
                    {
                        Log("Took {0} tries to find a safe(?) spot!", i);
                        Log("Landing spot: {0}", _newSpot.ToString());
                        return _newSpot;
                    }
                }
                Log("No safe spot found :(");
                return loc;
            }
            #endregion

            #region If 1 layer, but no LOS from above, attempt to land somewhere nearby
            else if (!WoWInternals.World.GameWorld.IsInLineOfSightOCD(new WoWPoint(loc.X, loc.Y, loc.Z+50), loc))
            {
                Random rand = new Random();
                var i = 1;
                var _newSpot = new WoWPoint(0, 0, 0);
                while (i < 100)
                {
                    _newSpot = new WoWPoint((loc.X + rand.Next(-i, i)), (loc.Y + rand.Next(-i, i)), 0);
                    i = i + 1;
                    Logic.Pathing.Navigator.FindHeight(_newSpot.X, _newSpot.Y, out _newSpot.Z);
                    if (Navigator.CanNavigateFully(_newSpot, loc) && clearSpot(_newSpot))
                    {
                        Log("Took {0} tries to find a safe(?) spot!", i);
                        Log("Landing point: {0}", _newSpot.ToString());
                        return _newSpot;
                    }
                }
                Log("No safe spot found :(");
                return loc;
            }
            #endregion
            else
            {
                return loc;
            }
        }
        #endregion

        #region Checking a possible landing spot for clearance
        public static bool clearSpot(WoWPoint loc)
        {
            for (double i = -5.0; i <= 5.0; i = i + 0.5)
            {
                for (double j = -5.0; j <= 5.0; j = j + 0.5)
                {
                    var _tempLoc = new WoWPoint(loc.X + i, loc.Y + j, loc.Z);
                    if (Logic.Pathing.Navigator.FindHeights(_tempLoc.X, _tempLoc.Y).Count() != 1 ||
                        !WoWInternals.World.GameWorld.IsInLineOfSightOCD(new WoWPoint(_tempLoc.X, _tempLoc.Y, _tempLoc.Z + 50), _tempLoc))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        #endregion

        #region Perform "safeFlight", flying to point A and then Navigator'ing to the real target
        public static void performSafeFlight(WoWPoint landLoc, WoWPoint targetLoc)
        {
            if (_me.Location.Distance(targetLoc) > 2)
            {
                while (landLoc.Distance(_me.Location) > 1)
                {
                    Flightor.MoveTo(landLoc);
                    Thread.Sleep(100);
                }
                Flightor.MountHelper.Dismount();
                while (targetLoc.Distance(_me.Location) > 1)
                {
                    Navigator.MoveTo(targetLoc);
                    Thread.Sleep(100);
                }
            }
        }
        #endregion

        #region Mailing with MOLL-E
        public static void Mail()
        {
            if (_me.IsGhost || _me.Dead || !_hasMOLLE)
                return;
            if (UInt32.Parse(Lua.GetReturnValues("return GetItemCooldown(" + molle + ")")[0]) == 0)
            {
                Log("Using MOLL-E");
                Lua.DoString("UseItemByName(" + molle + ")");
                Thread.Sleep(2000);
                ObjectManager.Update();
                var unitList = ObjectManager.GetObjectsOfType<WoWGameObject>();
                foreach (WoWGameObject unit in unitList)
                {
                    if (unit.SubType == WoWGameObjectType.Mailbox)
                    {
                        unit.Interact();
                        Thread.Sleep(2500);
                        Logic.Vendors.MailAllItems();
                        break;
                    }
                }
            }
            else
            {
                var _closestBox = Styx.Logic.Profiles.ProfileManager.CurrentProfile.MailboxManager.GetClosestMailbox().Location;
                var _safeSpot = findSafeFlightPoint(_closestBox);
                if (_safeSpot.Equals(_closestBox))
                {
                    flyTo(_currentPOI.Location);
                }
                else { performSafeFlight(_safeSpot, _closestBox); }
                ObjectManager.Update();
                var unitList = ObjectManager.GetObjectsOfType<WoWGameObject>();
                foreach (WoWGameObject unit in unitList)
                {
                    if (unit.SubType == WoWGameObjectType.Mailbox)
                    {
                        unit.Interact();
                        Thread.Sleep(2500);
                        Logic.Vendors.MailAllItems();
                        break;
                    }
                }
            }
            return;
        }
        #endregion

        #region Banking 
        //Currently not working, no easy method to deposit items. Getting out the guild chest should still work tho.
        public static void Bank(bool guildBank)
        {
            if (_me.IsGhost || _me.Dead)
                return;
            if (guildBank)
            {
                if (_hasGuildChest && UInt32.Parse(Lua.GetReturnValues("return GetSpellCooldown("+guildChest+")")[0]) == 0)
                {
                    Log("Guild Chest available and off cooldown, using");
                    Lua.DoString("CastSpellByID("+guildChest+")");
                    Thread.Sleep(4000);
                    ObjectManager.Update();
                    var unitList = ObjectManager.GetObjectsOfType<WoWObject>();
                    foreach (WoWObject unit in unitList)
                    {
                        if (unit.Entry == 206603)
                        {
                            unit.Interact();
                            Thread.Sleep(2500);
                             
                            break;
                        }
                    }
                    return;
                }
            }
            if (!guildBank)
            {
                if (_hasJeeves && UInt32.Parse(Lua.GetReturnValues("return GetItemCooldown("+jeeves+")")[0]) == 0)
                {
                    Log("Jeeves available, summoning!");
                    Lua.DoString("UseItemByName("+jeeves+")");
                    Thread.Sleep(2500);
                    ObjectManager.Update();
                    var unitList = new List<WoWObject>();
                    
                    foreach (WoWObject unit in unitList)
                    {
                        if (unit.Entry == 35642)
                        {
                            unit.Interact();
                            Thread.Sleep(2500);
                            Logic.Inventory.Frames.Gossip.GossipFrame gf = Logic.Inventory.Frames.Gossip.GossipFrame.Instance;
                            if (gf.GossipOptionEntries != null)
                            {
                                gf.SelectGossipOption(0);
                                //deposit stuff
                                break;
                            }
                        }
                    }
                    return;
                }
            }
        }
        #endregion

        #region Logging methods
        private static void Log(bool debug, string format, params object [] args)
        {
            if (debug)
                Logging.WriteDebug("[FieldRepair] {0}", string.Format(format, args));
            else
                Logging.Write("[FieldRepair] {0}", string.Format(format, args));
        }
        
        private static void Log(string format)
        {
            Log(false, format);
        }
        
        private static void Log(string format, params object [] args)
        {
            Log(false, format, args);
        }
        #endregion
    }
}
                    
