// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System.Diagnostics;

namespace NetcoreDbgPlugin
{
	[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
	public class StackFrame : CrowEditBase.StackFrame
	{
		public bool HasCLRAddress => ClrAddress != null;
		public CLRAddress ClrAddress;

		public StackFrame(MITupple frame)
		{
			if (frame.TryGetAttributeValue("level", out MIAttribute level))
				this.Level = int.Parse(level.Value);
			//File = frame.GetAttributeValue("file");
			FileFullPath = frame.GetAttributeValue("fullname")?.Replace("\\\\", "\\");
			int.TryParse(frame.GetAttributeValue("line"), out Line);
			int.TryParse(frame.GetAttributeValue("col"), out Column);
			int.TryParse(frame.GetAttributeValue("end-line"), out LineEnd);
			int.TryParse(frame.GetAttributeValue("end-col"), out ColumnEnd);
			Function = frame.GetAttributeValue("func");
			Address = frame.GetAttributeValue("addr");
			MITupple clrAddrs = frame["clr-addr"] as MITupple;
			if (clrAddrs != null)
				ClrAddress = new CLRAddress(clrAddrs);
		}
		public override string ToString() => $"{Level}:{FileFullPath}({Line},{Column} {Function})";
		string GetDebuggerDisplay() => ToString();
	}			
}
