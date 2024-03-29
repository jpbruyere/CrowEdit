﻿// Copyright (c) 2013-2019  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Glfw;
using System.Reflection;
using System.Runtime.Loader;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using Crow.DebugLogger;
using System.Linq;
using CrowEditBase;
using System.Threading;
using Crow.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using static CrowEditBase.CrowEditBase;
using Drawing2D;

namespace Crow
{
	public class CrowService : Service {
		public CrowService () : base () {

			loadCrowAssemblies ();

			initCommands ();

			//resolve other plugins dependencies
			//AssemblyLoadContext.GetLoadContext (Assembly.GetExecutingAssembly ()).Resolving += resolvePluginRefs;

			if (CrowEditBase.CrowEditBase.App.TryGetWindow ("#CECrowPlugin.ui.winLogGraph.crow", out Window win))
				win.DataSource = this;
		}
		/*Assembly resolvePluginRefs (AssemblyLoadContext ctx, AssemblyName assemblyName)
			=> App.TryGetPlugin ("CERoslynPlugin", out Plugin roslynPlugin) ?
				roslynPlugin.Load (assemblyName) : null;*/

		/*static IntPtr resolveUnmanaged(Assembly assembly, String libraryName)
		{

			switch (libraryName)
			{
				case "glfw3":
					return NativeLibrary.Load("glfw", assembly, null);
				case "rsvg-2.40":
					return NativeLibrary.Load("rsvg-2", assembly, null);
			}
			Console.WriteLine($"[UNRESOLVE] {assembly} {libraryName}");
			return IntPtr.Zero;
		}*/

