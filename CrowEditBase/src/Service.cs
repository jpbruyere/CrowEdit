// Copyright (c) 2021-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;
using System.Linq;
using Crow;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace CrowEditBase
{
	public abstract class Service : CrowEditComponent {
		public enum Status {
			Init,
			Running,
			Paused,
			Stopped
		}
		LogItem log;
		protected void Log(LogType type, string message) => log.Add(type, message);
		protected Service () {
			Name = this.GetType().Name;
			log = CrowEditBase.App.MainLog;
			initCommands();
			//ensureConfigWinDataSource();
			Log(LogType.Low, $"[{Name}] Service Instanciated");
		}
		public Command CMDStart, CMDStop, CMDPause, CMDOpenConfig;
		public CommandGroup Commands;
		void initCommands() {
			CMDStart = new ActionCommand ("Start", Start, "#icons.play-button.svg", true);
			CMDStop = new ActionCommand ("Stop", Stop, "#icons.stop.svg", false);
			CMDPause = new ActionCommand ("Pause", Pause, "#icons.pause-symbol.svg", false);
			CMDOpenConfig = new ActionCommand ("Service configuration",
				() => CrowEditBase.App.LoadWindow (ServiceWindowsPath[0], this), "#icons.cogwheel.svg", true);
			Commands = new CommandGroup (CMDStart, CMDPause, CMDStop, CMDOpenConfig);
		}
		Status currentState;
		public Status CurrentState {
			get => currentState;
			protected set {
				if (currentState == value)
					return;
				Status previousState = currentState;
				currentState = value;
				NotifyValueChanged (currentState);
				NotifyValueChanged ("IsRunning", IsRunning);

				onStateChange (previousState, currentState);
			}
		}
		public bool IsRunning => currentState == Status.Running;
		protected virtual void onStateChange (Status previousState, Status newState) {
			CMDStart.CanExecute = !IsRunning;
			CMDPause.CanExecute = IsRunning;
			CMDStop.CanExecute = IsRunning || CurrentState == Status.Paused;
			Log(LogType.High, $"[{Name}] Status: {previousState} -> {newState}");
		}
		/*protected void ensureConfigWinDataSource() {
			if (CrowEditBase.App.TryGetWindow (ServiceWindowsPath[0], out Window win))
				win.DataSource = this;
		}*/
		public readonly string Name;
		public abstract void Start ();
		public abstract void Stop ();
		public abstract void Pause ();
		//windows having this service as datasource
		public virtual string[] ServiceWindowsPath => ["#ui.winServiceConfig.crow"];

		public virtual Document OpenDocument (string fullPath) => null;
	}
}