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
		static CrowEdit()
		{
			System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()).ResolvingUnmanagedDll += resolveUnmanaged;
			Interface.CrowAssemblyNames = new string[] {"CrowEditBase"};
		}
#endif		
		static void Main ()
		{
			using (CrowEdit app = new CrowEdit ())
				app.Run	();
		}
		public CrowEdit () : base (Configuration.Global.Get<int>("MainWinWidth"), Configuration.Global.Get<int>("MainWinHeight")) {
			
			initPlugins ();
		}
		public override void ProcessResize(Rectangle bounds)
		{
			base.ProcessResize(bounds);
			Configuration.Global.Set ("MainWinWidth", clientRectangle.Width);
			Configuration.Global.Set ("MainWinHeight", clientRectangle.Height);
		}

		protected override void OnInitialized () {
			base.OnInitialized ();

			//SetWindowIcon ("#CrowEdit.images.crow.png");
		
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
			saveOpenedDocumentList ();
			saveWinConfigs ();	
		}
		DockStack mainDock;
				public void saveWinConfigs() {
			Configuration.Global.Set ("WinConfigs", mainDock.ExportConfig ());
			Configuration.Global.Save ();
		}
		public Command CMDSave, CMDSaveAs, CMDQuit, CMDHelp, CMDAbout, CMDOptions;

		public CommandGroup AllCommands => new CommandGroup (
			FileCommands,
			EditCommands,	
			ViewCommands
		);		
		public CommandGroup ViewCommands = new CommandGroup ("View",
 			new Command("Explorer", (sender) => loadWindowWithThisDataSource (sender, "#CrowEdit.ui.windows.winFileExplorer.crow")),
			new Command("Editor", (sender) => loadWindowWithThisDataSource (sender, "#CrowEdit.ui.windows.winEditor.crow"))
		);
		void initCommands (){
			FileCommands = new CommandGroup ("File",
	 			new Command("New", createNewFile, "#CrowEdit.ui.icons.blank-file.svg"),
				new Command("Open...", openFileDialog, "#CrowEdit.ui.icons.outbox.svg"),
				new Command ("save", default(Action), "#CrowEdit.ui.icons.inbox.svg", false),
				new Command ("Save As...", default(Action), "#CrowEdit.ui.icons.inbox.svg", false),
				new Command("Options", openOptionsDialog, "#CrowEdit.ui.icons.tools.svg"),
				new Command("Quit", base.Quit, "#CrowEdit.ui.icons.sign-out.svg")
			);
			EditCommands = new CommandGroup ("Edit",
				new Command ("Undo", default(Action), "#CrowEdit.ui.icons.reply.svg", false),
				new Command ("Redo", default(Action), "#CrowEdit.ui.icons.share-arrow.svg", false),
				new Command ("Cut", default(Action), "#CrowEditBase.ui.icons.scissors.svg", false),
				new Command ("Copy", default(Action), "#CrowEditBase.ui.icons.copy-file.svg", false),
				new Command ("Paste", default(Action), "#CrowEditBase.ui.icons.paste-on-document.svg", false)

			);
			
			CMDHelp = new Command(new Action(() => System.Diagnostics.Debug.WriteLine("help"))) { Caption = "Help", Icon = new SvgPicture("#CrowEdit.ui.icons.question.svg")};
		}

		static void loadWindowWithThisDataSource(object sender, string path) {
			Widget w = sender as Widget;
			CrowEdit e = w.IFace as CrowEdit;
			e.loadWindow (path, e);
		}
		public void reloadWinConfigs() {
			string conf = Configuration.Global.Get<string>("WinConfigs");
			if (string.IsNullOrEmpty (conf))
				return;
			mainDock.ImportConfig (conf, this);
		}
		public Window loadWindow (string path, object dataSource = null){
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
		public void closeWindow (string path){
			Widget g = FindByName (path);
			if (g != null)
				DeleteWidget (g);
		}		


		PluginsLoadContext pluginsCtx;
		void initPlugins () {
			/**** test ******/
			Document.AddFileAssociation (".crow", "CrowEdit.Xml.XmlDocument, CEXmlPlugin, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
			/**** test ******/
			if (string.IsNullOrEmpty (PluginsDirecory))			
				PluginsDirecory = Path.Combine (
					Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), ".config", "CrowEdit", "Plugins");

			pluginsCtx = new PluginsLoadContext (PluginsDirecory);
		}
		protected override Document openOrCreateFile (string filePath) {
			TextDocument doc = null;
			CurrentFilePath = filePath;
			string ext = Path.GetExtension (CurrentFilePath);

			using (System.Runtime.Loader.AssemblyLoadContext.ContextualReflectionScope ctx = pluginsCtx.EnterContextualReflection ()) {				

				Type docType = Type.GetType (Document.GetDocumentClass (ext));

				doc = docType == null ? new TextDocument (this, CurrentFilePath)
					: (TextDocument)Activator.CreateInstance (docType, new object[] {this, CurrentFilePath});

			}
			
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
			if (OpenOrSelectFile ((sender as FileDialog).SelectedFile) is TextDocument textDocument)
				CurrentDocument = textDocument;
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
			TextDocument doc = OpenedDocuments.FirstOrDefault (d => d.FullPath == fi.FullName);
			if (doc != null)
				CurrentDocument = doc;
			/*else
				openOrCreateFile (fi.FullName);*/
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
			TextDocument doc = OpenedDocuments.FirstOrDefault (d => d.FullPath == lastCurDoc);
			if (doc != null)
				CurrentDocument = doc;
		}

		
	}
}