		/*void updateCrowApp () {
			if (App.CurrentProject is CERoslynPlugin.SolutionProject sol) {
				if (sol.StartupProject is CERoslynPlugin.MSBuildProject csprj) {

				}
			}else if (App.CurrentProject is CERoslynPlugin.MSBuildProject csprj){
				CERoslynPlugin.MSBuildProject project = App.CurrentProject as CERoslynPlugin.MSBuildProject;
				Console.WriteLine ($"{project.Name}: {project.IsCrowProject}");

			}


		}*/
		#region Commands
		public Command CMDStartRecording, CMDStopRecording, CMDRefresh;
		public Command CMDGotoParentEvent, CMDEventHistoryForward, CMDEventHistoryBackward;
		public CommandGroup LoggerCommands => new CommandGroup (CMDRefresh, CMDStartRecording, CMDStopRecording);
		public CommandGroup EventCommands => new CommandGroup(
				CMDGotoParentEvent, CMDEventHistoryBackward, CMDEventHistoryForward);
		public ActionCommand CMDOptions_SelectCrowAssemblyLocation => new ActionCommand ("...",
			() => {
				FileDialog dlg = App.LoadIMLFragment<FileDialog> (@"
				<FileDialog Caption='Select Crow.dll assembly' CurrentDirectory='{CrowDbgAssemblyLocation}'
							ShowFiles='true' ShowHidden='true' />");
				dlg.OkClicked += (sender, e) => CrowDbgAssemblyLocation = (sender as FileDialog).SelectedFileFullPath;
				dlg.DataSource = this;
			}
		);
		public ActionCommand CMDOptions_AddCrowAssembly => new ActionCommand ("Add Assembly with Crow Ressource",
			() => {
				FileDialog dlg = App.LoadIMLFragment<FileDialog> (@"
				<FileDialog Caption='Select Assembly with Crow Ressources' CurrentDirectory='{CrowDbgAssemblyLocation}'
							ShowFiles='true' ShowHidden='true' />");
				dlg.OkClicked += (sender, e) => {
					crowAssemblies.Add ((sender as FileDialog).SelectedFileFullPath);
					saveCrowAssemblies ();
				};
				dlg.DataSource = this;
			}
		);

		public ActionCommand CMDViewPreview;

		void initCommands ()
		{
			CMDViewPreview = new ActionCommand("Crow Preview", () => App.LoadWindow ("#CECrowPlugin.ui.winCrowPreview.crow", App));

			CMDRefresh = new ActionCommand ("Refresh", refresh, "#icons.refresh.svg", IsRunning);
			CMDStartRecording = new ActionCommand ("Start Recording", () => Recording = true, "#icons.circle.svg", false);
			CMDStopRecording = new ActionCommand ("Stop Recording", stopRecording, "#icons.circle-red.svg", false);

			CMDGotoParentEvent = new ActionCommand("parent", ()=> { CurrentEvent = CurrentEvent?.parentEvent; }, "#icons.level-up.svg", false);
			CMDEventHistoryBackward = new ActionCommand("back.", currentEventHistoryGoBack, "#icons.previous.svg", false);
			CMDEventHistoryForward = new ActionCommand("forw.", currentEventHistoryGoForward, "#icons.forward-arrow.svg", false);
		}
		#endregion

		public void LoadIML (string imlSource) {
			if (CurrentState == Status.Running)
				delSetSource (imlSource);
		}
		Exception currentException;
		public string ErrorMessage = "";
		public bool ServiceIsInError;
		object dbgIFace;
		AssemblyLoadContext crowLoadCtx;
		Assembly crowAssembly, thisAssembly;
		Type dbgIfaceType;

		#region dbgIface delegates
		Action<int, int> delResize;
		Func<int, int, bool> delMouseMove;
		Func<float, bool> delMouseWheelChanged;
		Func<MouseButton, bool> delMouseDown, delMouseUp;
		Func<char, bool> delKeyPress;
		Func<Key, int, Modifier, bool> delKeyDown, delKeyUp;
		FieldInfo fiDbgIFace_IsDirty;
		Action delResetDebugger;
		Action<object, string> delSaveDebugLog;
		Func<ISurface> delGetMainSurface;
		Action<string> delSetSource;
		Action delReloadIml;
		Func<double> delGetZoomFactor;
		Action<double> delSetZoomFactor;


		FieldInfo fiDbg_IncludedEvents, fiDbg_ConsoleOutput, fiDbgIFace_MaxLayoutingTries, fiDbgIFace_MaxDiscardCount, fiDbgIFace_Terminate;
		#endregion

		bool recording, debugLogIsEnabled;
		IList<DbgEvtType> recordedEvents = new ObservableList<DbgEvtType>(new DbgEvtType[] {DbgEvtType.Widget} );
		DbgEvtType addRecordedEvents = DbgEvtType.None;
		public bool HasVkvgBackend { get; private set; }
		public int RefreshRate {
			get => Configuration.Global.Get<int> ("RefreshRate", 10);
			set {
				if (RefreshRate == value)
					return;
				Configuration.Global.Set ("RefreshRate", value);
				NotifyValueChanged(value);
			}
		}
		public int MaxLayoutingTries {
			get => Configuration.Global.Get<int> ("MaxLayoutingTries", 30);
			set {
				if (MaxLayoutingTries == value)
					return;
				Configuration.Global.Set ("MaxLayoutingTries", value);
				NotifyValueChanged(value);
				fiDbgIFace_MaxLayoutingTries.SetValue (null, value);
			}
		}
		public int MaxDiscardCount {
			get => Configuration.Global.Get<int> ("MaxDiscardCount", 5);
			set {
				if (MaxDiscardCount == value)
					return;
				Configuration.Global.Set ("MaxDiscardCount", value);
				NotifyValueChanged(value);
				fiDbgIFace_MaxDiscardCount.SetValue (null, value);
			}
		}
		public bool PreviewHasError => currentException != null;
		public Exception CurrentException {
			get => currentException;
			private set {
				if (currentException == value)
					return;
				currentException = value;
				NotifyValueChanged (currentException);
				NotifyValueChanged ("PreviewHasError", PreviewHasError);
			}
		}
		public bool DebugLogIsEnabled {
			get => debugLogIsEnabled;
			set {
				if (debugLogIsEnabled == value)
					return;
				debugLogIsEnabled = value;
				CMDStartRecording.CanExecute = debugLogIsEnabled & !Recording;
				CMDStopRecording.CanExecute = debugLogIsEnabled & Recording;
				NotifyValueChanged (debugLogIsEnabled);
			}
		}
		public bool Recording {
			get => recording;
			set {
				if (recording == value)
					return;
				recording = IsRunning & DebugLogIsEnabled & value;
				if (recording) {
					fiDbg_IncludedEvents.SetValue (dbgIFace, RecordedEvents.ToList());
					CMDStartRecording.CanExecute = false;
					CMDStopRecording.CanExecute = true;
				} else {
					fiDbg_IncludedEvents.SetValue (dbgIFace, new List<DbgEvtType>());
					CMDStartRecording.CanExecute = debugLogIsEnabled;
					CMDStopRecording.CanExecute = false;
				}
				NotifyValueChanged(recording);
			}
		}
		public IList<DbgEvtType> RecordedEvents {
			get => recordedEvents;
			set {
				if (recordedEvents == value)
					return;
				recordedEvents = value;
				if (Recording)
					fiDbg_IncludedEvents.SetValue (dbgIFace, value);
				NotifyValueChanged (recordedEvents);
			}
		}
		public DbgEvtType AddRecordedEvents {
			get => addRecordedEvents;
			set {
				if (addRecordedEvents == value)
					return;
				addRecordedEvents = value;
				NotifyValueChanged (addRecordedEvents);
			}
		}		
		public string DebugLogFilePath {
			get => Configuration.Global.Get<string> ("DebugLogFilePath");
			set {
				if (DebugLogFilePath == value)
					return;
				Configuration.Global.Set ("DebugLogFilePath", value);
				NotifyValueChanged (value);
			}
		}
		void updateCrowDebuggerState (string errorMsg = null) {
			ErrorMessage = errorMsg;
			ServiceIsInError = errorMsg != null;
			NotifyValueChanged ("ServiceErrorMessage", (object)ErrorMessage);
			NotifyValueChanged ("ServiceIsInError",  ServiceIsInError);
		}

		#region DesignInterface callbacks
		//those methods are called by designed interface
		void getMouseScreenCoordinates (out int x, out int y) {
			x = mouseScreenPos.X;
			y = mouseScreenPos.Y;
		}
		IEnumerable<object> getStyling () {
			if (App.CurrentProject is CERoslynPlugin.SolutionProject sol) {
				if (sol.StartupProject is CERoslynPlugin.MSBuildProject csprj) {
					foreach (var style in csprj.Flatten.OfType<CERoslynPlugin.ProjectItemNode>()
						.Where (pin=>pin.NodeType == NodeType.EmbeddedResource && pin.FullPath.EndsWith (".style", StringComparison.OrdinalIgnoreCase)))
						yield return style.FullPath;
				}
			}
			yield return crowAssembly;
		}
		Stream getStreamFromPath (string path) {
			if (App.CurrentProject is CERoslynPlugin.SolutionProject sol) {
				if (sol.StartupProject is CERoslynPlugin.MSBuildProject csprj) {
					return csprj.GetStreamFromTargetPath (path);
				}
			}
			return null;
		}
		#endregion
		static string defaultCrowAssemblyLocation =>
			System.IO.Path.Combine (System.IO.Path.GetDirectoryName (Assembly.GetEntryAssembly().Location), "Crow.dll");
		public override void Start()
		{
			if (CurrentState == Status.Running)
				return;

			if (!File.Exists (CrowDbgAssemblyLocation))	{
				DebugLogIsEnabled = false;
				updateCrowDebuggerState($"Crow.dll for debugging file not found");
				return;
			}
			List<string> additionalResolvePath = new List<string>();
			additionalResolvePath.Add (System.IO.Path.GetDirectoryName(CrowDbgAssemblyLocation));
			foreach (string assemblyPath in crowAssemblies)
				additionalResolvePath.Add (System.IO.Path.GetDirectoryName(assemblyPath));

			//crowLoadCtx?.Unload();
			crowLoadCtx = new AssemblyLoadContext("CrowDebuggerLoadContext");
			crowLoadCtx.Resolving += (context, assemblyName) => {
				foreach (string path in additionalResolvePath) {
					string assemblyPath = System.IO.Path.Combine (path, assemblyName.Name + ".dll");
					if (!File.Exists (assemblyPath))
						continue;
					return crowLoadCtx.LoadFromAssemblyPath (assemblyPath);
				}
				return null;
			};

			//using (crowLoadCtx.EnterContextualReflection()) {
				crowAssembly = crowLoadCtx.LoadFromAssemblyPath (CrowDbgAssemblyLocation);
				thisAssembly = crowLoadCtx.LoadFromAssemblyPath (new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);

				foreach (string assemblyPath in crowAssemblies)
					crowLoadCtx.LoadFromAssemblyPath (assemblyPath);

				Type debuggerType = crowAssembly.GetType("Crow.DbgLogger");
				DebugLogIsEnabled = (bool)debuggerType.GetField("IsEnabled").GetValue(null);

				dbgIfaceType = thisAssembly.GetType("CECrowPlugin.DebugInterface");

				dbgIFace = Activator.CreateInstance (dbgIfaceType, new object[] {CrowEditBase.CrowEditBase.App.WindowHandle});

				delResize = (Action<int, int>)Delegate.CreateDelegate(typeof(Action<int, int>),
											dbgIFace, dbgIfaceType.GetMethod("Resize"));

				delMouseMove = (Func<int, int, bool>)Delegate.CreateDelegate(typeof(Func<int, int, bool>),
											dbgIFace, dbgIfaceType.GetMethod("OnMouseMove"));

				delMouseWheelChanged = (Func<float, bool>)Delegate.CreateDelegate(typeof(Func<float, bool>),
											dbgIFace, dbgIfaceType.GetMethod("OnMouseWheelChanged"));


				delMouseDown = (Func<MouseButton, bool>)Delegate.CreateDelegate(typeof(Func<MouseButton, bool>),
											dbgIFace, dbgIfaceType.GetMethod("OnMouseButtonDown"));

				delMouseUp = (Func<MouseButton, bool>)Delegate.CreateDelegate(typeof(Func<MouseButton, bool>),
											dbgIFace, dbgIfaceType.GetMethod("OnMouseButtonUp"));

				delKeyDown = (Func<Key, int, Modifier, bool>)Delegate.CreateDelegate(typeof(Func<Key, int, Modifier, bool>),
											dbgIFace, dbgIfaceType.GetMethod("OnKeyDown", new Type[] { typeof(Key), typeof(int), typeof (Modifier)}));
				delKeyUp = (Func<Key, int, Modifier, bool>)Delegate.CreateDelegate(typeof(Func<Key, int, Modifier, bool>),
											dbgIFace, dbgIfaceType.GetMethod("OnKeyUp", new Type[] { typeof(Key), typeof(int), typeof (Modifier)}));
				delKeyPress = (Func<char, bool>)Delegate.CreateDelegate(typeof(Func<char, bool>),
											dbgIFace, dbgIfaceType.GetMethod("OnKeyPress"));


				delGetMainSurface = (Func<ISurface>)Delegate.CreateDelegate(typeof(Func<ISurface>),
											dbgIFace, dbgIfaceType.GetProperty("MainSurface").GetGetMethod());
				delSetSource = (Action<string>)Delegate.CreateDelegate(typeof(Action<string>),
											dbgIFace, dbgIfaceType.GetProperty("Source").GetSetMethod());
				delReloadIml = (Action)Delegate.CreateDelegate(typeof(Action), dbgIFace, dbgIfaceType.GetMethod("ReloadIml"));

				/*delGetZoomFactor = (Func<double>)Delegate.CreateDelegate(typeof(Func<double>),
											dbgIFace, dbgIfaceType.GetProperty("ZoomFactor").GetGetMethod());
				delSetZoomFactor = (Action<double>)Delegate.CreateDelegate(typeof(Action<double>),
											dbgIFace, dbgIfaceType.GetProperty("ZoomFactor").GetSetMethod());*/

				fiDbgIFace_Terminate = dbgIfaceType.GetField("Terminate");
				fiDbgIFace_IsDirty = dbgIfaceType.GetField("IsDirty");
				fiDbgIFace_MaxLayoutingTries = dbgIfaceType.GetField("MaxLayoutingTries", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
				fiDbgIFace_MaxDiscardCount = dbgIfaceType.GetField("MaxDiscardCount", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);

				fiDbg_IncludedEvents = debuggerType.GetField("IncludedEvents");
				fiDbg_ConsoleOutput = debuggerType.GetField("ConsoleOutput");
				delResetDebugger = (Action)Delegate.CreateDelegate(typeof(Action), null, debuggerType.GetMethod("Reset"));
				/*delSaveDebugLog = (Action<object, string>)Delegate.CreateDelegate(typeof(Action<object, string>),
											null, debuggerType.GetMethod("Save", new Type[] {dbgIfaceType, typeof(string)}));*/
				//HasVkvgBackend = (bool)dbgIfaceType.GetField ("HaveVkvgBackend", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetValue (null);
				dbgIfaceType.GetMethod("RegisterDebugInterfaceCallback").Invoke (dbgIFace, new object[] {this} );
				dbgIfaceType.GetMethod("Run").Invoke (dbgIFace, null);

				fiDbgIFace_MaxLayoutingTries.SetValue (null, MaxLayoutingTries);
				fiDbgIFace_MaxDiscardCount.SetValue (null, MaxDiscardCount);

				CurrentState = Status.Running;

				App.ViewCommands.Add (CMDViewPreview);

				updateCrowDebuggerState();

				//delSetZoomFactor (ZoomFactor);
		}
		public override void Stop()
		{
			if (CurrentState == Status.Running)
				fiDbgIFace_Terminate.SetValue (dbgIFace, true);
			App.ViewCommands.Remove (CMDViewPreview);

			Recording = false;
			DebugLogIsEnabled = false;
			crowLoadCtx = null;
			CurrentState = Status.Stopped;
		}
		public override void Pause()
		{
			CurrentState = Status.Paused;
		}
		public override string ConfigurationWindowPath => "#CECrowPlugin.ui.winConfiguration.crow";
		Project activeSolution;
		public Project ActiveSolution {
			get => activeSolution;
			set {
				//CERoslynPlugin.SolutionProject sol = value as CERoslynPlugin.SolutionProject;
				if (activeSolution == value)
					return;
				activeSolution = value;
				NotifyValueChanged (activeSolution);
			}
		}

		public string CrowDbgAssemblyLocation {
			get => Configuration.Global.Get<string> ("CrowDbgAssemblyLocation", defaultCrowAssemblyLocation);
			set {
				if (CrowDbgAssemblyLocation == value)
					return;
				Configuration.Global.Set ("CrowDbgAssemblyLocation", value);
				NotifyValueChanged(value);
			}
		}
		public double ZoomFactor {
			get => Configuration.Global.Get<Double> ("CrowPreviewZoomFactor", 1.0);
			set {
				if (ZoomFactor == value)
					return;
				Configuration.Global.Set<Double> ("CrowPreviewZoomFactor", value);
				NotifyValueChanged (value);
				if (CurrentState == Status.Running)
					delSetZoomFactor (value);
			}
		}
		//assemblies with crow resources in order of loading
		IList<string> crowAssemblies = new ObservableList<string> ();
		public IList<string> CrowAssemblies => crowAssemblies;
		void saveCrowAssemblies () {
			if (crowAssemblies.Count > 0)
				Configuration.Global.Set ("CrowAssemblies", crowAssemblies.Aggregate ((a, b)=> $"{a};{b}"));
			else
				Configuration.Global.Set ("CrowAssemblies", "");
		}
		void loadCrowAssemblies () {
			crowAssemblies.Clear ();
			if (!Configuration.Global.TryGet<string> ("CrowAssemblies", out string assemblies))
				return;
			foreach (string a in assemblies.Split (';'))
				crowAssemblies.Add (a);
		}

		protected override void onStateChange(Status previousState, Status newState)
		{
			base.onStateChange(previousState, newState);
			CMDRefresh.CanExecute = IsRunning;
		}
		
		#region Mouse & Keyboard		
		Point mouseScreenPos;//absolute on screen position.
		public void onKeyDown(KeyEventArgs e)
		{
			if (CurrentState == Status.Running) {
				try
				{
					e.Handled = delKeyDown (e.Key, e.ScanCode, e.Modifiers);//KeyEventArgs being defined in Crow...
				}
				catch (System.Exception ex)
				{
					Console.WriteLine($"[Error][DebugIFace key down]{ex}");
				}
			}
		}
		public void onKeyUp(KeyEventArgs e)
		{
			if (CurrentState == Status.Running) {
				try
				{
					e.Handled = delKeyUp (e.Key, e.ScanCode, e.Modifiers);
				}
				catch (System.Exception ex)
				{
					Console.WriteLine($"[Error][DebugIFace key up]{ex}");
				}
			}
		}
		public void onKeyPress(KeyPressEventArgs e)
		{
			if (CurrentState == Status.Running) {
				try
				{
					e.Handled = delKeyPress (e.KeyChar);
				}
				catch (System.Exception ex)
				{
					Console.WriteLine($"[Error][DebugIFace key press]{ex}");
				}
			}
		}
		public void onMouseMove(Point _mouseScreenPos, MouseMoveEventArgs e)
		{
			if (CurrentState == Status.Running) {
				try
				{
					mouseScreenPos = _mouseScreenPos;//absolute on screen position.
					e.Handled = delMouseMove ((int)(e.X / ZoomFactor), (int)(e.Y / ZoomFactor));//DebugInterface local coordinate for mouse.
				}
				catch (System.Exception ex)
				{
					Console.WriteLine($"[Error][DebugIFace mouse move]{ex}");
				}
			}
		}
		public void onMouseDown(MouseButtonEventArgs e)
		{
			if (CurrentState == Status.Running) {
				try
				{
					e.Handled = delMouseDown (e.Button);
				}
				catch (System.Exception ex)
				{
					Console.WriteLine($"[Error][DebugIFace mouse down]{ex}");
				}
			}
		}
		public void onMouseUp(MouseButtonEventArgs e)
		{
			if (CurrentState == Status.Running) {
				try
				{
					e.Handled = delMouseUp (e.Button);
				}
				catch (System.Exception ex)
				{
					Console.WriteLine($"[Error][DebugIFace mouse up]{ex}");
				}
			}
		}
		public void onMouseWheel(MouseWheelEventArgs e)
		{
			if (CurrentState == Status.Running) {
				try
				{
					e.Handled = delMouseWheelChanged (e.Delta);
				}
				catch (System.Exception ex)
				{
					Console.WriteLine($"[Error][DebugIFace mouse wheel change]{ex}");
				}
			}
		}
		#endregion

		public ISurface MainSurface => IsRunning ? delGetMainSurface() : null;
		public void Resize (int width, int height) {
			if (IsRunning)
				delResize (width, height);
		}
		public void ResetDirtyState () {
			if (IsRunning)
				fiDbgIFace_IsDirty.SetValue (dbgIFace, false);
		}
		public bool GetDirtyState => IsRunning ? (bool)fiDbgIFace_IsDirty.GetValue (dbgIFace) : false;

		#region Debug log
		IList<DbgEvent> events;
		IList<DbgWidgetRecord> widgets;
		public IList<DbgEvent> Events {
			get => events;
			set {
				if (events == value)
					return;
				events = value;
				NotifyValueChanged (nameof (Events), events);
			}
		}
		public IList<DbgWidgetRecord> Widgets {
			get => widgets;
			set {
				if (widgets == value)
					return;
				widgets = value;
				NotifyValueChanged (nameof (Widgets), widgets);
			}
		}
		void refresh () {
			if (!IsRunning)
				Start ();
			if (IsRunning)
				delReloadIml ();
			//updateCrowApp();
		}
		void stopRecording () {
			if (!Recording)
				return;
			Recording = false;
			getLog ();
			CrowEditBase.CrowEditBase.App.LoadWindow ("#CECrowPlugin.ui.winDebugLog.crow", this);
		}
		int firstWidgetIndexToGet = 0;
		public object LogMutex = new object ();
		void getLog () {

			using (Stream stream = new MemoryStream (1024)) {
				Type debuggerType = crowAssembly.GetType("Crow.DbgLogger");
				MethodInfo miSave = debuggerType.GetMethod("Save",
					new Type[] {
						dbgIfaceType,
						typeof(Stream),
						typeof(int),
						typeof(bool)
					});


				List<DbgWidgetRecord> widgets = new List<DbgWidgetRecord>();
				List<DbgEvent> events = new List<DbgEvent>();
				miSave.Invoke(null, new object[] {dbgIFace, stream, firstWidgetIndexToGet, true});
				stream.Seek(0, SeekOrigin.Begin);
				DbgLogger.Load (stream, events, widgets);

				lock (LogMutex) {
					for (int i = 0; i < widgets.Count; i++) {
						widgets[i].listIndex = i;
						//Widgets.Add	(widgets[i]);
					}
					for (int i = 0; i < events.Count; i++) {
						//Events.Add (events[i]);
						updateWidgetEvents (widgets, events[i]);
					}
				}
				Events = events;
				Widgets = widgets;
				firstWidgetIndexToGet += widgets.Count;
				/*if (widgets.Count > 0 && firstWidgetIndexToGet != widgets.Last().InstanceIndex + 1)
					Debugger.Break ();*/
			}
		}
		void updateWidgetEvents (IList<DbgWidgetRecord> widgets, DbgEvent evt) {
			if (evt is DbgWidgetEvent we)
				widgets.FirstOrDefault (w => w.InstanceIndex == we.InstanceIndex)?.Events.Add (we);
			if (evt.Events == null)
				return;
			foreach (DbgEvent e in evt.Events)
				updateWidgetEvents (widgets, e);
		}
		void saveLogToDebugLogFilePath () {

		}
		void loadLogFromDebugLogFilePath () {

		}

		DbgEvent curEvent;
		bool disableCurrentEventHistory;
		Stack<DbgEvent> CurrentEventHistoryForward = new Stack<DbgEvent>();
		Stack<DbgEvent> CurrentEventHistoryBackward = new Stack<DbgEvent>();
		DbgWidgetRecord curWidget = new DbgWidgetRecord();
		public string[] AllEventTypes => Enum.GetNames (typeof(DbgEvtType));
		string searchEventType;
		DbgWidgetRecord searchWidget;
		public string SearchEventType {
			get => searchEventType;
			set {
				if (searchEventType == value)
					return;
				searchEventType = value;
				NotifyValueChanged (searchEventType);
			}
		}

		public DbgWidgetRecord SearchWidget {
			get => searchWidget;
			set {
				if (searchWidget == value)
					return;
				searchWidget = value;
				NotifyValueChanged (searchWidget);
			}
		}
		public DbgEvent CurrentEvent {
			get => curEvent;
			set {
				if (curEvent == value)
					return;

				if (!disableCurrentEventHistory) {
					CurrentEventHistoryForward.Clear ();
					CMDEventHistoryForward.CanExecute = false;
					if (!(value == null || curEvent == null)) {
						CurrentEventHistoryBackward.Push (curEvent);
						CMDEventHistoryBackward.CanExecute = true;
					}
				}

				curEvent = value;

				NotifyValueChanged (nameof (CurrentEvent), curEvent);
				NotifyValueChanged ("CurEventChildEvents", curEvent?.Events);
				NotifyValueChanged ("CurWidgetProperties", CurWidgetProperties);

				if (CurrentEvent != null && CurrentEvent.parentEvent != null)
					CMDGotoParentEvent.CanExecute = true;
				else
					CMDGotoParentEvent.CanExecute = false;
			}
		}
		void currentEventHistoryGoBack () {
			disableCurrentEventHistory = true;
			if (CurrentEvent != null) {
				CurrentEventHistoryForward.Push (CurrentEvent);
				CMDEventHistoryForward.CanExecute = true;
			}
			CurrentEvent = CurrentEventHistoryBackward.Pop ();
			CMDEventHistoryBackward.CanExecute = CurrentEventHistoryBackward.Count > 0;

			disableCurrentEventHistory = false;
		}

		void currentEventHistoryGoForward () {
			disableCurrentEventHistory = true;
			CurrentEventHistoryBackward.Push (CurrentEvent);
			CMDEventHistoryBackward.CanExecute = true;
			CurrentEvent = CurrentEventHistoryForward.Pop ();
			CMDEventHistoryForward.CanExecute = CurrentEventHistoryForward.Count > 0;

			disableCurrentEventHistory = false;
		}

		public DbgWidgetRecord CurrentWidget {
			get => curWidget;
			set {
				if (curWidget == value)
					return;
				curWidget = value;
				NotifyValueChanged (nameof (CurrentWidget), curWidget);
				NotifyValueChanged ("CurWidgetRootEvents", curWidget?.RootEvents);
				NotifyValueChanged ("CurrentWidgetEvents", curWidget?.Events);
				NotifyValueChanged ("CurWidgetProperties", CurWidgetProperties);
			}
		}
		public List<DbgWidgetEvent> CurWidgetRootEvents => curWidget == null? new List<DbgWidgetEvent>() : curWidget.RootEvents;

		public IEnumerable<KeyValuePair<string, string>> CurWidgetProperties {
			get {
				if (curWidget == null)
					return null;
				long endTime = curEvent == null ? long.MaxValue : curEvent.end;
				Dictionary<string, string> result = new Dictionary<string, string> ();
				foreach (DbgWidgetEvent evt in curWidget?.Events?.Where (e => e.type == DbgEvtType.GOSetProperty && e.begin <= endTime)){
					string[] tmp = evt.Message.Split('=');
					if (result.ContainsKey (tmp[0]))
						result[tmp[0]] = tmp[1];
					else
						result.Add (tmp[0], tmp[1]);
				}
				return result;
			}
		}
		#endregion
	}
}