			// Behavior originally contributed by Natfoth.
//
// WIKI DOCUMENTATION:
//     http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Custom_Behavior:_CastSpellOn
//
// QUICK DOX:
//      Allows you to cast a specific spell on a target.  Useful for training dummies and starting quests.
//
//  Parameters (required, then optional--both listed alphabetically):
//      MobId: Id of the target (NPC or Object) on which the spell should be cast.
//      SpellId: Spell that should be used to cast on the target
//      X,Y,Z: The general location where the targets can be found.
//
//      HpLeftAmount [Default:110 hitpoints]: How low the hitpoints on the target should be before attempting
//              to cast a spell on the target.   Note this is an absolute value, and not a percentage.
//      MinRange [Default:3 yards]: minimum distance from the target at which a cast attempt shoudl be made.
//      NumOfTimes [Default:1]: The number of times to perform th casting action on viable targets in the area.
//      QuestId [Default:none]:
//      QuestCompleteRequirement [Default:NotComplete]:
//      QuestInLogRequirement [Default:InLog]:
//              A full discussion of how the Quest* attributes operate is described in
//              http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Programming_Cookbook:_QuestId_for_Custom_Behaviors
//      Range [Default:25 yards]: maximum distance from the target at which a cast attempt should be made.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Styx.Logic.BehaviorTree;
using Styx.Logic.Combat;
using Styx.Logic.Pathing;
using Styx.Logic.Questing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using TreeSharp;
using Action = TreeSharp.Action;


namespace Styx.Bot.Quest_Behaviors
{
    public class CastSpellOn : CustomForcedBehavior
    {
        public CastSpellOn(Dictionary&lt;string, string&gt; args)
            : base(args)
        {
            try
            {
                Location = GetAttributeAsNullable&lt;WoWPoint&gt;(&quot;&quot;, false, ConstrainAs.WoWPointNonEmpty, null) ?? Me.Location;
                MinRange = GetAttributeAsNullable&lt;double&gt;(&quot;MinRange&quot;, false, ConstrainAs.Range, null) ?? 3;
                MobHpPercentLeft = GetAttributeAsNullable&lt;double&gt;(&quot;MobHpPercentLeft&quot;, false, ConstrainAs.Percent, new[] { &quot;HpLeftAmount&quot; }) ?? 110;
                MobIds = GetNumberedAttributesAsArray&lt;int&gt;(&quot;MobId&quot;, 1, ConstrainAs.MobId, new[] { &quot;NpcId&quot; });
                NumOfTimes = GetAttributeAsNullable&lt;int&gt;(&quot;NumOfTimes&quot;, false, ConstrainAs.RepeatCount, null) ?? 1;
                QuestId = GetAttributeAsNullable&lt;int&gt;(&quot;QuestId&quot;, false, ConstrainAs.QuestId(this), null) ?? 0;
                QuestRequirementComplete = GetAttributeAsNullable&lt;QuestCompleteRequirement&gt;(&quot;QuestCompleteRequirement&quot;, false, null, null) ?? QuestCompleteRequirement.NotComplete;
                QuestRequirementInLog = GetAttributeAsNullable&lt;QuestInLogRequirement&gt;(&quot;QuestInLogRequirement&quot;, false, null, null) ?? QuestInLogRequirement.InLog;
                Range = GetAttributeAsNullable&lt;double&gt;(&quot;Range&quot;, false, ConstrainAs.Range, null) ?? 25;
                SpellIds = GetNumberedAttributesAsArray&lt;int&gt;(&quot;SpellId&quot;, 1, ConstrainAs.SpellId, null);
                //SpellId = GetAttributeAsNullable&lt;int&gt;(&quot;SpellId&quot;, false, ConstrainAs.SpellId, null) ?? 0;
                SpellName = GetAttributeAs&lt;string&gt;(&quot;SpellName&quot;, false, ConstrainAs.StringNonEmpty, new[] { &quot;spellname&quot; }) ?? &quot;&quot;;

                Counter = 1;

                // Semantic checks
                if (Range &lt;= MinRange)
                {
                    LogMessage(&quot;fatal&quot;, &quot;\&quot;Range\&quot; attribute must be greater than \&quot;MinRange\&quot; attribute.&quot;);
                    IsAttributeProblem = true;
                }

                SpellId = SpellIds.FirstOrDefault(id =&gt; SpellManager.HasSpell(id));

                foreach (int i in SpellIds)
                {
                    if (SpellManager.HasSpell(i))
                    {
                        SpellId = i;
                        break;
                    }
                }

                CastSelf = false;

                if (MobIds.FirstOrDefault() == 0)
                    CastSelf = true;
            }

            catch (Exception except)
            {
                // Maintenance problems occur for a number of reasons.  The primary two are...
                // * Changes were made to the behavior, and boundary conditions weren't properly tested.
                // * The Honorbuddy core was changed, and the behavior wasn't adjusted for the new changes.
                // In any case, we pinpoint the source of the problem area here, and hopefully it
                // can be quickly resolved.
                LogMessage(&quot;error&quot;, &quot;BEHAVIOR MAINTENANCE PROBLEM: &quot; + except.Message
                                    + &quot;\nFROM HERE:\n&quot;
                                    + except.StackTrace + &quot;\n&quot;);
                IsAttributeProblem = true;
            }
        }


