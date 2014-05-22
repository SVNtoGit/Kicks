using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;

namespace HighVoltz.Composites
{
    [XmlRoot("Professionbuddy")]
    public class PbDecorator : PrioritySelector
    {
        public static bool EndOfWhileLoopReturn;

        WaitTimer _antiAfkTimer = new WaitTimer(TimeSpan.FromMinutes(2));

        public PbDecorator(params Composite[] children) : base(children)
        {
        }

        private bool CanRun
        {
            get { return StyxWoW.IsInWorld && !ExitBehavior() && Professionbuddy.Instance.IsRunning; }
        }

        private static LocalPlayer Me
        {
            get { return StyxWoW.Me; }
        }

        protected override IEnumerable<RunStatus> Execute(object context)
        {
            if (CanRun)
            {
                // keep the bot from going afk.
                if (_antiAfkTimer.IsFinished)
                {
                   KeyboardManager.AntiAfk();
                    _antiAfkTimer.Reset();
                }
                bool shouldBreak = false;
                EndOfWhileLoopReturn = false;
                foreach (Composite child in Children.SkipWhile(c => Selection != null && c != Selection))
                {
                    child.Start(context);
                    Selection = child;
                    while (child.Tick(context) == RunStatus.Running)
                    {
                        if (!CanRun)
                        {
                            shouldBreak = true;
                            break;
                        }
                        yield return RunStatus.Running;
                    }
                    if (shouldBreak)
                        break;
                    if (EndOfWhileLoopReturn)
                        yield return RunStatus.Failure;
                    if (child.LastStatus == RunStatus.Success)
                        yield return RunStatus.Success;
                }
                Selection = null;
            }
            yield return RunStatus.Failure;
        }

        public void Reset()
        {
            EndOfWhileLoopReturn = false;
            Selection = null;
            foreach (IPBComposite comp in Children)
            {
                comp.Reset();
            }
        }

        public static bool ExitBehavior()
        {
            return ((Me.IsActuallyInCombat && !Me.Mounted) ||
                    (Me.IsActuallyInCombat && !Me.IsFlying &&
                     Mount.ShouldDismount(Util.GetMoveToDestination()))) ||
                   !Me.IsAlive || Me.HealthPercent <= 40 ;
        }
    }
}