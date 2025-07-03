// Copyright (c) 2021-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using CrowEdit.Xml;
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
		public static bool Is(this Token tok, StyleTokenType type) => (StyleTokenType)tok.Type == type;
	}
	public class StyleSyntaxAnalyser : SyntaxAnalyser {
		public StyleSyntaxAnalyser (ReadOnlyTextBuffer document) : base (document) {}

		bool skipTriviaAndComments(MultiNodeSyntax currentNode) {
			while (tryPeekFlag(out Token token, TokenType.Trivia)) {
				switch(token.GetTokenType()) {
					case (StyleTokenType)TokenType.LineBreak:
						Read();
						break;
					case StyleTokenType.LineCommentStart:
						MultiNodeSyntax cmt = new CommentTriviaSyntax(false);
						cmt.AddChild(new SingleTokenSyntax(Read()));
						if (tryPeek(TokenType.LineComment))
							cmt.AddChild(new SingleTokenSyntax(Read()));
						currentNode.AddChild(cmt);
						break;
					case StyleTokenType.BlockCommentStart:
						MultiNodeSyntax bc = new CommentTriviaSyntax(true);
						bc.AddChild(new SingleTokenSyntax(Read()));
						while(tryPeek(out Token tok)) {
							if (tok.Type == TokenType.BlockCommentEnd)	{
								bc.AddChild(new SingleTokenSyntax(Read()));
								break;
							}
							if (tok.Type == TokenType.LineBreak) {
								Read();
							} else {
								bc.AddChild(new SingleTokenSyntax(Read()));
							}
						}
						currentNode.AddChild(bc);
						break;
					default:
						Read();
						break;
				}
			}

			return !EOF;
		}

		bool accept(MultiNodeSyntax node, Enum tokenType) {
			if (EOF)
				return false;
			if (Peek().Type == (TokenType)tokenType) {
				node.AddChild(new SingleTokenSyntax(Read()));
				return true;
			}
			return false;
		}


		ConstantDefinitionSyntax processNode(ConstantDefinitionSyntax cstDef) {
			cstDef.AddChild(new SingleTokenSyntax(Read()));//read equal
			if (skipTriviaAndComments(cstDef))
				if (accept(cstDef, StyleTokenType.MemberValueOpen))
					if (accept(cstDef, StyleTokenType.MemberValuePart))
						if (accept(cstDef, StyleTokenType.MemberValueClose))
							accept(cstDef, StyleTokenType.EndOfExpression);
			return cstDef;
		}
		StyleDefinitionSyntax processNode(StyleDefinitionSyntax styleDef) {
			while (Peek().Is(StyleTokenType.Comma)) {
				styleDef.AddChild(new SingleTokenSyntax(Read()));
				if (!skipTriviaAndComments(styleDef))
					break;
				if (!Peek().Is(StyleTokenType.Name))
					break;
				styleDef.AddChild(new StyleIdentifierSyntax(Read()));
				if (!skipTriviaAndComments(styleDef))
					break;
			}
			if (Peek().Is(StyleTokenType.OpeningBrace))
				styleDef.AddChild(processNode(new MemberListSyntax()));

			return styleDef;
		}
		MemberListSyntax processNode(MemberListSyntax memberList) {
			memberList.AddChild(new SingleTokenSyntax(Read()));
			while (skipTriviaAndComments(memberList)) {
					if (cancel.IsCancellationRequested)
						break;

				if (Peek().Is(StyleTokenType.Name)) {
					memberList.AddChild(processNode(new MemberSyntax(new MemberIdentifierSyntax(Read()))));
					continue;
				}
				if (Peek().Is(StyleTokenType.ClosingBrace))
					memberList.AddChild(new SingleTokenSyntax(Read()));
				break;
			} 

			return memberList;
		}
		MemberSyntax processNode(MemberSyntax member) {
			if (skipTriviaAndComments(member))
				if (accept(member, StyleTokenType.EqualSign)) 
					if (skipTriviaAndComments(member))
						member.AddChild(processNode(new ImlValueSyntax()));
			return member;
		}
		ImlValueSyntax processNode(ImlValueSyntax iml) {
			if (accept(iml, StyleTokenType.MemberValueOpen)) {
				while (tryPeek(out Token tok)) {
					if (cancel.IsCancellationRequested)
						break;
					if (tok.Is(StyleTokenType.ConstantRefOpen)) {
						iml.AddChild(processNode(new ConstanteReferenceSyntax()));
					} else if (tok.Is(StyleTokenType.MemberValueClose)) {
						iml.AddChild(new SingleTokenSyntax(Read()));
						accept(iml, StyleTokenType.EndOfExpression);			
						break;
					} else  if (tok.Is(StyleTokenType.MemberValuePart)) {
						iml.AddChild(new SingleTokenSyntax(Read()));
					} else  {
						iml.AddChild(new UnexpectedTokenSyntax(Read()));
						break;
					}
				}
			}								

			return iml;
		}
		ConstanteReferenceSyntax processNode(ConstanteReferenceSyntax cst) {
			if (accept(cst, StyleTokenType.ConstantRefOpen))
				if (accept(cst, StyleTokenType.ConstantName))
					accept(cst, StyleTokenType.ClosingBrace);
			return cst;
		}
		
		public override async Task<SyntaxRootNode> Process (CancellationToken cancel = default) {
			Tokenizer tokenizer = new StyleTokenizer();
			Token[] tokens = tokenizer.Tokenize(source.Source.Span);
			tokIdx = 0;
			this.cancel = cancel;

			Root = new StyleRootSyntax (source, tokens);
			while (!EOF) {
				if (cancel.IsCancellationRequested)
					break;
				if (!skipTriviaAndComments(Root))
					break;
				if (!Peek().Is(StyleTokenType.Name)) {
					Root.AddChild(new UnexpectedTokenSyntax(Read()));
					continue;
				}
				Token name = Read();
				if (!skipTriviaAndComments(Root)) {
					Root.AddChild(new UnexpectedTokenSyntax(name));	
					break;
				}
				if (Peek().Is(StyleTokenType.EqualSign)) {
					Root.AddChild(processNode(new ConstantDefinitionSyntax(new ConstantNameSyntax(name))));
				} else {
					Root.AddChild(processNode(new StyleDefinitionSyntax(new StyleIdentifierSyntax(name))));
				}
			}

			return Root;
		}
	}
}