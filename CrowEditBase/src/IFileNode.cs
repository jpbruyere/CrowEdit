// Copyright (c) 2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;
using Crow;
using CrowEditBase;
using static CrowEditBase.CrowEditBase;

namespace CrowEditBase
{	
	public interface IFileNode
	{
		string FullPath { get; }
	}
}
