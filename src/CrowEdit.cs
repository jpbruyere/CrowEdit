﻿// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow;
using System.IO;
using System.Collections.Generic;
using Crow.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using CrowEditBase;
using System.Linq;
using System.Text;
using Drawing2D;

namespace CrowEdit
{
	public class CrowEdit : CrowEditBase.CrowEditBase
	{
		static Assembly last_chance_resolve (System.Runtime.Loader.AssemblyLoadContext context, AssemblyName assemblyName)
		{
			foreach (Plugin plugin in App.Plugins) {
				if (plugin.TryGet (assemblyName, out Assembly assembly))
					return assembly;
			}
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine($"[UNRESOLVE] {assemblyName}");
			Console.ResetColor();
			return null;
		}
		static CrowEdit()
		{
			System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()).Resolving += last_chance_resolve;
		}

		static void Main ()
		{
			/*DbgLogger.IncludedEvents.AddRange ( new DbgEvtType[] {
				DbgEvtType.MouseEnter,
				DbgEvtType.MouseLeave,
				DbgEvtType.WidgetMouseDown,
				DbgEvtType.WidgetMouseUp,
				DbgEvtType.WidgetMouseClick,
				DbgEvtType.HoverWidget
			});*/

			CrowEdit.CrowAssemblyNames = new string[] {"CrowEditBase"};
			using (CrowEdit app = new CrowEdit ())
				app.Run	();
		}
		public CrowEdit () : base (Configuration.Global.Get<int>("MainWinWidth", 800), Configuration.Global.Get<int>("MainWinHeight", 600)) {


		}
		public override void ProcessResize(Rectangle bounds)
		{
			base.ProcessResize(bounds);
			Configuration.Global.Set ("MainWinWidth", clientRectangle.Width);
			Configuration.Global.Set ("MainWinHeight", clientRectangle.Height);
		}

		protected override void OnInitialized () {
			base.OnInitialized ();

			initCommands ();

			loadPlugins ();

			SetWindowIcon ("#Crow.Icons.crow.png");

			if (CurrentDir == null)
				CurrentDir = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);

			Widget w = Load ("#CrowEdit.ui.main.crow");
			w.DataSource = this;

			mainDock = w.FindByName ("mainDock") as DockStack;

			reloadWinConfigs ();

			reopenLastProjectList ();

