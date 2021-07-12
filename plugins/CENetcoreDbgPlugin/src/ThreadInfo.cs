// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System.Diagnostics;

namespace NetcoreDbgPlugin
{
	public class ThreadInfo : CrowEditBase.ThreadInfo
	{
		public ThreadInfo(MITupple frame)
		{
			Id = int.Parse(frame.GetAttributeValue("id"));
			Name = frame.GetAttributeValue("name");
			IsStopped = frame.GetAttributeValue("state") == "stopped";
		}
	}
}
