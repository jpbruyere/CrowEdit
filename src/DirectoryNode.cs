// Copyright (c) 2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Crow;

namespace CrowEditBase
{
	public class DirectoryNode : TreeNode
	{
		DirectoryInfo info;
		public DirectoryInfo Info => info;
		public DirectoryNode (DirectoryInfo info, TreeNode parent = null) : base (parent) {
			this.info = info;
		}
		public override string Name => info.Name;

		public override CommandGroup Commands =>
			new CommandGroup(
				new Command ("Set as root", ()=> {CrowEditBase.App.CurrentDir = info.FullName;})				
			);		
		public TreeNode [] GetFileSystemTreeNodeOrdered () 
			=> info.GetFileSystemInfos ().OrderBy (f => f.Attributes).ThenBy (f => f.Name)
			.Select (d=> d is DirectoryInfo dinfo ? (TreeNode)new DirectoryNode(dinfo, this) : (TreeNode)new FileNode (d as FileInfo, this)).ToArray ();

	}


}
