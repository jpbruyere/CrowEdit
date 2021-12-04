// Copyright (c) 2021-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;
using CrowEditBase;

namespace CECrowPlugin.Style
{
	public static class Extensions {
		public static StyleTokenType GetTokenType (this Token tok) {
			return (StyleTokenType)tok.Type;
		}
		public static void SetTokenType (this Token tok, StyleTokenType type) {
			tok.Type = (TokenType)type;
		}
	}
	public class StyleSyntaxAnalyser : SyntaxAnalyser {
		public override SyntaxNode Root => currentNode;
		public StyleSyntaxAnalyser (StyleDocument source) : base (source) {
			this.source = source;
		}

		public override void Process () {
			StyleDocument doc = source as StyleDocument;
			Exceptions = new List<SyntaxException> ();
			currentNode = new StyleRootSyntax (doc);
			currentLine = 0;
			Span<Token> toks = source.Tokens;
			tokIdx = 0;

			int firstNameIdx = -1;

			while (tokIdx < toks.Length) {
				Token curTok = toks[tokIdx];
				if (curTok.Type == TokenType.LineBreak)
					currentLine++;
				else if (!curTok.Type.HasFlag (TokenType.Trivia)) {
					/*if (currentNode is StyleRootSyntax root) {
						if (firstNameIdx < 0) {
							if (curTok.GetTokenType()  == StyleTokenType.Name) {
								firstNameIdx = tokIdx;
							} else {
								Exceptions.Add (new SyntaxException  ("Unexpected Token", curTok));
							}

						}

					}*/
				}
				tokIdx++;
			}
			while (currentNode.Parent != null) {
				if (!currentNode.LastTokenOffset.HasValue)
					storeCurrentNode (-1);
				else
					currentNode = currentNode.Parent;
			}
			setCurrentNodeEndLine (currentLine);
		}
	}
}