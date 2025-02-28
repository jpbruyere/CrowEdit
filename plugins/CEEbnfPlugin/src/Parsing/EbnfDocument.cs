// Copyright (c) 2013-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow.Text;
using System.Collections;
using CrowEditBase;
using Drawing2D;

namespace CrowEdit.Ebnf
{
	public static class Extensions {
		public static EbnfTokenType GetTokenType (this Token tok) {
			return (EbnfTokenType)tok.Type;
		}
		public static void SetTokenType (this Token tok, EbnfTokenType type) {
			tok.Type = (TokenType)type;
		}
	}
	public class EbnfDocument : SourceDocument {

		public EbnfDocument (string fullPath, string editorPath) : base (fullPath, editorPath) {

		}
		protected override SyntaxAnalyser CreateSyntaxAnalyser() => new EbnfSyntaxAnalyser (this);

		public override IList GetSuggestions (int absoluteTextPos, int currentTokenIndex, SyntaxNode CurrentNode, CharLocation loc) {
			Token currentToken = GetTokenByIndex(currentTokenIndex);
			return null;
		}

		public override Color GetColorForToken(TokenType tokType)
		{
			EbnfTokenType xmlTokType = (EbnfTokenType)tokType;
			if (xmlTokType == EbnfTokenType.OpenBracket || xmlTokType == EbnfTokenType.ClosingBracket)
				return Colors.RebeccaPurple;
			if (xmlTokType == EbnfTokenType.StringDelimiter)
				return Colors.DarkGoldenRod;
			if (xmlTokType == EbnfTokenType.StringLiteral)
				return Colors.DarkGoldenRod;

			if (xmlTokType.HasFlag (EbnfTokenType.Punctuation))
				return Colors.DarkGrey;
			if (xmlTokType == EbnfTokenType.SymbolName)
				return Colors.Blue;
			if (xmlTokType == EbnfTokenType.Name)
				return Colors.Green;
			return base.GetColorForToken(tokType);

		}
	}
}