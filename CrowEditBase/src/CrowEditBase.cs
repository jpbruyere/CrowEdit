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

namespace CrowEditBase
{	
	public abstract class CrowEditBase : Interface {
		protected class DocumentClientClassList : List<Type> {
			string defaultClass;
		}
		protected Dictionary<string, DocumentClientClassList> FileAssociations = new Dictionary<string, DocumentClientClassList> ();
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
				FileAssociations.Add (extension, new DocumentClientClassList ());
			if (!FileAssociations[extension].Contains (clientClass))
				FileAssociations[extension].Add (clientClass);
			
		}
		public void RemoveFileAssociationByType (Type clientClass) {

			//FileAssociations.Values Where (t=>t == clientClass);
		}
		public bool TryGetDefaultTypeForExtension (string extension, out Type clientType) {
			clientType = FileAssociations.ContainsKey (extension) ? FileAssociations[extension].FirstOrDefault () : null;
			return clientType != null;
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
				foreach (var node in Projects.SelectMany (child => child.Flatten))
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

		public Document CurrentDocument {
			get => currentDocument;
			set {
				if (currentDocument == value)
					return;

				currentDocument?.UnselectDocument ();

				currentDocument = value;				
				NotifyValueChanged (currentDocument);

				if (currentDocument == null)
					return;

				currentDocument.SelectDocument ();
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

		protected abstract Document openOrCreateFile (string filePath);
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
					Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), ".config", "CrowEdit", "Plugins");

			foreach (string pluginDir in Directory.GetDirectories (PluginsDirecory)) {
				Plugin plugin = new Plugin (pluginDir);
				Plugins.Add (plugin);
				plugin.Load ();
			}
		}		

	}
}