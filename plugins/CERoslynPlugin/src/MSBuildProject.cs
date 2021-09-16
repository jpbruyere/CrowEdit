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
using System.Runtime.InteropServices;
using System.Collections.Generic;

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;

using CrowEditBase;
using static CrowEditBase.CrowEditBase;

using Project = CrowEditBase.Project;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CERoslynPlugin
{
	public class MSBuildProject : Project {
		ProjectInSolution projectInSolution;
		SolutionProject solutionProject;
		Microsoft.Build.Evaluation.Project project;
		CSharpCompilationOptions compileOptions;
		CSharpParseOptions parseOptions;

		CommandGroup commands;


		public string RootDir => project.DirectoryPath;

		static string[] defaultTargets = { "Clean", "Restore", "Build", "Rebuild", "Pack", "Publish"};
		public override CommandGroup Commands => commands;
		public CommandGroup CMDSBuild { get; private set; }
		public Command CMDSetAsStartupProject { get; private set; }


		internal MSBuildProject (SolutionProject solution, ProjectInSolution projectInSolution) : base (projectInSolution.AbsolutePath) {
			this.projectInSolution = projectInSolution;
			this.solutionProject = solution;

			commands = new CommandGroup (CMDLoad, CMDUnload, CMDReload);

			CMDSetAsStartupProject = new ActionCommand ("Set as startup", () => solutionProject.StartupProject = this);
			CMDSBuild = new CommandGroup ("Build");

			foreach (string target in defaultTargets)
				CMDSBuild.Add (new ActionCommand (target, () => Build (target), null, false));

			commands.Add (CMDSBuild.Commands.ToArray());

			Load ();
		}


		public override void Load () {
			if (IsLoaded)
				return;
			try
			{
				using (var ctx = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext (this.GetType().Assembly).EnterContextualReflection()) {
					ProjectRootElement projectRootElt = ProjectRootElement.Open (projectInSolution.AbsolutePath);
					project = new Microsoft.Build.Evaluation.Project (projectInSolution.AbsolutePath, null, null, solutionProject.projectCollection);

					ProjectProperty msbuildProjExtPath = project.GetProperty ("MSBuildProjectExtensionsPath");
					ProjectProperty msbuildProjFile = project.GetProperty ("MSBuildProjectFile");

					string[] props = { "EnableDefaultItems", "EnableDefaultCompileItems", "EnableDefaultNoneItems", "EnableDefaultEmbeddedResourceItems" };

					foreach (string pr in props) {
						ProjectProperty pp = project.AllEvaluatedProperties.Where (ep => ep.Name == pr).FirstOrDefault ();
						if (pp == null)
							project.SetProperty (pr, "true");
					}

					project.ReevaluateIfNecessary ();

					parseOptions = CSharpParseOptions.Default;

					ProjectProperty langVersion = project.GetProperty ("LangVersion");
					if (langVersion != null && Enum.TryParse<LanguageVersion> (langVersion.EvaluatedValue, out LanguageVersion lv))
						parseOptions = parseOptions.WithLanguageVersion (lv);
					else
						parseOptions = parseOptions.WithLanguageVersion (LanguageVersion.Default);

					ProjectProperty constants = project.GetProperty ("DefineConstants");
					if (constants != null)
						parseOptions = parseOptions.WithPreprocessorSymbols (constants.EvaluatedValue.Split (';'));

					populateTreeNodes ();
				}

				if (OutputKind != OutputKind.DynamicallyLinkedLibrary)
					commands.Add (CMDSetAsStartupProject);

				CMDSBuild.ToggleAllCommand (true);

				IsLoaded = true;
			}
			catch (System.Exception ex)
			{
				Console.WriteLine (ex);
			}
		}
		public override void Unload () {
			CMDSBuild.ToggleAllCommand (false);
			if (commands.Contains (CMDSetAsStartupProject))
				commands.Remove (CMDSetAsStartupProject);
			IsLoaded = false;
		}
		public void Build () => Build ("Build");
		public void Build (params string[] targets)
		{
			//using (var ctx = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext (this.GetType().Assembly).EnterContextualReflection()) {
				ProjectInstance pi = BuildManager.DefaultBuildManager.GetProjectInstanceForBuild (project);
				BuildRequestData request = new BuildRequestData (pi, targets,null,BuildRequestDataFlags.ProvideProjectStateAfterBuild);
				BuildResult result = BuildManager.DefaultBuildManager.Build (solutionProject.buildParams, request);
			//}
		}

		TreeNode rootNode;
		public TreeNode RootNode {
			get => rootNode;
			set {
				if (rootNode == value)
					return;
				rootNode = value;
				NotifyValueChanged (rootNode);
				NotifyValueChanged ("Children", Children);
			}
		}
		public IList<TreeNode> Children => rootNode?.Childs;
		public override string Icon {
			get {
				switch (Path.GetExtension (FullPath)) {
				case ".csproj":
					return "#icons.file_type_csproj.svg";
				default:
					return "#icons.file_type_vscode.svg";
				}
			}
		}

		public bool IsCrowProject {
			get {
				foreach (ProjectItemNode reference in rootNode.Childs[0].Flatten.OfType<ProjectItemNode>()) {
					switch (reference.NodeType)	{
						case NodeType.PackageReference:
							if (reference.Caption == "Crow")
								return true;
							break;
						case NodeType.ProjectReference:
							if (App.TryGetProject<MSBuildProject> (reference.FullPath, out MSBuildProject msbp) && msbp.IsCrowProject)
								return true;
							break;
					}
				}
				return false;
			}
		}
		public bool IsStartupProject => solutionProject.StartupProject == this;
		public override bool ContainsFile (string fullPath) =>
			rootNode.Flatten.OfType<ProjectItemNode> ().Any (f => f.FullPath == fullPath);

		void populateTreeNodes ()
		{
			TreeNode root = new ProjectNode (this);
			VirtualNode refs = new VirtualNode ("References", NodeType.ReferenceGroup);
			root.AddChild (refs);


			foreach (ProjectItem pn in project.AllEvaluatedItems) {
				//IDE.ProgressNotify (1);

				switch (pn.ItemType) {
				case "ProjectReferenceTargets":
					/*Commands.Add (new Crow.Command (new Action (() => Build (pn.EvaluatedInclude))) {
						Caption = pn.EvaluatedInclude,
					});*/
					break;
				case "Reference":
				case "PackageReference":
				case "ProjectReference":
					refs.AddChild (new ProjectItemNode (pn));
					break;
				case "Compile":
				case "None":
				case "EmbeddedResource":
					TreeNode curNode = root;
					try {
						string file = pn.EvaluatedInclude;
						string treePath = file;
						if (pn.HasMetadata ("Link"))
							treePath = pn.GetMetadataValue ("Link");
						string [] folds = treePath.Split (new char [] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
						for (int i = 0; i < folds.Length - 1; i++) {
							TreeNode nextNode = curNode.Childs.OfType<VirtualNode>().FirstOrDefault (n => n.Caption == folds [i] && n.NodeType == NodeType.VirtualGroup);
							if (nextNode == null) {
								nextNode = new VirtualNode (folds [i], NodeType.VirtualGroup);
								curNode.AddChild (nextNode);
							}
							curNode = nextNode;
						}
						/*ProjectItemNode pi = new ProjectItemNode (this, pn);

						switch (Path.GetExtension (file)) {
						case ".cs":
							pi = new CSProjectItem (pi);
							break;
						case ".crow":
						case ".template":
						case ".goml":
						case ".itemp":
						case ".imtl":
							pi = new ImlProjectItem (pi);
							break;
						case ".style":
							pi = new StyleProjectItem (pi);
							break;
						default:
							pi = new ProjectFileNode (pi);
							break;
						}*/
						curNode.AddChild (new ProjectItemNode (pn));

					} catch (Exception ex) {
						Console.ForegroundColor = ConsoleColor.DarkRed;
						Console.WriteLine (ex);
						Console.ResetColor ();
					}

					break;
				default:
					/*Console.ForegroundColor = ConsoleColor.Grey;
					Console.WriteLine ($"Unhandled Item Type: {pn.ItemType} {pn.EvaluatedInclude}");
					Console.ResetColor ();*/
					break;
				}
			}
			root.SortChilds ();
			RootNode = root;

			/*foreach (var item in root.Childs) {
				Childs.Add (item);
				item.Parent = this;
			}*/
		}

		public override string Name => project == null ? projectInSolution.ProjectName : project.GetProperty ("MSBuildProjectName").EvaluatedValue;
		public string ToolsVersion => project.ToolsVersion;
		public string DefaultTargets => project.Xml.DefaultTargets;
		public ICollection<ProjectProperty> Properties => project.Properties;
		public ICollection<ProjectProperty> PropertiesSorted => project.Properties.OrderBy(p=>p.Name).ToList();
		public string AssemblyName => project.GetProperty ("AssemblyName").EvaluatedValue;
		public string OutputPath => project.GetProperty ("OutputPath").EvaluatedValue;
		public string IntermediateOutputPath => project.GetProperty ("IntermediateOutputPath").EvaluatedValue;
		public string OutputType => project.GetProperty ("OutputType").EvaluatedValue;
		public string OutputAssembly =>
			Path.Combine (project.GetPropertyValue ("OutputPath"), project.GetPropertyValue ("TargetFrameworks"), AssemblyName + AssemblyExtension);
		public string AssemblyExtension => RuntimeInformation.IsOSPlatform (OSPlatform.Windows) ? ".exe" : "";
		public OutputKind OutputKind {
			get {
                switch (OutputType) {
				case "Library":
					return OutputKind.DynamicallyLinkedLibrary;
				case "Exe":
					return OutputKind.ConsoleApplication;
				case "WinExe":
					return OutputKind.WindowsApplication;
				default:
					return OutputKind.ConsoleApplication;
                }
            }
        }
		public string RootNamespace => project.GetProperty ("RootNamespace").EvaluatedValue;
		public bool AllowUnsafeBlocks => bool.Parse (project.GetProperty ("AllowUnsafeBlocks").EvaluatedValue);
		public bool NoStdLib =>	bool.Parse (project.GetProperty ("NoStdLib").EvaluatedValue);
		public bool TreatWarningsAsErrors => bool.Parse (project.GetProperty ("TreatWarningsAsErrors").EvaluatedValue);
		public bool SignAssembly =>	bool.Parse (project.GetProperty ("SignAssembly").EvaluatedValue);
		public string TargetFrameworkVersion => project.GetProperty ("TargetFrameworkVersion").EvaluatedValue;
		public string Description => project.GetProperty ("Description").EvaluatedValue;
		public string StartupObject => project.GetProperty ("StartupObject").EvaluatedValue;
		public bool DebugSymbols => bool.Parse (project.GetProperty ("DebugSymbols").EvaluatedValue);
		public int WarningLevel => int.Parse (project.GetProperty ("WarningLevel").EvaluatedValue);


		public Stream GetStreamFromTargetPath (string targetPath) {
			IEnumerable<ProjectItemNode> piNodes = RootNode.Flatten.OfType<CERoslynPlugin.ProjectItemNode>();
			if (targetPath.StartsWith ('#')) {
				targetPath = targetPath.Substring (1);
				ProjectItemNode pin = piNodes.FirstOrDefault (n =>
					n.NodeType == NodeType.EmbeddedResource &&
					n.HasMetadataValue ("LogicalName", targetPath));
				if (pin != null)
					return new FileStream (pin.FullPath, FileMode.Open);
			} else {
				ProjectItemNode pin = piNodes.FirstOrDefault (n =>
					n.NodeType == NodeType.None &&
					(n.HasMetadataValue ("CopyToOutputDirectory", "PreserveNewest") || n.HasMetadataValue ("CopyToOutputDirectory", "Always")) &&
					n.EvaluatedInclude == targetPath);
				if (pin != null)
					return new FileStream (pin.FullPath, FileMode.Open);
			}
			return null;
		}
	}
}