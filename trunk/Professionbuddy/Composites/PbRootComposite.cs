using Styx;
using Styx.CommonBot;
using Styx.TreeSharp;

namespace HighVoltz.Composites
{
    public class PbRootComposite : PrioritySelector
    {
        public PbRootComposite(PbDecorator pbBotBase, BotBase secondaryBot)
            : base(pbBotBase, secondaryBot == null ? new PrioritySelector() : secondaryBot.Root)
        {
            SecondaryBot = secondaryBot;
        }

        public PbDecorator PbBotBase
        {
            get { return Children[0] as PbDecorator; }
            set { Children[0] = value; }
        }

        public BotBase SecondaryBot { get; set; }

        // hackish fix but needed.
        public void AddSecondaryBot()
        {
            Children[1] = SecondaryBot.Root;
        }
    }
}