// Copyright (c) 2021-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;

namespace CrowEditBase
{
	public class SyntaxException : Exception {
		public readonly Token Token;
		public SyntaxException(string message, Token token = default, Exception innerException = null)
				: base (message, innerException) {
			Token = token;
		}
        public override string ToString() => $"{Message}, {Token}";
    }
}