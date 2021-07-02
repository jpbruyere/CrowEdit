// Copyright (c) 2021-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Crow;
using Crow.Text;

namespace CrowEditBase
{
	public class TextDocument : Document {
		public TextDocument (Interface iFace, string fullPath)
			: base (iFace, fullPath) {			
			reloadFromFile ();
		}

		string source, origSource;
		System.Text.Encoding encoding = System.Text.Encoding.UTF8;

		public string Source {
			get => source;
			set {
				if (source == value)
					return;
				source = value;
				NotifyValueChanged (source);
				NotifyValueChanged ("IsDirty", IsDirty);
				CMDSave.CanExecute = IsDirty;
			}
		}

		public override bool IsDirty => origSource != source;
				/// dictionnary of object per document client, when not null, client must reload content of document.
		Dictionary<object, List<TextChange>> registeredClients = new Dictionary<object, List<TextChange>>();
		public override bool TryGetState<T>(object client, out T state) {
			state = default;
			if (editorRWLock.TryEnterReadLock (10)) {				
				try {
					state = (T)(object)registeredClients[client];
					registeredClients[client] = null;
				} finally {					
					editorRWLock.ExitReadLock ();
				}
			}
			return state != null;
		}
		public override void RegisterClient(object client)
		{
			editorRWLock.EnterWriteLock ();
			registeredClients.Add (client, null);
			notifyClient (client, new TextChange (0, 0, Source));
			editorRWLock.ExitWriteLock ();
		}
		public override void UnregisterClient(object client)
		{
			editorRWLock.EnterWriteLock ();
			registeredClients.Remove (client);
			editorRWLock.ExitWriteLock ();
		}
		void notifyClients (TextChange tc, object triggeringClient = null) {
			object[] clients = registeredClients.Keys.ToArray ();
			for (int i = 0; i < clients.Length; i++) {
				if (clients[i] != triggeringClient)					
					notifyClient (clients[i], tc);
			}
		}
		void notifyClient (object client, TextChange tc) {
			if (registeredClients[client] == null)
				registeredClients[client] = new List<TextChange> ();
			registeredClients[client].Add (tc);
		}
		


		protected override void writeToDisk () {
			using (Stream s = new FileStream(FullPath, FileMode.Create)) {
				using (StreamWriter sw = new StreamWriter (s, encoding))
					sw.Write (source);
			}
			origSource = source;
			NotifyValueChanged ("IsDirty", IsDirty);
		}
		protected override void readFromDisk()
		{
			using (Stream s = new FileStream (FullPath, FileMode.Open)) {						
				using (StreamReader sr = new StreamReader (s)) {
					Source = origSource = sr.ReadToEnd ();
					encoding = sr.CurrentEncoding;
				}
			}
		}
		protected override void initNewFile()
		{
			Source = origSource = "";
		}
		protected override void reloadFromFile () {
			editorRWLock.EnterWriteLock ();
			try {
				if (File.Exists (FullPath))
					readFromDisk ();
				else
					initNewFile ();				
				resetUndoRedo ();
			} finally {
				editorRWLock.ExitWriteLock ();
			}
		}
		protected Stack<TextChange> undoStack = new Stack<TextChange> ();
		protected Stack<TextChange> redoStack = new Stack<TextChange> ();


		
		protected void saveFileDialog_OkClicked (object sender, EventArgs e)
		{
			FileDialog fd = sender as FileDialog;

			if (string.IsNullOrEmpty (fd.SelectedFileFullPath))
				return;

			if (File.Exists(fd.SelectedFileFullPath)) {
				MessageBox.ShowModal (iFace, MessageBox.Type.YesNo, "File exists, overwrite?")
					.Yes += (sender2, e2) => {
						FullPath = fd.SelectedFileFullPath;
						writeToDisk ();
					};
				return;
			}
			FullPath = fd.SelectedFileFullPath;
			writeToDisk ();
		}


		protected override void undo () {
			editorRWLock.EnterWriteLock ();
			try {
				if (undoStack.TryPop (out TextChange tc)) {
					redoStack.Push (tc.Inverse (source));
					CMDRedo.CanExecute = true;
					apply (tc);
					notifyClients (tc);
					//editor.SetCursorPosition (tch.End + tch.ChangedText.Length);
				}
				if (undoStack.Count == 0)
					CMDUndo.CanExecute = false;
			} finally {
				editorRWLock.ExitWriteLock ();
			}
		}
		protected override void redo () {
			editorRWLock.EnterWriteLock ();
			try {
				if (redoStack.TryPop (out TextChange tc)) {
					undoStack.Push (tc.Inverse (source));
					CMDUndo.CanExecute = true;
					apply (tc);
					notifyClients (tc);
				}
				if (redoStack.Count == 0)
					CMDRedo.CanExecute = false;
			} finally {
				editorRWLock.ExitWriteLock ();
			}

		}
		protected void resetUndoRedo () {
			undoStack.Clear ();
			redoStack.Clear ();
			CMDUndo.CanExecute = false;
			CMDRedo.CanExecute = false;			
		}
		protected bool disableTextChangedEvent = false;
		protected virtual void apply (TextChange change) {
			Span<char> tmp = stackalloc char[source.Length + (change.ChangedText.Length - change.Length)];
			ReadOnlySpan<char> src = source.AsSpan ();
			src.Slice (0, change.Start).CopyTo (tmp);
			if (!string.IsNullOrEmpty (change.ChangedText))
				change.ChangedText.AsSpan ().CopyTo (tmp.Slice (change.Start));
			src.Slice (change.End).CopyTo (tmp.Slice (change.Start + change.ChangedText.Length));
			Source = tmp.ToString ();
		}
		protected void applyTextChange (TextChange change, object triggeringEditor = null) {
			editorRWLock.EnterWriteLock ();
			try {
				undoStack.Push (change.Inverse (source));
				redoStack.Clear ();
				CMDUndo.CanExecute = true;
				CMDRedo.CanExecute = false;
				apply (change);
				notifyClients (change, triggeringEditor);
			} finally {
				editorRWLock.ExitWriteLock ();
			}

		}
		protected void onTextChanged (object sender, TextChangeEventArgs e) {
			applyTextChange (e.Change, sender);
		}
	}
}