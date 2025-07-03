// Copyright (c) 2021-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Crow.Text;
using CrowEditBase;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CERoslynPlugin
{
	class CsharpSyntaxWalkerBridge : CSharpSyntaxWalker
	{
		#region CTOR
		public CsharpSyntaxWalkerBridge (CSRootSyntax root, CancellationToken cancel = default) : base (SyntaxWalkerDepth.StructuredTrivia)
		{
			this.cancel = cancel;
			currentNode = Root = root;
			Toks = new List<Token>(100);
		}
		#endregion

		public CSRootSyntax Root;
		public List<Token> Toks;
		MultiNodeSyntax currentNode;
		int startOfTok;
		CancellationToken cancel;

		public override void Visit (Microsoft.CodeAnalysis.SyntaxNode node)
		{
			if (cancel.IsCancellationRequested)
				return;
			currentNode = currentNode.AddChild(new CSSyntaxNode(node)) as MultiNodeSyntax;
			
			base.Visit (node);

			currentNode = currentNode.Parent;
		}
		public override void VisitToken (SyntaxToken token)
		{
			if (cancel.IsCancellationRequested)
				return;

			VisitLeadingTrivia (token);

			Microsoft.CodeAnalysis.Text.TextSpan fs = token.Span;
			/*if (SyntaxFacts.IsLiteralExpression (token.Kind ()))
				addMultilineToken(token.ToString(), token.Span, (TokenType)token.RawKind);
			else*/
			if (fs.Length == 0)
				Debug.WriteLine($"Empty token: {token}");
			else {
				Microsoft.CodeAnalysis.Text.TextSpan span = token.Span;
				Token tok = new Token(fs.Start,fs.Length,(TokenType)token.RawKind);
				Toks.Add(tok);
				currentNode.AddChild(new CSToken(token, tok));
			}

			VisitTrailingTrivia (token);
		}
        public override void VisitTrivia (SyntaxTrivia trivia)
		{
			if (cancel.IsCancellationRequested)
				return;

			SyntaxKind kind = trivia.Kind ();
			Microsoft.CodeAnalysis.Text.TextSpan span = trivia.Span;
			if (kind == SyntaxKind.EndOfLineTrivia) {
				Toks.Add (new Token(span.Start, span.Length, TokenType.LineBreak));
				return;
			}
			if (trivia.HasStructure)
				this.Visit ((CSharpSyntaxNode)trivia.GetStructure());
			else if (trivia.IsKind (SyntaxKind.DisabledTextTrivia) || trivia.IsKind (SyntaxKind.MultiLineCommentTrivia))
                addMultilineToken(trivia.ToString(), trivia.Span, (TokenType)trivia.RawKind);
			else {
				Toks.Add (new Token(span.Start, span.Length, (TokenType)trivia.RawKind));
			}
		}
		
		void addTok (ref SpanCharReader reader, int offset, Enum tokType) {
			if (reader.CurrentPosition == startOfTok)
				return;
			Token tok = new Token((TokenType)tokType,startOfTok + offset, reader.CurrentPosition + offset);
			Toks.Add (tok);
			currentNode.AddChild(new CSToken(default, tok));

			startOfTok = reader.CurrentPosition;
		}		
		void addMultilineToken(ReadOnlySpan<char> txt, Microsoft.CodeAnalysis.Text.TextSpan span, TokenType mainType) {
			SpanCharReader reader = new SpanCharReader(txt);
			startOfTok = 0;

			while(!reader.EndOfSpan) {
				if (cancel.IsCancellationRequested)
					return;
				switch (reader.Peek) {
					case '\x85':
					case '\x2028':
					case '\xA':
						addTok (ref reader, span.Start, mainType);
						reader.Read();
						addTok (ref reader, span.Start, TokenType.LineBreak);
						break;
					case '\xD':
						addTok (ref reader, span.Start, mainType);
						reader.Read();
						if (reader.IsNextCharIn ('\xA', '\x85'))
							reader.Read();
						addTok (ref reader, span.Start, TokenType.LineBreak);
						break;
					case '\x20':
					case '\x9':
						addTok (ref reader, span.Start, mainType);
						char c = reader.Read();
						while (reader.TryPeek (c))
							reader.Read();
						addTok (ref reader, span.Start, c == '\x20' ? TokenType.WhiteSpace : TokenType.Tabulation);
						break;
					default:
						reader.Read();
						break;
				}
			}			
			addTok (ref reader, span.Start, mainType);
		}				
    }
}