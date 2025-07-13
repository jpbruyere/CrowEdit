// Copyright (c) 2021-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;
using System.Linq;
using Crow;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Runtime.Loader;
using System.Text;
using Drawing2D;

namespace CrowEditBase
{
	public static class Extensions {
		public static bool TryCast<T>(this object o, out T result) {
			result = default;
			if (o != null) {
				Type tIn = o.GetType();
				Type tOut = typeof(T);
				if (tOut.IsAssignableFrom(tIn)) {
					result = (T)o;
					return true;
				}
			}
			return false;
		}
	}
	public abstract class CrowEditBase : Interface {
		public static CrowEditBase App;
		public CrowEditBase (int width, int height, bool singleThreaded = true) : base (width, height, singleThreaded) {
			App = this;
			MainLog = GetLog("CrowEdit");
			MainLog.IsOpened = true;
			Log(LogType.Normal,"Crow edit started");
		}

		protected DockStack mainDock;
		protected const string _defaultFileName = "unnamed.txt";
		Document currentDocument;
		Editor currentEditor;
		Project currentProject;
		public CommandGroup CommandsRoot, FileCommands, EditCommands, ViewCommands;
		public ObservableList<Document> OpenedDocuments = new ObservableList<Document> ();
		public ObservableList<Service> Services = new ObservableList<Service> ();
		public ObservableList<Plugin> Plugins = new ObservableList<Plugin> ();
		public ObservableList<Project> Projects = new ObservableList<Project> ();


		#region logging
		LogItem currentLog;
		public LogItem CurrentLog {
			get => currentLog;
			set {
				if (currentLog == value)
					return;

				if (currentLog != null)
					currentLog.IsSelected = false;

				currentLog = value;
				NotifyValueChanged (currentLog);

				if (currentLog == null)
					return;

				currentLog.IsSelected = true;
			}
		}		
		public ObservableList<LogItem> Logs = new ObservableList<LogItem>();
		public ObservableList<LogItem> OpenedLogs = new ObservableList<LogItem>();
		internal LogItem MainLog;
		public void Log(LogType type, string message) {
			MainLog.Add (type, message);
		}
		public void ResetLog () {
			MainLog.ResetLog();
		}
		public LogItem GetLog(string name) {
			LogItem li = Logs.FirstOrDefault(l=>string.Equals(l.Name,name,StringComparison.OrdinalIgnoreCase));
			if (li == null) {
				li = new LogItem(name);
				lock (Logs)	{
					lock(UpdateMutex)
						Logs.Add(li);
				}
			}
			return li;
		}
		internal void OpenLog(LogItem li) {
			lock(OpenedLogs)
				OpenedLogs.Add(li);
		}
		internal void CloseLog(LogItem li) {
			lock(OpenedLogs) {
				if (li.IsSelected) {
					int idx = OpenedLogs.IndexOf(li);
					OpenedLogs.RemoveAt(idx);
					int count = OpenedLogs.Count();
					if (idx < count)
						OpenedLogs[idx].IsSelected = true;
					else if (count > 0)
						OpenedLogs[count-1].IsSelected = true;
				} else
					OpenedLogs.Remove(li);
			}
		}

		#endregion

		#region File associations and supported editors
		protected Dictionary<string, List<Type>> FileAssociations = new Dictionary<string, List<Type>> ();
		protected Dictionary<string, List<string>> SupportedEditors = new Dictionary<string, List<string>> ();
		public void AddFileAssociation (string extension, Type clientClass) {
			if (!FileAssociations.ContainsKey (extension))
				FileAssociations.Add (extension, new List<Type> ());
			if (!FileAssociations[extension].Contains (clientClass))
				FileAssociations[extension].Add (clientClass);
			NotifyValueChanged ("EditorItemTemplates", (object)EditorItemTemplates);
		}
		public void RemoveFileAssociationByType (Type clientClass) {

			//FileAssociations.Values Where (t=>t == clientClass);
		}
		public bool TryGetDefaultTypeForExtension (string extension, out Type clientType) {
			clientType = FileAssociations.ContainsKey (extension) ? FileAssociations[extension].FirstOrDefault () : null;
			return clientType != null;
		}
		public void AddSupportedEditor (string extension, string editorPath) {
			if (!SupportedEditors.ContainsKey (extension))
				SupportedEditors.Add (extension, new List<string> ());
			if (!SupportedEditors[extension].Contains (editorPath))
				SupportedEditors[extension].Add (editorPath);
			NotifyValueChanged ("EditorItemTemplates", (object)EditorItemTemplates);
		}
		public bool TryGetDefaultEditorForDocumentType (string extension, out string editorPath) {
			editorPath = SupportedEditors.ContainsKey (extension) ? SupportedEditors[extension].FirstOrDefault () : null;
			return editorPath != null;
		}
		#endregion

