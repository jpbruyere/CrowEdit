// Copyright (c) 2013-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;

namespace CrowEditBase
{
	public class TokenizerException : Exception {
		public readonly int Position;
		public TokenizerException (int position, string message) : base (message) {
			Position = position;
		}
	}
}
