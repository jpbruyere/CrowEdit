// Copyright (c) 2021-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System.Collections.Generic;
using System.Linq;
using Crow;
using static CrowEditBase.CrowEditBase;

namespace CrowEditBase
{
	public abstract class Project : TreeNode {
		bool isLoaded;
		protected Project parent;
		public abstract bool ContainsFile (string fullPath);
		public IEnumerable<Project> SubProjetcs => Childs.OfType<Project> ();
		public virtual IEnumerable<Project> FlattenProjetcs {
			get {
				yield return this;
				foreach (var node in SubProjetcs.SelectMany (sp => sp.FlattenProjetcs))
					yield return node;
			}
		}
		public string FullPath { get ; private set; }
		public abstract string Name { get; }
		public override string Caption => Name;
		public override NodeType NodeType => NodeType.Project;

		public bool IsLoaded {
			get => isLoaded;
			set {
				if (value == isLoaded)
					return;

				isLoaded = value;

				NotifyValueChanged (isLoaded);

				CMDLoad.CanExecute = !IsLoaded;
				CMDReload.CanExecute = CMDUnload.CanExecute = IsLoaded;
			}
		}
		public Project (string fullPath) {
			initCommands ();
			FullPath = fullPath;
		}
		public Command CMDLoad, CMDUnload, CMDReload, CMDClose;
		public override CommandGroup Commands => new CommandGroup (
			CMDLoad, CMDUnload, CMDReload, CMDClose);

		void initCommands () {
			CMDLoad = new ActionCommand ("Load", Load, "#icons.reply.svg",  false);
			CMDUnload = new ActionCommand ("Unload", Unload, "#icons.share-arrow.svg", false);
			CMDReload = new ActionCommand ("Reload", () => { Unload(); Load();}, "#icons.refresh.svg", false);
			CMDClose = new ActionCommand ("Close", Close, "#icons.share-arrow.svg", true);
		}

		public abstract void Load ();
		public virtual void Unload () {
			IsLoaded = false;
		}
		public virtual void Close () {
			if (App.CurrentProject == this)
				App.CurrentProject = null;
			App.Projects.Remove (this);
			IsLoaded = false;
		}
		public override string Icon => "#icons.question.svg";
	}
}