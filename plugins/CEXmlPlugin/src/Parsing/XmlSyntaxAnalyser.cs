// Copyright (c) 2021-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using CrowEditBase;

namespace CrowEdit.Xml
{
	public class XmlSyntaxAnalyser : SyntaxAnalyser {
        public XmlSyntaxAnalyser (XmlDocument document) : base (document) {}
		public virtual void ProcessAttributeValueSyntax(AttributeSyntax attrib) {
			//attrib.valueTok = tokIdx - attrib.TokenIndexBase;
		}
		public override async Task<SyntaxRootNode> Process () {
			Tokenizer tokenizer = new XmlTokenizer();
			ReadOnlyTextBuffer buff = document.ImmutableBufferCopy;
			Token[] tokens = tokenizer.Tokenize(buff.Source.Span);
			Root = new XMLRootSyntax (buff, tokens);

			currentLine = 0;
			tokIdx = 0;

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