// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using Crow.Text;
using Crow;
using System.Collections;
using CrowEditBase;
using static CrowEditBase.CrowEditBase;
using Drawing2D;

namespace CECrowPlugin.Style
{
	public class StyleDocument : SourceDocument {


		public StyleDocument (string fullPath, string editorPath) : base (fullPath, editorPath) {
			App.GetService<CrowService> ()?.Start ();

			/*if (project is MSBuildProject msbp) {
				if (msbp.IsCrowProject)
			}*/
		}

		protected override SyntaxAnalyser CreateSyntaxAnalyser() => new StyleSyntaxAnalyser (ImmutableBufferCopy);

		public override IList GetSuggestions (int absoluteTextPos, int currentTokenIndex, SyntaxNode CurrentNode, CharLocation loc) {
			Token currentToken = GetTokenByIndex(currentTokenIndex);
			/*Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.WriteLine ($"Tok: {this.CurrentTokenString} {((StyleTokenType)CurrentToken.Type).ToString()}");
			Console.ResetColor();*/
			return null;
		}
		public override string GetTokenTypeString (TokenType tokenType) => ((StyleTokenType)tokenType).ToString();
		public override Color GetColorForToken(Token token, SyntaxNode node = null)
		{
			TokenType tokType = token.Type;
			StyleTokenType xmlTokType = (StyleTokenType)tokType;
			if (xmlTokType.HasFlag (StyleTokenType.Punctuation))
				return Colors.DarkGrey;
			if (tokType.HasFlag (TokenType.WhiteSpace))
				return Colors.Silver;					
			if (xmlTokType.HasFlag (StyleTokenType.Trivia))
				return Colors.DimGrey;
			
				
			if (xmlTokType == StyleTokenType.ConstantName)
				return Colors.DarkCyan;
			if (xmlTokType.HasFlag (StyleTokenType.Name)) {
				if (token.syntaxNode is ConstantNameSyntax)
					return Colors.DarkCyan;
				if (token.syntaxNode is StyleIdentifierSyntax)
					return Colors.Blue;
				if (token.syntaxNode is MemberIdentifierSyntax)
					return Colors.Green;
				return Colors.Red;
			}
				
			if (xmlTokType == StyleTokenType.MemberValuePart)
				return Colors.DarkGoldenRod;
			if (xmlTokType == StyleTokenType.EqualSign)
				return Colors.Black;
			if (xmlTokType == StyleTokenType.Unknown)
				return Colors.Red;
			return Colors.DarkRed;
		}
	}
}