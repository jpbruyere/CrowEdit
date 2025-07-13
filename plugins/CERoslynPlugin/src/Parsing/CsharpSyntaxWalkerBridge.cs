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
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

			if (node.IsKind(SyntaxKind.UsingDirective))
				currentNode = currentNode.AddChild(new CSUsingDirectiveSyntax(node)) as MultiNodeSyntax;
			else
				currentNode = currentNode.AddChild(new CSSyntaxNode(node)) as MultiNodeSyntax;
			
			base.Visit (node);

			currentNode = currentNode.Parent;
		}
        /*public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
			currentNode = currentNode.AddChild(new CSUsingDirectiveSyntax(node)) as MultiNodeSyntax;
            base.VisitUsingDirective(node);
			currentNode = currentNode.Parent;
        }*/
		public override void VisitToken (SyntaxToken token)
		{
			if (cancel.IsCancellationRequested)
				return;

			VisitLeadingTrivia (token);

			Microsoft.CodeAnalysis.Text.TextSpan fs = token.Span;
			if (SyntaxFacts.IsLiteralExpression (token.Kind ()))
				addMultilineToken(token.ToString(), token.Span, (TokenType)token.RawKind);
			else if (fs.Length == 0)
				Debug.WriteLine($"Empty token: {token}");
			else {
				Microsoft.CodeAnalysis.Text.TextSpan span = token.Span;
				Token tok = new Token(fs.Start,fs.Length,(TokenType)token.RawKind);
				Toks.Add(tok);
				currentNode.AddChild(new CSToken(token, tok));
			}

			VisitTrailingTrivia (token);
		}
		TriviaPos triviaPos = TriviaPos.none;
        public override void VisitLeadingTrivia(SyntaxToken token)
        {
			triviaPos = TriviaPos.leading;
            base.VisitLeadingTrivia(token);
			triviaPos = TriviaPos.none;
        }
        public override void VisitTrailingTrivia(SyntaxToken token)
        {
			triviaPos = TriviaPos.trailing;
            base.VisitTrailingTrivia(token);
			triviaPos = TriviaPos.none;
        }

        public override void VisitTrivia (SyntaxTrivia trivia)
		{

			if (cancel.IsCancellationRequested)
				return;
			
			//currentNode = currentNode.AddChild(new CSTriviaSyntax(trivia, triviaPos)) as MultiNodeSyntax;

			SyntaxKind kind = trivia.Kind ();
			Microsoft.CodeAnalysis.Text.TextSpan span = trivia.Span;
			if (kind == SyntaxKind.EndOfLineTrivia)
				Toks.Add (new Token(span.Start, span.Length, TokenType.LineBreak));
			else if (trivia.HasStructure)
				this.Visit ((CSharpSyntaxNode)trivia.GetStructure());
			else if (trivia.IsKind (SyntaxKind.MultiLineCommentTrivia)) {
				currentNode = currentNode.AddChild(new CSTriviaSyntax(trivia, triviaPos)) as MultiNodeSyntax;
				addMultilineToken(trivia.ToString(), trivia.Span, TokenType.BlockComment);
				currentNode = currentNode.Parent;
			} else if (trivia.IsKind (SyntaxKind.DisabledTextTrivia)) {
				currentNode = currentNode.AddChild(new CSTriviaSyntax(trivia, triviaPos)) as MultiNodeSyntax;
                addMultilineToken(trivia.ToString(), trivia.Span, (TokenType)trivia.RawKind);
				currentNode = currentNode.Parent;
			}
			else
				Toks.Add (new Token(span.Start, span.Length, (TokenType)trivia.RawKind));
			
			//currentNode = currentNode.Parent;
		}
        /*public override void VisitDeclarationExpression(DeclarationExpressionSyntax node)
        {
			Console.WriteLine($"DeclarationExpression:{node}");
            base.VisitDeclarationExpression(node);
        }
        public override void VisitDeclarationPattern(DeclarationPatternSyntax node)
        {
			Console.WriteLine($"DeclarationPattern: {node}");
            base.VisitDeclarationPattern(node);
        }*/
		
		void addTok (ref SpanCharReader reader, int offset, Enum tokType, bool createNode = true) {
			if (reader.CurrentPosition == startOfTok)
				return;
			Token tok = new Token((TokenType)tokType, startOfTok + offset, reader.CurrentPosition + offset);
			Toks.Add (tok);

			if (createNode)
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
						addTok (ref reader, span.Start, mainType, true);
						reader.Read();
						addTok (ref reader, span.Start, TokenType.LineBreak, false);
						break;
					case '\xD':
						addTok (ref reader, span.Start, mainType, true);
						reader.Read();
						if (reader.IsNextCharIn ('\xA', '\x85'))
							reader.Read();
						addTok (ref reader, span.Start, TokenType.LineBreak, false);
						break;
					/*case '\x20':
					case '\x9':
						addTok (ref reader, span.Start, mainType, false);
						char c = reader.Read();
						while (reader.TryPeek (c))
							reader.Read();
						addTok (ref reader, span.Start, c == '\x20' ? TokenType.WhiteSpace : TokenType.Tabulation, false);
						break;*/
					default:
						reader.Read();
						break;
				}
			}			
			addTok (ref reader, span.Start, mainType, true);
		}
    }
}