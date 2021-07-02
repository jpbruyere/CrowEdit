using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Crow;
using Crow.Text;
using CrowEditBase;

namespace CrowEdit
{
    public static class Extensions
    {
        public static TextChange Inverse (this TextChange tch, string src)
            => new TextChange (tch.Start, string.IsNullOrEmpty (tch.ChangedText) ? 0 : tch.ChangedText.Length,
                tch.Length == 0 ? "" : src.AsSpan (tch.Start, tch.Length).ToString());
		public static CommandGroup GetCommands (this System.IO.DirectoryInfo di) =>
			new CommandGroup(
				new Command ("Set as root", ()=> {CrowEdit.App.CurrentDir = di.FullName;})				
			);		
		public static CommandGroup GetCommands (this System.IO.FileInfo fi) =>
			new CommandGroup(
				new Command ("Open", ()=> {CrowEdit.App.OpenOrSelectFile (fi.FullName);}),
				new Command ("Close", ()=> {CrowEdit.App.CloseFile (fi.FullName);},null, CrowEdit.App.IsOpened (fi.FullName)),
				new Command ("Delete", (sender0) => {
					MessageBox.ShowModal (CrowEdit.App, MessageBox.Type.YesNo, $"Delete {fi.Name}?").Yes += (sender, e) => {
						System.IO.File.Delete(fi.FullName);
						Widget listContainer = ((sender0 as Widget).LogicalParent as Widget).DataSource as Widget;
						(listContainer.Parent as Group).RemoveChild(listContainer);
					};
				})
			);
		public static void OpenWithCrowEdit (this System.IO.FileInfo fi, object sender = null, EventArgs e = null) => CrowEdit.App.OpenOrSelectFile (fi.FullName);

		public static TreeNode [] GetFileSystemTreeNodeOrdered (this DirectoryInfo di) 
			=> di.GetFileSystemInfos ().OrderBy (f => f.Attributes).ThenBy (f => f.Name).Cast<TreeNode> ().ToArray ();
			

    }
}
