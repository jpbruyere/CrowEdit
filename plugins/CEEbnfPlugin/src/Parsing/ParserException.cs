// Copyright (c) 2021-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;

namespace CrowEdit.Ebnf
{
	class EbnfParserException : Exception {
		public int Line, Column;
		public EbnfParserException (string message, int line = 0, int column = 0) : base (message) {
			Line = line;
			Column = column;
		}
		public override string ToString() => $"{base.ToString()} ({Line},{Column})";
	}
}
