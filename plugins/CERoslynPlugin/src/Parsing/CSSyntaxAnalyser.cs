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
 		CSDocument csdoc;
		public CSSyntaxAnalyser (CSDocument document) : base (document) {
			csdoc = document;
		}

		public override async Task<SyntaxRootNode> Process (CancellationToken cancel = default) {
			ReadOnlyTextBuffer buff = document.ImmutableBufferCopy;
			CsharpSyntaxWalkerBridge bridge = new CsharpSyntaxWalkerBridge(new CSRootSyntax (buff), cancel);
			CSharpSyntaxNode csroot = await csdoc.tree.GetRootAsync(cancel);

			if (cancel.IsCancellationRequested)
				return null;

			bridge.Visit(csroot);
			bridge.Root.SetTokens (bridge.Toks.ToArray());
			Root = bridge.Root;
			return Root;
		}
	}
}