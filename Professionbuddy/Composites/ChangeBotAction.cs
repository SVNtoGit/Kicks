using System;
using System.ComponentModel;
using System.Windows.Media;
using Bots.BGBuddy.Helpers;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.TreeSharp;

namespace HighVoltz.Composites
{
    public sealed class ChangeBotAction : PBAction
    {
		WaitTimer _changeBotTimer ;
	    private BotBase _bot;

        public ChangeBotAction()
        {
            Properties["BotName"] = new MetaProp("BotName", typeof (string),
                                                 new DisplayNameAttribute(Pb.Strings["Action_ChangeBotAction_BotName"]));
            BotName = "";
        }

        [PbXmlAttribute]
        public string BotName
        {
            get { return (string) Properties["BotName"].Value; }
            set { Properties["BotName"].Value = value; }
        }

        public override string Name
        {
            get { return Pb.Strings["Action_ChangeBotAction_Name"]; }
        }

        public override string Title
        {
            get { return string.Format("{0}: {1}", Name, BotName); }
        }

        public override string Help
        {
            get { return Pb.Strings["Action_ChangeBotAction_Help"]; }
        }

        protected override RunStatus Run(object context)
        {
	        if (IsDone) 
				return RunStatus.Failure;
	        try
	        {
		        if (_changeBotTimer == null)
		        {
			        _changeBotTimer = new WaitTimer(TimeSpan.FromSeconds(10));
			        _changeBotTimer.Reset();
			        _bot = Util.GetBotByName(BotName);
			        if (_bot != null)
			        {
				        if (Professionbuddy.Instance.SecondaryBot == _bot)
				        {
					        IsDone = true;
					        return RunStatus.Failure;
				        }
				        Professionbuddy.ChangeSecondaryBot(BotName);
			        }
		        }
	        }
	        finally
	        {
		        // Wait until bot change completes or fails
		        if (_bot == null || _changeBotTimer != null && (_changeBotTimer.IsFinished || Professionbuddy.Instance.SecondaryBot == _bot))
		        {
			        IsDone = true;
			        _changeBotTimer = null;
		        }
	        }

	        if (IsDone)
	        {
		        if (_bot == null)
			        Professionbuddy.Err("No bot with name: {0} could be found", BotName);
		        else if (Professionbuddy.Instance.SecondaryBot == _bot)
			        Professionbuddy.Log("Successfuly changed secondary bot to: {0}", BotName);
		        else
			        Professionbuddy.Err("Unable to switch secondary bot to: {0}", BotName);
	        }
	        return RunStatus.Success;
        }

        public override object Clone()
        {
            return new ChangeBotAction {BotName = BotName};
        }
    }
}