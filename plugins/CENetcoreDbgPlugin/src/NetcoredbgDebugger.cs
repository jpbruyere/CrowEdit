// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CrowEditBase;
using Crow;
using CERoslynPlugin;

using static CrowEditBase.CrowEditBase;

namespace NetcoreDbgPlugin
{
	public class NetcoredbgDebugger : Debugger
	{
		System.Diagnostics.Process procdbg;
		public class Request {
			public readonly string Command;
			public Request (string command) {
				Command = command;
			}
			public override string ToString() => Command;
		}
		public class Request<T> : Request {
			public T RequestObject;
			public Request (T obj, string command) : base (command){
				RequestObject = obj;
			}
		}
		Queue<Request> pendingRequest = new Queue<Request>();
		MSBuildProject msbProject => Project as MSBuildProject;

		public override Project Project
		{
			get => base.Project;
			set
			{
				if (base.Project == value || !(value is MSBuildProject msbProj))
					return;
				base.Project = value;				
			}
		}
		void initDebugSession () {
			if (CurrentState != Status.Init || msbProject == null)
				return;
			
			bool result = procdbg.Start();

			procdbg.BeginOutputReadLine();

			CreateNewRequest($"-file-exec-and-symbols {msbProject.OutputAssembly}");
			CreateNewRequest($"-environment-cd {Path.GetDirectoryName(msbProject.OutputAssembly)}");

			foreach (BreakPoint bp in BreakPoints)
				InsertBreakPoint(bp);

			CurrentState = Status.Starting;
		}
		#region CTOR
		public NetcoredbgDebugger()
		{
			procdbg = new System.Diagnostics.Process();
			procdbg.StartInfo.FileName = App.GetService<NetcoreDbgService>().NetcoredbgPath;
			procdbg.StartInfo.Arguments = "--interpreter=mi";
			procdbg.StartInfo.CreateNoWindow = true;
			procdbg.StartInfo.RedirectStandardInput = true;
			procdbg.StartInfo.RedirectStandardOutput = true;
			procdbg.StartInfo.RedirectStandardError = true;
			procdbg.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;
			//procdbg.StartInfo.StandardInputEncoding = System.Text.Encoding.UTF8;
			procdbg.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;

			procdbg.EnableRaisingEvents = true;
			procdbg.OutputDataReceived += Procdbg_OutputDataReceived;
			procdbg.ErrorDataReceived += Procdbg_ErrorDataReceived;
			procdbg.Exited += Procdbg_Exited;

			BreakPoints.ListAdd += BreakPoints_ListAdd;
			BreakPoints.ListRemove += BreakPoints_ListRemove;
		}
		#endregion

		public void Terminate ()  {
			if (CurrentState == Status.Running || CurrentState == Status.Stopped)
				Stop ();
			procdbg?.Dispose();
		}
		/*protected override void ResetCurrentExecutingLocation()
		{
			if (executingFile == null)
				return;
			executingFile.ExecutingLine = -1;
			executingFile = null;
		}*/
		/// <summary>
		/// send request on netcoredbg process stdin
		/// </summary>
		void sendRequest(Request request)
		{
			DebuggerLog.Add($"<- {request}");
			procdbg.StandardInput.WriteLine(request);
		}

		/// <summary>
		/// enqueue new request, send it if no other request is pending
		/// </summary>
		public void CreateNewRequest(string request)
		{
			lock (pendingRequest)
			{
				pendingRequest.Enqueue(new Request (request));
				if (pendingRequest.Count == 1)
					sendRequest(pendingRequest.Peek());
			}
		}
		public void CreateNewRequest(Request request)
		{
			lock (pendingRequest)
			{
				pendingRequest.Enqueue(request);
				if (pendingRequest.Count == 1)
					sendRequest(pendingRequest.Peek());
			}
		}

		#region Debugger abstract class implementation
		public override void Start()
		{
			initDebugSession ();
			CreateNewRequest($"-exec-run");
		}
		public override void Pause()
		{
			CreateNewRequest($"-exec-interrupt");
		}
		public override void Continue()
		{
			CreateNewRequest($"-exec-continue");
		}
		public override void Stop()
		{
			CreateNewRequest($"-exec-abort");			
		}

		public override void StepIn()
		{
			CreateNewRequest($"-exec-step");
		}
		public override void StepOver()
		{
			CreateNewRequest($"-exec-next");
		}
		public override void StepOut()
		{
			CreateNewRequest($"-exec-finish");
		}

