// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System.Diagnostics;

namespace NetcoreDbgPlugin
{
	[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
	public class CLRAddress
	{
		public string ModuleID;
		public string MethodToken;
		public long IlOffset;
		public long NativeOffset;
		public CLRAddress(MITupple clrAddress)
		{
			ModuleID = clrAddress.GetAttributeValue("module-id");
			MethodToken = clrAddress.GetAttributeValue("method-token");
			IlOffset = long.Parse(clrAddress.GetAttributeValue("il-offset"));
			NativeOffset = long.Parse(clrAddress.GetAttributeValue("native-offset"));
		}
		public override string ToString() => $"Mod:{ModuleID} Meth:{MethodToken} IL:{IlOffset} Native:{NativeOffset}";
		private string GetDebuggerDisplay() => ToString();
	}
}
