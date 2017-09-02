//
//  Main.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2017 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using Crow;
using System.IO;
using System.Collections.Generic;

namespace CrowEdit
{
	public class CrowEdit : CrowWindow
	{
		public Command CMDNew, CMDOpen, CMDSave, CMDSaveAs, CMDQuit, CMDUndo, CMDRedo, CMDCut, CMDCopy, CMDPaste, CMDHelp, CMDAbout;

		string _curDir = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
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
				NotifyValueChanged ("Text", _text);
				NotifyValueChanged ("IsDirty", IsDirty);
			}
		}
		bool isDirty = false;
		public bool IsDirty { get { return _text != _origText; }}

		public string CurrentDir {
			get { return Configuration.Get<string>("CurrentDir"); }
			set {
				if (CurrentDir == value)
					return;
				Configuration.Set ("CurrentDir", value);
				NotifyValueChanged ("CurrentDir", _curDir);
			}
		}
		public string CurFilePath {
			get { return _curFilePath; }
			set {
				if (_curFilePath == value)
					return;
				_curFilePath = value;
				NotifyValueChanged ("CurFilePath", _curFilePath);
			}
		}
		public string CurFileFullPath { get { return Path.Combine(CurrentDir,CurFilePath); }}

		void initCommands(){
			CMDNew = new Command(new Action(() => newFile())) { Caption = "New", Icon = new SvgPicture("#CrowEdit.ui.icons.blank-file.svg")};
			CMDOpen = new Command(new Action(() => openFileDialog())) { Caption = "Open...", Icon = new SvgPicture("#CrowEdit.ui.icons.outbox.svg")};
			CMDSave = new Command(new Action(() => saveFileDialog())) { Caption = "Save", Icon = new SvgPicture("#CrowEdit.ui.icons.inbox.svg"), CanExecute = false};
			CMDSaveAs = new Command(new Action(() => saveFileDialog())) { Caption = "Save As...", Icon = new SvgPicture("#CrowEdit.ui.icons.inbox.svg"), CanExecute = false};
			CMDQuit = new Command(new Action(() => Quit (null, null))) { Caption = "Quit", Icon = new SvgPicture("#CrowEdit.ui.icons.sign-out.svg")};
			CMDUndo = new Command(new Action(() => undo())) { Caption = "Undo", Icon = new SvgPicture("#CrowEdit.ui.icons.reply.svg"), CanExecute = false};
			CMDRedo = new Command(new Action(() => redo())) { Caption = "Redo", Icon = new SvgPicture("#CrowEdit.ui.icons.share-arrow.svg"), CanExecute = false};
			CMDCut = new Command(new Action(() => Quit (null, null))) { Caption = "Cut", Icon = new SvgPicture("#CrowEdit.ui.icons.scissors.svg"), CanExecute = false};
			CMDCopy = new Command(new Action(() => Quit (null, null))) { Caption = "Copy", Icon = new SvgPicture("#CrowEdit.ui.icons.copy-file.svg"), CanExecute = false};
			CMDPaste = new Command(new Action(() => Quit (null, null))) { Caption = "Paste", Icon = new SvgPicture("#CrowEdit.ui.icons.paste-on-document.svg"), CanExecute = false};
			CMDHelp = new Command(new Action(() => System.Diagnostics.Debug.WriteLine("help"))) { Caption = "Help", Icon = new SvgPicture("#CrowEdit.ui.icons.question.svg")};

		}
		void newFile(){
			CurFilePath = "unamed.txt";
			_origText = _text = "";
			NotifyValueChanged ("Text", _text);
			NotifyValueChanged ("IsDirty", false);
			redoStack.Clear ();
			undoStack.Clear ();
			CMDRedo.CanExecute = false;
			CMDUndo.CanExecute = false;
			NotifyValueChanged ("CurFileFullPath", CurFileFullPath);
		}
		void undo(){
			string step = undoStack [undoStack.Count -1];
			redoStack.Add (_text);
			CMDRedo.CanExecute = true;
			undoStack.RemoveAt(undoStack.Count -1);

			_text = step;

			NotifyValueChanged ("Text", _text);
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
			NotifyValueChanged ("Text", _text);
			NotifyValueChanged ("IsDirty", IsDirty);

			if (redoStack.Count == 0)
				CMDRedo.CanExecute = false;
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
			CurFilePath = fd.SelectedFile;
			CurrentDir = fd.SelectedDirectory;

//			redoStack.Clear ();
//			undoStack.Clear ();
//			CMDRedo.CanExecute = false;
//			CMDUndo.CanExecute = false;
//
			NotifyValueChanged ("CurFileFullPath", CurFileFullPath);
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
			NotifyValueChanged ("CurFileFullPath", CurFileFullPath);
		}
		void onTextChanged (object sender, TextChangeEventArgs e)
		{
			System.Diagnostics.Debug.WriteLine ("text changed");
			NotifyValueChanged ("IsDirty", IsDirty);
		}

		[STAThread]
		static void Main ()
		{
			using (CrowEdit win = new CrowEdit ()) {
				win.Run	(30);
			}
		}
		public CrowEdit ()
			: base(800, 600,"Crow Simple Editor")
		{}

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			if (CurrentDir == null)
				CurrentDir = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);

			this.ValueChanged += CrowEdit_ValueChanged;
			initCommands ();

			Load ("#CrowEdit.ui.main.crow").DataSource = this;
			NotifyValueChanged ("CurFileFullPath", CurFileFullPath);
		}

		void textView_KeyDown (object sender, Crow.KeyboardKeyEventArgs e)
		{
			if (e.Control) {
				if (e.Key == Key.W) {
					if (e.Shift)
						CMDRedo.Execute ();
					else
						CMDUndo.Execute ();
				}
			}
		}

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