		public override void InsertBreakPoint(CrowEditBase.BreakPoint bp)
		{
			BreakPoint bk = bp as BreakPoint;
			CreateNewRequest (new Request<BreakPoint> (bk, $"-break-insert {bk.FileFullPath}:{bk.Line + 1}"));
		}
		public override void DeleteBreakPoint(CrowEditBase.BreakPoint bp)
		{
			if (bp.Index < 0)
				return;
			CreateNewRequest($"-break-delete {bp.Index}");
		}
		protected override void onCurrentFrameChanged () {
				if (CurrentFrame == null)
					return;
				tryGoTo((StackFrame)CurrentFrame);
				updateWatches ();
		}
		protected override void onCurrentThreadChanged () {
				if (CurrentThread == null)
					return;
				getStackFrames((ThreadInfo)CurrentThread);
				updateWatches ();
		}
		#endregion

		public void GetStackFrames(MIList list)
		{

		}

		private void BreakPoints_ListRemove(object sender, ListChangedEventArg e)
		{
			if (CurrentState == Status.Init)
				return;
			DeleteBreakPoint((BreakPoint)e.Element);
		}
		private void BreakPoints_ListAdd(object sender, ListChangedEventArg e)
		{
			if (CurrentState == Status.Init)
				return;
			InsertBreakPoint((BreakPoint)e.Element);
		}
		private void Procdbg_Exited(object sender, EventArgs e)
		{
			DebuggerLog.Add("GDB process Terminated.");
			
			CurrentState = Status.Init;
		}
		
		void getStackFrames(ThreadInfo thread = null)
		{
			if (thread == null)
				CreateNewRequest($"-stack-list-frames");
			else
				CreateNewRequest($"-stack-list-frames --thread {thread.Id}");
		}
		void getVariables(ThreadInfo thread = null, int stackLevel = 0)
		{
			CreateNewRequest($"-stack-list-variables");
		}
		void updateWatches () {
			foreach (Watch w in Watches)
				w.UpdateValue ();			
		}

		void tryGoTo(StackFrame frame)
		{
			/*if (string.IsNullOrEmpty(frame.FileFullName))
				return;
			executingLine = frame.Line - 1;
			string strPath = frame.FileFullName;

			if (project.TryGetProjectFileFromPath(strPath, out ProjectFileNode pf))
			{
				if (!pf.IsOpened)
					pf.Open();
				pf.IsSelected = true;

				executingFile = pf as CSProjectItem;
				executingFile.ExecutingLine = executingLine;
				executingFile.CurrentLine = executingLine;
			}
			else
			{
				ResetCurrentExecutingLocation();
				DebuggerLog.Add($"[ERROR]:current executing file ({strPath}) not found.");
			}*/
		}
		bool hasPendingRequest {
			get {
				lock (pendingRequest) {
					return pendingRequest.Count > 0;
				}
			}
		}

		void Procdbg_ErrorDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e) {
			DebuggerLog.Add($"-> Error: {e.Data}");
		}		

