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
		CELogger logger;
		internal CELogger Logger {
			get => logger;
			private set {
				if (logger == value)
					return;
				logger = value;
				NotifyValueChanged (logger);
			}
		}
		/*public LogLevel LogLevel {
			get => Crow.Configuration.Global.Get<LogLevel>("LogLevel");
			set {
				if (LogLevel == value)
					return;
				Crow.Configuration.Global.Set ("LogLevel", value);
				updateLogLevel ();
				NotifyValueChanged ("LogLevel", value);
			}
		}
		void updateLogLevel () {
			switch (LogLevel){
			case LogLevel.Minimal:
				Logger.Verbosity = Microsoft.Build.Framework.LoggerVerbosity.Minimal;
				break;
			case LogLevel.Normal:
				Logger.Verbosity = Microsoft.Build.Framework.LoggerVerbosity.Normal;
				break;
			case LogLevel.Full:
				Logger.Verbosity = Microsoft.Build.Framework.LoggerVerbosity.Detailed;
				break;
			case LogLevel.Debug:
				Logger.Verbosity = Microsoft.Build.Framework.LoggerVerbosity.Diagnostic;
				break;
			}
		}*/

		public RoslynService () : base () {
			configureDefaultSDKPathes ();
			//TODO static init to prevent rebinding on Service multiple instantiation
			AssemblyLoadContext pluginCtx = AssemblyLoadContext.GetLoadContext (Assembly.GetExecutingAssembly());
			pluginCtx.Resolving += msbuildResolve;

			foreach (string dll in Directory.GetFiles (MSBuildRoot, "*.dll")) {
				try {
					pluginCtx.LoadFromAssemblyPath (dll);
				} catch (Exception ex) {
					App.Log(LogType.Error, $"[RoslynService]{ex}");
				}
			}
			string capath = Path.Combine (MSBuildRoot, "Roslyn", "bincore");
			foreach (string dll in Directory.GetFiles (capath, "*.dll")) {
				try	{
					pluginCtx.LoadFromAssemblyPath (dll);
				} catch (Exception ex) {
					App.Log(LogType.Error, $"[RoslynService]{ex}");
				}

			}
		}
		Assembly msbuildResolve (AssemblyLoadContext context, AssemblyName assemblyName) {
			string assemblyPath = Path.Combine (MSBuildRoot, assemblyName.Name + ".dll");
			//return File.Exists (assemblyPath) ? context.LoadFromAssemblyPath (assemblyPath) : null;
			if (!File.Exists (assemblyPath))
				return null;
			Assembly a = context.LoadFromAssemblyPath (assemblyPath);
			App.Log (LogType.Message, $"[MSBuildResolve]{a},{a.CodeBase}");
			return a;
		}

		public override void Start() {
			Logger = new CELogger (
				Configuration.Global.Get<Microsoft.Build.Framework.LoggerVerbosity> ("CERoslyn.LoggerVerbosity",
					Microsoft.Build.Framework.LoggerVerbosity.Normal));
			//updateLogLevel ();

			Environment.SetEnvironmentVariable ("MSBUILD_EXE_PATH", Path.Combine (MSBuildRoot, "MSBuild.dll"));
			Environment.SetEnvironmentVariable ("MSBuildSDKsPath", Path.Combine (MSBuildRoot, "Sdks"));

			if (Environment.OSVersion.Platform == PlatformID.Unix)
				Environment.SetEnvironmentVariable ("FrameworkPathOverride", "/usr/lib/mono/4.5/");

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
		public Command CMDOptions_SelectSDKFolder => new ActionCommand ("...",
			() => {
				FileDialog dlg = App.LoadIMLFragment<FileDialog> (@"
				<FileDialog Caption='Select SDK Folder' CurrentDirectory='{SDKFolder}'
							ShowFiles='false' ShowHidden='true' />");
				dlg.OkClicked += (sender, e) => SDKFolder = (sender as FileDialog).SelectedFileFullPath;
				dlg.DataSource = this;
			}
		);
		public Command CMDOptions_SelectMSBuildRoot => new ActionCommand ("...",
			() => {
				FileDialog dlg = App.LoadIMLFragment<FileDialog> (@"
					<FileDialog Caption='Select MSBuild Root' CurrentDirectory='{MSBuildRoot}'
								ShowFiles='false' ShowHidden='true'/>");
				dlg.OkClicked += (sender, e) => MSBuildRoot = (sender as FileDialog).SelectedFileFullPath;
				dlg.DataSource = this;
			}
		);

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