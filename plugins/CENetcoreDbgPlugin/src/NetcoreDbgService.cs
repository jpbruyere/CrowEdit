// Copyright (c) 2013-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using CrowEditBase;
using static CrowEditBase.CrowEditBase;
using Crow;


namespace NetcoreDbgPlugin
{
	public class NetcoreDbgService : Service {
		public override string[] ServiceWindowsPath => ["#CENetcoreDbgPlugin.ui.winConfiguration.crow"];
		public NetcoreDbgService () : base () {
			initCommands();
			App.ValueChanged += app_ValueChanged;
		}
		~NetcoreDbgService() {
			App.ValueChanged -= app_ValueChanged;
		}
		#region commands
		public ActionCommand CMDViewDebug,CMDViewBreakPoints,CMDViewWatches,CMDViewThreads,CMDViewStackFrames;
		public CommandGroup ViewCommands;
		//public Command CMDDebugStart, CMDDebugPause, CMDDebugStop, CMDDebugStepIn, CMDDebugStepOver, CMDDebugStepOut;
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
			CMDViewDebug = new ActionCommand("Debug Window", () => App.LoadWindow ("#CENetcoreDbgPlugin.ui.winDebugging.crow", this));
			CMDViewBreakPoints = new ActionCommand("Break Points", () => App.LoadWindow ("#CENetcoreDbgPlugin.ui.winBreakPoints.crow", this));
			CMDViewStackFrames = new ActionCommand("Stack Frames", () => App.LoadWindow ("#CENetcoreDbgPlugin.ui.winStackFrames.crow", this));
			CMDViewWatches = new ActionCommand("Watches", () => App.LoadWindow ("#CENetcoreDbgPlugin.ui.winWatches.crow", this));
			CMDViewThreads = new ActionCommand("Threads", () => App.LoadWindow ("#CENetcoreDbgPlugin.ui.winThreads.crow", this));
			ViewCommands = new CommandGroup("Debugger", "#icons.bug.svg", CMDViewDebug, CMDViewWatches, CMDViewBreakPoints, CMDViewStackFrames, CMDViewThreads);
		}
		#endregion

		Project currentSolution;
		Project CurrentSolution {
			get => currentSolution;
			set {
				if (currentSolution == value)
					return;
				if (currentSolution != null)
					currentSolution.ValueChanged -= currentSolution_ValueChanged;
				currentSolution = value;
				if (currentSolution != null) {
					currentSolution.ValueChanged += currentSolution_ValueChanged;
					if (CurrentState == Status.Init)
						Start();
					else if (CurrentState == Status.Running && currentSolution is CERoslynPlugin.SolutionProject sol) {
						if (sol.StartupProject is CERoslynPlugin.MSBuildProject csprj) {
							DbgSession.Project = csprj;
						}
					}
				}
			}
		}
		void currentSolution_ValueChanged(object instance, ValueChangeEventArgs e) {
			if (CurrentState != Status.Running)
				return;
			if (e.MemberName == "StartupProject") {
				if (e.NewValue is CERoslynPlugin.MSBuildProject csprj)
					DbgSession.Project = csprj;
				else
					DbgSession.Project = null;
			}
		}

		void app_ValueChanged(object instance, ValueChangeEventArgs e) {
			if (e.MemberName == "CurrentProject") {
				if (e.NewValue is CERoslynPlugin.SolutionProject sol)
					CurrentSolution = sol;
				else
					CurrentSolution = null;
			}
		}
		public override void Start() {
			if (CurrentState == Status.Running)
				return;

			DbgSession = new NetcoredbgDebugger ();

			if (CurrentState != Status.Paused)
				App.ViewCommands.Add (ViewCommands);

			if (App.TryGetWindow("#CENetcoreDbgPlugin.ui.winDebugging.crow", out Window win) ) {
				win.DataSource = DbgSession;
			}
			if (App.TryGetWindow("#CENetcoreDbgPlugin.ui.winWatch.crow", out Window win2) ) {
				win2.DataSource = DbgSession;
			}
			if (App.TryGetWindow("#CENetcoreDbgPlugin.ui.winBreakPoints.crow", out Window win3) ) {
				win3.DataSource = DbgSession;
			}
			if (App.TryGetWindow("#CENetcoreDbgPlugin.ui.winStackFrames.crow", out Window win4) ) {
				win4.DataSource = DbgSession;
			}
			if (App.TryGetWindow("#CENetcoreDbgPlugin.ui.winThreads.crow", out Window win5) ) {
				win5.DataSource = DbgSession;
			}


			if (App.CurrentProject is CERoslynPlugin.SolutionProject sol) {
				if (sol.StartupProject is CERoslynPlugin.MSBuildProject csprj) {
					DbgSession.Project = csprj;
				}
			}
						
			CurrentState = Status.Running;
		}
		public override void Stop()
		{
			if (CurrentState != Status.Running)
				return;
			
			App.ViewCommands.Remove (ViewCommands);
			App.CloseWindow("#CENetcoreDbgPlugin.ui.winDebugging.crow");
			DbgSession.Terminate ();
			DbgSession = null;

			CurrentState = Status.Stopped;
		}
		public override void Pause()
		{
			if (CurrentState != Status.Running)
				return;

			DbgSession.Terminate ();
			CurrentState = Status.Paused;
		}

		#region Debugging session
		NetcoredbgDebugger dbg;
		public ObservableList<BreakPoint> BreakPoints = new ObservableList<BreakPoint> ();
		public string NetcoredbgPath {
			get => Configuration.Global.Get<string> ("NetcoredbgPath");
			set {
				if (value == NetcoredbgPath)
					return;
				Configuration.Global.Set ("NetcoredbgPath", value);
				NotifyValueChanged (value);
			}
		}
		public NetcoredbgDebugger DbgSession {
			get => dbg;
			set {
				if (dbg == value)
					return;
				dbg = value;
				NotifyValueChanged ("DbgSession", dbg);
			}
		}
		#endregion
	}
}