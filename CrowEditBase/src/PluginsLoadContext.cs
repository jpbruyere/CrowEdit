// Copyright (c) 2021-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Crow;
using static CrowEditBase.CrowEditBase;

namespace CrowEditBase
{
	
	/*public class Plugin {
		string path;
		Assembly assembly;
		Configuration config;

		public Plugin (Assembly assembly) {
			this.assembly = assembly;
		}
	}*/
	public class PluginsLoadContext : AssemblyLoadContext {
		public readonly Assembly MainAssembly;
		string pluginDirectory;
		public PluginsLoadContext (string pluginsDirectory)
			: base ($"CrowEditPluginsContext+{pluginsDirectory}", true) {
			this.pluginDirectory = pluginsDirectory;
			string pluginAssembly = Path.Combine (pluginsDirectory, $"{Path.GetFileName (pluginsDirectory)}.dll");
			MainAssembly = LoadFromAssemblyPath (pluginAssembly);
		}
		protected override Assembly Load(AssemblyName assemblyName) {			
			string assemblyPath = Path.Combine (pluginDirectory, assemblyName.Name + ".dll");			
			return File.Exists (assemblyPath) ? LoadFromAssemblyPath (assemblyPath) : null;
		}

	}
}
