// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)


using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using CrowEditBase;
using Crow;
using static CrowEditBase.CrowEditBase;

namespace CERoslynPlugin
{

	/*public enum CopyToOutputState {
		Never,
		Always,
		PreserveNewest
	}*/
	public class ProjectItemNode  : TreeNode, IFileNode 
	{

		ProjectItem projectItem;
		#region CTOR
		public ProjectItemNode (ProjectItem projectItem) {
			this.projectItem = projectItem;
		}
		#endregion		

		public override string Icon {
			get {
				switch (NodeType) {
				/*case NodeType.Reference:
					return CrowIDE.IcoReference;*/
				case NodeType.ProjectReference:
					return "#Crow.Icons.projectRef.svg";
				case NodeType.PackageReference:
					return "#icons.file_type_package.svg";
				case NodeType.ReferenceGroup:
					return "#icons.cubes.svg";
				case NodeType.VirtualGroup:
					return "#icons.folder.svg";
				case NodeType.Folder:
					return "#icons.folder.svg";
				case NodeType.EmbeddedResource:
				case NodeType.None:
				case NodeType.Compile:
					switch (Path.GetExtension (Caption).ToLower()) {
					case ".cs":
						return "#icons.file_type_csharp.svg";
					case ".svg":
						return "#icons.file_type_svg.svg";
					case ".crow":
					case ".xml":
						return "#icons.file_type_xml.svg";
					default:
						return "#icons.blank-file.svg";
					}
				default:
					return "#icons.blank-file.svg"; 
				}
			}
		}
		public string FullPath => 
			NodeType == NodeType.EmbeddedResource || NodeType == NodeType.None || NodeType == NodeType.Compile ?
				Path.Combine (GetRoot<ProjectNode>().Project.RootDir, projectItem.EvaluatedInclude) : null;

		public override CommandGroup Commands {
			get {
				switch (NodeType) {
				case NodeType.EmbeddedResource:
				case NodeType.None:
				case NodeType.Compile:
					return new CommandGroup (
						new Command ("Open", () => {
							App.OpenFile (FullPath);
						})
					);
				default:
					return null; 
				}
			}
		}
		public override string IconSub {
			get {
				switch (NodeType) {
				case NodeType.VirtualGroup:
				case NodeType.Folder:
					return IsExpanded.ToString();
				default:
					return null;
				}
			}
		
		}
		

		public override string Caption => Path.GetFileName (projectItem.EvaluatedInclude);
		public override NodeType NodeType {
			get {
				switch (projectItem.ItemType) {
					case "None":
						return NodeType.None;
					case "Compile":
						return NodeType.Compile;
					case "EmbeddedResource":
						return NodeType.EmbeddedResource;
					case "Reference":
						return NodeType.Reference;
					case "ProjectReference":
						return NodeType.ProjectReference;
					case "PackageReference":
						return NodeType.PackageReference;
					case "Folder":
						return NodeType.Folder;
					default:
						return NodeType.Unknown;
				}
			}
		}

		public override string ToString () => $"{NodeType}: {Caption}";
	}
}

