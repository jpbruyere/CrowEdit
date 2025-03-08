// Copyright (c) 2021-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

		public override async Task<SyntaxRootNode> Process () {
			Tokenizer tokenizer = new StyleTokenizer();
			ReadOnlyTextBuffer buff = document.ImmutableBufferCopy;
			Token[] tokens = tokenizer.Tokenize(buff.Source.Span);
			currentNode = Root = new StyleRootSyntax (buff, tokens);

			currentLine = 0;
			tokIdx = 0;
			/*
			while (tokIdx < tokens.Length) {
				if (!skipTrivia(true))
					break;
				Token curTok = tokens[tokIdx];
				if (currentNode is StyleRootSyntax srs) {
					if (tokens[tokIdx].GetTokenType() != StyleTokenType.Name) {
						addException ("Unexpected Token");
					} else {
						StyleIdentifierSyntax sis = new StyleIdentifierSyntax(currentLine, tokIdx);
						if (!skipTrivia(true)) {
							addException ("Unexpected end of file");
							break;
						}
						if (tokens[tokIdx].GetTokenType() == StyleTokenType.EqualSign) {
							ConstantDefinitionSyntax cds = new ConstantDefinitionSyntax(sis);
							cds.equal = tokIdx;
							if (!skipTrivia(true)) {
								addException ("Unexpected end of file");
								break;
							}


						}
						
					}
				}
			
				tokIdx++;
			}
			while (currentNode.Parent != null) {
				if (!currentNode.LastTokenIndex.HasValue)
					finishCurrentNode (-1);
				else
					currentNode = currentNode.Parent;
			}
			setCurrentNodeEndLine (currentLine);*/

			return Root;
		}
	}
}