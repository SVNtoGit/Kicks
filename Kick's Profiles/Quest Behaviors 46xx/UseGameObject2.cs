// Behavior originally contributed by Unknown.
//
// DOCUMENTATION:
//     
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Styx.Helpers;
using Styx.Logic.BehaviorTree;
using Styx.Logic.Pathing;
using Styx.Logic.Questing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using TreeSharp;
using Action = TreeSharp.Action;


namespace Styx.Bot.Quest_Behaviors.UseGameObject2
{
    public class UseGameObject : CustomForcedBehavior
    {
        public UseGameObject(Dictionary<string, string> args)
            : base(args)
        {
            try
            {
                // QuestRequirement* attributes are explained here...
                //    http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Programming_Cookbook:_QuestId_for_Custom_Behaviors
                // ...and also used for IsDone processing.
                Location    = GetXYZAttributeAsWoWPoint("", true, null) ?? WoWPoint.Empty;
                NumOfTimes  = GetAttributeAsNumOfTimes("NumOfTimes", false, null) ?? 1;
                ObjectId    = GetAttributeAsMobId("ObjectId", true, null) ?? 0;
                QuestId     = GetAttributeAsQuestId("QuestId", false, null) ?? 0;
                QuestRequirementComplete = GetAttributeAsEnum<QuestCompleteRequirement>("QuestCompleteRequirement", false, null) ?? QuestCompleteRequirement.NotComplete;
                QuestRequirementInLog    = GetAttributeAsEnum<QuestInLogRequirement>("QuestInLogRequirement", false, null) ?? QuestInLogRequirement.InLog;

                Counter = 1;
                MovedToTarget = false;
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
        public WoWPoint                 Location { get; private set; }
        public int                      ObjectId { get; private set; }
        public int                      NumOfTimes { get; private set; }
        public int                      QuestId { get; private set; }
        public QuestCompleteRequirement QuestRequirementComplete { get; private set; }
        public QuestInLogRequirement    QuestRequirementInLog { get; private set; }

        // Private variables for internal state
        private bool                    _isBehaviorDone;
        private List<WoWGameObject>     _objectList;
        private Composite               _root;

        // Private properties
        private int                     Counter { get; set; }
        private LocalPlayer             Me { get { return (ObjectManager.Me); } }
        private bool                    MovedToTarget { get; set; }


        public void UseGameObjectFunc()
        {
            UtilLogMessage("info", "Used ObjectId({0}) {1}/{2} times.", ObjectId, Counter, NumOfTimes);
            _objectList[0].Interact();
            StyxWoW.SleepForLagDuration();
            Counter++;
            Thread.Sleep(6000);
        }


        /// <summary>
        /// A Queue for npc's we need to talk to
        /// </summary>
        //private WoWUnit CurrentUnit { get { return ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(unit => unit.Distance < 100 && unit.Entry == MobId); } }

        #region Overrides of CustomForcedBehavior

        protected override Composite CreateBehavior()
        {

            return _root ?? (_root =
                new PrioritySelector(

                    new Decorator(ret => (QuestId != 0 && Me.QuestLog.GetQuestById((uint)QuestId) != null &&
                         Me.QuestLog.GetQuestById((uint)QuestId).IsCompleted),
                        new Action(ret => _isBehaviorDone = true)),

                    new Decorator(ret => Counter > NumOfTimes,
                        new Action(ret => _isBehaviorDone = true)),

                        new PrioritySelector(

                           new Decorator(ret => !MovedToTarget,
                                new Action(delegate
                                {
                                    WoWPoint destination1 = new WoWPoint(Location.X, Location.Y, Location.Z);
                                    WoWPoint[] pathtoDest1 = Styx.Logic.Pathing.Navigator.GeneratePath(Me.Location, destination1);

                                    foreach (WoWPoint p in pathtoDest1)
                                    {
                                        while (!Me.Dead && p.Distance(Me.Location) > 3)
                                        {
                                            if (Me.Combat)
                                            {
                                                break;
                                            }
                                            Thread.Sleep(100);
                                            WoWMovement.ClickToMove(p);
                                        }

                                        if (Me.Combat)
                                        {
                                            break;
                                        }
                                    }

                                    if (Me.Combat)
                                    {
                                        return RunStatus.Success;
                                    }
                                    else if (!Me.Combat)
                                    {
                                        MovedToTarget = true;
                                        return RunStatus.Success;
                                    }

                                    return RunStatus.Running;

                                })
                                ),

                            new Decorator(ret => StyxWoW.Me.IsMoving,
                                new Action(delegate
                                {
                                    WoWMovement.MoveStop();
                                    StyxWoW.SleepForLagDuration();
                                })
                                ),

                            new Decorator(ret => MovedToTarget,
                                new Action(delegate
                                {
                                    // CurrentUnit.Interact();

                                    ObjectManager.Update();

                                    _objectList = ObjectManager.GetObjectsOfType<WoWGameObject>()
                                        .Where(u => u.Entry == ObjectId && !u.InUse && !u.IsDisabled)
                                        .OrderBy(u => u.Distance).ToList();

                                    if (_objectList.Count >= 1)
                                    {

                                        WoWPoint destination1 = new WoWPoint(_objectList[0].Location.X, _objectList[0].Location.Y, _objectList[0].Location.Z);
                                        WoWPoint[] pathtoDest1 = Styx.Logic.Pathing.Navigator.GeneratePath(Me.Location, destination1);

                                        foreach (WoWPoint p in pathtoDest1)
                                        {
                                            while (!Me.Dead && p.Distance(Me.Location) > 3)
                                            {
                                                if (Me.Combat)
                                                {
                                                    break;
                                                }
                                                Thread.Sleep(100);
                                                WoWMovement.ClickToMove(p);
                                            }

                                            if (Me.Combat)
                                            {
                                                break;
                                            }
                                        }

                                        if (Me.Combat)
                                        {
                                            return RunStatus.Success;
                                        }
                                        else if (!Me.Combat)
                                        {
                                            Thread.Sleep(1000);
                                            UseGameObjectFunc();
                                        }
                                    }

                                    if (Me.Combat)
                                    {
                                        return RunStatus.Success;
                                    }


                                    if (Counter > NumOfTimes)
                                    {
                                        return RunStatus.Success;
                                    }
                                    return RunStatus.Running;
                                })
                                ),

                            new Action(ret => Navigator.MoveTo(Location))
                        )
                    ));
        }


        public override bool IsDone
        {
            get
            {
                return (_isBehaviorDone     // normal completion
                        || !UtilIsProgressRequirementsMet(QuestId, QuestRequirementInLog, QuestRequirementComplete));
            }
        }


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
                PlayerQuest quest = StyxWoW.Me.QuestLog.GetQuestById((uint)QuestId);

                TreeRoot.GoalText = this.GetType().Name + ": " + ((quest != null) ? ("\"" + quest.Name + "\"") : "In Progress");
            }
        }


        #endregion
    }
}
