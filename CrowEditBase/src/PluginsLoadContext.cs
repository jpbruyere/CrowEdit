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
		public readonly string Name;
		readonly string fullPath;
		public PluginsLoadContext (string pluginDirectory)
			: base (Path.GetFileName (pluginDirectory), false) {
			fullPath = pluginDirectory;
			Name = Path.GetFileName (pluginDirectory);

			string pluginAssembly = Path.Combine (fullPath, $"{Name}.dll");
			MainAssembly = LoadFromAssemblyPath (pluginAssembly);
		}
		protected override Assembly Load(AssemblyName assemblyName) {			
			string assemblyPath = Path.Combine (fullPath, assemblyName.Name + ".dll");			
			return File.Exists (assemblyPath) ? LoadFromAssemblyPath (assemblyPath) : null;
		}

	}
}
