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
	public class FileNode : TreeNode
	{
		FileInfo info;
		Document doc;
		public override CommandGroup Commands =>
			new CommandGroup(
				new Command ("Open", Open),				
				new Command ("Delete", (sender0) => {
					MessageBox.ShowModal (CrowEditBase.App, MessageBox.Type.YesNo, $"Delete {info.Name}?").Yes += (sender, e) => {
						System.IO.File.Delete(info.FullName);
						Widget listContainer = ((sender0 as Widget).LogicalParent as Widget).DataSource as Widget;
						(listContainer.Parent as Group).RemoveChild(listContainer);
					};
				})
			);		
		public FileNode (FileInfo info, TreeNode parent = null) : base (parent) {
			this.info = info;
		}
		public override string Name => info.Name;
		
		public bool IsOpen => doc != null;

		public void Open () {
			doc = CrowEditBase.App.OpenOrSelectFile (info.FullName);
			if (doc is TextDocument td)
				CrowEditBase.App.CurrentDocument = td;
			NotifyValueChanged ("IsOpen", IsOpen);
		}
		public void OnOpenClick (object sender, EventArgs e) => Open();
	}


}
