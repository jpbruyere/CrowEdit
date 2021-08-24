// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;
using Crow;

namespace CrowEditBase
{
	public class VirtualNode : TreeNode
	{
		NodeType nodeType;
		string caption;
		public VirtualNode (string caption, NodeType type)	{
			this.caption = caption;
			nodeType = type;
		}

		public override string Icon => "#icons.folder.svg";
		public override string IconSub => IsExpanded.ToString ();

		public override string Caption => caption;
		public override NodeType NodeType => nodeType;

		public override CommandGroup Commands {
			get {
				return null; 
			}
		}
	}

}
