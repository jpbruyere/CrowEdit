// Copyright (c) 2021-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;

namespace CrowEditBase
{
	public class SyntaxException : Exception {
		public readonly Token Token;
		public readonly ReadOnlyMemory<char> SourceText;
		public SyntaxException(string message, Token token = default, ReadOnlyMemory<char> textBuffer = default, Exception innerException = null)
				: base (message, innerException) {
			Token = token;
			SourceText = textBuffer;
		}
		public string TokenString => SourceText.Span.Slice(Token.Start,Token.Length).ToString();
        public override string ToString() => $"{Message}: {TokenString}";
    }
}