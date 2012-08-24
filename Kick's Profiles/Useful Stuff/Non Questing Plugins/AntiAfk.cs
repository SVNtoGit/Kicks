/*
 * LetMeFly performs the following:
 * - Jumps to fly when mounted and running on the ground
 * - Adjusts hotspots of current profile a few yards off ground
 * - Returns to ground when stuck or when POI is nearby
 * - Uses Flight Form and Swift Flight Form when available
 * 
 * Author: lofi
 */

using Styx.Helpers;
using Styx.Logic;
using Styx.Logic.BehaviorTree;
using Styx.Logic.Combat;
using Styx.Logic.POI;
using Styx.Logic.Pathing;
using Styx.Logic.Profiles;
using Styx.Plugins.PluginClass;
using Styx.WoWInternals.WoWObjects;
using Styx.WoWInternals;
using Styx;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using System;

namespace AntiAfk
{
#region form

    public class SettingsForm : Form
    {
        private FlowLayoutPanel panel = new FlowLayoutPanel();
        private Label LabelAntiAfk = new Label();
          
        private TextBox TextAntiAfk = new TextBox();
          
        public SettingsForm()
        {
            LabelAntiAfk.Text = "Seconds:";
             	    

            TextAntiAfk.Text = AntiAfk.settings.AntiAfk.ToString();
            TextAntiAfk.Width = 180;
               
            panel.Dock = DockStyle.Fill;
            panel.Controls.Add(LabelAntiAfk);
            panel.Controls.Add(TextAntiAfk);
            
              
            this.Text = "Timeout";
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Height = 90;
            this.Width = 190;
            this.Controls.Add(panel);
        }

        protected override void Dispose(bool disposing)
        {
            AntiAfk.settings.AntiAfk = int.Parse(TextAntiAfk.Text);
               
            AntiAfk.settings.Save();
            base.Dispose(disposing);
        }
    }

    #endregion

    #region settings

    class  AntiAfkSettings : Settings
    {
        public  AntiAfkSettings() : base(Logging.ApplicationPath + "\\Settings\\AntiAfk_" + StyxWoW.Me.Name + ".xml") 
        {
        Load();
        }

        [Setting, DefaultValue(180)]
        public int AntiAfk { get; set; }

       }

    #endregion




    class AntiAfk : HBPlugin
    {
        // User configurations
        
        public override string Name { get { return "- Custom - AntiAfk"; } }
        public override string Author { get { return "bcrazy"; } }
        public override Version Version { get { return new Version(0, 0, 1); } }
        public override bool WantButton { get { return true; } }
        private LocalPlayer Me { get { return ObjectManager.Me; } }
        private Stopwatch antiafk = new Stopwatch();
        public static  AntiAfkSettings settings = new  AntiAfkSettings();
	

	public override string ButtonText
        {
            get
            {
                return "AntiAfk";
            }
        }

	public override void OnButtonPress()
        {
            new SettingsForm().Show();
        }


        public override void Pulse()
        {
            try
            {
                if (Me == null || !ObjectManager.IsInGame || Battlegrounds.IsInsideBattleground)
                {
                    return; // sanity check and disable except for Grind bot / Questing
                }

		
		if (!Battlegrounds.IsInsideBattleground || Me != null)
		 {   
		    antiafk.Start();
		    
		    if (antiafk.Elapsed.TotalSeconds > settings.AntiAfk)
		    {
                   Log("- Jumping a bit"); 
		   Styx.Helpers.KeyboardManager.PressKey((char)Keys.Space);
		   Thread.Sleep(1000);
		   Styx.Helpers.KeyboardManager.ReleaseKey((char)Keys.Space);
		   antiafk.Reset();
	            }
		}
             
	     }
 	    
            catch (Exception e)
            {
                Log("ERROR: " + e.Message + ". See debug log.");
                Logging.WriteDebug("exception:");
                Logging.WriteException(e);
            }
           
        }

    
        
        public override void Initialize()
        {
             Log("Loaded - Pulse Every: " + settings.AntiAfk + " seconds ");
        }

        
        private void Log(string format, params object[] args)
        {
            Logging.Write(Color.CadetBlue, "[AntiAfk] " + format, args);
        }
    }
}

