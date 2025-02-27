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
		public StyleSyntaxAnalyser (StyleDocument document) : base (document) {}

		public override SyntaxRootNode Process () {
			Tokenizer tokenizer = new StyleTokenizer();
			Token[] tokens = tokenizer.Tokenize(source.Span);

			currentNode = Root = new StyleRootSyntax (source, tokens);

			currentLine = 0;
			tokIdx = 0;

			int firstNameIdx = -1;

			while (tokIdx < tokens.Length) {
				Token curTok = tokens[tokIdx];
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
				if (!currentNode.TokenCount.HasValue)
					finishCurrentNode (-1);
				else
					currentNode = currentNode.Parent;
			}
			setCurrentNodeEndLine (currentLine);

			return Root;
		}
	}
}