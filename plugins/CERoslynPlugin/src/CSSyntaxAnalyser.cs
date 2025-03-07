// Copyright (c) 2021-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;
using CrowEditBase;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CERoslynPlugin
{
	public class CSRootSyntax : SyntaxRootNode {
		public CSRootSyntax (ReadOnlyMemory<char> source, Token[] tokens) : base (source, tokens) {	}
	}
	public class CSSyntaxNode : CrowEditBase.SyntaxNode {

		public CSSyntaxNode (int startLine, int tokenBase, int? lastTokenIdx = null)
			: base (startLine, tokenBase, lastTokenIdx) {
		}
    }
	
	public class CSSyntaxAnalyser : SyntaxAnalyser {
        /*protected override void Parse(SyntaxNode node)
        {
            throw new NotImplementedException();
        }*/

		public CSSyntaxAnalyser (CSDocument document) : base (document) {}

		public override SyntaxRootNode Process () {
			CSTokenizer tokenizer = new CSTokenizer();
			Token[] tokens = tokenizer.Tokenize(source.Span);


			
			CsharpSyntaxWalkerBridge bridge = new CsharpSyntaxWalkerBridge(new CSRootSyntax (source, tokens));
			bridge.Visit(tokenizer.syntaxTree.GetRoot());

			Root = bridge.Root;

			

			return Root;
		}
	}
	class CsharpSyntaxWalkerBridge : Microsoft.CodeAnalysis.CSharp.CSharpSyntaxWalker
	{
		public CSRootSyntax Root;
		CrowEditBase.SyntaxNode currentNode;
		public CsharpSyntaxWalkerBridge (CSRootSyntax root) : base (SyntaxWalkerDepth.StructuredTrivia)
		{
			currentNode = Root = root;
		}
		public override void Visit (Microsoft.CodeAnalysis.SyntaxNode node)
		{
			Location loc = node.GetLocation();
			LinePosition start = loc.GetLineSpan().StartLinePosition;
			LinePosition end = loc.GetLineSpan().EndLinePosition;

			int indexBase = Root.FindTokenIndexIncludingPosition(node.Span.Start);
			int lastTokIndex = Root.FindTokenIndexIncludingPosition(node.Span.End - 1);
			currentNode = currentNode.AddChild(new CSSyntaxNode(loc.GetLineSpan().StartLinePosition.Line, indexBase, lastTokIndex));
			
			base.Visit (node);
			currentNode.EndLine = end.Line;
			currentNode = currentNode.Parent;
		}
    }
}