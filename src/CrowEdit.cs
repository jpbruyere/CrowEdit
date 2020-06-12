// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow;
using System.IO;
using System.Collections.Generic;

namespace CrowEdit
{
	public class CrowEdit : Interface
	{
		public Command CMDNew, CMDOpen, CMDSave, CMDSaveAs, CMDQuit, CMDShowLeftPane,
			CMDUndo, CMDRedo, CMDCut, CMDCopy, CMDPaste, CMDHelp, CMDAbout, CMDOptions;

		string _curFilePath = "unamed.txt";
		string _text = "", _origText="";

		List<string> undoStack = new List<string>();
		List<string> redoStack = new List<string>();

		public string Text {
			get { return _text; }
			set {
				if (_text == value)
					return;
				undoStack.Add (_text);
				CMDUndo.CanExecute = true;
				redoStack.Clear ();
				CMDRedo.CanExecute = false;
				_text = value;
				NotifyValueChanged (_text);
				NotifyValueChanged ("IsDirty", IsDirty);
			}
		}
		bool isDirty = false;
		public bool IsDirty { get { return _text != _origText; }}

		public string CurrentDir {
			get { return Configuration.Global.Get<string>("CurrentDir"); }
			set {
				if (CurrentDir == value)
					return;
				Configuration.Global.Set ("CurrentDir", value);
				NotifyValueChanged (CurrentDir);
			}
		}
		public string CurFilePath {
			get { return _curFilePath; }
			set {
				if (_curFilePath == value)
					return;
				_curFilePath = value;
				NotifyValueChanged (_curFilePath);
			}
		}
		bool showLeftPane;
		public bool ShowLeftPane {
			get { return Configuration.Global.Get<bool> ("ShowLeftPane"); }
			set {
				if (ShowLeftPane == value)
					return;
				Configuration.Global.Set ("ShowLeftPane", value);
				NotifyValueChanged (ShowLeftPane);
			}
		}

		public string CurFileFullPath { get { return Path.Combine(CurrentDir,CurFilePath); }}

		void initCommands(){
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
		void undo(){
			string step = undoStack [undoStack.Count -1];
			redoStack.Add (_text);
			CMDRedo.CanExecute = true;
			undoStack.RemoveAt(undoStack.Count -1);

			_text = step;

			NotifyValueChanged ("Text", (object)_text);
			NotifyValueChanged ("IsDirty", IsDirty);

			if (undoStack.Count == 0)
				CMDUndo.CanExecute = false;
		}
		void redo(){
			string step = redoStack [redoStack.Count -1];
			undoStack.Add (_text);
			CMDUndo.CanExecute = true;
			redoStack.RemoveAt(redoStack.Count -1);
			_text = step;
			NotifyValueChanged ("Text", (object)_text);
			NotifyValueChanged ("IsDirty", IsDirty);

			if (redoStack.Count == 0)
				CMDRedo.CanExecute = false;
		}
		void openOptionsDialog(){
			Widget ed = this.FindByName("editor");
			Load ("#CrowEdit.ui.EditorOptions.crow").DataSource = ed;
		}
		void openFileDialog(){
			Load ("#CrowEdit.ui.openFile.crow").DataSource = this;
		}
		void saveFileDialog(){
			Load ("#CrowEdit.ui.saveFile.crow").DataSource = this;
		}
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

			if (!string.IsNullOrEmpty (fd.SelectedFile))
				CurFilePath = fd.SelectedFile;
			CurrentDir = fd.SelectedDirectory;

			System.Diagnostics.Debug.WriteLine (CurFileFullPath);
//			using (StreamWriter sr = new StreamWriter (fd.SelectedFile)) {
//				sr.Write(_text);
//			}
			_origText = _text;

			NotifyValueChanged ("IsDirty", false);
			NotifyValueChanged ("CurFileFullPath", (object)CurFileFullPath);
		}
		void onTextChanged (object sender, TextChangeEventArgs e)
		{
			//System.Diagnostics.Debug.WriteLine ("text changed");
			NotifyValueChanged ("IsDirty", IsDirty);
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
			CurFilePath = "unamed.txt";
			_origText = _text = "";
			NotifyValueChanged ("Text", (object)_text);
			NotifyValueChanged ("IsDirty", false);
			redoStack.Clear ();
			undoStack.Clear ();
			CMDRedo.CanExecute = false;
			CMDUndo.CanExecute = false;
			NotifyValueChanged ("CurFileFullPath", (object)CurFileFullPath);
		}
		void openFile (string filePath, string directory) {
			CurFilePath = filePath;
			CurrentDir = directory;

			redoStack.Clear ();
			undoStack.Clear ();
			CMDRedo.CanExecute = false;
			CMDUndo.CanExecute = false;

			NotifyValueChanged ("CurFileFullPath", (object)CurFileFullPath);
		}

		[STAThread]
		static void Main ()
		{
			using (CrowEdit win = new CrowEdit ()) 
				win.Run	();
		}
		public CrowEdit ()
			: base(800, 600)
		{}

		protected override void OnInitialized () {
			base.OnInitialized ();

			if (CurrentDir == null)
				CurrentDir = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);

			this.ValueChanged += CrowEdit_ValueChanged;
			initCommands ();
			Load ("#CrowEdit.ui.main.crow").DataSource = this;
			NotifyValueChanged ("CurFileFullPath", (object)CurFileFullPath);
		}

		/*void textView_KeyDown (object sender, Crow.KeyEventArgs e)
		{
			if (e.Control) {
				if (e.Key == Key.W) {
					if (e.Shift)
						CMDRedo.Execute ();
					else
						CMDUndo.Execute ();
				}
			}
		}*/

		void CrowEdit_ValueChanged (object sender, ValueChangeEventArgs e)
		{
			if (e.MemberName == "IsDirty" && isDirty != (bool)e.NewValue) {
				isDirty = (bool)e.NewValue;
				if (isDirty) {
					CMDSave.CanExecute = true;
					CMDSaveAs.CanExecute = true;
				}else{
					CMDSave.CanExecute = false;
					CMDSaveAs.CanExecute = false;
				}
			}
		}

	}
}

