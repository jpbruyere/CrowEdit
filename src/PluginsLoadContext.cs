// Copyright (c) 2021-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;

namespace CrowEdit
{
	public class PluginsLoadContext : AssemblyLoadContext {
		List<string> loadedPlugins = new List<string> ();
		public PluginsLoadContext ()
			: base ("CrowEditPluginsContext", true) {

		}
		protected override Assembly Load(AssemblyName assemblyName)
		{
			return loadedPlugins.Contains (assemblyName.Name) ?
				base.Load(assemblyName) : AssemblyLoadContext.Default.LoadFromAssemblyName (assemblyName);
		}

	}
}
