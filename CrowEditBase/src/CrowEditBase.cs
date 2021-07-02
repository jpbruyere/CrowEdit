// Copyright (c) 2021-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;
using System.Linq;
using Crow;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace CrowEditBase
{
	public abstract class CrowEditBase : Interface {
		public static CrowEditBase App;
		public CrowEditBase (int width, int height) : base (width, height) {
			App = this;
		}

		protected const string _defaultFileName = "unnamed.txt";

		TextDocument currentDocument;
		Editor currentEditor;
		public CommandGroup FileCommands, EditCommands;
		public ObservableList<TextDocument> OpenedDocuments = new ObservableList<TextDocument> ();
		public TextDocument CurrentDocument {
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

		public Document OpenOrSelectFile (string filePath) {
			if (string.IsNullOrEmpty (filePath))
				return null;
			TextDocument doc = OpenedDocuments.FirstOrDefault (d => d.FullPath == filePath);
			return doc ?? openOrCreateFile (filePath);
		}
		public void CloseFile (string filePath) =>
			closeDocument (OpenedDocuments.FirstOrDefault (d => d.FullPath == filePath));

		public void createNewFile(){
			openOrCreateFile (Path.Combine (CurFileDir, _defaultFileName));	
		}		

		protected abstract Document openOrCreateFile (string filePath);
		void closeDocument (TextDocument doc) {
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
			TextDocument doc = sender as TextDocument;
			if (doc.IsDirty) {
				MessageBox mb = MessageBox.ShowModal (this,
					                MessageBox.Type.YesNoCancel, $"{doc.FileName} has unsaved changes.\nSave it now?");
				mb.Yes += (object _sender, EventArgs _e) => { doc.Save (); closeDocument (doc); };
				mb.No += (object _sender, EventArgs _e) => closeDocument (doc);
			} else
				closeDocument (doc);
		}



	}
}