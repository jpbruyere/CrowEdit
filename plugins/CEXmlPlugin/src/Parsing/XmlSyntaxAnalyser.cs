// Copyright (c) 2021-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using CrowEditBase;

namespace CrowEdit.Xml
{
	public static class Extensions {
		public static XmlTokenType GetTokenType (this Token tok) {
			return (XmlTokenType)tok.Type;
		}
		public static void SetTokenType (this Token tok, XmlTokenType type) {
			tok.Type = (TokenType)type;
		}
		public static bool Is(this Token tok, XmlTokenType type) => (XmlTokenType)tok.Type == type;
	}	
	public class XmlSyntaxAnalyser : SyntaxAnalyser {
        public XmlSyntaxAnalyser (ReadOnlyTextBuffer document) : base (document) {}
		bool skipTriviaAndComments(MultiNodeSyntax currentNode, bool skipLineBreaks = true) {
			while (tryPeekFlag(out Token token, TokenType.Trivia)) {
				switch(token.GetTokenType()) {
					case (XmlTokenType)TokenType.LineBreak:
						if (!skipLineBreaks)
							return true;
						Read();
						break;
					case XmlTokenType.BlockCommentStart:
						MultiNodeSyntax bc = new CommentTriviaSyntax(true);
						bc.AddChild(new SingleTokenSyntax(Read()));
						while(tryPeek(out Token tok)) {
							if (tok.Type == TokenType.BlockCommentEnd)	{
								bc.AddChild(new SingleTokenSyntax(Read()));
								break;
							}
							if (tok.Type == TokenType.LineBreak) {
								if (!skipLineBreaks)
									return true;
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
		public virtual void ProcessAttributeValueSyntax(AttributeSyntax attrib) {
			//attrib.valueTok = tokIdx - attrib.TokenIndexBase;
		}
		AttributeSyntax processNode(AttributeSyntax attrib) {
			if (accept(attrib, XmlTokenType.EqualSign))
				if (accept(attrib, XmlTokenType.AttributeValueOpen))
					if(accept(attrib, XmlTokenType.AttributeValue))
						accept(attrib, XmlTokenType.AttributeValueClose);
			return attrib;
		}
		ElementEndTagSyntax processNode(ElementEndTagSyntax et) { 
			if (accept(et, XmlTokenType.ElementName))
				accept(et, XmlTokenType.ClosingSign);
			return et;
		}
		ProcessingInstructionSyntax processNode(ProcessingInstructionSyntax pi) {
			if (Peek().Is(XmlTokenType.PI_Target)) {
				pi.AddChild(new PITargetSyntax(Read()));
				while (skipTriviaAndComments(pi, false)) {
					if (Peek().Is(XmlTokenType.PI_End)) {
						pi.AddChild(new SingleTokenSyntax(Read()));
						break;
					}
					if (Peek().Is(XmlTokenType.AttributeName))
						pi.AddChild(processNode(new AttributeSyntax(Read())));
					else
						pi.AddChild(new UnexpectedTokenSyntax(Read()));
				}
			}
			return pi;
		}
		ElementSyntax processElement(ElementSyntax elt) {
			while (!EOF) {
				if (cancel.IsCancellationRequested)
					break;
				if (!skipTriviaAndComments(elt))
					break;
				if (Peek().Is(XmlTokenType.ElementOpen)) {
					processElementNode(elt);
				} else if (Peek().Is(XmlTokenType.EndElementOpen)) {
					elt.AddChild(processNode(new ElementEndTagSyntax(Read())));
					break;
				} else if (Peek().Is(XmlTokenType.PI_Start)) {
					elt.AddChild(processNode(new ProcessingInstructionSyntax(Read())));
				} else {
					elt.AddChild(new UnexpectedTokenSyntax(Read()));
				}
			}			
			return elt;
		}
		void processElementNode(MultiNodeSyntax node) {
			ElementStartTagSyntax start = new ElementStartTagSyntax(Read());
			if (accept (start, XmlTokenType.ElementName)) {
				while (skipTriviaAndComments(node)) {
					if (accept (start, XmlTokenType.EmptyElementClosing)) {
						node.AddChild(new EmptyElementSyntax(start));
						break;
					}
					if (accept (start, XmlTokenType.ClosingSign)) {
						node.AddChild(processElement(new ElementSyntax(start)));
						break;
					}					
					if (Peek().Is(XmlTokenType.AttributeName))
						start.AddChild(processNode(new AttributeSyntax(Read())));
					else
						start.AddChild(new UnexpectedTokenSyntax(Read()));
				}
			} else {
				start.AddChild(new UnexpectedTokenSyntax(Read()));
				node.AddChild(new ElementSyntax(start));
			}
		}		
		public override async Task<SyntaxRootNode> Process (CancellationToken cancel = default) {
			Tokenizer tokenizer = new XmlTokenizer();
			Token[] tokens = tokenizer.Tokenize(source.Source.Span);
			tokIdx = 0;
			this.cancel = cancel;//?
			
			Root = new XMLRootSyntax (source, tokens);
			while (!EOF) {
				if (cancel.IsCancellationRequested)
					break;
				if (!skipTriviaAndComments(Root))
					break;
				if (Peek().Is(XmlTokenType.ElementOpen)) {
					processElementNode(Root);
				} else if (Peek().Is(XmlTokenType.PI_Start)) {
					Root.AddChild(processNode(new ProcessingInstructionSyntax(Read())));
				} else {
					Root.AddChild(new UnexpectedTokenSyntax(Read()));
				}				
			}
			/*while (tokIdx < tokens.Length) {
				if (curTok.Type == TokenType.LineBreak)
					currentLine++;
				else if (!curTok.Type.HasFlag (TokenType.Trivia)) {
					if (currentNode is ElementStartTagSyntax tag) {
						if (curTok.GetTokenType() == XmlTokenType.AttributeName) {
							AttributeSyntax attribute = new AttributeSyntax (currentLine, tokIdx);
							attribute.name = 0;
							currentNode = currentNode.AddChild (attribute);
						} else if (curTok.GetTokenType() == XmlTokenType.ElementName)
							tag.name = tokIdx - tag.TokenIndexBase;
						else if (curTok.GetTokenType() == XmlTokenType.ClosingSign) {
							tag.close = tokIdx - tag.TokenIndexBase;
							finishCurrentNode ();
							currentNode.RemoveChild (tag);
							currentNode = currentNode.AddChild (new ElementSyntax (tag));
						} else if (curTok.GetTokenType() == XmlTokenType.EmptyElementClosing) {
							tag.close = tokIdx - tag.TokenIndexBase;
							finishCurrentNode ();
							currentNode.RemoveChild (tag);
							currentNode = currentNode.AddChild (new EmptyElementSyntax (tag));
							setCurrentNodeEndLine (currentLine);
							currentNode = currentNode.Parent;
						} else {
							addException ("Unexpected Token");
							finishCurrentNode (-1);
							continue;
						}
					} else if (currentNode is ElementSyntax elt) {
						if (curTok.GetTokenType() == XmlTokenType.ElementOpen)
							currentNode = currentNode.AddChild (new ElementStartTagSyntax (currentLine, tokIdx));
						else if (curTok.GetTokenType() == XmlTokenType.EndElementOpen) {
							currentNode = elt.AddChild (new ElementEndTagSyntax (currentLine, tokIdx));
						}
					} else if (currentNode is AttributeSyntax attrib) {
						if (curTok.GetTokenType() == XmlTokenType.EqualSign)
							if (attrib.equal.HasValue)
								addException ("Extra equal sign in attribute syntax");
							else
								attrib.equal = tokIdx - attrib.TokenIndexBase;
						else if (curTok.GetTokenType() == XmlTokenType.AttributeValueOpen)
							attrib.valueOpen = tokIdx - attrib.TokenIndexBase;
						else if (curTok.GetTokenType() == XmlTokenType.AttributeValue)
							ProcessAttributeValueSyntax (attrib);
						else if (curTok.GetTokenType() == XmlTokenType.AttributeValueClose) {
							attrib.valueClose = tokIdx - attrib.TokenIndexBase;
							finishCurrentNode ();
						} else {
							addException ("Unexpected Token");
							finishCurrentNode (-1);
							continue;
						}
					} else if (currentNode is ElementEndTagSyntax eltEndTag) {
						if (curTok.GetTokenType() == XmlTokenType.ElementName)
							eltEndTag.name = tokIdx - eltEndTag.TokenIndexBase;
						else if (curTok.GetTokenType() == XmlTokenType.ClosingSign) {
							eltEndTag.close = tokIdx - eltEndTag.TokenIndexBase;
							ElementSyntax es = eltEndTag.Parent as ElementSyntax;
							string eltEndTagName = eltEndTag.Name;
							es.EndTag = eltEndTag;
							finishCurrentNode (); 
							if (!string.Equals(es.StartTag.Name, eltEndTagName, StringComparison.Ordinal)) {
								addException ("Open/Close element name mismatch");
							}
							finishCurrentNode ();
						} else {
							addException ("Unexpected Token");
							finishCurrentNode (-1);
							finishCurrentNode (-1);
							continue;
						}
					} else if (currentNode is XMLRootSyntax) {
						switch (curTok.GetTokenType()) {
							case XmlTokenType.ElementOpen:
								currentNode = currentNode.AddChild (new ElementStartTagSyntax (currentLine, tokIdx));
								break;
							case XmlTokenType.PI_Start:
								currentNode = currentNode.AddChild (new ProcessingInstructionSyntax (currentLine, tokIdx));
								break;
							default:
								addException ("Unexpected Token");
								break;
						}
					} else if (currentNode is ProcessingInstructionSyntax pi) {
						if (curTok.GetTokenType() == XmlTokenType.PI_Target)
							pi.name = tokIdx - pi.TokenIndexBase;
						else if (curTok.GetTokenType() == XmlTokenType.PI_End) {
							pi.PIClose = tokIdx - pi.TokenIndexBase;
							finishCurrentNode ();
						} else if (curTok.GetTokenType() == XmlTokenType.AttributeName) {
							AttributeSyntax attribute = new AttributeSyntax (currentLine, tokIdx);
							attribute.name = 0;
							currentNode = currentNode.AddChild (attribute);
						} else {
							addException ("Unexpected Token");
							finishCurrentNode (-1);
							continue;
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
			//check why this is required..
			setCurrentNodeEndLine (currentLine);*/
			return Root;
		}
	}
}