// Copyright (c) 2021-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Crow;
using Crow.Text;
using static CrowEditBase.CrowEditBase;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Diagnostics;

namespace CrowEditBase
{
	public class TextDocument : Document {
		public TextDocument (string fullPath, string editorPath = "default")
			: base (fullPath, editorPath) {
			reloadFromFile ();
		}

		protected TextBuffer buffer;
		public ReadOnlySpan<char> source => buffer.ReadOnlySpan;
		public ReadOnlyMemory<char> ImmutableBufferCopy => buffer.ReadOnlyCopy;
		internal LineCollection Lines => buffer.GetLineListCopy();
		System.Text.Encoding encoding = System.Text.Encoding.UTF8;

		public override bool IsDirty => buffer.IsDirty;
				/// dictionnary of object per document client, when not null, client must reload content of document.
		Dictionary<object, List<TextChange>> registeredClients = new Dictionary<object, List<TextChange>>();
		public override bool TryGetState<T>(object client, out T state) {
			state = default;
			if (documentRWLock.TryEnterReadLock (10)) {
				try {
					state = (T)(object)registeredClients[client];
					registeredClients[client] = null;
				} finally {
					documentRWLock.ExitReadLock ();
				}
			}
			return state != null;
		}
		public override void RegisterClient(object client, bool initState = false)
		{
			EnterWriteLock();
			registeredClients.Add (client, null);
			if (initState)
				notifyClient (client,new TextChange (0, 0,source.ToString()));				
			ExitWriteLock();
		}
		public override void UnregisterClient(object client)
		{
			EnterWriteLock();
			registeredClients.Remove (client);
			ExitWriteLock();
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
					sw.Write (buffer.Span);
			}
			buffer.ResetDirtyState();
			NotifyValueChanged ("IsDirty", IsDirty);
			CMDSave.CanExecute = IsDirty;
		}
		protected override void readFromDisk()
		{
			using (Stream s = new FileStream (FullPath, FileMode.Open)) {
				using (StreamReader sr = new StreamReader (s)) {
					buffer = new TextBuffer(sr.ReadToEnd ());
					encoding = sr.CurrentEncoding;
				}
			}
		}
		protected override void initNewFile()
		{
			buffer = new TextBuffer("");
		}
		protected override void reloadFromFile () {
			documentRWLock.EnterWriteLock ();
			try {
				if (File.Exists (FullPath))
					readFromDisk ();
				else
					initNewFile ();
				resetUndoRedo ();
			} finally {
				documentRWLock.ExitWriteLock ();
			}
		}
		protected Stack<TextChange> undoStack = new Stack<TextChange> ();
		protected Stack<TextChange> redoStack = new Stack<TextChange> ();
		protected override void saveFileDialog_OkClicked (object sender, EventArgs e)
		{
			FileDialog fd = sender as FileDialog;

			if (string.IsNullOrEmpty (fd.SelectedFileFullPath))
				return;

			if (File.Exists(fd.SelectedFileFullPath)) {
				MessageBox.ShowModal (App, MessageBox.Type.YesNo, "File exists, overwrite?")
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
			documentRWLock.EnterWriteLock ();
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
				documentRWLock.ExitWriteLock ();
			}
		}
		protected override void redo () {
			documentRWLock.EnterWriteLock ();
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
				documentRWLock.ExitWriteLock ();
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

			buffer.Update(change);

			NotifyValueChanged ("IsDirty", IsDirty);
			CMDSave.CanExecute = IsDirty;			
		}
		protected void applyTextChange (TextChange change, object triggeringEditor = null) {
			documentRWLock.EnterWriteLock ();
			try {
				undoStack.Push (change.Inverse (source));
				redoStack.Clear ();
				CMDUndo.CanExecute = true;
				CMDRedo.CanExecute = false;
				apply (change);
				notifyClients (change, triggeringEditor);
			} finally {
				documentRWLock.ExitWriteLock ();
			}
		}
		protected void onTextChanged (object sender, TextChangeEventArgs e) {
			applyTextChange (e.Change, sender);
		}

		public string GetLineBreak () {
			documentRWLock.EnterReadLock ();
			try {
				return buffer.GetLineBreak();
			} finally {
				documentRWLock.ExitReadLock();
			}
		}
		public CharLocation GetLocation (int absolutePosition) {
			documentRWLock.EnterReadLock ();
			try {
				return buffer.GetLocation (absolutePosition);
			} finally {
				documentRWLock.ExitReadLock();
			}
		}
		public int GetAbsolutePosition (CharLocation loc) {
			documentRWLock.EnterReadLock ();
			try {
				return buffer.GetAbsolutePosition (loc);
			} finally {
				documentRWLock.ExitReadLock();
			}
		}
		public CharLocation EndLocation {
			get {
				documentRWLock.EnterReadLock ();
				try {
					return buffer.EndLocation;
				} finally {
					documentRWLock.ExitReadLock();
				}
			}
		}
		public int LinesCount {
			get {
				documentRWLock.EnterReadLock ();
				try {
					return buffer.LinesCount;
				} finally {
					documentRWLock.ExitReadLock();
				}
			}
		}
		public int Length {
			get {
				documentRWLock.EnterReadLock ();
				try {
					return buffer.Length;
				} finally {
					documentRWLock.ExitReadLock();
				}
			}
		}
		public TextLine GetLine (int index) {
			documentRWLock.EnterReadLock ();
			try {
				return buffer.GetLine(index);
			} finally {
				documentRWLock.ExitReadLock();
			}
		}
		public void SetLine (int index, TextLine newValue) {
			documentRWLock.EnterReadLock ();
			try {
				buffer.SetLine(index, newValue);
			} finally {
				documentRWLock.ExitReadLock();
			}
		}		
		public ReadOnlySpan<char> GetLineText (int index) {
			documentRWLock.EnterReadLock ();
			try {
				return buffer.GetText (buffer.GetLine(index));
			} finally {
				documentRWLock.ExitReadLock();
			}
		}		
		public ReadOnlySpan<char> GetText (TextLine line) {
			documentRWLock.EnterReadLock ();
			try {
				return buffer.GetText (line);
			} finally {
				documentRWLock.ExitReadLock();
			}
		}
		public ReadOnlySpan<char> GetText (TextSpan span) {
			documentRWLock.EnterReadLock ();
			try {
				return buffer.GetText (span);
			} finally {
				documentRWLock.ExitReadLock();
			}
		}
		public char GetChar (int pos){
			documentRWLock.EnterReadLock ();
			try {
				return source[pos];
			} finally {
				documentRWLock.ExitReadLock();
			}
		}

		public virtual CharLocation GetWordStart (CharLocation loc) {
			documentRWLock.EnterReadLock ();
			try {
				int pos = buffer.GetAbsolutePosition (loc);
				//skip white spaces
				while (pos > 0 && !char.IsLetterOrDigit (source[pos-1]))
					pos--;
				while (pos > 0 && char.IsLetterOrDigit (source[pos-1]))
					pos--;
				return buffer.GetLocation (pos);
			} finally {
				documentRWLock.ExitReadLock();
			}
		}
		public virtual CharLocation GetWordEnd (CharLocation loc) {
			documentRWLock.EnterReadLock ();
			try {
				int pos = buffer.GetAbsolutePosition (loc);
				//skip white spaces
				while (pos < Length - 1 && !char.IsLetterOrDigit (source[pos]))
					pos++;
				while (pos < Length - 1 && char.IsLetterOrDigit (source[pos]))
					pos++;
				return buffer.GetLocation (pos);
			} finally {
				documentRWLock.ExitReadLock();
			}
		}
	}
}