        // Attributes provided by caller
        public WoWPoint Location { get; private set; }
        public double MinRange { get; private set; }
        public double MobHpPercentLeft { get; private set; }
        public int[] MobIds { get; private set; }
        public int NumOfTimes { get; private set; }
        public int QuestId { get; private set; }
        public QuestCompleteRequirement QuestRequirementComplete { get; private set; }
        public QuestInLogRequirement QuestRequirementInLog { get; private set; }
        public double Range { get; private set; }
        public bool CastSelf { get; private set; }
        public int[] SpellIds { get; private set; }
        public int SpellId { get; private set; }
        public string SpellName { get; private set; }

        // Private variables for internal state
        private bool _isBehaviorDone;
        private bool _isDisposed;
        private Composite _root;

        // Private properties
        private int Counter { get; set; }
        private LocalPlayer Me { get { return (ObjectManager.Me); } }
        public List&lt;WoWUnit&gt; MobList
        {
            get
            {
                if (MobHpPercentLeft &gt; 0)
                {
                    return (ObjectManager.GetObjectsOfType&lt;WoWUnit&gt;()
                                         .Where(u =&gt; MobIds.Contains((int)u.Entry) &amp;&amp; !u.Dead &amp;&amp; u.HealthPercent &lt;= MobHpPercentLeft)
                                         .OrderBy(u =&gt; u.Distance).ToList());
                }
                else
                {
                    return (ObjectManager.GetObjectsOfType&lt;WoWUnit&gt;()
                                         .Where(u =&gt; MobIds.Contains((int)u.Entry) &amp;&amp; !u.Dead)
                                         .OrderBy(u =&gt; u.Distance).ToList());
                }
            }
        }

        public WoWSpell CurrentBehaviorSpell
        {
            get
            {
                return WoWSpell.FromId(SpellId);
            }
        }

        public float maxSpellRange
        {
            get
            {
                if (CurrentBehaviorSpell.MaxRange == 0)
                    return 4;

                return CurrentBehaviorSpell.MaxRange;
            }
        }

        // DON'T EDIT THESE--they are auto-populated by Subversion
        public override string SubversionId { get { return (&quot;$Id$&quot;); } }
        public override string SubversionRevision { get { return (&quot;$Revision$&quot;); } }


        ~CastSpellOn()
        {
            Dispose(false);
        }


