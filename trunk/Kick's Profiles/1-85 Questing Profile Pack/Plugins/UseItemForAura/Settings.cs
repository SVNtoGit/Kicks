using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using Styx;
using Styx.Combat.CombatRoutine;
using Styx.Helpers;
using Styx.WoWInternals;
using ObjectManager = Styx.WoWInternals.ObjectManager;


namespace UseItemForAura
{
    class Settings
    {
        string File = "Plugins\\UseItemForAura\\";
        public Settings()
        {
            if (ObjectManager.Me != null)
                try
                {
                    Load();
                }
                catch (Exception e)
                {
                    Logging.Write(e.Message);
                }
        }

        public string QuestID = "0";
        public string AuraID = "0";
        public string ItemID = "0";
        public bool Combat = false;

        public void Load()
        {
            //    XmlTextReader reader;
            XmlDocument xml = new XmlDocument();
            XmlNode xvar;

            string sPath = Process.GetCurrentProcess().MainModule.FileName;
            sPath = Path.GetDirectoryName(sPath);
            sPath = Path.Combine(sPath, File);

            if (!Directory.Exists(sPath))
            {
                Logging.WriteDebug("UseItemForAura: Creating config directory");
                Directory.CreateDirectory(sPath);
            }

            sPath = Path.Combine(sPath, "UseItemForAura.config");

            Logging.WriteDebug("UseItemForAura: Loading config file: {0}", sPath);
            System.IO.FileStream fs = new System.IO.FileStream(@sPath, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite);
            try
            {
                xml.Load(fs);
                fs.Close();
            }
            catch (Exception e)
            {
                Logging.Write(e.Message);
                Logging.Write("UseItemForAura: Continuing with Default Config Values");
                fs.Close();
                return;
            }

            //            xml = new XmlDocument();

            try
            {
                //                xml.Load(reader);
                if (xml == null)
                    return;

                xvar = xml.SelectSingleNode("//UseItemForAura/QuestID");
                if (xvar != null)
                {
                    QuestID = Convert.ToString(xvar.InnerText);
                    Logging.WriteDebug("UseItemForAura: " + xvar.Name + "=" + QuestID.ToString());
                }
                xvar = xml.SelectSingleNode("//UseItemForAura/ItemID");
                if (xvar != null)
                {
                    ItemID = Convert.ToString(xvar.InnerText);
                    Logging.WriteDebug("UseItemForAura: " + xvar.Name + "=" + ItemID.ToString());
                }

                xvar = xml.SelectSingleNode("//UseItemForAura/AuraID");
                if (xvar != null)
                {
                    AuraID = Convert.ToString(xvar.InnerText);
                    Logging.WriteDebug("UseItemForAura: " + xvar.Name + "=" + AuraID.ToString());
                }
                xvar = xml.SelectSingleNode("//UseItemForAura/Combat");
                if (xvar != null)
                {
                    Combat = Convert.ToBoolean(xvar.InnerText);
                    Logging.WriteDebug("UseItemForAura: " + xvar.Name + "=" + Combat.ToString());
                }
            }
            catch (Exception e)
            {
                Logging.WriteDebug("UseItemForAura: PROJECTE EXCEPTION, STACK=" + e.StackTrace);
                Logging.WriteDebug("UseItemForAura: PROJECTE EXCEPTION, SRC=" + e.Source);
                Logging.WriteDebug("UseItemForAura: PROJECTE : " + e.Message);
            }
        }


    }
}
