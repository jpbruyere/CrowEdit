// Copyright (c) 2021-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CrowEditBase;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CERoslynPlugin
{
	public class CSRootSyntax : SyntaxRootNode {
		public CSRootSyntax (ReadOnlyTextBuffer source, Token[] tokens) : base (source, tokens) {	}
	}
	public class CSToken : SingleTokenSyntax {
		SyntaxToken cstoken;
		public CSToken(SyntaxToken token, Token tok) : base (tok) {
			cstoken = token;
		}
		public override string ToString() => $"TOK:{cstoken.Kind()}";
	}
	public class CSSyntaxNode : MultiNodeSyntax {
		Microsoft.CodeAnalysis.SyntaxNode node;
		public CSSyntaxNode(Microsoft.CodeAnalysis.SyntaxNode node) {
			this.node = node;
		}
		public override string ToString() => $"{node.Kind()}";

    }
	
	public class CSSyntaxAnalyser : SyntaxAnalyser {
        /*protected override void Parse(SyntaxNode node)
        {
            throw new NotImplementedException();
        }*/
		CSDocument csdoc;
		public CSSyntaxAnalyser (CSDocument document) : base (document) {
			csdoc = document;
		}

		public override async Task<SyntaxRootNode> Process () {
			CSTokenizer tokenizer = new CSTokenizer(csdoc.tree);
			ReadOnlyTextBuffer buff = document.ImmutableBufferCopy;
			Token[] tokens = tokenizer.Tokenize();
			CsharpSyntaxWalkerBridge bridge = new CsharpSyntaxWalkerBridge(new CSRootSyntax (buff, tokens));
			
			bridge.Visit(await tokenizer.syntaxTree.GetRootAsync());

			Root = bridge.Root;
			return Root;
		}
	}
	class CsharpSyntaxWalkerBridge : Microsoft.CodeAnalysis.CSharp.CSharpSyntaxWalker
	{
		public CSRootSyntax Root;
		MultiNodeSyntax currentNode;
		public CsharpSyntaxWalkerBridge (CSRootSyntax root) : base (SyntaxWalkerDepth.StructuredTrivia)
		{
			currentNode = Root = root;
		}
		public override void Visit (Microsoft.CodeAnalysis.SyntaxNode node)
		{
			/*Location loc = node.GetLocation();
			LinePosition start = loc.GetLineSpan().StartLinePosition;
			LinePosition end = loc.GetLineSpan().EndLinePosition;

			int indexBase = Root.FindTokenIndexIncludingPosition(node.Span.Start);
			int lastTokIndex = Root.FindTokenIndexIncludingPosition(node.Span.End - 1);*/
			currentNode = currentNode.AddChild(new CSSyntaxNode(node)) as MultiNodeSyntax;
			
			base.Visit (node);

			currentNode = currentNode.Parent;
		}

        public override void VisitToken(SyntaxToken token)
        {
			TextSpan fs = token.Span;
			if (token.Span.Length == 0)
				Debug.WriteLine($"Empty token: {token}");
			else {
				Token tok = new Token(fs.Start,fs.Length,(TokenType)token.RawKind);
				currentNode.AddChild(new CSToken(token, tok));
			}
            base.VisitToken(token);
        }
    }
}