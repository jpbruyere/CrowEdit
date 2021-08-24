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

			if (Flatten.OfType<MSBuildProject>().Any (msb => msb.IsCrowProject)) {
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
			get => projectCollection.GetGlobalProperty ("Configuration")?.ToString();			
			set {
				if (ActiveConfiguration == value)
					return;				
				projectCollection.SetGlobalProperty ("Configuration", value);
				NotifyValueChanged (value);
			}
		}
		public string ActivePlatform {
			get => projectCollection.GetGlobalProperty ("Platform")?.ToString();			
			set {
				if (ActivePlatform == value)
					return;				
				projectCollection.SetGlobalProperty ("Platform", value);
				NotifyValueChanged (value);
			}
		}
		public override string Name => Path.GetFileNameWithoutExtension (FullPath);
		public override string Icon => "#icons.file_type_sln2.svg";

		public override void Load () {
			projectCollection = new ProjectCollection (
				null,
				new ILogger [] { roslynService.Logger },
				ToolsetDefinitionLocations.Default
			);


			solutionFile = SolutionFile.Parse (FullPath);
			UserConfig = new Configuration (FullPath + ".user");

			//IDE.ProgressNotify (10);

			ActiveConfiguration = solutionFile.GetDefaultConfigurationName ();
			ActivePlatform = solutionFile.GetDefaultPlatformName ();

			projectCollection.SetGlobalProperty ("RestoreConfigFile", Path.Combine (
							Path.Combine (
								Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile), ".nuget"), "NuGet"),
								"NuGet.Config"));

			projectCollection.SetGlobalProperty ("SolutionDir", Path.GetDirectoryName (FullPath) + Path.DirectorySeparatorChar);			
			projectCollection.SetGlobalProperty ("DefaultItemExcludes", "obj/**/*;bin/**/*");

			//IDE.ProgressNotify (10);

			//ide.projectCollection.HostServices
			buildParams = new BuildParameters (projectCollection) {
				Loggers = projectCollection.Loggers,
				LogInitialPropertiesAndItems = false,
				LogTaskInputs = false,				
				UseSynchronousLogging = true
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

			subProjects = new List<Project> ();
			IList<Project> targetChildren = subProjects;
			foreach (ProjectInSolution pis in solutionFile.ProjectsInOrder) {
				/*if (!string.IsNullOrEmpty (pis.ParentProjectGuid))
					targetChildren = allSolutionNodes.FirstOrDefault (sn => sn.ProjectGuid == pis.ParentProjectGuid).Childs;
				else
					targetChildren = this.Children;*/

				switch (pis.ProjectType) {
				case SolutionProjectType.KnownToBeMSBuildFormat:					
					targetChildren.Add (new MSBuildProject (this, pis));
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
		}
	}
}