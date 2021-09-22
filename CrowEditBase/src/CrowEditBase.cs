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

namespace CrowEditBase
{
	public abstract class CrowEditBase : Interface {
		protected Dictionary<string, List<Type>> FileAssociations = new Dictionary<string, List<Type>> ();
		protected Dictionary<Type, List<string>> SupportedEditors = new Dictionary<Type, List<string>> ();
		ObservableList<LogEntry> logs = new ObservableList<LogEntry>();
		public ObservableList<LogEntry> MainLog => logs;

		public void Log(LogType type, string message) {
			lock (logs)
				logs.Add (new LogEntry(type, message));
		}
		public void ResetLog () {
			lock (logs)
				logs.Clear ();
		}

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
		public void AddSupportedEditor (Type clientClass, string editorPath) {
			if (!SupportedEditors.ContainsKey (clientClass))
				SupportedEditors.Add (clientClass, new List<string> ());
			if (!SupportedEditors[clientClass].Contains (editorPath))
				SupportedEditors[clientClass].Add (editorPath);
			NotifyValueChanged ("EditorItemTemplates", (object)EditorItemTemplates);
		}
		public bool TryGetDefaultEditorForDocumentType (Type clientType, out string editorPath) {
			editorPath = SupportedEditors.ContainsKey (clientType) ? SupportedEditors[clientType].FirstOrDefault () : null;
			return editorPath != null;
		}



		public static CrowEditBase App;
		public CrowEditBase (int width, int height) : base (width, height) {
			App = this;
		}

		protected const string _defaultFileName = "unnamed.txt";

		Document currentDocument;
		Editor currentEditor;
		Project currentProject;
		public CommandGroup CommandsRoot, FileCommands, EditCommands, ViewCommands;
		public ObservableList<Document> OpenedDocuments = new ObservableList<Document> ();
		public ObservableList<Service> Services = new ObservableList<Service> ();
		public ObservableList<Plugin> Plugins = new ObservableList<Plugin> ();
		public ObservableList<Project> Projects = new ObservableList<Project> ();
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
				foreach (var node in Projects.SelectMany (child => child.FlattenSubProjetcs))
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
		public string CurrentDir {
			get => Configuration.Global.Get<string>("CurrentDir");
			set {
				if (CurrentDir == value)
					return;
				Configuration.Global.Set ("CurrentDir", value);
				NotifyValueChanged (CurrentDir);
			}
		}
		public string PluginsDirecory {
			get => Configuration.Global.Get<string>("PluginsDirecory");
			set {
				if (PluginsDirecory == value)
					return;
				Configuration.Global.Set ("PluginsDirecory", value);
				NotifyValueChanged (PluginsDirecory);
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
			if (doc == CurrentDocument) {
				if (OpenedDocuments.Count > 0)
					CurrentDocument = OpenedDocuments[Math.Min (idx, OpenedDocuments.Count - 1)];
				else
					CurrentDocument = null;
			}
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
				g = Load (path);
				g.Name = path;
				g.DataSource = dataSource;
				return g as Window;
			} catch (Exception ex) {
				Console.WriteLine (ex.ToString ());
			}
			return null;
		}
		public bool TryGetWindow (string path, out Window window) {
			window = FindByName (path) as Window;
			return window != null;
		}
		public void CloseWindow (string path){
			Widget g = FindByName (path);
			if (g != null)
				DeleteWidget (g);
		}


		protected void loadPlugins () {
			if (string.IsNullOrEmpty (PluginsDirecory))
				PluginsDirecory = Path.Combine (
					Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), ".config", "CrowEdit", "plugins");

			foreach (string pluginDir in Directory.GetDirectories (PluginsDirecory)) {
				Plugin plugin = new Plugin (pluginDir);
				Plugins.Add (plugin);
				plugin.Load ();
			}
		}


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
					<HorizontalStack Spacing='0'>
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
						<Label Text='{../../tb.CurrentColumn}' Margin='3'/>
					</HorizontalStack>
				</VerticalStack>
			</ListItem>
		</ItemTemplate>
	";
	#endregion


#region main options
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