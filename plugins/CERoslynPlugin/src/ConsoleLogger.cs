using System.Reflection;
// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Diagnostics;
using System.Linq;
using Crow;
using Microsoft.Build.Framework;

namespace CERoslynPlugin
{
	public class ConsoleLogger : ILogger
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

		public ConsoleLogger (LoggerVerbosity verbosity = LoggerVerbosity.Diagnostic)
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

        private void EventSource_TaskFinished (object sender, TaskFinishedEventArgs e) {
			Console.WriteLine (e.Message);
		}

		private void EventSource_TaskStarted (object sender, TaskStartedEventArgs e) {
			Console.WriteLine (e.Message);
		}

		private void EventSource_TargetFinished (object sender, TargetFinishedEventArgs e) {			
			Console.WriteLine (e.Message);
		}

		private void EventSource_TargetStarted (object sender, TargetStartedEventArgs e) {
			Console.WriteLine (e.Message);
		}
		private void EventSource_MessageRaised (object sender, BuildMessageEventArgs e) {
			Console.WriteLine (e.Message);
		}
        private void EventSource_AnyEventRaised (object sender, BuildEventArgs e) {
			Console.WriteLine (e.Message);
		}

        private void EventSource_MessageRaised_Minimal (object sender, BuildMessageEventArgs e) {
			if (e.Importance == MessageImportance.High)
				Console.WriteLine (e.Message);
		}
		private void EventSource_MessageRaised_Normal (object sender, BuildMessageEventArgs e) {
			if (e.Importance != MessageImportance.Low)
				Console.WriteLine (e.Message);
		}
		private void EventSource_MessageRaised_All (object sender, BuildMessageEventArgs e) {			
			Console.WriteLine (e.Message);
		}
		void EventSource_ProjectStarted (object sender, ProjectStartedEventArgs e)
		{
			Console.WriteLine (e.Message);
		}
		void EventSource_ProjectFinished (object sender, ProjectFinishedEventArgs e)
		{
			Console.WriteLine (e.Message);
		}
		void EventSource_ErrorRaised (object sender, BuildErrorEventArgs e)
		{
			Console.WriteLine (e.Message);
		}
		private void EventSource_WarningRaised (object sender, BuildWarningEventArgs e) {
			Console.WriteLine (e.Message);
		}

		public void Shutdown ()
		{
			if (eventSource != null)
				unregisterHandles ();
		}
	}
}
