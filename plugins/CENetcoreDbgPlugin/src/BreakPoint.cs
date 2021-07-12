// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System.Diagnostics;

namespace NetcoreDbgPlugin
{
	[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
	public class BreakPoint : CrowEditBase.BreakPoint
	{
		public BreakPoint(string fileFullPath, int line, bool isEnabled = true) : base (fileFullPath, line, isEnabled) {
		}

		public void Update (MITupple bkpt) {
			Index = int.Parse (bkpt.GetAttributeValue("number"));
			Type = bkpt.GetAttributeValue("type");
			Disp = bkpt.GetAttributeValue("disp");
			IsEnabled = bkpt.GetAttributeValue("enabled") == "y";
			if (bkpt.TryGetAttributeValue("warning", out string warning))
				Warning = warning;
			else {
				Warning = null;
				Function = bkpt.GetAttributeValue("func");
				//FileName = bkpt.GetAttributeValue("file");
				FileFullPath = bkpt.GetAttributeValue("fullname")?.Replace("\\\\", "\\");
				Line = int.Parse (bkpt.GetAttributeValue("line")) - 1;

				/*if (project.TryGetProjectFileFromPath(FileFullName, out ProjectFileNode pf))
					File = pf as CSProjectItem;*/

			}			
		}

		public override string ToString() => $"{Index}:{Type} {FileFullPath}:{Line} enabled:{IsEnabled}";
		private string GetDebuggerDisplay() => ToString();
	}
}
