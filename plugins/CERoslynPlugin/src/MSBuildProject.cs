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
using Microsoft.CodeAnalysis.CSharp;

namespace CERoslynPlugin
{
	public class MSBuildProject : Project {
		ProjectInSolution projectInSolution;
		SolutionProject solutionProject;
		Microsoft.Build.Evaluation.Project project;
		CSharpCompilationOptions compileOptions;
		public CSharpParseOptions parseOptions;

		public override string Name => projectInSolution.ProjectName;
		public string RootDir => project.DirectoryPath;

		public MSBuildProject (SolutionProject solution, ProjectInSolution projectInSolution) : base (projectInSolution.AbsolutePath) {
			this.projectInSolution = projectInSolution;
			this.solutionProject = solution;

			Load ();
		}

		public override void Load () {
			if (IsLoaded)
				return;
			try
			{
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
				
				IsLoaded = true;			
			}
			catch (System.Exception ex)
			{				
				Console.WriteLine (ex);
			}
		}
		public override void Unload () {

			IsLoaded = true;
		}
		public void Build () => Build ("Build");
		public void Build (params string[] targets)
		{
			ProjectInstance pi = BuildManager.DefaultBuildManager.GetProjectInstanceForBuild (project);			
			BuildRequestData request = new BuildRequestData (pi, targets,null,BuildRequestDataFlags.ProvideProjectStateAfterBuild);			
			BuildResult result = BuildManager.DefaultBuildManager.Build (solutionProject.buildParams, request);
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
		public IList<TreeNode> Children => rootNode.Childs;
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


		void populateTreeNodes ()
		{
			TreeNode root = new ProjectNode (this);
			VirtualNode refs = new VirtualNode ("References", NodeType.ReferenceGroup);
			root.AddChild (refs);


			foreach (ProjectItem pn in project.AllEvaluatedItems) {								
				//IDE.ProgressNotify (1);

				switch (pn.ItemType) {
				case "ProjectReferenceTargets":
					/*Commands.Add (new Crow.Command (new Action (() => Compile (pn.EvaluatedInclude))) {
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
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine ($"Unhandled Item Type: {pn.ItemType} {pn.EvaluatedInclude}");
					Console.ResetColor ();
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
	}
}