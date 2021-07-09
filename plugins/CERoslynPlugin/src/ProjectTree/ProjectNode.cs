// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;
using Crow;
using CrowEditBase;

namespace CERoslynPlugin
{
	public class ProjectNode : TreeNode
	{
		public MSBuildProject Project { get; private set;}
		public ProjectNode (MSBuildProject project)	{
			Project = project;
		}

		public override string Caption => Project.Name;
		public override string Icon => "#icons.question.svg";
		public override NodeType NodeType => NodeType.VirtualGroup;
	}

}
