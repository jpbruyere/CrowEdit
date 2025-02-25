// Copyright (c) 2021-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;

namespace CrowEditBase
{
	public class SyntaxException : Exception {
		public readonly Token Token;
		public readonly SyntaxAnalyser SyntaxAnalyser;
		public SyntaxException(string message, Token token = default, SyntaxAnalyser syntaxAnalyser = null, Exception innerException = null)
				: base (message, innerException) {
			Token = token;
			SyntaxAnalyser = syntaxAnalyser;
		}
		public string TokenString => SyntaxAnalyser.Root.GetTokenString(Token);
        public override string ToString() => $"{Message}: {TokenString}";
    }
}