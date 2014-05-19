using Styx.Helpers;
using Styx.Logic.BehaviorTree;
using Styx.Logic.Combat;
using Styx.Logic.Pathing;
using Styx.Plugins.PluginClass;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DHXPickBuddy
{
    public class DHXPickBuddy : HBPlugin
    {
        #region HBformdetails
        public override string Name { get { return "DHX Pick Pocket Buddy"; } }
        public override string Author { get { return "DHX: Protopally"; } }
        public override Version Version { get { return new Version(0, 1); } }
        public override bool WantButton { get { return true; } }
				public override string ButtonText { get { return "DHX"; } }
				public static LocalPlayer Me { get { return ObjectManager.Me; } }
				public static Dictionary<ulong, DateTime> PPBL = new Dictionary<ulong, DateTime>();
				static float PickRange = 5;
				static bool FirstRun = true;
        #endregion
        #region UserVariables
        #endregion
        #region VariablesAndStuff
        #endregion
        public override void Pulse()
					{
					#region pulse Checks
					if (Me.Combat || Me.IsGhost || Me.IsResting || Me.Dead || Me.Looting) return;
					if (!TreeRoot.IsRunning) return;
					if (FirstRun)
						{
						Logging.Write(Color.Purple, "DHX PickPocket Buddy Loaded.");
						GetGlyphs();
						FirstRun = false;
						}
					#endregion pulse checks
					#region stealth test
					while (!Me.Auras.ContainsKey("Stealth"))
						{
						Thread.Sleep(10);
						WoWMovement.MoveStop();
						SpellManager.Cast("Stealth");
						Thread.Sleep(10);
						if (Me.Auras.ContainsKey("Stealth"))
							{
							break;
							}
						if (Me.Combat) { return; }
						}
					#endregion stealth test
					updateBL();
					if (getPick() != null) getPick().Target();
					if (Me.GotTarget)
						{
						while (Me.CurrentTarget.Distance > 0)
							{
							if (Me.Combat) return;
							GotoPick();
							if (Me.CurrentTarget.Distance <= PickRange)
								{
								Styx.Logic.Combat.SpellManager.Cast(921);
								Thread.Sleep(1500);
								AddBL();
								return;
								}
							}
						}
        }
        public override void OnButtonPress()
        {
				foreach (KeyValuePair<ulong, DateTime> ips in PPBL)
						{
						Logging.Write("fyjtyjk");
						Logging.Write(ips.Key.ToString() + " " + ips.Value.ToString());
						}
        }
				public void GotoPick()
					{
					safeMoveTo(WoWMathHelper.CalculatePointAtSide(getPick().Location, getPick().Rotation, PickRange - 2, MEBEHIND()));
					if(Me.Combat) return;
					}
				public bool MEBEHIND()
					{
					if (Me.IsSafelyBehind(getPick())) { return false; }
					else { return true; }
					}
				public static WoWUnit getPick()
					{
					ObjectManager.Update();
					List<WoWUnit> mobs = (from o in ObjectManager.ObjectList
																where o is WoWUnit && o.Distance <= PickRange + 10
																let u = o.ToUnit()
																where u.IsAlive && !searchBL(u) && (u.CreatureType == Styx.WoWCreatureType.Humanoid || u.CreatureType == Styx.WoWCreatureType.Undead)
																select u).ToList();
					return GetClosestUnitFromList(mobs);
					}
			public static WoWUnit GetClosestUnitFromList(List<WoWUnit> MOB)
				{
				WoWUnit closest = null;
				double closestDist = double.MaxValue;
				foreach (WoWUnit u in MOB)
					{
					double dist = u.DistanceSqr;
					if (dist < closestDist && u.InLineOfSight)
						{
						closestDist = dist;
						closest = u;
						}
					}
				MOB.Clear();
				return closest;
				}
			public static void AddBL()
				{
				if (Me.GotTarget)
					{
					PPBL.Add(Me.CurrentTargetGuid, DateTime.Now.AddMinutes(10));
					}
				}
			public void updateBL()
				{
				if (PPBL != null)
					{
					foreach (KeyValuePair<ulong, DateTime> BLI in PPBL)
						{
						if (BLI.Value <= DateTime.Now)
							{
							PPBL.Remove(BLI.Key);
							}
						}
					}
				}
			public static bool searchBL(WoWUnit MOB)
				{
				if (PPBL != null)
					{
					foreach (KeyValuePair<ulong, DateTime> BLI in PPBL)
						{
						if (BLI.Key == MOB.Guid)
							{
							return true;
							}
						}
					return false;
					}
				else return false;
				}
			private static void safeMoveTo(WoWPoint point)
				{
				try
					{
					WoWPoint movePoint = point;

					if (Navigator.GeneratePath(Me.Location, movePoint).Length == 0)
						{
						Logging.Write("Failed to generate path, aborting...");
						throw new Exception("restart");
						}
					else
						{
						Navigator.MoveTo(movePoint);
						Thread.Sleep(100);
						}
					}
				catch (System.Threading.ThreadAbortException)
					{
					Logging.WriteDebug("Thread abort exception");
					throw;
					}
				catch (Exception c)
					{
					Logging.WriteDebug("An exception occurred: " + c);
					}
				}
			public static void GetGlyphs()
				{
				try
					{
					var glyph1 = Lua.GetReturnValues("return GetGlyphLink(1)", "stuffnthings.lua");
					var glyph2 = Lua.GetReturnValues("return GetGlyphLink(2)", "stuffnthings.lua");
					var glyph3 = Lua.GetReturnValues("return GetGlyphLink(3)", "stuffnthings.lua");
					var glyph4 = Lua.GetReturnValues("return GetGlyphLink(4)", "stuffnthings.lua");
					var glyph5 = Lua.GetReturnValues("return GetGlyphLink(5)", "stuffnthings.lua");
					var glyph6 = Lua.GetReturnValues("return GetGlyphLink(6)", "stuffnthings.lua");
					if (glyph1 != null) { if (glyph1[0].Contains("Glyph of Pick Pocket")) { PickRange = 10; Logging.Write(Color.Purple, "Glyph of Pick Pocket found, Extending Pick Pocket Range."); } }
					if (glyph2 != null) { if (glyph2[0].Contains("Glyph of Pick Pocket")) { PickRange = 10; Logging.Write(Color.Purple, "Glyph of Pick Pocket found, Extending Pick Pocket Range."); } }
					if (glyph3 != null) { if (glyph3[0].Contains("Glyph of Pick Pocket")) { PickRange = 10; Logging.Write(Color.Purple, "Glyph of Pick Pocket found, Extending Pick Pocket Range."); } }
					if (glyph4 != null) { if (glyph4[0].Contains("Glyph of Pick Pocket")) { PickRange = 10; Logging.Write(Color.Purple, "Glyph of Pick Pocket found, Extending Pick Pocket Range."); } }
					if (glyph5 != null) { if (glyph5[0].Contains("Glyph of Pick Pocket")) { PickRange = 10; Logging.Write(Color.Purple, "Glyph of Pick Pocket found, Extending Pick Pocket Range."); } }
					if (glyph6 != null) { if (glyph6[0].Contains("Glyph of Pick Pocket")) { PickRange = 10; Logging.Write(Color.Purple, "Glyph of Pick Pocket found, Extending Pick Pocket Range."); } }
					}
				catch (Exception)
					{
					//WoWPulsator.Pulse(PulseFlags.);
					//Utils.Log("**** Exception in glyph check");
					}
				}
    }
}                