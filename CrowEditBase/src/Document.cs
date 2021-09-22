// Copyright (c) 2021-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;
using System.Threading;
using Crow;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using static CrowEditBase.CrowEditBase;

namespace CrowEditBase
{
	public abstract class Document : CrowEditComponent {
		public Document (string fullPath, string editorPath) {
			initCommands ();
			EditorPath = editorPath;
			FullPath = fullPath;
		}
		/// <summary>
		/// Editor used to open the document, can't be changed once opened
		/// </summary>
		/// <remark>
		/// The editor path is used as an ID for itemTemplate selection
		/// </remark>
		/// <value></value>
		public string EditorPath { get; private set; }//the ressource path is used as an id for editor template selection.
		public event EventHandler CloseEvent;

		protected ReaderWriterLockSlim editorRWLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		public void EnterReadLock () => editorRWLock.EnterReadLock ();
		public void ExitReadLock () => editorRWLock.ExitReadLock ();

		public abstract bool TryGetState<T> (object client, out T state);
		public abstract void RegisterClient (object client);
		public abstract void UnregisterClient (object client);

		DateTime accessTime;
		string fullPath;

		public string FullPath {
			get => fullPath;
			set {
				if (fullPath == value)
					return;

				fullPath = value;

				NotifyValueChanged (fullPath);
				NotifyValueChanged ("FileName", (object)FileName);
				NotifyValueChanged ("FileDirectory", (object)Extension);
				NotifyValueChanged ("Extension", (object)Extension);
			}
		}
		public string FileDirectory => System.IO.Path.GetDirectoryName (FullPath);
		public string FileName => System.IO.Path.GetFileName (FullPath);
		public string Extension => System.IO.Path.GetExtension (FullPath);
		public bool ExternalyModified => File.Exists (FullPath) ?
			(DateTime.Compare (accessTime, System.IO.File.GetLastWriteTime (FullPath)) < 0) : false;
		public void OnQueryClose (object sender, EventArgs e){
			CloseEvent.Raise (this, null);
		}
		public void SaveAs () {
			App.LoadIMLFragment (
			"<FileDialog Width='60%' Height='50%' Caption='Save File' CurrentDirectory='{FileDirectory}' OkClicked='saveFileDialog_OkClicked'/>"
			).DataSource = this;
		}
		public void Save () {
			if (File.Exists (FullPath))
				writeToDisk ();
			else
				SaveAs ();
		}

		public Command CMDUndo, CMDRedo, CMDSave, CMDSaveAs;

		Command CMDClose, CMDCloseOther;
		public CommandGroup TabCommands => new CommandGroup (
			CMDClose, CMDCloseOther
		);

		protected virtual void initCommands () {
			CMDUndo = new ActionCommand ("Undo", undo, "#icons.reply.svg",  false);
			CMDRedo = new ActionCommand ("Redo", redo, "#icons.share-arrow.svg", false);
			CMDSave = new ActionCommand ("save", Save, "#icons.inbox.svg", false);
			CMDSaveAs = new ActionCommand ("Save As...", SaveAs, "#icons.inbox.svg");
			CMDClose = new ActionCommand ("Close", () => App.CloseDocument (this), "#icons.sign-out.svg");
			CMDCloseOther = new ActionCommand ("Close Others", () => App.CloseOthers (this), "#icons.inbox.svg");
		}
		protected abstract void undo();
		protected abstract void redo();
		protected abstract void writeToDisk ();
		protected abstract void readFromDisk ();
		protected abstract void initNewFile ();
		protected virtual void reloadFromFile () {
			editorRWLock.EnterWriteLock ();
			try {
				if (File.Exists (FullPath))
					readFromDisk ();
				else
					initNewFile ();
			} finally {
				editorRWLock.ExitWriteLock ();
			}
		}
		public abstract bool IsDirty { get; }

		public override string ToString() => FullPath;

		public override bool IsSelected {
			get => base.IsSelected;
			set {
				if (isSelected == value)
					return;
				base.IsSelected = value;
				if (App.TryFindFileNode (FullPath, out IFileNode node))
					(node as TreeNode).IsSelected = isSelected;
			}
		}
	}
}