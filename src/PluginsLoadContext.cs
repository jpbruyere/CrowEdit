// Copyright (c) 2021-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace CrowEdit
{
	public class PluginsLoadContext : AssemblyLoadContext {
		string pluginsDirectory;		
		public PluginsLoadContext (string pluginsDirectory)
			: base ("CrowEditPluginsContext", true) {
			this.pluginsDirectory = pluginsDirectory;

			loadPlugins ();			
		}
		void loadPlugins () {
			foreach (string f in Directory.GetFiles (pluginsDirectory)) {
				this.LoadFromAssemblyPath (f);
			}
		}
		protected override Assembly Load(AssemblyName assemblyName) => null;
	}
}
