// Copyright (c) 2021-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;
using CrowEditBase;

namespace CECrowPlugin
{
	public class StyleSyntaxAnalyser : SyntaxAnalyser {
		public override SyntaxNode Root => CurrentNode;
		public StyleSyntaxAnalyser (StyleDocument source) : base (source) {
			this.source = source;
		}

		SyntaxNode CurrentNode;
		Token previousTok;
		IEnumerator<Token> iter;
		int currentLine;
		public override void Process () {
			StyleDocument doc = source as StyleDocument;
			Exceptions = new List<SyntaxException> ();
			CurrentNode = new StyleRootSyntax (doc);
			previousTok = default;
			iter = doc.Tokens.AsEnumerable().GetEnumerator ();
			currentLine = 0;

			bool notEndOfSource = iter.MoveNext ();
			while (notEndOfSource) {
				if (iter.Current.Type == TokenType.LineBreak)
					currentLine++;
				else if (!iter.Current.Type.HasFlag (TokenType.Trivia)) {
				}

				previousTok = iter.Current;
				notEndOfSource = iter.MoveNext ();
			}
			while (currentNode.Parent != null) {
				if (!currentNode.EndToken.HasValue)
					storeCurrentNode (previousTok, currentLine);
				else
					currentNode = currentNode.Parent;
			}
			setCurrentNodeEndLine (currentLine);
		}
	}
}