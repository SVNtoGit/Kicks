using System;
using Styx;
using Styx.WoWInternals;
using Styx.Plugins.PluginClass;
using Styx.Helpers;

namespace PluginAntiDrown
{


    public class AntiDrown : HBPlugin
    {
        public override string Name { get { return "Anti-Drown"; } }
        public override string Author { get { return "Sychotix"; } }
        public override Version Version { get { return _version; } }
        private readonly Version _version = new Version(1, 0, 0, 0);
        private bool breathing = false;
        private uint value;


        public override void Pulse()
        {

            value = ObjectManager.Me.GetMirrorTimerInfo(MirrorTimerType.Breath).CurrentTime;

            //Debug value printing
            //Logging.Write("Value "+value);

            //If already going up for air
            if (breathing)
                WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend);
            //If we have less than 60 seconds of air
            //Cases: -If there is no bar displayed (i.e. on top of water, value will be 0)
            //       -If you currently have no breath, value goes to around 4294966095
            else if ((value < 60000) && ObjectManager.Me.IsAlive && ObjectManager.Me.IsSwimming
                           && (value != 0) || (value > 900001))
            {
                WoWMovement.Move(WoWMovement.MovementDirection.JumpAscend);
                Logging.Write("Anti-Drown: Running out of breath! Going up for air!");
                breathing = true;
            }

            //Stop breathing once air is full
            if (value > (ObjectManager.Me.GetMirrorTimerInfo(MirrorTimerType.Breath).MaxValue - 5000))
                breathing = false;


        }
    }
}