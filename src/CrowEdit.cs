// Copyright (c) 2013-2025  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
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
using System.Diagnostics;

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
			using (CrowEdit app = new CrowEdit ())
				app.Run	();
		}
		public CrowEdit () : base (Configuration.Global.Get<int>("MainWinWidth", 800), Configuration.Global.Get<int>("MainWinHeight", 600), true) {	}
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

			reloadSyntaxTheme ();

			reloadLogsConfigs ();
			
			reloadWinConfigs ();

			lock(UpdateMutex) {
				foreach (Service service in Services) {
					foreach (string winPath in service.ServiceWindowsPath) {
						if (TryGetWindow (winPath, out Window win))
							win.DataSource = service;
					}
				}
			}

			reopenLastProjectList ();

			reopenLastDocumentList ();
		}
		public override void Terminate()
		{
			saveProjectList ();
			saveOpenedDocumentList ();
			saveLogsConfig ();
			saveWinConfigs ();
		}
		
		public Command CMDSave, CMDSaveAs, CMDQuit, CMDHelp, CMDAbout, CMDOptions;
		public Command CMDSyntaxTheme_Reload, CMDSyntaxTheme_Save, CMDSyntaxTheme_SaveAs;

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
	 			new ActionCommand("Explorer", () => LoadWindow ("#CrowEdit.ui.windows.winFileExplorer.crow", this), "#icons.folder.svg"),
				new ActionCommand("Editors", () => {
					if (!TryGetWindow("#CrowEdit.ui.windows.winEditor.crow", out Window we)) {
						LoadWindow ("#CrowEdit.ui.windows.winEditor.crow", this);
						selecteLastCurrentDocument();
					}
				}, "#icons.edit.svg"),
				new ActionCommand("Exceptions", () => LoadWindow ("#CrowEdit.ui.windows.winExceptions.crow", this), "#icons.exclamation.svg"),
				new ActionCommand("Projects", () => LoadWindow ("#CrowEdit.ui.windows.winProjects.crow", this)),
				new ActionCommand("Logs", () => LoadWindow ("#CrowEdit.ui.windows.winLogs.crow", this), "#icons.log.svg"),
				new ActionCommand("Services", () => LoadWindow ("#CrowEdit.ui.windows.winServices.crow", this), "#icons.services.svg"),
				new ActionCommand("Plugins", () => LoadWindow ("#CrowEdit.ui.windows.winPlugins.crow", this), "#icons.puzzle-piece.svg"),
				new ActionCommand("Syntax Tree", () => LoadWindow ("#CrowEdit.ui.windows.winSyntaxExplorer.crow", this), "#icons.plugins.svg"),
				new ActionCommand("Syntax Theme Editor", () => LoadWindow ("#CrowEdit.ui.windows.winThemeEditor.crow", this), "#icons.palette.svg")
			);
			CMDHelp = new ActionCommand("Help", () => System.Diagnostics.Debug.WriteLine("help"), "#icons.question.svg");

			CommandsRoot = new CommandGroup (
				FileCommands,
				EditCommands,
				ViewCommands,
				new CommandGroup ("Help", CMDHelp)
			);

			CMDSyntaxTheme_Reload = new ActionCommand ("Reload", () => reloadSyntaxTheme ());
			CMDSyntaxTheme_Save   = new ActionCommand ("Save", () => saveSyntaxTheme ());
			CMDSyntaxTheme_SaveAs = new ActionCommand ("Save As...", () => saveSyntaxThemeAs ());
		}

		static void loadWindowWithThisDataSource(object sender, string path) {
			Widget w = sender as Widget;
			CrowEdit e = w.IFace as CrowEdit;
			e.LoadWindow (path, e);
		}
	

		protected override Document openOrCreateFile (string filePath, string editorPath = null) {
			Document doc = null;
			CurrentFilePath = filePath;
			try {
				string ext = Path.GetExtension (CurrentFilePath);
				if (TryGetDefaultTypeForExtension (ext, out Type clientType)) {
					if (typeof(Document).IsAssignableFrom (clientType)) {
						if (editorPath == null)
							TryGetDefaultEditorForDocumentType (ext, out editorPath);
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
				Log(LogType.Error, $"Unable to open {filePath}.\n{ex.Message}");
				Debug.WriteLine(ex.Message);
				Debug.WriteLine(ex.StackTrace);
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
			selecteLastCurrentDocument();
		}
		void selecteLastCurrentDocument() {
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
        public override Stream GetStreamFromPath(string path)
        {
            return base.GetStreamFromPath(path);
        }

		#region syntax theme options/loading
		Dictionary<string, TextFormatting> syntaxTheme;
		public Dictionary<string, TextFormatting> SyntaxTheme {
			get => syntaxTheme;
			set {
				if (syntaxTheme == value)
					return;
				syntaxTheme = value;
				NotifyValueChanged (SyntaxTheme);
			}
		}
		public string SyntaxThemeDirectory {
			get => Configuration.Global.Get<string> ("SyntaxThemeDirectory") ??
					Path.Combine (Path.GetDirectoryName (Assembly.GetEntryAssembly ().Location), "SyntaxThemes");
			set {
				if (SyntaxThemeDirectory == value)
					return;
				Configuration.Global.Set ("SyntaxThemeDirectory", value);
				NotifyValueChanged ("SyntaxThemeDirectory", (object)SyntaxThemeDirectory);
				NotifyValueChanged ("AvailableSyntaxThemes", (object)AvailableSyntaxThemes);
				reloadSyntaxTheme();
			}
		}
		public string syntaxThemeFile => Path.Combine (SyntaxThemeDirectory, $"{SyntaxThemeName}.syntax");
		public string[] AvailableSyntaxThemes {
			get {
				if (!Directory.Exists(SyntaxThemeDirectory))
					return null;
				string[] tmp = Directory.GetFiles (SyntaxThemeDirectory);
                for (int i = 0; i < tmp.Length; i++)
					tmp[i] = Path.GetFileNameWithoutExtension (tmp[i]);
				return tmp;
            }
        }
		public string SyntaxThemeName {
			get => Configuration.Global.Get<string> ("SyntaxThemeName");
			set {
				if (SyntaxThemeName == value)
					return;
				Configuration.Global.Set ("SyntaxThemeName", value);
				NotifyValueChanged ("SyntaxThemeName", (object)SyntaxThemeName);
				reloadSyntaxTheme ();
			}
		}
		public Command CMDOptions_SyntaxThemeDirectory => new ActionCommand ("...",
			() => {
				FileDialog dlg = App.LoadIMLFragment<FileDialog> (@"
				<FileDialog Caption='Select Syntax Themes Folder' CurrentDirectory='{SyntaxThemeDirectory}'
							ShowFiles='false' ShowHidden='true' />");
				dlg.OkClicked += (sender, e) => SyntaxThemeDirectory = (sender as FileDialog).SelectedFileFullPath;
				dlg.DataSource = this;
			}
		);

		void reloadSyntaxTheme () {
			if (!File.Exists (syntaxThemeFile))
				return;
			Dictionary<string, TextFormatting> theme = new Dictionary<string, TextFormatting> ();
			using (StreamReader sr = new StreamReader(syntaxThemeFile)) {
				while (!sr.EndOfStream) {
					string l = sr.ReadLine ();
					string[] tmp = l.Split ('=');
					theme.Add (tmp[0].Trim (), TextFormatting.Parse (tmp[1].Trim ()));
				}
            }
			SyntaxTheme = theme;
		}
		void saveSyntaxTheme () {
			using (StreamWriter sw = new StreamWriter (syntaxThemeFile)) {
				foreach (string key in SyntaxTheme.Keys) {
					sw.WriteLine ($"{key} = {SyntaxTheme[key]}");
				}
			}
		}
		void saveSyntaxThemeAs ()
		{
			FileDialog fd = LoadIMLFragment<FileDialog> (@"<FileDialog Width='60%' Height='50%' Caption='Save as ...' CurrentDirectory='" +
				SyntaxThemeDirectory + "' SelectedFile='" +
				Path.GetFileName(syntaxThemeFile) + "' OkClicked='saveSyntaxThemeAsDialog_OkClicked'/>");
			fd.DataSource = this;
			fd.OkClicked += (sender, e) => {
				FileDialog fd = sender as FileDialog;

				if (string.IsNullOrEmpty (fd.SelectedFileFullPath))
					return;

				if (File.Exists(fd.SelectedFileFullPath)) {
					MessageBox mb = MessageBox.ShowModal (this, MessageBox.Type.YesNo, "File exists, overwrite?");
					mb.Yes += (sender2, e2) => {
						SyntaxThemeName = Path.GetFileNameWithoutExtension(fd.SelectedFile);
						SyntaxThemeDirectory = fd.SelectedDirectory;
						saveSyntaxTheme ();
					};
					return;
				}

				SyntaxThemeName = Path.GetFileNameWithoutExtension(fd.SelectedFile);
				saveSyntaxTheme ();
			};
		}
		#endregion
    }
}

