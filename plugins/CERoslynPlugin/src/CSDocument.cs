// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Linq;
using Crow.Text;
using System.Collections.Generic;
using System.Diagnostics;
using Crow;
using IML = Crow.IML;
using System.Collections;
using System.Reflection;
using CrowEditBase;
using Drawing2D;

//using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static CrowEditBase.CrowEditBase;
using System.Threading;


namespace CERoslynPlugin
{
	/*public static class Extensions {
		public static CSTokenType GetTokenType (this Token tok) => (XmlTokenType)tok.Type;
		public static void SetTokenType (this Token tok, CSTokenType type) => tok.Type = (TokenType)type;
	}*/
	public class CSDocument : SourceDocument {

		static CSDocument () {
			App.GetService<RoslynService> ()?.Start ();
		}

		public CSDocument (string fullPath, string editorPath)	: base (fullPath, editorPath) {	}

		#region SourceDocument abstract class implementation
		protected override SyntaxAnalyser CreateSyntaxAnalyser() => new CSSyntaxAnalyser (ImmutableBufferCopy);

		public override IList GetSuggestions (int absoluteTextPos, int currentTokenIndex, SyntaxNode CurrentNode, CharLocation loc)
		{
			/*Token currentToken = GetTokenByIndex(currentTokenIndex);
			throw new NotImplementedException();*/
			return null;
		}
		#endregion

		public override string GetTokenTypeString (TokenType tokenType) => ((SyntaxKind)tokenType).ToString();
		public override Color GetColorForToken(Token token)
		{
			SyntaxKind syntaxKind = (SyntaxKind)token.Type;
			TokenType tokType = token.Type;
			CSTokenType xmlTokType = (CSTokenType)tokType;

			if (SyntaxFacts.IsPredefinedType(syntaxKind))
				return Colors.Fuchsia;
			if (SyntaxFacts.IsLiteralExpression(syntaxKind))
				return Colors.DarkGoldenRod;
			if (SyntaxFacts.IsPreprocessorKeyword(syntaxKind))
				return Colors.Indigo;
			if (SyntaxFacts.IsKeywordKind(syntaxKind))
				return Colors.DodgerBlue;
			if (SyntaxFacts.IsPunctuation(syntaxKind))
				return Colors.Black;

			if (syntaxKind == SyntaxKind.IdentifierToken)
				return Colors.Blue;
			
			if (xmlTokType.HasFlag (CSTokenType.Punctuation))
				return Colors.DarkGrey;
			if (tokType.HasFlag (TokenType.WhiteSpace))
				return Colors.Silver;			
			if (xmlTokType.HasFlag (CSTokenType.Trivia))
				return Colors.DimGrey;
			else if (xmlTokType == CSTokenType.Name)
				return Colors.Green;
			if (xmlTokType == CSTokenType.TypeKeyword)
				return Colors.Blue;
			if (xmlTokType == CSTokenType.Keyword)
				return Colors.DarkBlue;
			if (xmlTokType == CSTokenType.VisibilityKeyword)
				return Colors.SlateBlue;
			if (xmlTokType == CSTokenType.Directive)
				return Colors.Black;
			if (xmlTokType == CSTokenType.Operator)
				return Colors.DarkSlateBlue;
			return Colors.Red;

		}

        protected override void apply(TextChange change)
        {
			buffer.Update(change);
			NotifyValueChanged ("IsDirty", IsDirty);
			CMDSave.CanExecute = IsDirty;

			parse();
        }
    }
}