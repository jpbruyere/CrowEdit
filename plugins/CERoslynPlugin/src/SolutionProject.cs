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

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;

using CrowEditBase;
using static CrowEditBase.CrowEditBase;

using Project = CrowEditBase.Project;

namespace CERoslynPlugin
{
	public class SolutionProject : Project {
		RoslynService roslynService;
		public SolutionProject (string fullPath) : base (fullPath) {
			roslynService = App.GetService<RoslynService> ();
			roslynService?.Start ();

			Load();

			if (FlattenProjetcs.OfType<MSBuildProject>().Any (msb => msb.IsCrowProject)) {
				Console.WriteLine ("Is crow project!!");
			}
		}

		SolutionFile solutionFile;

		internal ProjectCollection projectCollection { get; private set; }
		internal BuildParameters buildParams { get; private set; }
		public Configuration UserConfig { get; private set; }
		public IEnumerable<string> Configurations => solutionFile.SolutionConfigurations.Select (sc => sc.ConfigurationName).Distinct ().ToList ();
		public IEnumerable<string> Platforms => solutionFile.SolutionConfigurations.Select (sc => sc.PlatformName).Distinct ().ToList ();
		public string ActiveConfiguration {
			get => UserConfig.Get<string> ("ActiveConfiguration");
			set {
				if (ActiveConfiguration == value)
					return;
				UserConfig.Set ("ActiveConfiguration", value);
				NotifyValueChanged (value);
			}
		}
		public string ActivePlatform {
			get => UserConfig.Get<string> ("ActivePlatform");
			set {
				if (ActiveConfiguration == value)
					return;
				UserConfig.Set ("ActivePlatform", value);
				NotifyValueChanged (value);
			}
		}
		public override bool ContainsFile (string fullPath) =>
				FlattenProjetcs.Any (f => f.ContainsFile (fullPath));
		public override string Name => Path.GetFileNameWithoutExtension (FullPath);
		public override string Icon => "#icons.file_type_sln2.svg";
		public Project StartupProject {
			get => FlattenProjetcs.FirstOrDefault (p => p.FullPath == UserConfig.Get<string> ("StartupProject"));
			set {
				if (value == StartupProject)
					return;

				StartupProject?.NotifyValueChanged ("StatusIcon", (object)StartupProject?.StatusIcon);

				if (value == null)
					UserConfig.Set ("StartupProject", "");
				else {
					UserConfig.Set ("StartupProject", value.FullPath);
					value.NotifyValueChanged ("StatusIcon", (object)value.StatusIcon);
				}
				NotifyValueChanged ("StartupProject", StartupProject);
			}
		}

		public override NodeType NodeType => NodeType.ProjectGroup;
		public override IEnumerable<Project> FlattenProjetcs {
			get {
				foreach (var node in SubProjetcs.SelectMany (sp => sp.FlattenProjetcs))
					yield return node;
			}
		}

		public override void Load () {
			//Dictionary<string,string> globalProperties = new Dictionary<string, string>();
			//globalProperties.Add ("Configuration", "Debug");
			projectCollection = new ProjectCollection (
				null,//globalProperties,
				new ILogger [] { roslynService.Logger },
				ToolsetDefinitionLocations.Default
			);


			solutionFile = SolutionFile.Parse (FullPath);
			UserConfig = new Configuration (FullPath + ".user");


			//IDE.ProgressNotify (10);

			//projectCollection has to be recreated to change global properties
			if (string.IsNullOrEmpty (ActiveConfiguration))
				ActiveConfiguration = solutionFile.GetDefaultConfigurationName ();
			if (string.IsNullOrEmpty (ActivePlatform))
				ActivePlatform = solutionFile.GetDefaultPlatformName ();

			projectCollection.SetGlobalProperty ("RestoreConfigFile", Path.Combine (
							Path.Combine (
								Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), ".nuget"), "NuGet"),
								"NuGet.Config"));

			projectCollection.SetGlobalProperty ("DefaultItemExcludes", "obj/**/*;bin/**/*");

			//projectCollection.SetGlobalProperty ("RoslynTargetsPath", Path.Combine(roslynService.MSBuildRoot, "Roslyn"));

			//IDE.ProgressNotify (10);

			//ide.projectCollection.HostServices
			buildParams = new BuildParameters (projectCollection) {
				Loggers = projectCollection.Loggers,
				LogInitialPropertiesAndItems = true,
				LogTaskInputs = true,
				UseSynchronousLogging = true,
				ResetCaches = true,
				DetailedSummary = true
			};

			//projectCollection.IsBuildEnabled = false;

			BuildManager.DefaultBuildManager.ResetCaches ();

			//IDE.ProgressNotify (10);
			//ide.projectCollection.SetGlobalProperty ("RoslynTargetsPath", Path.Combine (Startup.msbuildRoot, @"Roslyn\"));
			//ide.projectCollection.SetGlobalProperty ("MSBuildSDKsPath", Path.Combine (Startup.msbuildRoot, @"Sdks\"));
			//ide.projectCollection.SetGlobalProperty ("MSBuildExtensionsPath", @"C:\Program Files\dotnet\sdk\5.0.100");
			//ide.projectCollection.SetGlobalProperty ("MSBuildBinPath", @"C:\Program Files\dotnet\sdk\5.0.100");
			//ide.projectCollection. ("MSBuildToolsPath", @"C:\Program Files\dotnet\sdk\5.0.100");
			//ide.projectCollection.to
			//------------

			TreeNode targetNode = this;
			foreach (ProjectInSolution pis in solutionFile.ProjectsInOrder) {
				/*if (!string.IsNullOrEmpty (pis.ParentProjectGuid))
					targetChildren = allSolutionNodes.FirstOrDefault (sn => sn.ProjectGuid == pis.ParentProjectGuid).Childs;
				else
					targetChildren = this.Children;*/

				switch (pis.ProjectType) {
				case SolutionProjectType.KnownToBeMSBuildFormat:
					targetNode.AddChild (new MSBuildProject (this, pis));
					break;
				/*case SolutionProjectType.SolutionFolder:
					targetChildren.Add (new SolutionFolder (this, pis));
					break;
				case SolutionProjectType.Unknown:
					break;
				case SolutionProjectType.WebProject:
					break;
				case SolutionProjectType.WebDeploymentProject:
					break;
				case SolutionProjectType.EtpSubProject:
					break;
				case SolutionProjectType.SharedProject:
					break;					*/
				/*default:
					targetChildren.Add (new SolutionNode (this, pis));
					break;*/
				}
				//IDE.ProgressNotify (10);
			}

			IsLoaded = true;
			//Console.WriteLine (projectCollection.Get ("Configuration"));
			/*if (StartupProject is MSBuildProject msbProj)
				msbProj?.DesignBuild();*/
		}

		void build (params string[] targets) {
			BuildRequestData buildRequest = new BuildRequestData (FullPath, null, "Current", targets, null);
			BuildResult buildResult = BuildManager.DefaultBuildManager.Build (buildParams, buildRequest);
		}
	}
}