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
	public class CrowEdit : Interface
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
		static void Main ()
		{
			using (CrowEdit win = new CrowEdit ()) 
				win.Run	();
		}
		static CrowEdit()
		{
			System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()).ResolvingUnmanagedDll += resolveUnmanaged;
			Interface.CrowAssemblyNames = new string[] {"CrowEditBase"};
		}
		public CrowEdit () : base(800, 600)	{ }

#endif		
		public Command CMDNew, CMDOpen, CMDSave, CMDSaveAs, CMDQuit, CMDShowLeftPane,
			CMDHelp, CMDAbout, CMDOptions;

		
		PluginsLoadContext pluginsCtx;
		void initPlugins () {
			pluginsCtx = new PluginsLoadContext ();
		}
		public ObservableList<TextDocument> OpenedDocuments = new ObservableList<TextDocument> ();

		const string _defaultFileName = "unnamed.txt";

		
		void undo () {
		}
		void redo () {
		}

		TextDocument currentDocument;
		public TextDocument CurrentDocument {
			get => currentDocument;
			set {
				if (currentDocument == value)
					return;

				currentDocument?.UnselectDocument ();

				currentDocument = value;				
				NotifyValueChanged (currentDocument);

				currentDocument?.SelectDocument ();
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
		public bool ShowLeftPane {
			get => Configuration.Global.Get<bool> ("ShowLeftPane");
			set {
				if (ShowLeftPane == value)
					return;
				Configuration.Global.Set ("ShowLeftPane", value);
				NotifyValueChanged (ShowLeftPane);
			}
		}
		public bool ReopenLastFile {
			get => Configuration.Global.Get<bool> ("ReopenLastFile");
			set {
				if (ReopenLastFile == value)
					return;
				Configuration.Global.Set ("ReopenLastFile", value);
				NotifyValueChanged (ReopenLastFile);
			}
		}

		void initCommands (){
			CMDNew = new Command("New", createNewFile, "#CrowEdit.ui.icons.blank-file.svg");
			CMDOpen = new Command("Open...", openFileDialog, "#CrowEdit.ui.icons.outbox.svg");
			

			CMDQuit = new Command("Quit", base.Quit, "#CrowEdit.ui.icons.sign-out.svg");

			CMDHelp = new Command(new Action(() => System.Diagnostics.Debug.WriteLine("help"))) { Caption = "Help", Icon = new SvgPicture("#CrowEdit.ui.icons.question.svg")};

			CMDOptions = new Command("Editor Options", openOptionsDialog, new SvgPicture("#CrowEdit.ui.icons.tools.svg"));
			CMDShowLeftPane = new Command ("Show Left Pane", () => ShowLeftPane = !ShowLeftPane);			
		}
		void createNewFile(){
			openOrCreateFile (Path.Combine (CurFileDir, _defaultFileName));	
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
			FileDialog fd = sender as FileDialog;
			if (string.IsNullOrEmpty (fd.SelectedFile))
				return;
			TextDocument doc = OpenedDocuments.FirstOrDefault (d => d.FullPath == fd.SelectedFileFullPath);
			if (doc != null)
				CurrentDocument = doc;
			else
				openOrCreateFile (fd.SelectedFileFullPath);
		}

		void openOrCreateFile (string filePath) {
			TextDocument doc = null;
			CurrentFilePath = filePath;
			string ext = Path.GetExtension (CurrentFilePath);
			switch (ext) {
				case ".crow":
					doc = new Xml.XmlDocument (this, CurrentFilePath);
					break;
				default:
					doc = new TextDocument (this, CurrentFilePath);
					break;
			}
			
			doc.CloseEvent += onQueryCloseDocument;
			OpenedDocuments.Add (doc);
			CurrentDocument = doc;
		}
		void closeDocument (TextDocument doc) {
			int idx = OpenedDocuments.IndexOf (doc);
			OpenedDocuments.Remove (doc);
			if (doc == CurrentDocument) {
				if (OpenedDocuments.Count > 0)
					CurrentDocument = OpenedDocuments[Math.Min (idx, OpenedDocuments.Count - 1)];
				else
					CurrentDocument = null;
			}
		}

		void goUpDirClick (object sender, MouseButtonEventArgs e) {
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
			else
				openOrCreateFile (fi.FullName);
		}

		
		void onQueryCloseDocument (object sender, EventArgs e) {
			TextDocument doc = sender as TextDocument;
			if (doc.IsDirty) {
				MessageBox mb = MessageBox.ShowModal (this,
					                MessageBox.Type.YesNoCancel, $"{doc.FileName} has unsaved changes.\nSave it now?");
				mb.Yes += (object _sender, EventArgs _e) => { doc.Save (); closeDocument (doc); };
				mb.No += (object _sender, EventArgs _e) => closeDocument (doc);
			} else
				closeDocument (doc);
		}

		protected override void OnInitialized () {
			base.OnInitialized ();

			if (CurrentDir == null)
				CurrentDir = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
			
			initCommands ();
			Load ("#CrowEdit.ui.main.crow").DataSource = this;		

		}		
	}
}