        public void Dispose(bool isExplicitlyInitiatedDispose)
        {
            if (!_isDisposed)
            {
                // NOTE: we should call any Dispose() method for any managed or unmanaged
                // resource, if that resource provides a Dispose() method.

                // Clean up managed resources, if explicit disposal...
                if (isExplicitlyInitiatedDispose)
                {
                    // empty, for now
                }

                // Clean up unmanaged resources (if any) here...
                TreeRoot.GoalText = string.Empty;
                TreeRoot.StatusText = string.Empty;

                // Call parent Dispose() (if it exists) here ...
                base.Dispose();
            }

            _isDisposed = true;
        }


        Composite CreateSpellBehavior
        {
            get
            {
                return new Action(c =&gt;
                {
                    if (SpellId &gt; 0)
                    {

                        MobList[0].Target();
                        MobList[0].Face();
                        Thread.Sleep(300);
                        SpellManager.Cast(SpellId);

                        if (Me.QuestLog.GetQuestById((uint)QuestId) == null || QuestId == 0)
                        {
                            Counter++;
                        }
                        Thread.Sleep(300);
                        return RunStatus.Success;
                    }
                    else
                    {
                        _isBehaviorDone = true;
                        return RunStatus.Success;
                    }
                });
            }
        }


        private Composite CreateRootBehavior()
        {
            return new PrioritySelector(
                new Decorator(
                    ret =&gt; !IsDone &amp;&amp; StyxWoW.Me.IsAlive,
                    new PrioritySelector(
                        new Decorator(ret =&gt; Counter &gt; NumOfTimes &amp;&amp; QuestId == 0,
                                        new Sequence(
                                            new Action(ret =&gt; TreeRoot.StatusText = &quot;Finished!&quot;),
                                            new WaitContinue(120,
                                                new Action(delegate
                                                {
                                                    _isBehaviorDone = true;
                                                    return RunStatus.Success;
                                                }))
                                            )),
                        new DecoratorContinue(ret =&gt; CastSelf,
                                    new Sequence(
                                        new Action(ret =&gt; TreeRoot.StatusText = &quot;Casting Spell - &quot; + SpellId + &quot; On Mob: &quot; + MobList[0].Name + &quot; Yards Away &quot; + MobList[0].Location.Distance(Me.Location)),
                                        new Action(ret =&gt; WoWMovement.MoveStop()),
                                        new Action(ret =&gt; Thread.Sleep(300)),
                                        CreateSpellBehavior
                                        )
                                ),

                        new Decorator(ret =&gt; MobList.Count &gt; 0 &amp;&amp; !Me.IsCasting &amp;&amp; SpellManager.CanCast(SpellId),
                            new Sequence(
                                   new DecoratorContinue(ret =&gt; MobList[0].Location.Distance(Me.Location) &gt;= maxSpellRange || !MobList[0].InLineOfSpellSight,
                                    new Sequence(
                                        new Action(ret =&gt; TreeRoot.StatusText = &quot;Moving To Mob - &quot; + MobList[0].Name + &quot; Yards Away: &quot; + MobList[0].Location.Distance(Me.Location)),
                                        new Action(ret =&gt; Navigator.MoveTo(MobList[0].Location))
                                        )
                                ),
                                new DecoratorContinue(ret =&gt; MobList.Count &gt; 0 &amp;&amp; !Me.IsCasting &amp;&amp; SpellManager.CanCast(SpellId, MobList[0]),
                                    new Sequence(
                                           new DecoratorContinue(ret =&gt; MobList[0].Location.Distance(Me.Location) &gt;= maxSpellRange || !MobList[0].InLineOfSpellSight,
                                            new Sequence(
                                                new Action(ret =&gt; TreeRoot.StatusText = &quot;Moving To Mob - &quot; + MobList[0].Name + &quot; Yards Away: &quot; + MobList[0].Location.Distance(Me.Location)),
                                                new Action(ret =&gt; Navigator.MoveTo(MobList[0].Location))
                                                )
                                        ))),
                                new DecoratorContinue(ret =&gt; MobList[0].Location.Distance(Me.Location) &lt; CurrentBehaviorSpell.MinRange,
                                    new Sequence(
                                        new Action(ret =&gt; TreeRoot.StatusText = &quot;Too Close, Backing Up&quot;),
                                        new Action(ret =&gt; MobList[0].Face()),
                                        new Action(ret =&gt; Thread.Sleep(100)),
                                        new Action(ret =&gt; WoWMovement.Move(WoWMovement.MovementDirection.Backwards)),
                                        new Action(ret =&gt; Thread.Sleep(2000)),
                                        new Action(ret =&gt; WoWMovement.MoveStop(WoWMovement.MovementDirection.Backwards))
                                        )),
                                new DecoratorContinue(ret =&gt; MobList[0].Location.Distance(Me.Location) &gt;= CurrentBehaviorSpell.MinRange &amp;&amp; MobList[0].Location.Distance(Me.Location) &lt;= maxSpellRange &amp;&amp; MobList[0].InLineOfSpellSight,
                                    new Sequence(
                                        new Action(ret =&gt; TreeRoot.StatusText = &quot;Casting Spell - &quot; + SpellId + &quot; On Mob: &quot; + MobList[0].Name + &quot; Yards Away &quot; + MobList[0].Location.Distance(Me.Location)),
                                        new Action(ret =&gt; WoWMovement.MoveStop()),
                                        new Action(ret =&gt; Thread.Sleep(300)),
                                        CreateSpellBehavior
                                        )
                                )
                                )),
                        //Fix for Charge and other Spells which needs a target
                        new Decorator(ret =&gt; MobList[0].Location.Distance(Me.Location) &gt;= 40,
                            new Sequence(
                                new Action(ret =&gt; TreeRoot.StatusText = &quot;Targetting On Mob: &quot; + MobList[0].Name + &quot; Yards Away &quot; + MobList[0].Location.Distance(Me.Location)),
                                new Action(ret =&gt; MobList[0].Target()),
                                new Action(ret =&gt; Thread.Sleep(300)),
                                CreateSpellBehavior
                                )
                        )
                )));
        }


