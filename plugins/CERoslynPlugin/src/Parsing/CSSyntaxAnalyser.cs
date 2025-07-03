// Copyright (c) 2021-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System.Threading;
using System.Threading.Tasks;
using CrowEditBase;
using Microsoft.CodeAnalysis.CSharp;

namespace CERoslynPlugin
{
	public class CSSyntaxAnalyser : SyntaxAnalyser {
		public CSSyntaxAnalyser (ReadOnlyTextBuffer document) : base (document) { }

		public override async Task<SyntaxRootNode> Process (CancellationToken cancel = default) {
			CSharpSyntaxTree tree = (CSharpSyntaxTree)CSharpSyntaxTree.ParseText (source.Source.Span.ToString(), CSharpParseOptions.Default, "", null);
			CsharpSyntaxWalkerBridge bridge = new CsharpSyntaxWalkerBridge(new CSRootSyntax (source), cancel);
			CSharpSyntaxNode csroot = await tree.GetRootAsync(cancel);

			if (cancel.IsCancellationRequested)
				return null;

			bridge.Visit(csroot);
			bridge.Root.SetTokens (bridge.Toks.ToArray());
			Root = bridge.Root;
			return Root;
		}
	}
}