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
	internal class CELogger : ILogger, IValueChange {
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public void NotifyValueChanged (string MemberName, object _value)
		{
			ValueChanged.Raise (this, new ValueChangeEventArgs (MemberName, _value));
		}
		#endregion
		IEventSource eventSource;
		LoggerVerbosity verbosity;
		MessageImportance maxMsgImportance;

		public LoggerVerbosity Verbosity {
			get => verbosity;
			set {
				if (verbosity == value)
					return;
				verbosity = value;
				NotifyValueChanged ("Verbosity", verbosity);
				Configuration.Global.Set<Microsoft.Build.Framework.LoggerVerbosity> ("CERoslyn.LoggerVerbosity", verbosity);

				switch (verbosity) {
				case LoggerVerbosity.Minimal:
					maxMsgImportance = MessageImportance.High;
					break;
				case LoggerVerbosity.Normal:
					maxMsgImportance = MessageImportance.High;
					break;
				case LoggerVerbosity.Detailed:
					maxMsgImportance = MessageImportance.Normal;
					break;
				case LoggerVerbosity.Diagnostic:
					maxMsgImportance = MessageImportance.Low;
					break;
				}

			}
		}
		public string Parameters { get; set; }

		public CELogger (LoggerVerbosity verbosity = LoggerVerbosity.Detailed)
		{
			Verbosity = verbosity;
		}
		public void Initialize (IEventSource eventSource) {
			this.eventSource = eventSource;
			registerHandles ();
		}


		void registerHandles () {
			eventSource.WarningRaised += EventSource_WarningRaised;
			eventSource.ErrorRaised += EventSource_ErrorRaised;
			eventSource.BuildStarted += EventSource_Progress_BuildStarted;
			eventSource.BuildFinished += EventSource_Progress_BuildFinished;
			eventSource.MessageRaised += EventSource_MessageRaised;
			eventSource.ProjectStarted += EventSource_ProjectStarted;
			eventSource.ProjectFinished += EventSource_ProjectFinished;
			eventSource.TargetStarted += EventSource_TargetStarted;
			eventSource.TargetFinished += EventSource_TargetFinished;
			eventSource.TaskStarted += EventSource_TaskStarted;
			eventSource.TaskFinished += EventSource_TaskFinished;
		}

		void unregisterHandles () {
			eventSource.WarningRaised -= EventSource_WarningRaised;
			eventSource.ErrorRaised -= EventSource_ErrorRaised;
			eventSource.BuildStarted -= EventSource_Progress_BuildStarted;
			eventSource.BuildFinished -= EventSource_Progress_BuildFinished;
			eventSource.MessageRaised -= EventSource_MessageRaised;
			eventSource.ProjectStarted -= EventSource_ProjectStarted;
			eventSource.ProjectFinished -= EventSource_ProjectFinished;
			eventSource.TargetStarted -= EventSource_TargetStarted;
			eventSource.TargetFinished -= EventSource_TargetFinished;
			eventSource.TaskStarted -= EventSource_TaskStarted;
			eventSource.TaskFinished -= EventSource_TaskFinished;
		}
		void log (LogType type, string message) {
			string[] lines = Regex.Split (message, "\r\n|\r|\n");//|\r|\n|\\\\n");
			for	(int i=0; i<lines.Length;i++)
				App.Log (type, lines[i]);
		}
		void EventSource_Progress_BuildStarted (object sender, BuildStartedEventArgs e)
		{
			App.ResetLog ();
			log (LogType.High, "Build starting.");
		}
		void EventSource_Progress_BuildFinished (object sender, BuildFinishedEventArgs e)
		{
			log (LogType.High, e.Succeeded ? "Build Succeed." : "Build Failed.");
		}

		private void EventSource_TaskFinished (object sender, TaskFinishedEventArgs e) {
			if (Verbosity == LoggerVerbosity.Diagnostic)
				log (LogType.Custom1, e.Message);
		}

		private void EventSource_TaskStarted (object sender, TaskStartedEventArgs e) {
			if (Verbosity == LoggerVerbosity.Diagnostic)
				log (LogType.Custom1, e.Message);
		}

		private void EventSource_TargetFinished (object sender, TargetFinishedEventArgs e) {
			if (Verbosity >= LoggerVerbosity.Detailed)
				log (LogType.Custom2, e.Message);
		}

		private void EventSource_TargetStarted (object sender, TargetStartedEventArgs e) {
			if (Verbosity >= LoggerVerbosity.Detailed)
				log (LogType.Custom2, e.Message);
		}
		private void EventSource_MessageRaised (object sender, BuildMessageEventArgs e) {
			if (Verbosity == LoggerVerbosity.Quiet || e.Importance > maxMsgImportance)
				return;
			if (e.Importance == MessageImportance.High)
				log (LogType.High, e.Message);
			else if (e.Importance == MessageImportance.Normal)
				log (LogType.Normal, e.Message);
			else
				log (LogType.Low, e.Message);
		}
		void EventSource_ProjectStarted (object sender, ProjectStartedEventArgs e)
		{
			log (LogType.Custom3, e.Message);
		}
		void EventSource_ProjectFinished (object sender, ProjectFinishedEventArgs e)
		{
			log (LogType.Custom3, e.Message);
		}
		void EventSource_ErrorRaised (object sender, BuildErrorEventArgs e)
		{
			log (LogType.Error, e.Message);
		}
		private void EventSource_WarningRaised (object sender, BuildWarningEventArgs e) {
			log (LogType.Warning, e.Message);
		}

		public void Shutdown ()
		{
			if (eventSource != null)
				unregisterHandles ();
		}
	}
}
