// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
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

namespace CrowEdit
{
	public class CrowEdit : CrowEditBase.CrowEditBase
	{		
#if NETCOREAPP
		static IntPtr resolveUnmanaged(Assembly assembly, String libraryName)
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
		}
		
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
			System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()).ResolvingUnmanagedDll += resolveUnmanaged;
			System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()).Resolving += last_chance_resolve;
		}
#endif		
		static void Main ()
		{
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

			loadPlugins ();			
			reopenLastProjectList ();

			SetWindowIcon ("#Crow.Icons.crow.png");
		
			if (CurrentDir == null)
				CurrentDir = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
			
			initCommands ();

			Widget w = Load ("#CrowEdit.ui.main.crow");
			w.DataSource = this;

			mainDock = w.FindByName ("mainDock") as DockStack;

			reloadWinConfigs ();

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

		public CommandGroup AllCommands => new CommandGroup (
			FileCommands,
			EditCommands,	
			ViewCommands
		);		
		public CommandGroup ViewCommands = new CommandGroup ("View",
 			new Command("Explorer", (sender) => loadWindowWithThisDataSource (sender, "#CrowEdit.ui.windows.winFileExplorer.crow")),
			new Command("Editors", (sender) => loadWindowWithThisDataSource (sender, "#CrowEdit.ui.windows.winEditor.crow")),
			new Command("Projects", (sender) => loadWindowWithThisDataSource (sender, "#CrowEdit.ui.windows.winProjects.crow")),
			new Command("Logs", (sender) => loadWindowWithThisDataSource (sender, "#CrowEdit.ui.windows.winLogs.crow")),
			new Command("Crow Preview", (sender) => loadWindowWithThisDataSource (sender, "#CECrowDebugLog.ui.winCrowPreview.crow")),
			new Command("Services", (sender) => loadWindowWithThisDataSource (sender, "#CrowEdit.ui.windows.winServices.crow")),
			new Command("Plugins", (sender) => loadWindowWithThisDataSource (sender, "#CrowEdit.ui.windows.winPlugins.crow"))
		);
		void initCommands (){
			FileCommands = new CommandGroup ("File",
	 			new Command("New", createNewFile, "#icons.blank-file.svg"),
				new Command("Open...", openFileDialog, "#icons.outbox.svg"),
				new Command ("save", default(Action), "#icons.inbox.svg", false),
				new Command ("Save As...", default(Action), "#icons.inbox.svg", false),
				new Command("Options", openOptionsDialog, "#icons.tools.svg"),
				new Command("Quit", base.Quit, "#icons.sign-out.svg")
			);
			EditCommands = new CommandGroup ("Edit",
				new Command ("Undo", default(Action), "#icons.reply.svg", false),
				new Command ("Redo", default(Action), "#icons.share-arrow.svg", false),
				new Command ("Cut", default(Action), "#icons.scissors.svg", false),
				new Command ("Copy", default(Action), "#icons.copy-file.svg", false),
				new Command ("Paste", default(Action), "#icons.paste-on-document.svg", false)

			);
			
			CMDHelp = new Command(new Action(() => System.Diagnostics.Debug.WriteLine("help"))) { Caption = "Help", Icon = new SvgPicture("#CrowEdit.ui.icons.question.svg")};
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
		
		protected override Document openOrCreateFile (string filePath) {
			Document doc = null;
			CurrentFilePath = filePath;

			string ext = Path.GetExtension (CurrentFilePath);
			if (TryGetDefaultTypeForExtension (ext, out Type clientType)) {
				if (typeof(Document).IsAssignableFrom (clientType))				
					doc = (Document)Activator.CreateInstance (clientType, new object[] {CurrentFilePath});
				else if (typeof(Service).IsAssignableFrom (clientType))
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
			if (OpenFile ((sender as FileDialog).SelectedFile) is Document doc)
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
			if (e.NewValue is IFileNode fi) {
				if (string.IsNullOrEmpty (fi.FullPath) || ! File.Exists (fi.FullPath))
					return;
				if (TryGetDefaultTypeForExtension (Path.GetExtension (fi.FullPath), out Type clientType)) {
					if (typeof(Document).IsAssignableFrom (clientType))	{
						if (OpenedDocuments.FirstOrDefault (d => d.FullPath == fi.FullPath) is Document doc)					
							CurrentDocument = doc;					
					/*} else if (typeof(Service).IsAssignableFrom (clientType))
						doc = GetService (clientType)?.OpenDocument (CurrentFilePath);*/
					} else if (typeof(Project).IsAssignableFrom (clientType)) {
						if (Projects.FirstOrDefault (p=>p.FullPath == fi.FullPath) is Project prj)
							CurrentProject = prj;
					}
				}
			}			
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