		public T GetService<T> () where T : Service {
			T service = Services.OfType<T>().FirstOrDefault ();
			if (service == null) {
				service = Activator.CreateInstance<T> ();
				Services.Add (service);
			}
			return service;
		}
		public Service GetService (Type serviceType) {
			Service service = Services.FirstOrDefault (s => s.GetType() == serviceType);
			if (service == null) {
				service = (Service)Activator.CreateInstance (serviceType);
				Services.Add (service);
			}
			return service;
		}
		public bool TryGetPlugin (string pluginName, out Plugin plugin) {
			plugin = Plugins.FirstOrDefault (p=>p.Name == pluginName);
			return plugin != null;
		}
		//TODO:flattened project
		public IEnumerable<Project> FlattenProjects {
			get {
				foreach (var node in Projects.SelectMany (child => child.FlattenProjetcs))
					yield return node;
			}
		}
		public bool TryGetProject<T> (string projectFullPath, out T proj) where T : Project {
			proj = FlattenProjects.FirstOrDefault (p=>p.FullPath == projectFullPath) as T;
			return proj != null;
		}
		public bool TryGetProject (string projectFullPath, out Project proj) {
			proj = FlattenProjects.FirstOrDefault (p=>p.FullPath == projectFullPath);
			return proj != null;
		}
		public bool TryGetContainingProject (string fullPath, out Project containingProject) {
			containingProject = FlattenProjects.FirstOrDefault (p => p.ContainsFile (fullPath));
			return containingProject != null;
		}
		public bool TryFindFileNode (string fullPath, out IFileNode node) {
			foreach	 (Project prj in Projects) {
				if (prj.TryFindFileNode (fullPath, out IFileNode n)) {
					node = n;
					return true;
				}
			}
			node = null;
			return false;
		}

		public CommandGroup SyntaxViewCommands => 
			currentDocument is SourceDocument src ? new CommandGroup (src.CMDRefreshSyntaxTree) : null;
		public Document CurrentDocument {
			get => currentDocument;
			set {
				if (currentDocument == value)
					return;

				if (currentDocument != null)
					currentDocument.IsSelected = false;

				currentDocument = value;
				NotifyValueChanged (currentDocument);

				if (currentDocument == null)
					return;

				currentDocument.IsSelected = true;
				FileCommands[2] = currentDocument.CMDSave;
				FileCommands[3] = currentDocument.CMDSaveAs;
				EditCommands[0] = currentDocument.CMDUndo;
				EditCommands[1] = currentDocument.CMDRedo;

				NotifyValueChanged("SyntaxViewCommands", SyntaxViewCommands);
			}
		}
		public Project CurrentProject {
			get => currentProject;
			set {
				if (currentProject == value)
					return;
				currentProject = value;
				NotifyValueChanged (currentProject);
			}
		}
		public Editor CurrentEditor {
			get => currentEditor;
			set {
				if (currentEditor == value)
					return;
				currentEditor = value;
				NotifyValueChanged (currentEditor);

				if (currentEditor == null)
					return;
				EditCommands[2] = currentEditor.CMDCut;
				EditCommands[3] = currentEditor.CMDCopy;
				EditCommands[4] = currentEditor.CMDPaste;
			}
		}
		SyntaxException currentException;
		public SyntaxException CurrentException {
			get => currentException;
			set {
				if (currentException == value)
					return;
				currentException = value;
				NotifyValueChanged(currentException);
			}
		} 
		public string CurrentDir {
			get => Configuration.Global.Get<string>("CurrentDir");
			set {
				if (CurrentDir == value)
					return;
				Configuration.Global.Set ("CurrentDir", value);
				NotifyValueChanged (CurrentDir);
			}
		}
		public string PluginsDirectory {
			get => Configuration.Global.Get<string>("PluginsDirectory", defaultPluginsDirectory);
			set {
				if (PluginsDirectory == value)
					return;
				Configuration.Global.Set ("PluginsDirectory", value);
				NotifyValueChanged (PluginsDirectory);
			}
		}
		public string CurrentFilePath {
			get => Configuration.Global.Get<string> ("CurrentFilePath");
			set {
				if (CurrentFilePath == value)
					return;
				Configuration.Global.Set ("CurrentFilePath", value);
				NotifyValueChanged (CurrentFilePath);
			}
		}
		public string CurFileName {
			get => string.IsNullOrEmpty (CurrentFilePath) ? _defaultFileName : Path.GetFileName (CurrentFilePath);
		}
		public string CurFileDir {
			get => string.IsNullOrEmpty (CurrentFilePath) ? CurrentDir : Path.GetDirectoryName (CurrentFilePath);
		}


