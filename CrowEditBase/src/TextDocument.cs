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

namespace CrowEditBase
{
	public class TextDocument : Document {
		public TextDocument (string fullPath, string editorPath = "default")
			: base (fullPath, editorPath) {
			reloadFromFile ();
		}

		string source, origSource;
		System.Text.Encoding encoding = System.Text.Encoding.UTF8;
		protected bool mixedLineBreak = false;
		protected string lineBreak = null;

		public string Source {
			get => source;
			set {
				if (source == value)
					return;
				source = value;

				getLines();

				NotifyValueChanged (source);
				NotifyValueChanged ("IsDirty", IsDirty);
				CMDSave.CanExecute = IsDirty;
			}
		}
		protected LineCollection lines;

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
			source = tmp.ToString ();

			lines.Update (change);
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
		protected void getLines () {
			editorRWLock.EnterWriteLock ();
			if (lines == null)
				lines = new LineCollection (10);
			else
				lines.Clear ();

			if (string.IsNullOrEmpty (source))
				lines.Add (new TextLine (0, 0, 0));
			else
				lines.Update (source);
			editorRWLock.ExitWriteLock ();
		}
		public string GetLineBreak () {
			editorRWLock.EnterReadLock ();
			try {
				if (string.IsNullOrEmpty (lineBreak)) {
					mixedLineBreak = false;

					if (lines.Count == 0 || lines[0].LineBreakLength == 0)
						lineBreak = Environment.NewLine;
					else {
						lineBreak = source.GetLineBreak (lines[0]).ToString ();

						for (int i = 1; i < lines.Count; i++) {
							ReadOnlySpan<char> lb = source.GetLineBreak (lines[i]);
							if (!lb.SequenceEqual (lineBreak)) {
								mixedLineBreak = true;
								break;
							}
						}
					}
				}
				return lineBreak;
			} finally {
				editorRWLock.ExitReadLock();
			}
		}
		public CharLocation GetLocation (int absolutePosition) {
			editorRWLock.EnterReadLock ();
			try {
				return lines.GetLocation (absolutePosition);
			} finally {
				editorRWLock.ExitReadLock();
			}
		}
		public int GetAbsolutePosition (CharLocation loc) {
			editorRWLock.EnterReadLock ();
			try {
				return lines.GetAbsolutePosition (loc);
			} finally {
				editorRWLock.ExitReadLock();
			}
		}
		public CharLocation EndLocation {
			get {
				editorRWLock.EnterReadLock ();
				try {
					return new CharLocation (lines.Count - 1, lines[lines.Count - 1].Length);
				} finally {
					editorRWLock.ExitReadLock();
				}
			}
		}
		public int LinesCount {
			get {
				editorRWLock.EnterReadLock ();
				try {
					return lines.Count;
				} finally {
					editorRWLock.ExitReadLock();
				}
			}
		}
		public int Lenght {
			get {
				editorRWLock.EnterReadLock ();
				try {
					return source.Length;
				} finally {
					editorRWLock.ExitReadLock();
				}
			}
		}
		public TextLine GetLine (int index) {
			editorRWLock.EnterReadLock ();
			try {
				return lines[index];
			} finally {
				editorRWLock.ExitReadLock();
			}
		}
		public ReadOnlySpan<char> GetText (TextLine line) {
			editorRWLock.EnterReadLock ();
			try {
				return source.GetLine (line);
			} finally {
				editorRWLock.ExitReadLock();
			}
		}
		public ReadOnlySpan<char> GetText (TextSpan span) {
			editorRWLock.EnterReadLock ();
			try {
				return source.AsSpan (span.Start, span.Length);
			} finally {
				editorRWLock.ExitReadLock();
			}
		}
		public char GetChar (int pos){
			editorRWLock.EnterReadLock ();
			try {
				return source[pos];
			} finally {
				editorRWLock.ExitReadLock();
			}
		}

		public virtual CharLocation GetWordStart (CharLocation loc) {
			editorRWLock.EnterReadLock ();
			try {
				int pos = lines.GetAbsolutePosition (loc);
				//skip white spaces
				while (pos > 0 && !char.IsLetterOrDigit (source[pos-1]))
					pos--;
				while (pos > 0 && char.IsLetterOrDigit (source[pos-1]))
					pos--;
				return lines.GetLocation (pos);
			} finally {
				editorRWLock.ExitReadLock();
			}
		}
		public virtual CharLocation GetWordEnd (CharLocation loc) {
			editorRWLock.EnterReadLock ();
			try {
				int pos = lines.GetAbsolutePosition (loc);
				//skip white spaces
				while (pos < Lenght - 1 && !char.IsLetterOrDigit (source[pos]))
					pos++;
				while (pos < Lenght - 1 && char.IsLetterOrDigit (source[pos]))
					pos++;
				return lines.GetLocation (pos);
			} finally {
				editorRWLock.ExitReadLock();
			}
		}
	}
}