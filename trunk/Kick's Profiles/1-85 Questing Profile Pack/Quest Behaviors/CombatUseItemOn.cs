// Behavior originally contributed by Raphus Recreated by Natfoth;
//
// WIKI DOCUMENTATION:
//     http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Custom_Behavior:_CombatUseItemOn
//
// QUICK DOX:
//      Uses an item on a target while the toon is in combat.   The caller can determine at what point
//      in combat the item will be used:
//          * When the target's health drops below a certain percentage
//          * When the target is casting a particular spell
//          * When the target gains a particular aura
//          * When the toon gains a particular aura
//          * one or more of the above happens
//
//  Parameters (required, then optional--both listed alphabetically):
//      ItemId: Id of the item to use on the targets.
//      MobId1, MobId2, ...MobIdN [Required: 1]: Id of the targets on which to use the item.
//
//      CastingSpellId [Default:none]: waits for the target to be casting this spell before using the item.
//      HasAuraId [Default:none]: waits for the toon to acquire this aura before using the item.
//      MobHasAuraId [Default:none]: waits for the target to acquire this aura before using the item.
//      MobHpPercentLeft [Default:0 percent]: waits for the target's hitpoints to fall below this percentage
//              before using the item.
//
//      NumOfTimes [Default:1]: number of times to use the item on a viable target
//      QuestId [Default:none]:
//      QuestCompleteRequirement [Default:NotComplete]:
//      QuestInLogRequirement [Default:InLog]:
//              A full discussion of how the Quest* attributes operate is described in
//              http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Programming_Cookbook:_QuestId_for_Custom_Behaviors
//      X, Y, Z [Default:Toon's current location]: world-coordinates of the general location where the targets can be found.
//
//  Notes:
//      * One or more of CastingSpellId, HasAuraId, MobHasAuraId, or MobHpPercentLeft must be specified.
//
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using CommonBehaviors.Actions;
using Styx.Helpers;
using Styx.Logic.BehaviorTree;
using Styx.Logic.Combat;
using Styx.Logic.POI;
using Styx.Logic.Pathing;
using Styx.Logic.Questing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using TreeSharp;
using Action = TreeSharp.Action;


