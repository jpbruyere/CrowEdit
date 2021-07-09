// Copyright (c) 2021-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
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
	public class Plugin : CrowEditComponent {
		string FullPath;
		bool isLoaded;
		PluginsLoadContext loadContext;

		public Assembly Load (AssemblyName assemblyName)
			=> loadContext.LoadFromAssemblyName (assemblyName);
		public virtual bool IsLoaded {
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
		public readonly string Name;
		public Plugin (string fullPath) {
			initCommands ();			
			FullPath = fullPath;
			Name = Path.GetFileNameWithoutExtension (FullPath);
		}
		public Command CMDLoad, CMDUnload, CMDReload;
		public CommandGroup Commands => new CommandGroup (
			CMDLoad, CMDUnload, CMDReload);
		
		protected virtual void initCommands () {
			CMDLoad = new Command ("Load", Load, "#icons.reply.svg",  false);
			CMDUnload = new Command ("Unload", Unload, "#icons.share-arrow.svg", false);
			CMDReload = new Command ("Reload", () => { Unload(); Load();}, "#icons.refresh.svg", false);		
		}

		public void Load () {
			if (isLoaded)
				return;

			if (loadContext == null)
				loadContext = new PluginsLoadContext(FullPath);
			
			App.AddCrowAssembly (loadContext.MainAssembly);

			string defaultConfigName = loadContext.MainAssembly.GetManifestResourceNames ().FirstOrDefault(c=>c.EndsWith ("default.conf"));			
			if (!string.IsNullOrEmpty (defaultConfigName)) {
				Configuration config = new Configuration (loadContext.MainAssembly.GetManifestResourceStream (defaultConfigName));
				string fileAssociations = config.Get<string> ("FileAssociations");
				if (!string.IsNullOrEmpty (fileAssociations)) {
					try
					{
						foreach (string associations in fileAssociations.Split (';')) {
							string[] typeExts = associations.Split (':');
							Type clientClass = loadContext.MainAssembly.GetType (typeExts[0]);
							foreach (string ext in typeExts[1].Split (','))	
								App.AddFileAssociation (ext, clientClass);					
						}
					}
					catch (System.Exception ex)	{					
						throw;
					}
				}
			}

			IsLoaded = true;
		}
		public void Unload () {
			if (!isLoaded)
				return;

			App.RemoveCrowAssembly (loadContext.MainAssembly);

			IsLoaded = false;
		}
	}
}