        #region Overrides of CustomForcedBehavior

        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(

                        new Decorator(ret =&gt; MobList.Count == 0,
                            new Sequence(
                                    new Action(ret =&gt; TreeRoot.StatusText = &quot;Moving To Location - X: &quot; + Location.X + &quot; Y: &quot; + Location.Y),
                                    new Action(ret =&gt; Navigator.MoveTo(Location)),
                                    new Action(ret =&gt; Thread.Sleep(300))
                                )
                            )

                        )
                    );
        }


        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
                // Semantic coherency...
                // We had to defer this check (from the constructor) until after OnStart() was called.
                // If this behavior was used as a consequence of a class-specific action, or a quest
                // that has already been completed, then having this check in the constructor yields
                // confusing (and wrong) error messages.  Thus, we needed to defer the check until
                // we actually tried to _use_ the behavior--not just create it.
                if (!SpellManager.HasSpell(SpellId))
                {
                    WoWSpell spell = WoWSpell.FromId(SpellId);

                    LogMessage(&quot;fatal&quot;, &quot;Toon doesn't know SpellId({0}, \&quot;{1}\&quot;)&quot;,
                                        SpellId,
                                        ((spell != null) ? spell.Name : &quot;unknown&quot;));
                    _isBehaviorDone = true;
                    return;
                }


                if (TreeRoot.Current != null &amp;&amp; TreeRoot.Current.Root != null &amp;&amp; TreeRoot.Current.Root.LastStatus != RunStatus.Running)
                {
                    var currentRoot = TreeRoot.Current.Root;
                    if (currentRoot is GroupComposite)
                    {
                        var root = (GroupComposite)currentRoot;
                        root.InsertChild(0, CreateRootBehavior());
                    }
                }


                PlayerQuest quest = StyxWoW.Me.QuestLog.GetQuestById((uint)QuestId);

                TreeRoot.GoalText = this.GetType().Name + &quot;: &quot; + ((quest != null) ? (&quot;\&quot;&quot; + quest.Name + &quot;\&quot;&quot;) : &quot;In Progress&quot;);
            }
        }

        #endregion
    }
}