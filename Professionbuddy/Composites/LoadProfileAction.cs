using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Net;
using Styx.Common.Helpers;
using Styx.CommonBot.Profiles;
using Styx.TreeSharp;

namespace HighVoltz.Composites
{

	#region LoadProfileAction

	public sealed class LoadProfileAction : PBAction
	{
		#region LoadProfileType enum

		public enum LoadProfileType
		{
			Honorbuddy,
			Professionbuddy
		}

		#endregion

		private readonly WaitTimer _loadProfileTimer = new WaitTimer(TimeSpan.FromSeconds(5));
		private bool _loadedProfile;

		public LoadProfileAction()
		{
			Properties["Path"] = new MetaProp(
				"Path",
				typeof (string),
				new EditorAttribute(
					typeof (PropertyBag.FileLocationEditor),
					typeof (UITypeEditor)),
				new DisplayNameAttribute(Pb.Strings["Action_Common_Path"]));

			Properties["ProfileType"] = new MetaProp(
				"ProfileType",
				typeof (LoadProfileType),
				new DisplayNameAttribute(Pb.Strings["Action_LoadProfileAction_ProfileType"]));

			Properties["IsLocal"] = new MetaProp(
				"IsLocal",
				typeof (bool),
				new DisplayNameAttribute(Pb.Strings["Action_LoadProfileAction_IsLocal"]));

			Path = "";
			ProfileType = LoadProfileType.Honorbuddy;
			IsLocal = true;
		}

		[PbXmlAttribute]
		public LoadProfileType ProfileType
		{
			get { return (LoadProfileType) Properties["ProfileType"].Value; }
			set { Properties["ProfileType"].Value = value; }
		}

		[PbXmlAttribute]
		public string Path
		{
			get { return (string) Properties["Path"].Value; }
			set { Properties["Path"].Value = value; }
		}

		[PbXmlAttribute]
		public bool IsLocal
		{
			get { return (bool) Properties["IsLocal"].Value; }
			set { Properties["IsLocal"].Value = value; }
		}

		public string AbsolutePath
		{
			get
			{
				if (!IsLocal)
					return Path;
				
				return string.IsNullOrEmpty(Pb.CurrentProfile.XmlPath)
					? string.Empty 
					: System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Pb.CurrentProfile.XmlPath), Path);
			}
		}

		public override string Name
		{
			get { return Pb.Strings["Action_LoadProfileAction_Name"]; }
		}

		public override string Title
		{
			get { return string.Format("{0}: {1}", Name, Path); }
		}

		public override string Help
		{
			get { return Pb.Strings["Action_LoadProfileAction_Help"]; }
		}

		protected override RunStatus Run(object context)
		{
			if (!IsDone)
			{
				if (!_loadedProfile)
				{
					if (Load())
					{
						_loadProfileTimer.Reset();
					}
					_loadedProfile = true;
				} // We need to wait for a profile to load because the profile might be loaded asynchronously
				if (_loadProfileTimer.IsFinished || (!string.IsNullOrEmpty(ProfileManager.XmlLocation) && ProfileManager.XmlLocation.Equals(AbsolutePath)))
				{
					IsDone = true;
				}
			}
			return RunStatus.Failure;
		}

		public bool Load()
		{
			var absPath = AbsolutePath;

			if (IsLocal && !string.IsNullOrEmpty( ProfileManager.XmlLocation) && ProfileManager.XmlLocation.Equals(absPath, StringComparison.CurrentCultureIgnoreCase))
				return false;
			try
			{
				Professionbuddy.Debug(
					"Loading Profile :{0}, previous profile was {1}",
					Path,
					ProfileManager.XmlLocation ?? "[No Profile]");
				if (string.IsNullOrEmpty(Path))
				{
					ProfileManager.LoadEmpty();
				}
				else if (!IsLocal)
				{
					var req = WebRequest.Create(Path);
					req.Proxy = null;
					using (WebResponse res = req.GetResponse())
					{
						using (var stream = res.GetResponseStream())
						{
							ProfileManager.LoadNew(stream);
						}
					}
				}
				else if (File.Exists(absPath))
				{
					ProfileManager.LoadNew(absPath, true);
				}
				else
				{
					Professionbuddy.Err("{0}: {1}", Pb.Strings["Error_UnableToFindProfile"], Path);
					return false;
				}
			}
			catch (Exception ex)
			{
				Professionbuddy.Err("{0}", ex);
				return false;
			}
			return true;
		}

		public override object Clone()
		{
			return new LoadProfileAction {Path = Path, ProfileType = ProfileType, IsLocal = IsLocal};
		}

		public override void Reset()
		{
			_loadedProfile = false;
			base.Reset();
		}
	}

	#endregion
}