namespace Styx.Bot.Quest_Behaviors
{
    public class CombatUseItemOn : CustomForcedBehavior
    {
        public CombatUseItemOn(Dictionary&lt;string, string&gt; args)
            : base(args)
        {

            try
            {
                CastingSpellId = GetAttributeAsNullable&lt;int&gt;(&quot;CastingSpellId&quot;, false, ConstrainAs.SpellId, null) ?? 0;
                MaxRange = GetAttributeAsNullable&lt;double&gt;(&quot;MaxRange&quot;, false, ConstrainAs.Range, null) ?? 25;
                MinRange = GetAttributeAsNullable&lt;double&gt;(&quot;MinRange&quot;, false, ConstrainAs.Range, null) ?? 0;
                HasAuraId = GetAttributeAsNullable&lt;int&gt;(&quot;HasAuraId&quot;, false, ConstrainAs.AuraId, new[] { &quot;HasAura&quot; }) ?? 0;
                ItemId = GetAttributeAsNullable&lt;int&gt;(&quot;ItemId&quot;, true, ConstrainAs.ItemId, null) ?? 0;
                Location = GetAttributeAsNullable&lt;WoWPoint&gt;(&quot;&quot;, false, ConstrainAs.WoWPointNonEmpty, null) ?? Me.Location;
                MobIds = GetNumberedAttributesAsArray&lt;int&gt;(&quot;MobId&quot;, 1, ConstrainAs.MobId, new[] { &quot;NpcId&quot; });
                MobHasAuraId = GetAttributeAsNullable&lt;int&gt;(&quot;MobHasAuraId&quot;, false, ConstrainAs.AuraId, new[] { &quot;NpcHasAuraId&quot;, &quot;NpcHasAura&quot; }) ?? 0;
                MobHpPercentLeft = GetAttributeAsNullable&lt;double&gt;(&quot;MobHpPercentLeft&quot;, false, ConstrainAs.Percent, new[] { &quot;NpcHpLeft&quot;, &quot;NpcHPLeft&quot; }) ?? 0;
                NumOfTimes = GetAttributeAsNullable&lt;int&gt;(&quot;NumOfTimes&quot;, false, ConstrainAs.RepeatCount, null) ?? 1;
                QuestId = GetAttributeAsNullable&lt;int&gt;(&quot;QuestId&quot;, false, ConstrainAs.QuestId(this), null) ?? 0;
                UseOnce = GetAttributeAsNullable&lt;bool&gt;(&quot;UseOnce&quot;, false, null, null) ?? true;
                WaitTime = GetAttributeAsNullable&lt;int&gt;(&quot;WaitTime&quot;, false, ConstrainAs.Milliseconds, null) ?? 500;
                QuestRequirementComplete = GetAttributeAsNullable&lt;QuestCompleteRequirement&gt;(&quot;QuestCompleteRequirement&quot;, false, null, null) ?? QuestCompleteRequirement.NotComplete;
                QuestRequirementInLog = GetAttributeAsNullable&lt;QuestInLogRequirement&gt;(&quot;QuestInLogRequirement&quot;, false, null, null) ?? QuestInLogRequirement.InLog;

                // semantic coherency checks --
                if ((CastingSpellId == 0) &amp;&amp; (HasAuraId == 0) &amp;&amp; (MobHasAuraId == 0) &amp;&amp; (MobHpPercentLeft == 0))
                {
                    LogMessage(&quot;error&quot;, &quot;One or more of the following attributes must be specified:\n&quot;
                                         + &quot;CastingSpellId, HasAuraId, MobHasAuraId, MobHpPercentLeft&quot;);
                    IsAttributeProblem = true;
                }

                WaitTimer = new Helpers.WaitTimer(TimeSpan.FromMilliseconds(WaitTime));
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
        public int CastingSpellId { get; private set; }
        public double MaxRange { get; private set; }
        public double MinRange { get; private set; }
        public int HasAuraId { get; private set; }
        public int ItemId { get; private set; }
        public WoWPoint Location { get; private set; }
        public int MobHasAuraId { get; private set; }
        public double MobHpPercentLeft { get; private set; }
        public int[] MobIds { get; private set; }
        public int NumOfTimes { get; private set; }
        public int QuestId { get; private set; }
        public bool UseOnce { get; private set; }
        public int WaitTime { get; private set; }
        public Helpers.WaitTimer WaitTimer { get; private set; }
        public QuestCompleteRequirement QuestRequirementComplete { get; private set; }
        public QuestInLogRequirement QuestRequirementInLog { get; private set; }

        // Private variables for internal state
        private bool _isBehaviorDone;
        private bool _isDisposed;
        private Composite _root;

        private readonly List&lt;ulong&gt; _npcBlacklist = new List&lt;ulong&gt;();

        // Private properties
        private int Counter { get; set; }
        public WoWItem Item { get { return Me.CarriedItems.FirstOrDefault(i =&gt; i.Entry == ItemId &amp;&amp; i.Cooldown == 0); } }
        private LocalPlayer Me { get { return (ObjectManager.Me); } }
        public WoWUnit Mob { get { return (ObjectManager.GetObjectsOfType&lt;WoWUnit&gt;()
                                     .Where(u =&gt; MobIds.Contains((int)u.Entry) &amp;&amp; !u.Dead)
                                     .OrderBy(u =&gt; u.Distance).FirstOrDefault()); } }

        public WoWUnit Lootable = ObjectManager.GetObjectsOfType&lt;WoWUnit&gt;().OrderBy(u =&gt; u.Distance).
                                FirstOrDefault(u =&gt; u.Lootable);

        // DON'T EDIT THESE--they are auto-populated by Subversion
        public override string SubversionId { get { return (&quot;$Id$&quot;); } }
        public override string SubversionRevision { get { return (&quot;$Revision$&quot;); } }


        ~CombatUseItemOn()
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

        private Composite RootCompositeOverride()
        {
            return new PrioritySelector(

                new Decorator(nat =&gt; !_isBehaviorDone &amp;&amp; Me.IsAlive &amp;&amp; (Counter &gt;= NumOfTimes || (Me.QuestLog.GetQuestById((uint)QuestId) != null &amp;&amp; Me.QuestLog.GetQuestById((uint)QuestId).IsCompleted)),
                    new Sequence(
                        new Action(nat =&gt; TreeRoot.StatusText = &quot;Finished CombatUseItemOn!&quot;),
                        new Action(nat =&gt; _isBehaviorDone = true),
                        new Action(nat =&gt; RunStatus.Success))),

               new Decorator(nat =&gt; Mob == null,
                    new Sequence(
                        new Action(nat =&gt; TreeRoot.StatusText = &quot;Moving To Location&quot;),
                        new Action(nat =&gt; Navigator.MoveTo(Location)))),

                new Decorator(nat =&gt; !WaitTimer.IsFinished || (Me.IsActuallyInCombat &amp;&amp; !MobIds.Contains((int)Me.CurrentTarget.Entry)),
                    new ActionAlwaysSucceed()),

                new Decorator(nat =&gt; WaitTimer.IsFinished &amp;&amp; Mob != null &amp;&amp; Mob.DistanceSqr &lt;= MinRange * MinRange &amp;&amp; !Navigator.CanNavigateFully(Me.Location, WoWMathHelper.CalculatePointFrom(Me.Location, Mob.Location, (float)(MinRange - Mob.Distance) - 3f)),
                    new Sequence(
                        new Action(nat =&gt; TreeRoot.StatusText = &quot;Too Close. Backing up&quot;),
                        new Action(nat =&gt; WoWMovement.Move(WoWMovement.MovementDirection.Backwards)))),

                new Decorator(nat =&gt; WaitTimer.IsFinished &amp;&amp; Mob != null &amp;&amp; Mob.DistanceSqr &lt;= MinRange * MinRange &amp;&amp; Navigator.CanNavigateFully(Me.Location, WoWMathHelper.CalculatePointFrom(Me.Location, Mob.Location, (float)(MinRange - Mob.Distance) - 3f)),
                    new Sequence(
                        new Action(nat =&gt; TreeRoot.StatusText = &quot;Too Close. Backing up&quot;),
                        new Action(nat =&gt; Navigator.MoveTo(WoWMathHelper.CalculatePointFrom(Me.Location, Mob.Location, (float)(MinRange - Mob.Distance) - 3f))))),


               new Decorator(nat =&gt; WaitTimer.IsFinished &amp;&amp; Mob != null &amp;&amp; Mob.DistanceSqr &gt;= MaxRange * MaxRange,
                    new Sequence(
                        new Action(nat =&gt; TreeRoot.StatusText = &quot;Moving to Target&quot;),
                        new Action(nat =&gt; Navigator.MoveTo(Mob.Location)))),


              new Decorator(nat =&gt; Me.IsMoving &amp;&amp; WaitTimer.IsFinished &amp;&amp; Me.CurrentTarget != null &amp;&amp; MobIds.Contains((int)Me.CurrentTarget.Entry) &amp;&amp; Item != null &amp;&amp; Me.CurrentTarget.DistanceSqr &lt; MaxRange * MaxRange &amp;&amp; Me.CurrentTarget.DistanceSqr &gt; MinRange * MinRange &amp;&amp; (!UseOnce || !_npcBlacklist.Contains(Me.CurrentTarget.Guid)) &amp;&amp;
                                               ((CastingSpellId != 0 &amp;&amp; Me.CurrentTarget.CastingSpellId == CastingSpellId) ||
                                               (MobHasAuraId != 0 &amp;&amp; Me.CurrentTarget.Auras.Values.Any(a =&gt; a.SpellId == MobHasAuraId)) ||
                                               (MobHpPercentLeft != 0 &amp;&amp; Me.CurrentTarget.HealthPercent &lt;= MobHpPercentLeft) ||
                                               (HasAuraId != 0 &amp;&amp; Me.HasAura(WoWSpell.FromId(HasAuraId).Name))),
                  new Sequence(
                        new Action(nat =&gt; WoWMovement.MoveStop()),
                        new Action(nat =&gt; SpellManager.StopCasting()),
                        new Action(nat =&gt; Mob.Target()))),


              new Decorator(nat =&gt; WaitTimer.IsFinished &amp;&amp; Me.CurrentTarget != null &amp;&amp; MobIds.Contains((int)Me.CurrentTarget.Entry) &amp;&amp; Item != null &amp;&amp; Me.CurrentTarget.DistanceSqr &lt; MaxRange * MaxRange &amp;&amp; Me.CurrentTarget.DistanceSqr &gt; MinRange * MinRange &amp;&amp; (!UseOnce || !_npcBlacklist.Contains(Me.CurrentTarget.Guid)) &amp;&amp;
                                               ((CastingSpellId != 0 &amp;&amp; Me.CurrentTarget.CastingSpellId == CastingSpellId) ||
                                               (MobHasAuraId != 0 &amp;&amp; Me.CurrentTarget.Auras.Values.Any(a =&gt; a.SpellId == MobHasAuraId)) ||
                                               (MobHpPercentLeft != 0 &amp;&amp; Me.CurrentTarget.HealthPercent &lt;= MobHpPercentLeft) ||
                                               (HasAuraId != 0 &amp;&amp; Me.HasAura(WoWSpell.FromId(HasAuraId).Name))),
                            new Sequence(
                                new Action(nat =&gt; SpellManager.StopCasting()),
                                new Action(nat =&gt; Logging.Write(Color.FromName(&quot;Aqua&quot;), &quot;[CUIO] Using Item : &quot; + Item.Name)),
                                new Action(nat =&gt; TreeRoot.StatusText = &quot;Using Item : &quot; + Item.Name),
                                new Action(nat =&gt; _npcBlacklist.Add(Me.CurrentTarget.Guid)),
                                new Action(nat =&gt; Item.UseContainerItem()),
                                new Action(nat =&gt; WaitTimer.Reset()),
                                new DecoratorContinue(nat =&gt; QuestId == 0,
                                         new Action(nat =&gt; Counter++))))


                );
        }

        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(
                    new Decorator(
                        ret =&gt; !Me.Combat,
                            new PrioritySelector(

                                new Decorator(nat =&gt; Lootable != null &amp;&amp; BotPoi.Current.Type == PoiType.Loot,
                                    new ActionAlwaysSucceed()),

                                new Decorator(
                                    ret =&gt; Mob == null,
                                    new Sequence(
                                        new Action(ret =&gt; TreeRoot.StatusText = &quot;Moving to location&quot;),
                                        new Action(ret =&gt; Navigator.MoveTo(Location)))),
                                new Decorator(
                                    ret =&gt; Mob != null &amp;&amp; Mob.Distance &gt; MaxRange,
                                    new Action(ret =&gt; Navigator.MoveTo(Mob.Location))),
                                new Decorator(
                                    ret =&gt; Me.CurrentTarget == null &amp;&amp; Mob.Distance &lt;= MaxRange,
                                    new Action(ret =&gt; Mob.Target())),
                                new Decorator(
                                    ret =&gt; RoutineManager.Current.PullBehavior != null,
                                    RoutineManager.Current.PullBehavior),
                                new Action(ret =&gt; RoutineManager.Current.Pull()))),
                    RootCompositeOverride()
                ));
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
                if (TreeRoot.Current != null &amp;&amp; TreeRoot.Current.Root != null &amp;&amp; TreeRoot.Current.Root.LastStatus != RunStatus.Running)
                {
                    var currentRoot = TreeRoot.Current.Root;
                    if (currentRoot is GroupComposite)
                    {
                        var root = (GroupComposite)currentRoot;
                        root.InsertChild(0, RootCompositeOverride());
                    }
                }


                PlayerQuest quest = StyxWoW.Me.QuestLog.GetQuestById((uint)QuestId);

                TreeRoot.GoalText = GetType().Name + &quot;: &quot; + ((quest != null) ? quest.Name : &quot;In Progress&quot;);
            }
        }
    }
}