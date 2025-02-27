// Copyright (c) 2021-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;
using CrowEditBase;

namespace CERoslynPlugin
{
	public class CSRootSyntax : SyntaxRootNode {
		public CSRootSyntax (ReadOnlyMemory<char> source, Token[] tokens) : base (source, tokens) {	}
	}
	
	public class CSSyntaxAnalyser : SyntaxAnalyser {
        /*protected override void Parse(SyntaxNode node)
        {
            throw new NotImplementedException();
        }*/

		public CSSyntaxAnalyser (CSDocument document) : base (document) {}

		public override SyntaxRootNode Process () {
			Tokenizer tokenizer = new CSTokenizer();
			Token[] tokens = tokenizer.Tokenize(source.Span);

			currentNode = Root = new CSRootSyntax (source, tokens);
			return Root;
		}
	}
}