		public bool IsOpened (string filePath) =>
			string.IsNullOrEmpty (filePath) ? false : OpenedDocuments.Any (d => d.FullPath == filePath);
		public bool TryGetOpenedDocument (string fullPath, out Document doc) {
			doc = OpenedDocuments.FirstOrDefault (d => d.FullPath == fullPath);
			return doc != null;
		}

		public Document OpenFile (string filePath) {
			if (string.IsNullOrEmpty (filePath))
				return null;
			Document doc = OpenedDocuments.FirstOrDefault (d => d.FullPath == filePath);
			return doc ?? openOrCreateFile (filePath);
		}
		public void CloseFile (string filePath) =>
			CloseDocument (OpenedDocuments.FirstOrDefault (d => d.FullPath == filePath));
		public void CloseOthers (string filePath) {
			foreach (Document doc in OpenedDocuments.Where (d => d.FullPath != filePath))
				CloseDocument (doc);
		}
		public void CloseOthers (Document document) {
			Document[] docs = OpenedDocuments.Where (d => d != document).ToArray();
			lock (UpdateMutex) {
				foreach (Document doc in docs)
					CloseDocument (doc);
			}
		}

		public void createNewFile(){
			openOrCreateFile (Path.Combine (CurFileDir, _defaultFileName));
		}

		protected abstract Document openOrCreateFile (string filePath, string editorPath = null);
		public void CloseDocument (Document doc) {
			if (doc == null)
				return;
			int idx = OpenedDocuments.IndexOf (doc);
			OpenedDocuments.Remove (doc);
			doc.CloseEvent -= onQueryCloseDocument;
			if (CurrentDocument == null && OpenedDocuments.Count > 0)
				CurrentDocument = OpenedDocuments[Math.Min (idx, OpenedDocuments.Count - 1)];
		}
		protected void onQueryCloseDocument (object sender, EventArgs e) {
			Document doc = sender as Document;
			if (doc.IsDirty) {
				MessageBox mb = MessageBox.ShowModal (this,
					                MessageBox.Type.YesNoCancel, $"{doc.FileName} has unsaved changes.\nSave it now?");
				mb.Yes += (object _sender, EventArgs _e) => { doc.Save (); CloseDocument (doc); };
				mb.No += (object _sender, EventArgs _e) => CloseDocument (doc);
			} else
				CloseDocument (doc);
		}

