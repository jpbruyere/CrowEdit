// Copyright (c) 2013-2019  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using CrowEditBase;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using static CrowEditBase.CrowEditBase;
using Crow;


namespace NetcoreDbgPlugin
{
	public class NetcoreDbgService : Service {
		public override string ConfigurationWindowPath => "#CENetcoreDbgPlugin.ui.winConfiguration.crow";
		public NetcoreDbgService () : base () {
			initCommands();
			App.ViewCommands.Add (CMDViewDebug);
		}
		#region commands
		public ActionCommand CMDViewDebug;
		public Command CMDOptions_SelectNetcoredbgPath => new ActionCommand ("...",
			() => {
				FileDialog dlg = App.LoadIMLFragment<FileDialog> (@"
					<FileDialog Caption='Select netcoredbg executable path' CurrentDirectory='{NetcoredbgPath}'
								ShowFiles='true' ShowHidden='true'/>");
				dlg.OkClicked += (sender, e) => NetcoredbgPath = (sender as FileDialog).SelectedFileFullPath;
				dlg.DataSource = this;
			}
		);
		void initCommands ()
		{
			CMDViewDebug = new ActionCommand("Debug Window", () => App.LoadWindow ("#CENetcoreDbgPlugin.ui.winDebugging.crow", dbg));
		}
		#endregion
		public string NetcoredbgPath {
			get => Configuration.Global.Get<string> ("NetcoredbgPath");
			set {
				if (value == NetcoredbgPath)
					return;
				Configuration.Global.Set ("NetcoredbgPath", value);
				NotifyValueChanged (value);
			}
		}
		NetcoredbgDebugger dbg;
		public override void Start() {
			if (CurrentState == Status.Running)
				return;
			dbg = new NetcoredbgDebugger ();
			CurrentState = Status.Running;
		}
		public override void Stop()
		{
			if (CurrentState != Status.Running)
				return;

			dbg.Terminate ();
			dbg = null;
			CurrentState = Status.Stopped;
		}
		public override void Pause()
		{
			if (CurrentState != Status.Running)
				return;

			dbg.Terminate ();
			CurrentState = Status.Paused;
		}

	}
}