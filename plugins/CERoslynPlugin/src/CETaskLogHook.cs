using System.Reflection;
// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Diagnostics;
using System.Linq;
using Crow;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;

using static CrowEditBase.CrowEditBase;

namespace CERoslynPlugin
{
	internal class CETaskLogHook : ILogger
	{
		IEventSource eventSource;
		public LoggerVerbosity Verbosity { get; set; }
		public string Parameters { get; set; }

		public CETaskLogHook ()	{}
		public void Initialize (IEventSource eventSource) {
			this.eventSource = eventSource;
			registerHandles ();
		}


		void registerHandles () {
			eventSource.TaskStarted += EventSource_TaskStarted;
			eventSource.TaskFinished += EventSource_TaskFinished;
			eventSource.TargetStarted += EventSource_TargetStarted;
			eventSource.TargetFinished += EventSource_TargetFinished;
		}

		void unregisterHandles () {
			eventSource.TaskStarted -= EventSource_TaskStarted;
			eventSource.TaskFinished -= EventSource_TaskFinished;
			eventSource.TargetStarted -= EventSource_TargetStarted;
			eventSource.TargetFinished -= EventSource_TargetFinished;
		}

		private void EventSource_TaskFinished (object sender, TaskFinishedEventArgs e) {
			Console.WriteLine ($"Task <- {sender} {e}");
		}

		private void EventSource_TaskStarted (object sender, TaskStartedEventArgs e) {
			Console.WriteLine ($"Task -> {sender} {e}");
		}
		private void EventSource_TargetFinished (object sender, TargetFinishedEventArgs e) {
			
		}

		private void EventSource_TargetStarted (object sender, TargetStartedEventArgs e) {
			
		}

		public void Shutdown ()
		{
			if (eventSource != null)
				unregisterHandles ();
		}
	}
}
