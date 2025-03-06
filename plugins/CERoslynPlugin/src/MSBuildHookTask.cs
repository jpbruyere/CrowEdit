using System;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using CERoslynPlugin;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace CrowIdeBuildTasks
{
	public class CrowIdeHookBuildEvent : CustomBuildEventArgs {
		public string HookedItemsName;
		public string ProjectFullPath;
		public ITaskItem[] HookedItems;
		public CrowIdeHookBuildEvent (string hookedItemsName, string projectFullPath, ITaskItem[] hookedItems)
			: base ($"CrowIde Hook: Project:{projectFullPath} Items:{hookedItemsName}", "ResolvedReferences", "HookTask") {
				HookedItemsName = hookedItemsName;
				ProjectFullPath = projectFullPath;
				HookedItems = hookedItems;
			}
	}
	public class CEHookTask : Task
	{

		public ITaskItem[] HookedItems {
			get;
			set;
		}
		public ITaskItem ProjectFullPath {
			get;
			set;
		}
		public ITaskItem HookedItemsName {
			get;
			set;
		}
		public ITaskItem OutputDirectory {
			get;
			set;
		}


		public override bool Execute () {
			var host = this.HostObject;
			if (host != null) {
				host.GetType().GetMethod("MSBuildHookTaskCallBack").Invoke(host, new object[] {HookedItemsName, HookedItems});
				Log.LogMessage (MessageImportance.High, $"CEHookTask -> {HookedItemsName.ToString()}");
			} 
			
			//BuildEngine.LogCustomEvent (new CrowIdeHookBuildEvent (HookedItemsName.ToString(), ProjectFullPath.ToString(), HookedItems));
/*			string path = OutputDirectory == null ? "" : OutputDirectory.ToString();
			path = Path.Combine(path, $"{HookedItemsName.ToString()}.txt");
			using (Stream s = new FileStream(path, FileMode.Create)) {
				using (StreamWriter sw = new StreamWriter (s)) {
					sw.WriteLine ($"{ProjectFullPath.ToString()} -> {HookedItemsName.ToString()}");
					if (HookedItems != null) {
						foreach	(ITaskItem ti in HookedItems) {
							sw.WriteLine ($"\t{ti.ToString()}");
							foreach (var mn in ti.MetadataNames)
								sw.WriteLine ($"\t\t{mn, -50} = {ti.GetMetadata (mn.ToString())}");
						}
						Log.LogMessage (MessageImportance.High, $"HookTask -> {HookedItemsName.ToString()}, {path}");
					}
				}
			}
*/			
			
			return true;
		}
	}
}
