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
		protected Service () {
			CMDStart = new Command ("Start", Start, "#icons.play-button.svg", true);
			CMDStop = new Command ("Stop", Stop, "#icons.stop.svg", false);
			CMDPause = new Command ("Pause", Pause, "#icons.pause-symbol.svg", false);
			CMDOpenConfig = new Command ("Service configuration",
				() => CrowEditBase.App.LoadWindow (ConfigurationWindowPath, this), "#icons.cogwheel.svg", true);
			Commands = new CommandGroup (CMDStart, CMDPause, CMDStop, CMDOpenConfig);

			if (CrowEditBase.App.TryGetWindow (ConfigurationWindowPath, out Window win))
				win.DataSource = this;			
		}
		public Command CMDStart, CMDStop, CMDPause, CMDOpenConfig;
		public CommandGroup Commands;
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
		}

		public abstract void Start ();
		public abstract void Stop ();
		public abstract void Pause ();
		public virtual string ConfigurationWindowPath => "#CrowEditBase.ui.winServiceConfig.crow";

		public virtual Document OpenDocument (string fullPath) => null;
	}
}