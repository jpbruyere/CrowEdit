// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow;
using System.IO;
using System.Collections.Generic;
using Crow.Text;
using System.Reflection;
using System.Runtime.InteropServices;

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

		static CrowEdit()
		{
			System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()).ResolvingUnmanagedDll += resolveUnmanaged;
		}
#endif		
		public Command CMDNew, CMDOpen, CMDSave, CMDSaveAs, CMDQuit, CMDShowLeftPane,
			CMDUndo, CMDRedo, CMDCut, CMDCopy, CMDPaste, CMDHelp, CMDAbout, CMDOptions;

		const string _defaultFileName = "unnamed.txt";
		string source = "";
		int dirtyUndoLevel;//
		public new bool IsDirty { get { return undoStack.Count != dirtyUndoLevel; } }
		public string Source {
			get => source;
			set {
				if (source == value)
					return;
				source = value;
				NotifyValueChanged (source);
			}
		}
		public CommandGroup EditorCommands => new CommandGroup (CMDUndo, CMDRedo, CMDCut, CMDCopy, CMDPaste, CMDSave, CMDSaveAs);

		Stack<TextChange> undoStack = new Stack<TextChange> ();
		Stack<TextChange> redoStack = new Stack<TextChange> ();
		
		void undo () {
			if (undoStack.TryPop (out TextChange tch)) {
				redoStack.Push (tch.Inverse (source));
				CMDRedo.CanExecute = true;
				apply (tch);
				editor.SetCursorPosition (tch.End + tch.ChangedText.Length);
			}
			if (undoStack.Count == 0)
				CMDUndo.CanExecute = false;
		}
		void redo () {
			if (redoStack.TryPop (out TextChange tch)) {
				undoStack.Push (tch.Inverse (source));
				CMDUndo.CanExecute = true;
				apply (tch);
				editor.SetCursorPosition (tch.End + tch.ChangedText.Length);
			}
			if (redoStack.Count == 0)
				CMDRedo.CanExecute = false;
		}
		bool disableTextChangedEvent = false;
		void apply (TextChange change) {
			Span<char> tmp = stackalloc char[source.Length + (change.ChangedText.Length - change.Length)];
			ReadOnlySpan<char> src = source.AsSpan ();
			src.Slice (0, change.Start).CopyTo (tmp);
			if (!string.IsNullOrEmpty(change.ChangedText))
				change.ChangedText.AsSpan ().CopyTo (tmp.Slice (change.Start));
			src.Slice (change.End).CopyTo (tmp.Slice (change.Start + change.ChangedText.Length));
			disableTextChangedEvent = true;
			Source = tmp.ToString ();
			disableTextChangedEvent = false;
			NotifyValueChanged ("IsDirty", IsDirty);
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
			CMDNew = new Command(new Action(() => onNewFile())) { Caption = "New", Icon = new SvgPicture("#CrowEdit.ui.icons.blank-file.svg")};
			CMDOpen = new Command(new Action(() => openFileDialog())) { Caption = "Open...", Icon = new SvgPicture("#CrowEdit.ui.icons.outbox.svg")};
			CMDSave = new Command(new Action(() => saveFileDialog())) { Caption = "Save", Icon = new SvgPicture("#CrowEdit.ui.icons.inbox.svg"), CanExecute = false};
			CMDSaveAs = new Command(new Action(() => saveFileDialog())) { Caption = "Save As...", Icon = new SvgPicture("#CrowEdit.ui.icons.inbox.svg"), CanExecute = false};
			CMDQuit = new Command(new Action(() => base.Quit())) { Caption = "Quit", Icon = new SvgPicture("#CrowEdit.ui.icons.sign-out.svg")};
			CMDUndo = new Command(new Action(() => undo())) { Caption = "Undo", Icon = new SvgPicture("#CrowEdit.ui.icons.reply.svg"), CanExecute = false};
			CMDRedo = new Command(new Action(() => redo())) { Caption = "Redo", Icon = new SvgPicture("#CrowEdit.ui.icons.share-arrow.svg"), CanExecute = false};
			CMDCut = new Command(new Action(() => Quit ())) { Caption = "Cut", Icon = new SvgPicture("#CrowEdit.ui.icons.scissors.svg"), CanExecute = false};
			CMDCopy = new Command(new Action(() => Quit ())) { Caption = "Copy", Icon = new SvgPicture("#CrowEdit.ui.icons.copy-file.svg"), CanExecute = false};
			CMDPaste = new Command(new Action(() => Quit ())) { Caption = "Paste", Icon = new SvgPicture("#CrowEdit.ui.icons.paste-on-document.svg"), CanExecute = false};
			CMDHelp = new Command(new Action(() => System.Diagnostics.Debug.WriteLine("help"))) { Caption = "Help", Icon = new SvgPicture("#CrowEdit.ui.icons.question.svg")};
			CMDOptions = new Command(new Action(() => openOptionsDialog())) { Caption = "Editor Options", Icon = new SvgPicture("#CrowEdit.ui.icons.tools.svg")};
			CMDShowLeftPane = new Command (new Action (() => ShowLeftPane = !ShowLeftPane)) { Caption = "Show Left Pane" };
		}
		void onNewFile(){
			if (IsDirty) {
				MessageBox mb = MessageBox.ShowModal (this, MessageBox.Type.YesNo, "Current file has unsaved changes, are you sure?");
				mb.Yes += (sender, e) => newFile();
			} else
				newFile ();
		}		
		void openOptionsDialog() =>	Load ("#CrowEdit.ui.EditorOptions.crow").DataSource = this;
		void openFileDialog() => Load ("#CrowEdit.ui.openFile.crow").DataSource = this;
		void saveFileDialog() => Load ("#CrowEdit.ui.saveFile.crow").DataSource = this;
		void openFileDialog_OkClicked (object sender, EventArgs e)
		{
			FileDialog fd = sender as FileDialog;
			if (string.IsNullOrEmpty (fd.SelectedFile))
				return;
			openFile (fd.SelectedFile, fd.SelectedDirectory);
		}
		void saveFileDialog_OkClicked (object sender, EventArgs e)
		{
			FileDialog fd = sender as FileDialog;
			if (string.IsNullOrEmpty (fd.SelectedFile))
				return;			
			save (fd.SelectedFile, fd.SelectedDirectory);
		}		
		void onTextChanged (object sender, TextChangeEventArgs e)
		{
			if (disableTextChangedEvent)
				return;
			undoStack.Push (e.Change.Inverse(source));
			redoStack.Clear ();
			CMDUndo.CanExecute = true;
			CMDRedo.CanExecute = false;
			apply (e.Change);
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

			OnOpenFile (Path.GetFileName (fi.FullName), Path.GetDirectoryName (fi.FullName));
		}

		void OnOpenFile (string filePath, string directory) {
			if (IsDirty) {
				MessageBox mb = MessageBox.ShowModal (this, MessageBox.Type.YesNo, "Current file has unsaved changes, are you sure?");
				mb.Yes += (sender, e) => openFile (filePath, directory);
			} else
				openFile (filePath, directory);
		}
		void newFile () {
			CurrentFilePath = Path.Combine (CurFileDir, _defaultFileName);
			disableTextChangedEvent = true;
			Source = "";
			disableTextChangedEvent = false;
			resetUndoRedo ();
		}
		void openFile (string filePath, string directory) {
			CurrentFilePath = Path.Combine(directory, filePath);
			reloadFromFile ();
			resetUndoRedo ();
		}
		void save (string filePath, string directory) {
			CurrentFilePath = Path.Combine (directory, filePath);
			using (StreamWriter sr = new StreamWriter (CurrentFilePath)) {
				sr.Write (source);
			}
			dirtyUndoLevel = undoStack.Count;

			NotifyValueChanged ("IsDirty", IsDirty);
		}

		void reloadFromFile () {
			disableTextChangedEvent = true;
			if (File.Exists (CurrentFilePath)) {
				using (Stream s = new FileStream (CurrentFilePath, FileMode.Open)) {
					using (StreamReader sr = new StreamReader (s))
						Source = sr.ReadToEnd ();
				}
			}
			disableTextChangedEvent = false;
			resetUndoRedo ();
		}
		void resetUndoRedo () {
			undoStack.Clear ();
			redoStack.Clear ();
			CMDUndo.CanExecute = false;
			CMDRedo.CanExecute = false;
			dirtyUndoLevel = 0;
		}				
		static void Main ()
		{
			using (CrowEdit win = new CrowEdit ()) 
				win.Run	();
		}
		public CrowEdit () : base(800, 600)	{}

		protected override void OnInitialized () {
			base.OnInitialized ();

			if (CurrentDir == null)
				CurrentDir = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
			
			initCommands ();
			Load ("#CrowEdit.ui.main.crow").DataSource = this;
			editor = FindByName ("tb") as TextBox;

			if (ReopenLastFile)
				reloadFromFile ();
		}
		TextBox editor;
		void textView_KeyDown (object sender, Crow.KeyEventArgs e) {
			if (Ctrl && e.Key == Glfw.Key.W) {
				if (Shift)
					CMDRedo.Execute ();
				else
					CMDUndo.Execute ();

			}
		}
	}
}

