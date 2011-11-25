using System;
using System.Linq;
using Styx;
using Styx.Helpers;
using Styx.Logic.Combat;
using Styx.Plugins.PluginClass;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;


namespace Smarter
{
    public class Smarter : HBPlugin
    {
        private bool _firstPulse = true;
        private readonly WaitTimer _satchelCheckTimer = new WaitTimer(new TimeSpan(0, 0, 5, 0));
        private readonly WaitTimer _moteCheckTimer = WaitTimer.TenSeconds;
        private readonly WaitTimer _swimCheckTimer = WaitTimer.OneSecond;
        private readonly WaitTimer _eternalCheck = WaitTimer.TenSeconds;
        private readonly WaitTimer _bloatedStomachsCheckTimer = WaitTimer.TenSeconds;
        private readonly LocalPlayer _me = ObjectManager.Me;
        private bool _hasAquaticForm;


        public override void Pulse()
        {
            if (_firstPulse)
            {
                if (SpellManager.HasSpell("Aquatic Form"))
                {
                    _hasAquaticForm = true;
                }
                _firstPulse = false;
            }


            if (_hasAquaticForm && _swimCheckTimer.IsFinished)
            {
                SwimCheck();
                _swimCheckTimer.Reset();
            }
            if (_eternalCheck.IsFinished)
            {
                EternalCheck();
                _eternalCheck.Reset();
            }
            if (_satchelCheckTimer.IsFinished)
            {
                SatchelCheck();
                _satchelCheckTimer.Reset();
            }
            if (_moteCheckTimer.IsFinished)
            {
                MoteCheck();
                _moteCheckTimer.Reset();
            }
            if (_bloatedStomachsCheckTimer.IsFinished)
            {
                BloatedStomachsCheck();
                _bloatedStomachsCheckTimer.Reset();
            }
        }


        private void EternalCheck()
        {
            if (_me.Shapeshift != ShapeshiftForm.Normal) return;


            if (!_me.BagItems.Exists(o => o.Name.Contains("Crystallized"))) return;


            var items = _me.BagItems.Where(o => o.Name.Contains("Crystallized") && o.StackCount >= 10).ToList();
            for (int i = 0; i < items.Count(); i++)
            {
                items[i].Use(true);
                StyxWoW.SleepForLagDuration();
            }
        }


        private void SwimCheck()
        {
            if (!_me.IsSwimming) return;


            if (_me.Shapeshift == ShapeshiftForm.Aqua) return;


            SpellManager.Cast("Aquatic Form");
            StyxWoW.SleepForLagDuration();
        }


        private void MoteCheck()
        {
            if (_me.Shapeshift != ShapeshiftForm.Normal) return;


            if (!_me.BagItems.Exists(o => o.Name.Contains("Mote"))) return;

            var items = _me.BagItems.Where(o => o.Name.Contains("Mote of") && o.StackCount >= 10).ToList();
            for (int i = 0; i < items.Count(); i++)
            {
                items[i].Use(true);
                StyxWoW.SleepForLagDuration();
            }
        }
		
        private void BloatedStomachsCheck()
        {
            if (_me.Shapeshift != ShapeshiftForm.Normal) return;

            if (!_me.BagItems.Exists(o => o.Name.Contains("Bloated"))) return;

            var items = _me.BagItems.Where(o => o.Name.Contains("Strange Bloated Stomach")).ToList();
            for (int i = 0; i < items.Count(); i++)
            {
                items[i].Use(true);
                StyxWoW.SleepForLagDuration();
            }
        }
		
        private void SatchelCheck()
        {
            if (!_me.BagItems.Exists(o => o.Name == "Satchel of Helpful Goods")) return;

            var items = _me.BagItems.Where(o => o.Name == "Satchel of Helpful Goods").ToList();
            for (var i = 0; i < items.Count(); i++)
            {
                items[i].Use(true);
                StyxWoW.SleepForLagDuration();
            }
        }


        public override string Name
        {
            get { return "GetItRightGetItTight"; }
        }


        public override string Author
        {
            get { return "Smarter"; }
        }


        public override Version Version
        {
            get { return new Version(0, 0, 1, 0); }
        }
    }
}