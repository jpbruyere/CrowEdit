// Copyright (c) 2021-2025  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow.Text;

namespace CrowEditBase
{
	public class ReadOnlyTextBuffer {
		public readonly ReadOnlyMemory<char> Source;
		public readonly LineCollection Lines;
		public ReadOnlyTextBuffer(ReadOnlyMemory<char> source, LineCollection lines) {
			Source = source;
			Lines = lines;
		}
	}	
}