		public Window LoadWindow (string path, object dataSource = null){
			try {
				Widget g = FindByName (path);
				if (g != null)
					return g as Window;
				if (TryGetConfigFromFloatingWinConfigs(path, out string floatingConfig))
					g = DockWindow.CreateFromFloatingConfigString(this, floatingConfig);
				else
					g = Load (path);
				g.Name = path;
				g.DataSource = dataSource;
				return g as Window;
			} catch (Exception ex) {
				Log (LogType.Error, ex.ToString ());
			}
			return null;
		}
		public bool TryGetWindow (string path, out Window window) {
			window = FindByName (path) as Window;
			return window != null;
		}
		public void CloseWindow (string path){
			Widget g = FindByName (path);
			if (g is DockWindow dockwin) {
				if (dockwin.IsFloating)
					saveWinConfigs();
			}
			if (g != null)
				DeleteWidget (g);
			
		}
		void saveFloatingWinConfigs() {
			StringBuilder floatings = new StringBuilder (512);
			DockWindow[] floatingWins = GraphicTree.OfType<DockWindow> ().ToArray ();
			if (floatingWins.Length > 0) {
				for (int i = 0; i < floatingWins.Length - 1; i++) {
					floatings.Append (floatingWins[i].FloatingConfigString);
					floatings.Append ('|');
				}
				floatings.Append (floatingWins[floatingWins.Length - 1].FloatingConfigString);
			}
			Configuration.Global.Set ("FloatingWinConfigs", floatings.ToString ());
		}
		protected bool TryGetConfigFromFloatingWinConfigs(string winPath, out string conf) {
			if (Configuration.Global.TryGet<string>("FloatingWinConfigs", out conf) && !string.IsNullOrEmpty(conf)) {
				string[] floatings = conf.Split ('|');
				for (int i = 0; i < floatings.Length; i++) {
					if (floatings[i].Split(';')[0] == winPath) {
						conf = floatings[i];
						return true;
					}
				}				
			}
			return false;
		}
		protected void saveWinConfigs() {
			Configuration.Global.Set ("WinConfigs", mainDock.ExportConfig ());

			saveFloatingWinConfigs();

			Configuration.Global.Save ();
		}
		protected void reloadWinConfigs() {

			if (Configuration.Global.TryGet<string>("WinConfigs", out string conf) && !string.IsNullOrEmpty(conf))
				mainDock.ImportConfig (conf, this);
			if (Configuration.Global.TryGet<string>("FloatingWinConfigs", out conf) && !string.IsNullOrEmpty(conf)) {
				string[] floatings = conf.Split ('|');
				for (int i = 0; i < floatings.Length; i++)
					DockWindow.CreateFromFloatingConfigString (this, floatings[i], this);
			}
		}
		protected void reloadLogsConfigs() {

			if (Configuration.Global.TryGet<string>("OpenedLogs", out string conf) && !string.IsNullOrEmpty(conf)) {
				string[] logs = conf.Split ('|');
				for (int i = 0; i < logs.Length; i++)
					App.GetLog(logs[i]).IsOpened = true;
				if (Configuration.Global.TryGet<string>("CurrentLog", out string curLog) && !string.IsNullOrEmpty(curLog))
					App.GetLog(curLog).IsSelected = true;
			}
		}	
		protected void saveLogsConfig() {
			lock (OpenedLogs) {
				string openLogs = OpenedLogs.Count > 0 ?
					OpenedLogs.Select(li=>li.Name).Aggregate((a,b) => a + "|" + b) : null;
				Configuration.Global.Set("OpenedLogs", openLogs);
				Configuration.Global.Set("CurrentLog", CurrentLog?.Name);
			}
		}
		public ActionCommand CMDOptions_SelectPluginsDirectory => new ActionCommand ("...",
			() => {
				FileDialog dlg = App.LoadIMLFragment<FileDialog> (@"
				<FileDialog Caption='Select CrowEdit Directory' CurrentDirectory='{PluginsDirectory}'
							ShowFiles='false' ShowHidden='true' />");
				dlg.OkClicked += (sender, e) => PluginsDirectory = (sender as FileDialog).SelectedFileFullPath;
				dlg.DataSource = this;
			}
		);
		public ActionCommand CMDOptions_ResetPluginsDirectory => new ActionCommand ("Reset",
			() => {
				PluginsDirectory = defaultPluginsDirectory;
			}
		);
		static string defaultPluginsDirectory =>
			Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), ".config", "CrowEdit", "plugins");
		protected void loadPlugins () {
			Log(LogType.Message, $"Searching for plugins in {PluginsDirectory}");

			if (!Directory.Exists (PluginsDirectory)) {
				Log(LogType.Error, $"Plugins directory not found: {PluginsDirectory}");
				return;
			}
				
			List<Plugin> pluginsToReload = new List<Plugin>();
			foreach (string pluginDir in Directory.GetDirectories (PluginsDirectory)) {
				Plugin plugin = new Plugin (pluginDir);
				Plugins.Add (plugin);
				if (!plugin.Load ())
					pluginsToReload.Add(plugin);
				
			}
			foreach (Plugin p in pluginsToReload) {
				p.Unload();
				if (!p.Load())
					App.Log(LogType.Warning, $"Plugin load failed: {p.Name}");
			}
				
		}
		public IEnumerable<AssemblyLoadContext> AllLoadContexts =>
			System.Runtime.Loader.AssemblyLoadContext.All;