			reopenLastDocumentList ();
		}
		public override void Terminate()
		{
			saveProjectList ();
			saveOpenedDocumentList ();
			saveWinConfigs ();
		}
		DockStack mainDock;
		public Command CMDSave, CMDSaveAs, CMDQuit, CMDHelp, CMDAbout, CMDOptions;

		void initCommands (){
			FileCommands = new CommandGroup ("File",
	 			new ActionCommand("New", createNewFile, "#icons.blank-file.svg"),
				new ActionCommand("Open...", openFileDialog, "#icons.outbox.svg"),
				new ActionCommand ("save", default(Action), "#icons.inbox.svg", false),
				new ActionCommand ("Save As...", default(Action), "#icons.inbox.svg", false),
				new ActionCommand("Options", openOptionsDialog, "#icons.tools.svg"),
				new ActionCommand("Quit", base.Quit, "#icons.sign-out.svg")
			);
			EditCommands = new CommandGroup ("Edit",
				new ActionCommand ("Undo", default(Action), "#icons.reply.svg", false),
				new ActionCommand ("Redo", default(Action), "#icons.share-arrow.svg", false),
				new ActionCommand ("Cut", default(Action), "#icons.scissors.svg", false),
				new ActionCommand ("Copy", default(Action), "#icons.copy-file.svg", false),
				new ActionCommand ("Paste", default(Action), "#icons.paste-on-document.svg", false)

			);
			ViewCommands = new CommandGroup ("View",
	 			new ActionCommand("Explorer", () => LoadWindow ("#CrowEdit.ui.windows.winFileExplorer.crow", this)),
				new ActionCommand("Editors", () => LoadWindow ("#CrowEdit.ui.windows.winEditor.crow", this)),
				new ActionCommand("Projects", () => LoadWindow ("#CrowEdit.ui.windows.winProjects.crow", this)),
				new ActionCommand("Logs", () => LoadWindow ("#CrowEdit.ui.windows.winLogs.crow", this), "#icons.log.svg"),
				new ActionCommand("Services", () => LoadWindow ("#CrowEdit.ui.windows.winServices.crow", this), "#icons.services.svg"),
				new ActionCommand("Plugins", () => LoadWindow ("#CrowEdit.ui.windows.winPlugins.crow", this), "#icons.plugins.svg"),
				new ActionCommand("Syntax Tree", () => LoadWindow ("#CrowEdit.ui.windows.winSyntaxExplorer.crow", this), "#icons.plugins.svg")
			);
			CMDHelp = new ActionCommand("Help", () => System.Diagnostics.Debug.WriteLine("help"), "#icons.question.svg");

			CommandsRoot = new CommandGroup (
				FileCommands,
				EditCommands,
				ViewCommands,
				new CommandGroup ("Help", CMDHelp)
			);
		}


		static void loadWindowWithThisDataSource(object sender, string path) {
			Widget w = sender as Widget;
			CrowEdit e = w.IFace as CrowEdit;
			e.LoadWindow (path, e);
		}
		void saveWinConfigs() {
			Configuration.Global.Set ("WinConfigs", mainDock.ExportConfig ());

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

			Configuration.Global.Save ();
		}

		void reloadWinConfigs() {

			if (Configuration.Global.TryGet<string>("WinConfigs", out string conf) && !string.IsNullOrEmpty(conf))
				mainDock.ImportConfig (conf, this);
			if (Configuration.Global.TryGet<string>("FloatingWinConfigs", out conf) && !string.IsNullOrEmpty(conf)) {
				string[] floatings = conf.Split ('|');
				for (int i = 0; i < floatings.Length; i++)
					DockWindow.CreateFromFloatingConfigString (this, floatings[i], this);
			}

		}

		protected override Document openOrCreateFile (string filePath, string editorPath = null) {
			Document doc = null;
			CurrentFilePath = filePath;
			try {
				string ext = Path.GetExtension (CurrentFilePath);
				if (TryGetDefaultTypeForExtension (ext, out Type clientType)) {
					if (typeof(Document).IsAssignableFrom (clientType)) {
						if (editorPath == null)
							TryGetDefaultEditorForDocumentType (clientType, out editorPath);
						doc = (Document)Activator.CreateInstance (clientType, new object[] {CurrentFilePath, editorPath});
					}else if (typeof(Service).IsAssignableFrom (clientType))
						doc = GetService (clientType)?.OpenDocument (CurrentFilePath);
					else if (typeof(Project).IsAssignableFrom (clientType)) {
						Project prj = (Project)Activator.CreateInstance (clientType, new object[] {CurrentFilePath});
						Projects.Add (prj);
						CurrentProject = prj;
						return null;
					}
				}else
					doc = new TextDocument (CurrentFilePath);

				doc.CloseEvent += onQueryCloseDocument;
				OpenedDocuments.Add (doc);
				CurrentDocument = doc;
			} catch (Exception ex) {
				MessageBox.ShowModal (this, MessageBox.Type.Alert, $"Unable to open {filePath}.\n{ex.Message}");
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine (ex);
				Console.ResetColor();
			}
			return doc;
		}
		/*public TreeNode[] GetCurrentDirNodes =>
				(string.IsNullOrEmpty(CurrentDir) || !Directory.Exists (CurrentDir)) ?
					 null :	new DirectoryNode (new DirectoryInfo(CurrentDir)).GetFileSystemTreeNodeOrdered();*/
		public bool ReopenLastFile {
			get => Configuration.Global.Get<bool> ("ReopenLastFile");
			set {
				if (ReopenLastFile == value)
					return;
				Configuration.Global.Set ("ReopenLastFile", value);
				NotifyValueChanged (ReopenLastFile);
			}
		}

		void openOptionsDialog() =>	Load ("#CrowEdit.ui.EditorOptions.crow").DataSource = this;
		void openFileDialog() =>
			LoadIMLFragment (
				@"<FileDialog Width='60%' Height='50%' Caption='Open File' AlwaysOnTop='true'
					CurrentDirectory='{CurFileDir}'
					SelectedFile='{CurFileName}'
					OkClicked='openFileDialog_OkClicked'/>").DataSource = this;

		void openFileDialog_OkClicked (object sender, EventArgs e)
		{
			if (OpenFile ((sender as FileDialog).SelectedFileFullPath) is Document doc)
				CurrentDocument = doc;
		}

		void goUpDirClick (object sender, MouseButtonEventArgs e) {
			if (string.IsNullOrEmpty (CurrentDir))
				return;
			string root = Directory.GetDirectoryRoot (CurrentDir);
			if (CurrentDir == root)
				return;
			CurrentDir = Directory.GetParent (CurrentDir).FullName;
		}

		void Dv_SelectedItemChanged (object sender, SelectionChangeEventArgs e) {
			FileSystemInfo fi = e.NewValue as FileSystemInfo;
			if (fi == null)
				return;
			if (fi is DirectoryInfo)
				return;
			Document doc = OpenedDocuments.FirstOrDefault (d => d.FullPath == fi.FullName);
			if (doc != null)
				CurrentDocument = doc;
		}
		void tv_projects_SelectedItemChanged (object sender, SelectionChangeEventArgs e) {
			/*if (e.NewValue is Project prj) {
				CurrentProject = prj;
			}*/
			/*if (e.NewValue is IFileNode fi) {
				if (string.IsNullOrEmpty (fi.FullPath) || ! File.Exists (fi.FullPath))
					return;
				if (TryGetDefaultTypeForExtension (Path.GetExtension (fi.FullPath), out Type clientType)) {
					if (typeof(Document).IsAssignableFrom (clientType))	{
						if (OpenedDocuments.FirstOrDefault (d => d.FullPath == fi.FullPath) is Document doc)
							CurrentDocument = doc;
					//} else if (typeof(Service).IsAssignableFrom (clientType))
					//	doc = GetService (clientType)?.OpenDocument (CurrentFilePath);
					} else if (typeof(Project).IsAssignableFrom (clientType)) {
						if (Projects.FirstOrDefault (p=>p.FullPath == fi.FullPath) is Project prj)
							CurrentProject = prj;
					}
				}
			}*/
		}

		void saveOpenedDocumentList () {
			if (OpenedDocuments.Count == 0)
				Configuration.Global.Set ("OpenedItems", "");
			else
				Configuration.Global.Set ("OpenedItems", OpenedDocuments.Select(o => o.FullPath).Aggregate((a,b)=>$"{a};{b}"));
			Configuration.Global.Set ("CurrentDocument", CurrentDocument?.FullPath);
		}
		void reopenLastDocumentList () {
			string tmp = Configuration.Global.Get<string> ("OpenedItems");
			if (string.IsNullOrEmpty (tmp))
				return;
			foreach (string f in tmp.Split(';'))
				openOrCreateFile (f);
			string lastCurDoc = Configuration.Global.Get<string> ("CurrentDocument");
			if (string.IsNullOrEmpty (lastCurDoc))
				return;
			Document doc = OpenedDocuments.FirstOrDefault (d => d.FullPath == lastCurDoc);
			if (doc != null)
				CurrentDocument = doc;
		}
		void saveProjectList () {
			if (Projects.Count == 0)
				Configuration.Global.Set ("OpenedProjects", "");
			else
				Configuration.Global.Set ("OpenedProjects", Projects.Select(o => o.FullPath).Aggregate((a,b)=>$"{a};{b}"));
			Configuration.Global.Set ("CurrentProject", CurrentProject?.FullPath);
		}

		void reopenLastProjectList () {
			string tmp = Configuration.Global.Get<string> ("OpenedProjects");
			if (string.IsNullOrEmpty (tmp))
				return;
			foreach (string f in tmp.Split(';'))
				openOrCreateFile (f);
			string lastCurDoc = Configuration.Global.Get<string> ("CurrentProject");
			if (string.IsNullOrEmpty (lastCurDoc))
				return;
			Project prj = Projects.FirstOrDefault (d => d.FullPath == lastCurDoc);
			if (prj != null)
				CurrentProject = prj;
		}

	}
}

