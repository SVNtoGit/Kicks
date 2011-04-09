using System.Collections.Generic;
using System.Linq;
using Styx.Helpers;
using Styx.Logic.BehaviorTree;
using Styx.Logic.Pathing;
using Styx.Logic.Questing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;
using System.Diagnostics;
using Styx.Logic.Combat;

using Action = TreeSharp.Action;

namespace Styx.Bot.Quest_Behaviors
{
    /// <summary>
    /// VehicleMover by HighVoltz
    /// Moves to location while in a vehicle
    /// ##Syntax##
    /// VehicleID: ID of the vehicle
    /// UseNavigator: (optional) true/false. Setting to false will use Click To Move instead of the Navigator. Default true
    /// Precision: (optional) This behavior moves on to the next waypoint when at Precision distance or less to current waypoint. Default 4;
    /// MobID: (optional) NPC ID to cast spell on to cast spell on.. not required even if you specify a spellID
    /// SpellID: (optional) Casts spell after reaching location.
    /// CastTime: (optional) The Spell Cast Time. Default 0;
    /// X,Y,Z: The location where you want to move to
    /// </summary>
    /// 
    public class VehicleMover : CustomForcedBehavior
    {
        Dictionary<string, object> recognizedAttributes = new Dictionary<string, object>()
        {
            {"VehicleID",null},
            {"UseNavigator",null},
            {"Precision",null},
            {"MobID",null},
            {"SpellID",null},
            {"CastTime",null},
            {"X",null},
            {"Y",null},
            {"Z",null},
        };
        bool success = true;
        public VehicleMover(Dictionary<string, string> args)
            : base(args)
        {
            // tba. dictionary format is not documented.
            // CheckForUnrecognizedAttributes(recognizedAttributes);
            int vehicleID = 0;
            bool useNavigator = true;
            int precision = 0;
            int mobID = 0;
            int spellID = 0;
            int castTime = 0;
            WoWPoint point = WoWPoint.Empty;

            success = success && GetAttributeAsInteger("VehicleID", true, "0", 0, int.MaxValue, out vehicleID);
            success = success && GetAttributeAsBoolean("UseNavigator", false, "true", out useNavigator);
            success = success && GetAttributeAsInteger("Precision", false, "4", 2, int.MaxValue, out precision);
            success = success && GetAttributeAsInteger("MobID", false, "0", 0, int.MaxValue, out mobID);
            success = success && GetAttributeAsInteger("SpellID", false, "0", 0, int.MaxValue, out spellID);
            success = success && GetAttributeAsInteger("CastTime", false, "0", 0, int.MaxValue, out castTime);
            success = success && GetXYZAttributeAsWoWPoint("X", "Y", "Z", true, WoWPoint.Empty, out point);

            VehicleID = vehicleID;
            Precision = precision;
            SpellID = spellID;
            CastTime = castTime;
            UseNavigator = useNavigator;
            Location = point;
        }

        public int VehicleID { get; private set; }
        public int Precision { get; private set; }
        public int MobID { get; private set; }
        public int SpellID { get; private set; }
        public int CastTime { get; private set; }
        public bool UseNavigator { get; private set; }
        public WoWPoint Location { get; private set; }
        public WoWPoint[] Path { get; private set; }
        Stopwatch pauseSW = new Stopwatch();// add a small pause before casting.. 
        Stopwatch castSW = new Stopwatch();// cast timer.
        bool casted = false;
        int pathIndex = 0;

        public WoWObject Vehicle
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWObject>(true).Where(o => o.Entry == VehicleID).
                    OrderBy(o => o.Distance).FirstOrDefault();
            }
        }

        #region Overrides of CustomForcedBehavior
        private Composite root;
        protected override Composite CreateBehavior()
        {
            return root ??
                (root = new PrioritySelector(
                    new Decorator(c => success == false,
                        new Action(c =>
                        {
                            Err("Invalid or missing Attributes, Stopping HB");
                        })),
                    new Decorator(c => Vehicle == null,
                        new Action(c =>
                        {
                            Err("No Vehicle matching ID was found, Stopping HB");
                        })),
                    new Decorator(c => Vehicle.Location.Distance(Location) <= Precision,
                        CreateSpellBehavior),
                    new Decorator(c => UseNavigator && Path == null,
                        new Decorator(c => Path == null,
                            new Action(c =>
                            {
                                Path = Navigator.GeneratePath(Vehicle.Location, Location);
                                if (Path == null || Path.Length == 0)
                                {
                                    Err("Unable to genorate path to {0}\nStoping HB.", Location);
                                }
                            })
                        )
                    ),
                    new Action(c =>
                    {
                        WoWMovement.ClickToMove(moveToLocation);
                    })
                ));
        }

        Composite CreateSpellBehavior
        {
            get
            {
                return new Action(c =>
                {
                    if (SpellID > 0)
                    {
                        if (!casted)
                        {
                            if (!pauseSW.IsRunning)
                                pauseSW.Start();
                            if (pauseSW.ElapsedMilliseconds >= 1000)
                            {
                                if (ObjectManager.Me.IsMoving)
                                {
                                    WoWMovement.MoveStop();
                                    return;
                                }
                                if (MobID > 0)
                                {
                                    WoWUnit mob = ObjectManager.GetObjectsOfType<WoWUnit>(true).Where(o => o.Entry == MobID).
                                        OrderBy(o => o.Distance).FirstOrDefault();
                                    if (mob != null)
                                        mob.Target();
                                }
                                // getting a "Spell not learned" error if using HB's spell casting api..
                                Lua.DoString("CastSpellByID({0})", SpellID);
                                casted = true;
                                castSW.Start();
                                pauseSW.Stop();
                                pauseSW.Reset();
                                if (CastTime == 0)
                                    isDone = true;
                            }
                            return;
                        }
                        else if (castSW.ElapsedMilliseconds < CastTime)
                        {
                            return;
                        }
                    }
                    castSW.Stop();
                    castSW.Reset();
                    isDone = true;
                });
            }
        }

        WoWPoint moveToLocation
        {
            get
            {
                if (UseNavigator)
                {
                    if (Vehicle.Location.Distance(Path[pathIndex]) <= Precision && pathIndex < Path.Length - 1)
                        pathIndex++;
                    return Path[pathIndex];
                }
                else
                    return Location;
            }
        }

        void Err(string format, params object[] args)
        {
            Logging.Write(System.Drawing.Color.Red, "VehicleMover: " + format, args);
            TreeRoot.Stop();
        }

        void Log(string format, params object[] args)
        {
            Logging.Write("VehicleMover: " + format, args);
        }

        private bool isDone = false;

        public override bool IsDone { get { return isDone; } }

        public override void OnStart()
        {
            TreeRoot.GoalText = string.Format("Moving to:{0} while in Vehicle with ID {1} using {2}",
                Location, VehicleID, UseNavigator ? "Navigator" : "Click-To-Move");
        }

        #endregion
    }
}
