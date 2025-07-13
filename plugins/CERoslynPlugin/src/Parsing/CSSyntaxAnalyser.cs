// Copyright (c) 2021-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CrowEditBase;
using Microsoft.CodeAnalysis.CSharp;

namespace CERoslynPlugin
{
	public class CSSyntaxAnalyser : SyntaxAnalyser {
		public CSSyntaxAnalyser (ReadOnlyTextBuffer document) : base (document) { }

		Stopwatch timer = new Stopwatch();

		public override async Task<SyntaxRootNode> Process (CancellationToken cancel = default) {
			timer.Restart();
			CSharpSyntaxTree tree = (CSharpSyntaxTree)CSharpSyntaxTree.ParseText (source.Source.Span.ToString(), CSharpParseOptions.Default, "", null);
			timer.Stop();
			Console.WriteLine($"CSharpSyntaxTree.ParseText : {timer.ElapsedMilliseconds,-20}");
			timer.Start();
			CsharpSyntaxWalkerBridge bridge = new CsharpSyntaxWalkerBridge(new CSRootSyntax (source), cancel);
			CSharpSyntaxNode csroot = await tree.GetRootAsync(cancel);

			timer.Stop();
			Console.WriteLine($"tree.GetRootAsync : {timer.ElapsedMilliseconds,-20}");
			timer.Start();

			if (cancel.IsCancellationRequested)
				return null;

			bridge.Visit(csroot);

			timer.Stop();
			Console.WriteLine($"CsharpSyntaxWalkerBridge.Visit : {timer.ElapsedMilliseconds,-20}");

			bridge.Root.SetTokens (bridge.Toks.ToArray());
			Root = bridge.Root;

			/*foreach (var item in Enum.GetNames<SyntaxKind>())
			{
				ushort sk = (ushort)Enum.Parse<SyntaxKind>(item);
				Console.WriteLine($"{item,-50} {sk,-6:X4} {sk,-16:b16}");
			}*/
			

			Console.WriteLine($"CsharpSyntaxWalkerBridge.Visit : {timer.ElapsedMilliseconds,-20}");
			return Root;
		}
	}
}