﻿// Copyright (c) 2021-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;
using System.Linq;
using System.Threading;
using Crow;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using static CrowEditBase.CrowEditBase;

namespace CrowEditBase
{
	public abstract class Project : CrowEditComponent {		
		bool isLoaded;
		protected Project parent;
		protected IList<Project> subProjects;

		public Project Parent => parent;
		public IList<Project> SubProjects => subProjects;
		public IEnumerable<Project> Flatten {
			get {
				yield return this;
				if (subProjects != null) {
					foreach (var node in subProjects?.SelectMany (child => child.Flatten))
						yield return node;
				}
			}
		}
		public bool HasChildren => subProjects?.Count > 0;

		public string FullPath { get ; private set; }
		public abstract string Name { get; }
		public string Caption => Name;
		public bool IsLoaded {
			get { return isLoaded; }
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
		public virtual CommandGroup Commands => new CommandGroup (
			CMDLoad, CMDUnload, CMDReload, CMDClose);
		
		void initCommands () {
			CMDLoad = new Command ("Load", Load, "#icons.reply.svg",  false);
			CMDUnload = new Command ("Unload", Unload, "#icons.share-arrow.svg", false);
			CMDReload = new Command ("Reload", () => { Unload(); Load();}, "#icons.refresh.svg", false);		
			CMDClose = new Command ("Close", Close, "#icons.share-arrow.svg", true);
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
		public virtual string Icon => "#icons.question.svg";
	}
}