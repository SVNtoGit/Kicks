// Behavior originally contributed by HighVoltz.
//
// DOCUMENTATION:
//     
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Styx.Helpers;
using Styx.Logic.BehaviorTree;
using Styx.Logic.Pathing;
using Styx.Logic.Questing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using TreeSharp;
using Action = TreeSharp.Action;


namespace Styx.Bot.Quest_Behaviors.VehicleMover
{
    /// <summary>
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
        public VehicleMover(Dictionary<string, string> args)
            : base(args)
        {
            try
            {
                CastTime    = GetAttributeAsInteger("CastTime", false, 0, int.MaxValue, null) ?? 0;
                Location    = GetXYZAttributeAsWoWPoint("", true, null) ?? WoWPoint.Empty;
                MobId       = GetAttributeAsMobId("MobId", false, new [] { "MobID" }) ?? 0;
                Precision   = GetAttributeAsInteger("Precision", false, 2, int.MaxValue, null) ?? 4;
                SpellId     = GetAttributeAsSpellId("SpellId", false, new [] { "SpellID" }) ?? 0;
                UseNavigator = GetAttributeAsBoolean("UseNavigator", false, null) ?? true;
                VehicleId   = GetAttributeAsMobId("VehicleId", true, new [] { "VehicleID" }) ?? 0;
            }

			catch (Exception except)
			{
				// Maintenance problems occur for a number of reasons.  The primary two are...
				// * Changes were made to the behavior, and boundary conditions weren't properly tested.
				// * The Honorbuddy core was changed, and the behavior wasn't adjusted for the new changes.
				// In any case, we pinpoint the source of the problem area here, and hopefully it
				// can be quickly resolved.
				UtilLogMessage("error", "BEHAVIOR MAINTENANCE PROBLEM: " + except.Message
										+ "\nFROM HERE:\n"
										+ except.StackTrace + "\n");
				IsAttributeProblem = true;
			}
        }

        // Attributes provided by caller
        public int              CastTime { get; private set; }
        public WoWPoint         Location { get; private set; }
        public int              MobId { get; private set; }
        public int              Precision { get; private set; }
        public int              SpellId { get; private set; }
        public bool             UseNavigator { get; private set; }
        public int              VehicleId { get; private set; }

        // Private variables for internal state
        private bool            _casted = false;
        private Stopwatch       _castStopwatch = new Stopwatch();// cast timer.
        private bool            _isBehaviorDone;
        private int             _pathIndex = 0;
        private Stopwatch       _pauseStopwatch = new Stopwatch();// add a small pause before casting.. 
        private Composite       _root;

        // Private properties
        private WoWPoint[]      Path { get; private set; }
        private WoWObject       Vehicle { get { return ObjectManager.GetObjectsOfType<WoWObject>(true)
                                                                    .Where(o => o.Entry == VehicleId)
                                                                    .OrderBy(o => o.Distance)
                                                                    .FirstOrDefault();
                                        }}


        Composite CreateSpellBehavior
        {
            get
            {
                return new Action(c =>
                {
                    if (SpellId > 0)
                    {
                        if (!_casted)
                        {
                            if (!_pauseStopwatch.IsRunning)
                                _pauseStopwatch.Start();
                            if (_pauseStopwatch.ElapsedMilliseconds >= 1000)
                            {
                                if (ObjectManager.Me.IsMoving)
                                {
                                    WoWMovement.MoveStop();
                                    return;
                                }
                                if (MobId > 0)
                                {
                                    WoWUnit mob = ObjectManager.GetObjectsOfType<WoWUnit>(true).Where(o => o.Entry == MobId).
                                        OrderBy(o => o.Distance).FirstOrDefault();
                                    if (mob != null)
                                        mob.Target();
                                }
                                // getting a "Spell not learned" error if using HB's spell casting api..
                                Lua.DoString("CastSpellByID({0})", SpellId);
                                _casted = true;
                                _castStopwatch.Start();
                                _pauseStopwatch.Stop();
                                _pauseStopwatch.Reset();
                                if (CastTime == 0)
                                    _isBehaviorDone = true;
                            }
                            return;
                        }
                        else if (_castStopwatch.ElapsedMilliseconds < CastTime)
                        {
                            return;
                        }
                    }
                    _castStopwatch.Stop();
                    _castStopwatch.Reset();
                    _isBehaviorDone = true;
                });
            }
        }

        WoWPoint moveToLocation
        {
            get
            {
                if (UseNavigator)
                {
                    if (Vehicle.Location.Distance(Path[_pathIndex]) <= Precision && _pathIndex < Path.Length - 1)
                        _pathIndex++;
                    return Path[_pathIndex];
                }
                else
                    return Location;
            }
        }


        #region Overrides of CustomForcedBehavior

        protected override Composite CreateBehavior()
        {
            return _root ??
                (_root = new PrioritySelector(
                    new Decorator(c => Vehicle == null,
                        new Action(c =>
                        {
                            UtilLogMessage("fatal", "No VehicleId({0}) could be located.", VehicleId);
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
                                    UtilLogMessage("fatal", "Unable to generate path to {0}.", Location);
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

        public override bool IsDone { get { return _isBehaviorDone; } }

        public override void OnStart()
        {
            // This reports problems, and stops BT processing if there was a problem with attributes...
            // We had to defer this action, as the 'profile line number' is not available during the element's
            // constructor call.
            OnStart_HandleAttributeProblem();

            // If the quest is complete, this behavior is already done...
            // So we don't want to falsely inform the user of things that will be skipped.
            if (!IsDone)
            {
                TreeRoot.GoalText =  this.GetType().Name + ": In Progress";
                TreeRoot.StatusText = string.Format("Moving to:{0} while in Vehicle with ID {1} using {2}",
                                                    Location, VehicleId, UseNavigator ? "Navigator" : "Click-To-Move");
            }
        }

        #endregion
    }
}
