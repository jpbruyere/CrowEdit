// Copyright (c) 2021-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;
using System.Threading;
using Crow;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace CrowEditBase
{
	public abstract class Document : IValueChange, ISelectable {		
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public void NotifyValueChanged (string MemberName, object _value)
		{
			//Debug.WriteLine ("Value changed: {0}->{1} = {2}", this, MemberName, _value);
			ValueChanged.Raise (this, new ValueChangeEventArgs (MemberName, _value));
		}
		public void NotifyValueChanged (object _value, [CallerMemberName] string caller = null)
		{
			NotifyValueChanged (caller, _value);
		}
		#endregion
	
		#region ISelectable implementation
		public event EventHandler Selected;
		public event EventHandler Unselected;
		static Dictionary<string, string> fileAssociations = new Dictionary<string, string> ();
		public static void AddFileAssociation (string extension, string fullDocumentClassName) {
			fileAssociations.Add (extension, fullDocumentClassName);
		}
		public static string GetDocumentClass (string extension) =>
			fileAssociations.ContainsKey (extension) ?
				fileAssociations[extension] : "CrowEditBase.TextDocument, CrowEditBase, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

		public virtual bool IsSelected {
			get { return isSelected; }
			set {
				if (value == isSelected)
					return;

				isSelected = value;

				NotifyValueChanged ("IsSelected", isSelected);
			}
		}
		public void SelectDocument () => IsSelected = true;
		public void UnselectDocument () => IsSelected = true;
		#endregion
		public Document (Interface iFace, string fullPath) {
			this.iFace = iFace;
			initCommands ();
			FullPath = fullPath;
		}
		protected Interface iFace;
		public event EventHandler CloseEvent;

		protected ReaderWriterLockSlim editorRWLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		public void EnterReadLock () => editorRWLock.EnterReadLock ();
		public void ExitReadLock () => editorRWLock.ExitReadLock ();

		public abstract bool TryGetState<T> (object client, out T state);
		public abstract void RegisterClient (object client);
		public abstract void UnregisterClient (object client);

		DateTime accessTime;
		string fullPath;
		bool isSelected;

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
			iFace.LoadIMLFragment (
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
		
		protected virtual void initCommands () {
			CMDUndo = new Command ("Undo", undo, "#CrowEdit.ui.icons.reply.svg",  false);
			CMDRedo = new Command ("Redo", redo, "#CrowEdit.ui.icons.share-arrow.svg", false);
			CMDSave = new Command ("save", Save, "#CrowEdit.ui.icons.inbox.svg", false);
			CMDSaveAs = new Command ("Save As...", SaveAs, "#CrowEdit.ui.icons.inbox.svg");
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
	}
}