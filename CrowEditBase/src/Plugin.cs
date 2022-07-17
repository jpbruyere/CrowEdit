// Copyright (c) 2021-2022  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
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
		Type serviceClass;

		public Assembly Load (AssemblyName assemblyName)
			=> loadContext.LoadFromAssemblyName (assemblyName);

		public bool TryGet (AssemblyName assemblyName, out Assembly assembly) {
			assembly = loadContext.Assemblies.FirstOrDefault (a=>a.GetName().Name == assemblyName.Name);
			return assembly != null;
		}
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
			CMDLoad = new ActionCommand ("Load", Load, "#icons.reply.svg",  false);
			CMDUnload = new ActionCommand ("Unload", Unload, "#icons.share-arrow.svg", false);
			CMDReload = new ActionCommand ("Reload", () => { Unload(); Load();}, "#icons.refresh.svg", false);
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
				string mainService = config.Get<string> ("MainService");
				if (!string.IsNullOrEmpty (mainService)) {
					serviceClass = loadContext.MainAssembly.GetType (mainService);
					App.GetService (serviceClass)?.Start();
				}
				string fileAssociations = config.Get<string> ("FileAssociations");
				if (!string.IsNullOrEmpty (fileAssociations)) {
					try
					{
						foreach (string associations in fileAssociations.Split (';')) {
							string[] typeExts = associations.Split (':');
							Type clientClass = loadContext.MainAssembly.GetType (typeExts[0].Trim());
							foreach (string ext in typeExts[1].Split (','))//supported extension comma separated list
								App.AddFileAssociation (ext.Trim(), clientClass);
							if (typeExts.Length < 3)
								continue;
							foreach (string editorPath in typeExts[2].Split (','))//comma separated list of supported editor path.
								App.AddSupportedEditor (clientClass, editorPath.Trim());
						}
					}
					catch (System.Exception ex)	{
						Console.WriteLine ($"[Plugin]Error reading 'default.conf' for {FullPath}: {ex.Message}");
					}
				}
			}

			IsLoaded = true;
		}
		public void Unload () {
			if (!isLoaded)
				return;

			if (serviceClass != null)
				App.GetService (serviceClass)?.Stop();

			App.RemoveCrowAssembly (loadContext.MainAssembly);

			IsLoaded = false;
		}
	}
}