		void Procdbg_OutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e) {
			if (string.IsNullOrEmpty(e.Data))
				return;

			DebuggerLog.Add($"-> {e.Data}");

			char firstChar = e.Data[0];
			ReadOnlySpan<char> data = e.Data.AsSpan(1);

			if (firstChar == '(')
				return;

			int tokEnd = data.IndexOf(',');

			ReadOnlySpan<char> data_id = tokEnd < 0 ? data : data.Slice(0, tokEnd);
			if (tokEnd >= 0)
				data = data.Slice(tokEnd + 1);

			MITupple obj = MITupple.Parse (data);

			if (firstChar == '^')
			{
				Request request = null;
				lock (pendingRequest)
				{
					if (pendingRequest.Count > 0)
					{
						request = pendingRequest.Dequeue();
						if (pendingRequest.Count > 0)
							sendRequest(pendingRequest.Peek());
					}
				}

				if (data_id.SequenceEqual("running"))
				{
					CurrentState = Status.Running;
					CurrentFrame = null;
					CurrentThread = null;
					Threads.Clear();
					Frames.Clear();
				}
				else if (data_id.SequenceEqual("done"))
				{
					if (obj.Attributes.Count > 0)
					{
						if (obj[0].Name == "threads") {
							MIList threads = obj[0] as MIList;
							Threads.Clear();
							foreach (MITupple t in threads.Items)
								Threads.Add(new ThreadInfo(t));
						} else if (obj[0].Name == "stack") {
							MIList stack = obj[0] as MIList;
							Frames.Clear();
							foreach (MITupple f in stack.Items)
								Frames.Add(new StackFrame(f));
						} else if (request is Request<Watch> w) {
							/*if (w.Command.StartsWith ("-var-delete")) {
								Watches.Remove (w.RequestObject);
							} else*/
							if (w.Command.StartsWith("-var-list-children")) {
								if (int.Parse (obj.GetAttributeValue ("numchild")) > 0) {
									foreach (MITupple child in (obj["children"] as MIList).Items)
										w.RequestObject.Children.Add(new Watch(this, child));
								}
							} else if (w.Command.StartsWith ("-var-evaluate"))
								w.RequestObject.Value = obj.GetAttributeValue("value");
							else
								w.RequestObject.Update (obj);
						} else if (request is Request<BreakPoint> bpReq) {
							BreakPoint bp = bpReq.RequestObject;
							bp.Update (obj["bkpt"] as MITupple);
							
						} else
							DebuggerLog.Add($"=> request result not handled: {request}");
					}


				}
				else if (data_id.SequenceEqual("exit"))
				{
					DebuggerLog.Add($"=> exit request done: {request}");
					CreateNewRequest($"-gdb-exit");
				}
				else
					print_unknown_datas($"requested: {request} data:{e.Data}");

			}
			else if (firstChar == '*')
			{
				if (data_id.SequenceEqual("stopped"))
				{
					CurrentState = Status.Stopped;
					string reason = obj.GetAttributeValue("reason");
					if (reason == "exited")
					{
						CurrentState = Status.Ready;
						DebuggerLog.Add($"Exited({obj.GetAttributeValue("exit-code")})");
						//CreateNewRequest($"-gdb-exit");
					}
					else if (reason == "entry-point-hit" && !BreakOnStartup) {
						Continue();
					} else {
						DebuggerLog.Add($"Stopped reason:{reason}");

						StackFrame frame = new StackFrame(obj["frame"] as MITupple);
						if (reason == "breakpoint-hit") {							
							BreakPoint bp = (BreakPoint)BreakPoints.FirstOrDefault (bk=>bk.Index == int.Parse (obj.GetAttributeValue ("bkptno")));
							bp.UpdateLocation (frame);
						}

						tryGoTo(frame);

						CreateNewRequest($"-thread-info");
						getStackFrames();
						getVariables();
						updateWatches ();
					}

				} else
					print_unknown_datas(e.Data);
			} else if (firstChar == '=') {//EVENTS
				if (data_id.SequenceEqual("message")) {
					OutputLog.Add(obj.GetAttributeValue("text").ToString().Replace(@"\0", ""));
				} else if (data_id.SequenceEqual("breakpoint-modified")) {
					OutputLog.Add($"{e.Data}");
					MITupple bkpt = obj["bkpt"] as MITupple;
					BreakPoint bp = (BreakPoint)BreakPoints.FirstOrDefault (bk=>bk.Index == int.Parse (bkpt.GetAttributeValue("number")));
					bp.Update (bkpt);
				}
			} else
				print_unknown_datas(e.Data);
		}

		void print_unknown_datas(string data)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(data);
			Console.ResetColor();
		}

		public void WatchRequest(Watch w)
		{
			string strThread = CurrentThread == null ? "" : $"--thread {CurrentThread.Id}";
			string strLevel = CurrentFrame == null ? "" : $"--frame {CurrentFrame.Level}";
			CreateNewRequest (new Request<Watch> (w, $"-var-create {w.Name} {w.Expression} {strThread} {strLevel}"));
		}
		public void WatchChildrenRequest(Watch w)
		{			
			string strThread = CurrentThread == null ? "" : $"--thread {CurrentThread.Id}";
			string strLevel = CurrentFrame == null ? "" : $"--frame {CurrentFrame.Level}";			
			CreateNewRequest (new Request<Watch> (w, $"-var-list-children 1 {w.Name} {strThread} {strLevel}"));
		}

		public void OnValidateCommand(Object sender, ValidateEventArgs e)
		{
			CreateNewRequest(e.ValidatedText);
			(sender as TextBox).Text = "";
		}

		public void OnValidateNewWatch(Object sender, ValidateEventArgs e)
		{
			Watch w = new Watch(this, e.ValidatedText);
			Watches.Add(w);
			WatchRequest(w);
			(sender as TextBox).Text = "";
		}

	}
}
