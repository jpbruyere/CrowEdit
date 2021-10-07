// Copyright (c) 2021-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;
using CrowEditBase;

namespace CrowEdit.Xml
{
	public class XmlSyntaxAnalyser : SyntaxAnalyser {
		public override SyntaxNode Root => currentNode;
		public XmlSyntaxAnalyser (XmlDocument source) : base (source) {
			this.source = source;
		}

		Token previousTok;
		IEnumerator<Token> iter;

		int currentLine;

		public override void Process () {
			XmlDocument xmlDoc = source as XmlDocument;
			Exceptions = new List<SyntaxException> ();
			currentNode = new IMLRootSyntax (xmlDoc);
			previousTok = default;
			iter = xmlDoc.Tokens.AsEnumerable().GetEnumerator ();
			currentLine = 0;

			bool notEndOfSource = iter.MoveNext ();
			while (notEndOfSource) {
				if (iter.Current.Type == TokenType.LineBreak)
					currentLine++;
				else if (!iter.Current.Type.HasFlag (TokenType.Trivia)) {
					if (currentNode is ElementStartTagSyntax tag) {
						if (iter.Current.GetTokenType() == XmlTokenType.AttributeName) {
							AttributeSyntax attribute = new AttributeSyntax (currentLine, iter.Current);
							attribute.NameToken = iter.Current;
							currentNode = currentNode.AddChild (attribute);
						} else if (iter.Current.GetTokenType() == XmlTokenType.ElementName)
							tag.NameToken = iter.Current;
						else if (iter.Current.GetTokenType() == XmlTokenType.ClosingSign) {
							storeCurrentNode (iter.Current, currentLine);
							currentNode.RemoveChild (tag);
							currentNode = currentNode.AddChild (new ElementSyntax (tag));
						} else if (iter.Current.GetTokenType() == XmlTokenType.EmptyElementClosing) {
							storeCurrentNode (iter.Current, currentLine);
							currentNode.RemoveChild (tag);
							currentNode = currentNode.AddChild (new EmptyElementSyntax (tag));
							setCurrentNodeEndLine (currentLine);
							currentNode = currentNode.Parent;
						} else {
							Exceptions.Add (new SyntaxException  ("Unexpected Token", iter.Current));
							storeCurrentNode (previousTok, currentLine);
							continue;
						}
					} else if (currentNode is ElementSyntax elt) {
						if (iter.Current.GetTokenType() == XmlTokenType.ElementOpen)
							currentNode = currentNode.AddChild (new ElementStartTagSyntax (currentLine, iter.Current));
						else if (iter.Current.GetTokenType() == XmlTokenType.EndElementOpen) {
							elt.EndTag = new ElementEndTagSyntax (currentLine, iter.Current);
							currentNode = elt.AddChild (elt.EndTag);
						}
					} else if (currentNode is AttributeSyntax attrib) {
						if (iter.Current.GetTokenType() == XmlTokenType.EqualSign)
							if (attrib.EqualToken.HasValue)
								Exceptions.Add (new SyntaxException  ("Extra equal sign in attribute syntax", iter.Current));
							else
								attrib.EqualToken = iter.Current;
						else if (iter.Current.GetTokenType() == XmlTokenType.AttributeValueOpen)
							attrib.ValueOpenToken = iter.Current;
						else if (iter.Current.GetTokenType() == XmlTokenType.AttributeValue)
							attrib.ValueToken = iter.Current;
						else if (iter.Current.GetTokenType() == XmlTokenType.AttributeValueClose) {
							attrib.ValueCloseToken = iter.Current;
							storeCurrentNode (iter.Current, currentLine);
						} else {
							Exceptions.Add (new SyntaxException  ("Unexpected Token", iter.Current));
							storeCurrentNode (previousTok, currentLine);
							continue;
						}
					} else if (currentNode is ElementEndTagSyntax eltEndTag) {
						if (iter.Current.GetTokenType() == XmlTokenType.ElementName)
							eltEndTag.NameToken = iter.Current;
						else if (iter.Current.GetTokenType() == XmlTokenType.ClosingSign) {
							//go up 2 times
							storeCurrentNode (iter.Current, currentLine);
							storeCurrentNode (iter.Current, currentLine);
						} else {
							Exceptions.Add (new SyntaxException  ("Unexpected Token", iter.Current));
							storeCurrentNode (previousTok, currentLine);
							storeCurrentNode (previousTok, currentLine);
							continue;
						}
					} else if (currentNode is IMLRootSyntax) {
						switch (iter.Current.GetTokenType()) {
							case XmlTokenType.ElementOpen:
								currentNode = currentNode.AddChild (new ElementStartTagSyntax (currentLine, iter.Current));
								break;
							case XmlTokenType.PI_Start:
								currentNode = currentNode.AddChild (new ProcessingInstructionSyntax (currentLine, iter.Current));
								break;
							default:
								Exceptions.Add (new SyntaxException  ("Unexpected Token", iter.Current));
								break;
						}
					} else if (currentNode is ProcessingInstructionSyntax pi) {
						if (iter.Current.GetTokenType() == XmlTokenType.PI_Target)
							pi.NameToken = iter.Current;
						else if (iter.Current.GetTokenType() == XmlTokenType.PI_End) {
							storeCurrentNode (iter.Current, currentLine);
						} else if (iter.Current.GetTokenType() == XmlTokenType.AttributeName) {
							AttributeSyntax attribute = new AttributeSyntax (currentLine, iter.Current);
							attribute.NameToken = iter.Current;
							currentNode = currentNode.AddChild (attribute);
						} else {
							Exceptions.Add (new SyntaxException  ("Unexpected Token", iter.Current));
							storeCurrentNode (previousTok, currentLine);
							continue;
						}
					}
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