// Copyright (c) 2013-2019  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using CrowEditBase;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using static CrowEditBase.CrowEditBase;
using Crow;


namespace CERoslynPlugin
{	
	public class RoslynService : Service {
		public RoslynService () : base () {
			configureDefaultSDKPathes ();
			//TODO static init to prevent rebinding on Service multiple instantiation
			AssemblyLoadContext pluginCtx = AssemblyLoadContext.GetLoadContext (Assembly.GetExecutingAssembly());
			pluginCtx.Resolving += msbuildResolve;
		}
		Assembly msbuildResolve (AssemblyLoadContext context, AssemblyName assemblyName) {
			string assemblyPath = Path.Combine (MSBuildRoot, assemblyName.Name + ".dll");
			return File.Exists (assemblyPath) ? Assembly.LoadFrom (assemblyPath) : null;
		}

		public override void Start() {
			

			CurrentState = Status.Running;

		}
		public override void Stop()
		{
			CurrentState = Status.Stopped;
		}
		public override void Pause()
		{
			CurrentState = Status.Paused;
		}
		/*public override Document OpenDocument (string fullPath) {
			if (!IsRunning)
				Start ();
			using (AssemblyLoadContext.ContextualReflectionScope loadCtx = msBuildLoadCtx.EnterContextualReflection ()){
				Type t = Type.GetType ("CERoslynPlugin.CSDocument");
				Console.WriteLine (AssemblyLoadContext.CurrentContextualReflectionContext.Name);
				Console.WriteLine ($"{AssemblyLoadContext.GetLoadContext(t.Assembly).Name}");
				return (Document)Activator.CreateInstance (t, new object[] {fullPath});
			}
		}*/
		public override string ConfigurationWindowPath => "#CERoslynPlugin.ui.winConfiguration.crow";		

		public string SDKFolder {
			get => Configuration.Global.Get<string> ("SDKFolder");
			set {
				if (SDKFolder == value)
					return;
				Configuration.Global.Set ("SDKFolder", value);
				NotifyValueChanged (SDKFolder);
			}
		}
		public string MSBuildRoot {
			get => Configuration.Global.Get<string> ("MSBuildRoot");
			set {
				if (MSBuildRoot == value)
					return;
				Configuration.Global.Set ("MSBuildRoot", value);
				NotifyValueChanged (MSBuildRoot);
			}
		}
		public Command CMDOptions_SelectSDKFolder => new Command ("...",
			() => {				
				FileDialog dlg = App.LoadIMLFragment<FileDialog> (@"
				<FileDialog Caption='Select SDK Folder' CurrentDirectory='{SDKFolder}'
							ShowFiles='false' ShowHidden='true' />");
				dlg.OkClicked += (sender, e) => SDKFolder = (sender as FileDialog).SelectedFileFullPath;
				dlg.DataSource = this;
			}
		);
		public Command CMDOptions_SelectMSBuildRoot => new Command ("...",
			() => {
				FileDialog dlg = App.LoadIMLFragment<FileDialog> (@"
					<FileDialog Caption='Select MSBuild Root' CurrentDirectory='{MSBuildRoot}'
								ShowFiles='false' ShowHidden='true'/>");
				dlg.OkClicked += (sender, e) => MSBuildRoot = (sender as FileDialog).SelectedFileFullPath;
				dlg.DataSource = this;
			}
		);
		/*public Command CMDOptions_SelectNetcoredbgPath = new Command ("...",
			(sender) => {
				FileDialog dlg = App.LoadIMLFragment<FileDialog> (@"
					<FileDialog Caption='Select netcoredbg executable path' CurrentDirectory='{NetcoredbgPath}'
								ShowFiles='true' ShowHidden='true'/>
				");
				dlg.OkClicked += (sender, e) => ide.NetcoredbgPath = (sender as FileDialog).SelectedFileFullPath;
				dlg.DataSource = ide;
			}
		);*/


		void configureDefaultSDKPathes ()
		{			
			if (string.IsNullOrEmpty (SDKFolder)) {
				switch (Environment.OSVersion.Platform) {
				case PlatformID.Win32S:
				case PlatformID.Win32Windows:
				case PlatformID.Win32NT:
				case PlatformID.WinCE:
					SDKFolder = @"C:\Program Files\dotnet\sdk\";
					break;
				case PlatformID.Unix:
					SDKFolder = @"/usr/share/dotnet/sdk";
					break;
				default:
					throw new NotSupportedException ();
				}				
			}

			if (!string.IsNullOrEmpty (MSBuildRoot) && Directory.Exists(MSBuildRoot))
				return;

			List<SDKVersion> versions = new List<SDKVersion> ();
			foreach (string dir in Directory.EnumerateDirectories (SDKFolder)) {
				string dirName = Path.GetFileName (dir);
				if (SDKVersion.TryParse (dirName, out SDKVersion vers))
					versions.Add (vers);
			}
			versions.Sort ((a, b) => a.ToInt.CompareTo (b.ToInt));
			MSBuildRoot = versions.Count > 0 ? Path.Combine (SDKFolder, versions.Last ().ToString ()) : SDKFolder;
		}
	
		public class SDKVersion
		{
			public int major, minor, revision;
			public static bool TryParse (string versionString, out SDKVersion version) {
				version = null;
				if (string.IsNullOrEmpty (versionString))
					return false;
				string [] verNums = versionString.Split ('.');
				if (verNums.Length != 3)
					return false;
				if (!int.TryParse (verNums [0], out int maj))
					return false;
				if (!int.TryParse (verNums [1], out int min))
					return false;
				if (!int.TryParse (verNums [2], out int rev))
					return false;
				version = new SDKVersion { major = maj, minor = min, revision = rev };
				return true;
			}
			public long ToInt => major << 62 + minor << 60 + revision;
			public override string ToString () => $"{major}.{minor}.{revision}";
		}		
	}
}