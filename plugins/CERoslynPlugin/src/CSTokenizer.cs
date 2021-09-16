// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow.Text;
using System.Collections.Generic;
using CrowEditBase;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using SyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

namespace CERoslynPlugin
{
	public class CSTokenizer : Tokenizer
	{
		//CsharpSyntaxWalkerBridge bridge = new CsharpSyntaxWalkerBridge();
		int startOfTok;
		protected List<Token> Toks;
		void addTok (ref SpanCharReader reader, Enum tokType) {
			if (reader.CurrentPosition == startOfTok)
				return;
			Toks.Add (new Token((TokenType)tokType, startOfTok, reader.CurrentPosition));
			startOfTok = reader.CurrentPosition;
		}
		void skipWhiteSpacesAndLineBreaks (ref SpanCharReader reader) {
			while(!reader.EndOfSpan) {
				switch (reader.Peak) {
					case '\x85':
					case '\x2028':
					case '\xA':
						reader.Read();
						addTok (ref reader, TokenType.LineBreak);
						break;
					case '\xD':
						reader.Read();
						if (reader.IsNextCharIn ('\xA', '\x85'))
							reader.Read();
						addTok (ref reader, TokenType.LineBreak);														
						break;
					case '\x20':
					case '\x9':
						char c = reader.Read();									
						while (reader.TryPeak (c))
							reader.Read();
						addTok (ref reader, c == '\x20' ? TokenType.WhiteSpace : TokenType.Tabulation);
						break;
					default:
						return;
				}
			}
		}
		void skipWhiteSpaces (ref SpanCharReader reader) {
			while(!reader.EndOfSpan) {
				switch (reader.Peak) {
					case '\x20':
					case '\x9':
						char c = reader.Read();									
						while (reader.TryPeak (c))
							reader.Read();
						addTok (ref reader, c == '\x20' ? TokenType.WhiteSpace : TokenType.Tabulation);
						break;
					default:
						return;
				}
			}
		}

		public override Token[] Tokenize(string source)
		{
			SpanCharReader reader = new SpanCharReader(source);
			
			startOfTok = 0;
			//curState = States.Init;
			Toks = new List<Token>(100);

			/*while(!reader.EndOfSpan) {

				skipWhiteSpaces (ref reader);

				if (reader.EndOfSpan)
					break;

				switch (reader.Peak) {
					case '/':
						reader.Advance ();

						break;
				}
			}*/

			return Toks.ToArray();
		}
	}
	class CsharpSyntaxWalkerBridge : CSharpSyntaxWalker
	{
		List<Token> Toks;
		public CsharpSyntaxWalkerBridge () : base (SyntaxWalkerDepth.StructuredTrivia)
		{
			Toks = new List<Token>(100);
		}
		public override void Visit (SyntaxNode node)
		{			
			base.Visit (node);
		}
		public override void VisitToken (SyntaxToken token)
		{
			VisitLeadingTrivia (token);

			if (SyntaxFacts.IsLiteralExpression (token.Kind ())) {
				addMultilineTok (token);
			} else {
				Microsoft.CodeAnalysis.Text.TextSpan span = token.Span;
				Toks.Add (new Token(span.Start, span.Length, (TokenType)token.RawKind));
			}

			VisitTrailingTrivia (token);
		}

        public override void VisitTrivia (SyntaxTrivia trivia)
		{
			SyntaxKind kind = trivia.Kind ();
			if (trivia.HasStructure)
				this.Visit ((CSharpSyntaxNode)trivia.GetStructure());
			else if (trivia.IsKind (SyntaxKind.DisabledTextTrivia) || trivia.IsKind (SyntaxKind.MultiLineCommentTrivia))
                addMultilineTok (trivia);
			else {
				Microsoft.CodeAnalysis.Text.TextSpan span = trivia.Span;
				Toks.Add (new Token(span.Start, span.Length, (TokenType)trivia.RawKind));
			}
		}

		void addMultilineTok (SyntaxTrivia trivia) {

		}
		void addMultilineTok (SyntaxToken token) {

		}

	}
}