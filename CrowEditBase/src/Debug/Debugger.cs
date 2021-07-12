// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Runtime.CompilerServices;
using Crow;

namespace CrowEditBase
{
	public abstract class Debugger : CrowEditComponent
	{
		public enum Status
		{
			/// <summary>debugger process created</summary>
			Init,
			/// <summary>request loading sent</summary>
			Starting,
			/// <summary>executable loaded, breakpoints requested</summary>
			Ready,
			/// <summary>running state received</summary>
			Running,
			/// <summary>stopped event received</summary>
			Stopped,
		}

		protected Project project;
		public Command CMDDebugStart, CMDDebugPause, CMDDebugStop, CMDDebugStepIn, CMDDebugStepOver, CMDDebugStepOut;
		public virtual CommandGroup Commands => new CommandGroup (
			CMDDebugStart, CMDDebugPause, CMDDebugStop, CMDDebugStepIn, CMDDebugStepOver, CMDDebugStepOut);
		protected virtual void initCommands () {
			CMDDebugStart = new Command ("Start", Start, "#Icons.debug-play.svg");
			CMDDebugPause = new Command ("Pause", Pause, "#Icons.debug-pause.svg", false);
			CMDDebugStop = new Command ("Stop", Stop, "#Icons.debug-stop.svg", false);
			CMDDebugStepIn = new Command ("Step in", StepIn, "#Icons.debug-step-into.svg", false);
			CMDDebugStepOut = new Command ("Step out", StepOut, "#Icons.debug-step-out.svg", false);
			CMDDebugStepOver = new Command ("Step over", StepOver, "#Icons.debug-step-over.svg", false);
		}


		Status currentState = Status.Init;
		bool breakOnStartup = false;

		public Status CurrentState
		{
			get => currentState;
			set
			{
				if (currentState == value)
					return;
				currentState = value;

				CMDDebugStepIn.CanExecute = CMDDebugStepOut.CanExecute = CMDDebugStepOver.CanExecute =
					(CurrentState == Status.Stopped);
				CMDDebugStart.CanExecute = (CurrentState == Status.Ready || CurrentState == Status.Stopped);
				CMDDebugPause.CanExecute = CMDDebugStop.CanExecute = (CurrentState == Status.Running);
			}
		}
		StackFrame executingFile;
		int executingLine = -1;

		public ObservableList<string> OutputLog = new ObservableList<string>();
		public ObservableList<string> ErrorLog = new ObservableList<string>();
		public ObservableList<string> DebuggerLog = new ObservableList<string>();

		public ObservableList<StackFrame> Frames = new ObservableList<StackFrame>();
		public ObservableList<ThreadInfo> Threads = new ObservableList<ThreadInfo>();
		public ObservableList<Watch> Watches = new ObservableList<Watch>();
		public ObservableList<BreakPoint> BreakPoints = new ObservableList<BreakPoint>();

		ThreadInfo currentThread;
		StackFrame currentFrame;
		BreakPoint currentBreakPoint;

		public ThreadInfo CurrentThread
		{
			get => currentThread;
			set
			{
				if (currentThread == value)
					return;
				currentThread = value;
				NotifyValueChanged(currentThread);
			}
		}
		public StackFrame CurrentFrame
		{
			get => currentFrame;
			set
			{
				if (currentFrame == value)
					return;
				currentFrame = value;
				NotifyValueChanged(currentFrame);
				onCurrentFrameChanged ();
			}
		}
		protected abstract void onCurrentFrameChanged ();
		protected abstract void onCurrentThreadChanged ();
		public BreakPoint CurrentBreakPoint
		{
			get => currentBreakPoint;
			set
			{
				if (currentBreakPoint == value)
					return;
				currentBreakPoint = value;
				NotifyValueChanged(currentBreakPoint);
				if (currentBreakPoint == null)
					return;
				//tryGoTo(currentFrame);				
			}
		}

		public bool BreakOnStartup
		{
			get => breakOnStartup;
			set
			{
				if (BreakOnStartup == value)
					return;
				breakOnStartup = value;
				NotifyValueChanged(breakOnStartup);
			}
		}
		public virtual Project Project
		{
			get => project;
			set
			{
				if (project == value)
					return;
				project = value;
				NotifyValueChanged(Project);
			}
		}

		public abstract void Start();
		public abstract void Pause();
		public abstract void Continue();
		public abstract void Stop();

		public abstract void StepIn();
		public abstract void StepOver();
		public abstract void StepOut();

		public abstract void InsertBreakPoint(BreakPoint bp);
		public abstract void DeleteBreakPoint(BreakPoint bp);

		protected void ResetCurrentExecutingLocation() {

		}


	}
}