		#region Editor item templates
		public string EditorItemTemplates {
			get {
				StringBuilder sb = new StringBuilder (1024);
				sb.Append (defaultEditorITemps);
				foreach	(string editorPath in SupportedEditors.Values.SelectMany (a=>a).Distinct ())
					sb.Append ($"<ItemTemplate Path='{editorPath}' DataTest='EditorPath' DataType='{editorPath}'/>");
				return sb.ToString ();
			}
		}
		string defaultEditorITemps = @"
			<ItemTemplate>
				<ListItem IsVisible='{IsSelected}' IsSelected='{²IsSelected}' Selected=""{/tb.HasFocus='true'}"">
					<VerticalStack Spacing='0'>
						<HorizontalStack Spacing='0' Background='White'>
							<Editor Name='tb' Font='consolas, 12' Margin='5'
									Document='{}' TextChanged='onTextChanged'/>
							<ScrollBar Value='{²../tb.ScrollY}'
									LargeIncrement='{../tb.PageHeight}' SmallIncrement='1'
									CursorRatio='{../tb.ChildHeightRatio}' Maximum='{../tb.MaxScrollY}' />
						</HorizontalStack>
						<ScrollBar Style='HScrollBar' Value='{²../tb.ScrollX}'
								LargeIncrement='{../tb.PageWidth}' SmallIncrement='1'
								CursorRatio='{../tb.ChildWidthRatio}' Maximum='{../tb.MaxScrollX}' />
						<HorizontalStack Height='Fit' Spacing='3'>
							<Widget Width='Stretched'/>
							<Label Text='Line:' Foreground='Grey'/>
							<Label Text='{../../tb.CurrentLine}' Margin='3'/>
							<Label Text='col:' Foreground='Grey'/>
							<Label Text='{../../tb.TabulatedColumn}' Margin='3'/>
						</HorizontalStack>
					</VerticalStack>
				</ListItem>
			</ItemTemplate>
		";
		#endregion

		#region main options
		public int CrowUpdateInterval {
			get => Crow.Interface.UPDATE_INTERVAL;
			set {
				if (Crow.Interface.UPDATE_INTERVAL == value)
					return;
				Crow.Interface.UPDATE_INTERVAL = value;
				NotifyValueChanged (Crow.Interface.UPDATE_INTERVAL);
			}
		}
		public int CrowPollingInterval {
			get => Crow.Interface.POLLING_INTERVAL;
			set {
				if (Crow.Interface.POLLING_INTERVAL == value)
					return;
				Crow.Interface.POLLING_INTERVAL = value;
				NotifyValueChanged (Crow.Interface.POLLING_INTERVAL);
			}
		}
		public virtual Color MarginBackground {
			get => Configuration.Global.Get<Color> ("MarginBackground", Colors.Onyx);
			set {
				if (value == MarginBackground)
					return;
				Configuration.Global.Set ("MarginBackground", value);
				NotifyValueChanged ("MarginBackground", value);

				CurrentEditor?.RegisterForRedraw ();
			}
		}
		public bool PrintLineNumbers {
			get => Configuration.Global.Get<bool> ("PrintLineNumbers", true);
			set {
				if (PrintLineNumbers == value)
					return;
				Configuration.Global.Set ("PrintLineNumbers", value);
				NotifyValueChanged ("PrintLineNumbers", PrintLineNumbers);

				CurrentEditor?.RegisterForGraphicUpdate ();
			}
		}
		public bool ShowWhiteSpace {
			get => Configuration.Global.Get<bool> ("ShowWhiteSpace", false);
			set {
				if (ShowWhiteSpace == value)
					return;
				Configuration.Global.Set ("ShowWhiteSpace", value);
				NotifyValueChanged ("ShowWhiteSpace", ShowWhiteSpace);

				CurrentEditor?.RegisterForGraphicUpdate ();
			}
		}
		public bool IndentWithSpace {
			get => Configuration.Global.Get<bool> ("IndentWithSpace", false);
			set {
				if (IndentWithSpace == value)
					return;
				Configuration.Global.Set ("IndentWithSpace", value);
				NotifyValueChanged ("IndentWithSpace", IndentWithSpace);
			}
		}
		public int TabulationSize {
			get => Configuration.Global.Get<int> ("TabulationSize", 4);
			set {
				if (TabulationSize == value)
					return;
				Configuration.Global.Set ("TabulationSize", value);
				NotifyValueChanged ("TabulationSize", TabulationSize);
			}
		}

		//Folding
		public bool FoldingEnabled {
			get => Crow.Configuration.Global.Get<bool> ("FoldingEnabled", true);
			set {
				if (FoldingEnabled == value)
					return;
				Crow.Configuration.Global.Set ("FoldingEnabled", value);
				NotifyValueChanged (value);
			}
		}
		public bool AutoFoldRegions {
			get => Crow.Configuration.Global.Get<bool> ("AutoFoldRegions", true);
			set {
				if (AutoFoldRegions == value)
					return;
				Crow.Configuration.Global.Set ("AutoFoldRegions", value);
				NotifyValueChanged (value);
			}
		}
		public bool AutoFoldComments {
			get => Crow.Configuration.Global.Get<bool> ("AutoFoldComments", true);
			set {
				if (AutoFoldComments == value)
					return;
				Crow.Configuration.Global.Set ("AutoFoldComments", value);
				NotifyValueChanged (value);
			}
		}

		#endregion
	}
}