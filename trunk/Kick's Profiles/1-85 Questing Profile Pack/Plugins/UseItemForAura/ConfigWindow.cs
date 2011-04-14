using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Xml.Linq;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Serialization;

using Styx;
using Styx.Logic.Combat;
using Styx.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.Plugins.PluginClass;
using Styx.Logic.BehaviorTree;

using Styx.Logic.Pathing;
using Styx.Combat.CombatRoutine;
using Styx.Logic.Inventory.Frames.Quest;
using Styx.Logic.Questing;
using Styx.Plugins;
using Styx.Logic.Inventory.Frames.Gossip;
using Styx.Logic.Common;
using Styx.Logic.Inventory.Frames.Merchant;
using Styx.Logic;
using Styx.Logic.Profiles;
using Styx.Logic.Inventory.Frames.LootFrame;

namespace UseItemForAura
{
    public partial class ConfigWindow : Form
    {
        public ConfigWindow()
        {
            InitializeComponent();
            TBQuestID.Text = UseItemForAura.Settings.QuestID;
            TBItemID.Text = UseItemForAura.Settings.ItemID;
            TBAuraID.Text = UseItemForAura.Settings.AuraID;
            CBCombat.Checked = UseItemForAura.Settings.Combat;
        }

        private void BSave_Click(object sender, EventArgs e)
        {
            string File = "Plugins\\UseItemForAura\\";
            Logging.Write("UseItemForAura: SettingsSaved!");
            if (TBAuraID.Text == "")
                TBAuraID.Text = "0";
            if (TBItemID.Text == "")
                TBItemID.Text = "0";
            if (TBQuestID.Text == "")
                TBQuestID.Text = "0";
				

            if (TBAuraID.Text == "0")
                Logging.Write("UseItemForAura: Please insert a Aura ID or Plugin wont work.");
            if (TBItemID.Text == "0")
                Logging.Write("UseItemForAura: Please insert a Item ID or Plugin wont work.");

            XmlDocument xml;
            XmlElement root;
            XmlElement element;
            XmlText text;
            XmlComment xmlComment;

            string sPath = Process.GetCurrentProcess().MainModule.FileName;
            sPath = Path.GetDirectoryName(sPath);

            UseItemForAura.Settings.QuestID = TBQuestID.Text;
            UseItemForAura.Settings.ItemID = TBItemID.Text;
            UseItemForAura.Settings.AuraID = TBAuraID.Text;
            UseItemForAura.Settings.Combat = CBCombat.Checked;

            sPath = Path.Combine(sPath, File);

            if (!Directory.Exists(sPath))
            {
                Logging.WriteDebug("UseItemForAura: Creating config directory");
                Directory.CreateDirectory(sPath);
            }

            sPath = Path.Combine(sPath, "UseItemForAura.config");

            Logging.WriteDebug("UseItemForAura: Saving config file: {0}", sPath);
            xml = new XmlDocument();
            XmlDeclaration dc = xml.CreateXmlDeclaration("1.0", "utf-8", null);
            xml.AppendChild(dc);

            xmlComment = xml.CreateComment(
                "=======================================================================\n" +
                ".CONFIG  -  This is the Config File For Use Item for Aura - Questhelper\n\n" +
                "XML file containing settings to customize in the Use Item for Aura - Questhelper Plugin\n" +
                "It is STRONGLY recommended you use the Configuration UI to change this\n" +
                "file instead of direct changein it here.\n" +
                "========================================================================");

            //let's add the root element
            root = xml.CreateElement("UseItemForAura");
            root.AppendChild(xmlComment);

            //let's add another element (child of the root)
            element = xml.CreateElement("QuestID");
            text = xml.CreateTextNode(TBQuestID.Text.ToString());
            element.AppendChild(text);
            root.AppendChild(element);

            //let's add another element (child of the root)
            element = xml.CreateElement("ItemID");
            text = xml.CreateTextNode(TBItemID.Text.ToString());
            element.AppendChild(text);
            root.AppendChild(element);

            //let's add another element (child of the root)
            element = xml.CreateElement("AuraID");
            text = xml.CreateTextNode(TBAuraID.Text.ToString());
            element.AppendChild(text);
            root.AppendChild(element);

            //let's add another element (child of the root)
            element = xml.CreateElement("Combat");
            text = xml.CreateTextNode(CBCombat.Checked.ToString());
            element.AppendChild(text);
            root.AppendChild(element);

            xml.AppendChild(root);

            System.IO.FileStream fs = new System.IO.FileStream(@sPath, System.IO.FileMode.Create,
                                                               System.IO.FileAccess.Write);
            try
            {
                xml.Save(fs);
                fs.Close();
            }
            catch (Exception np)
            {
                Logging.Write(np.Message);
            }
        }
       
    }


}
