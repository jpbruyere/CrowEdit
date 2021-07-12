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
	internal class CELogger : ILogger
	{
		IEventSource eventSource;
		LoggerVerbosity verbosity;

		public LoggerVerbosity Verbosity {
			get => verbosity;
			set {
				if (verbosity == value)
					return;
				if (eventSource != null)
					unregisterHandles ();
				verbosity = value;
				if (eventSource != null)
					registerHandles ();
			}
		} 
		public string Parameters { get; set; }

		public CELogger (LoggerVerbosity verbosity = LoggerVerbosity.Detailed)
		{			
			this.verbosity = verbosity;
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

			switch (Verbosity) {
			case LoggerVerbosity.Minimal:
				eventSource.MessageRaised += EventSource_MessageRaised_Minimal;
				break;
			case LoggerVerbosity.Normal:
				eventSource.MessageRaised += EventSource_MessageRaised_Normal;
				eventSource.ProjectStarted += EventSource_ProjectStarted;
				eventSource.ProjectFinished += EventSource_ProjectFinished;
				break;
			case LoggerVerbosity.Detailed:
				eventSource.MessageRaised += EventSource_MessageRaised_All;
				eventSource.ProjectStarted += EventSource_ProjectStarted;
				eventSource.ProjectFinished += EventSource_ProjectFinished;
				eventSource.TargetStarted += EventSource_TargetStarted;
				eventSource.TargetFinished += EventSource_TargetFinished;
				eventSource.TaskStarted += EventSource_TaskStarted;
				eventSource.TaskFinished += EventSource_TaskFinished;
				break;
			case LoggerVerbosity.Diagnostic:
				eventSource.AnyEventRaised += EventSource_AnyEventRaised;
				break;
			}
		}

		void unregisterHandles () {
			eventSource.WarningRaised -= EventSource_WarningRaised;
			eventSource.ErrorRaised -= EventSource_ErrorRaised;


			switch (Verbosity) {
			case LoggerVerbosity.Minimal:
				eventSource.MessageRaised -= EventSource_MessageRaised_Minimal;
				break;
			case LoggerVerbosity.Normal:
				eventSource.MessageRaised -= EventSource_MessageRaised_Normal;
				eventSource.ProjectStarted -= EventSource_ProjectStarted;
				eventSource.ProjectFinished -= EventSource_ProjectFinished;
				break;
			case LoggerVerbosity.Detailed:
				eventSource.MessageRaised -= EventSource_MessageRaised_All;
				eventSource.ProjectStarted -= EventSource_ProjectStarted;
				eventSource.ProjectFinished -= EventSource_ProjectFinished;
				eventSource.TargetStarted -= EventSource_TargetStarted;
				eventSource.TargetFinished -= EventSource_TargetFinished;
				eventSource.TaskStarted -= EventSource_TaskStarted;
				eventSource.TaskFinished -= EventSource_TaskFinished;
				break;
			case LoggerVerbosity.Diagnostic:
				eventSource.AnyEventRaised -= EventSource_AnyEventRaised;
				break;
			}

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
			log (LogType.High, "Build Finished.");
			//ide.CurrentSolution.RaiseDiagnosticsValueChanged();
		}		

        private void EventSource_TaskFinished (object sender, TaskFinishedEventArgs e) {
			log (LogType.Custom1, e.Message);
		}

		private void EventSource_TaskStarted (object sender, TaskStartedEventArgs e) {
			log (LogType.Custom1, e.Message);
		}

		private void EventSource_TargetFinished (object sender, TargetFinishedEventArgs e) {			
			log (LogType.Custom2, e.Message);
		}

		private void EventSource_TargetStarted (object sender, TargetStartedEventArgs e) {
			log (LogType.Custom2, e.Message);
		}
		private void EventSource_MessageRaised (object sender, BuildMessageEventArgs e) {
			log (LogType.Normal, e.Message);
		}
        private void EventSource_AnyEventRaised (object sender, BuildEventArgs e) {
			log (LogType.Normal, e.Message);
		}

        private void EventSource_MessageRaised_Minimal (object sender, BuildMessageEventArgs e) {
			if (e.Importance == MessageImportance.High)
				log (LogType.High, e.Message);
		}
		private void EventSource_MessageRaised_Normal (object sender, BuildMessageEventArgs e) {
			if (e.Importance == MessageImportance.Normal)
				log (LogType.Normal, e.Message);
			else if(e.Importance == MessageImportance.High)
				log (LogType.High, e.Message);
		}
		private void EventSource_MessageRaised_All (object sender, BuildMessageEventArgs e) {			
			if (e.Importance == MessageImportance.Low)
				log (LogType.Low, e.Message);
			else if(e.Importance == MessageImportance.Normal)
				log (LogType.Normal, e.Message);
			else if(e.Importance == MessageImportance.High)
				log (LogType.High, e.Message);
		}
		void EventSource_ProjectStarted (object sender, ProjectStartedEventArgs e)
		{
			log (LogType.High, e.Message);
		}
		void EventSource_ProjectFinished (object sender, ProjectFinishedEventArgs e)
		{
			log (LogType.High, e.Message);
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
