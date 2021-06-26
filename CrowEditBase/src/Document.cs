// Copyright (c) 2021-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;
using System.Threading;
using Crow;
using System.Runtime.CompilerServices;

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
		protected abstract void initCommands ();
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