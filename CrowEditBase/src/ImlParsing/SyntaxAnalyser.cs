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
		public override SyntaxNode Root => CurrentNode;
		public XmlSyntaxAnalyser (XmlDocument source) : base (source) {
			this.source = source;
		}

		SyntaxNode CurrentNode;
		Token previousTok;
		IEnumerator<Token> iter;

		public override void Process () {
			XmlDocument xmlDoc = source as XmlDocument;
			Exceptions = new List<SyntaxException> ();
			CurrentNode = new IMLRootSyntax (xmlDoc);
			previousTok = default;
			iter = xmlDoc.Tokens.AsEnumerable().GetEnumerator ();		

			bool notEndOfSource = iter.MoveNext ();
			while (notEndOfSource) {
				if (!iter.Current.Type.HasFlag (TokenType.Trivia)) {
					if (CurrentNode is ElementStartTagSyntax tag) {
						if (iter.Current.GetTokenType() == XmlTokenType.AttributeName) {
							AttributeSyntax attribute = new AttributeSyntax (iter.Current);
							attribute.NameToken = iter.Current;
							CurrentNode = CurrentNode.AddChild (attribute);
						} else if (iter.Current.GetTokenType() == XmlTokenType.ElementName)
							tag.NameToken = iter.Current;
						else if (iter.Current.GetTokenType() == XmlTokenType.ClosingSign) {
							tag.EndToken = iter.Current;						
							CurrentNode = tag.Parent;
							CurrentNode.RemoveChild (tag);
							CurrentNode = CurrentNode.AddChild (new ElementSyntax (tag));
						} else if (iter.Current.GetTokenType() == XmlTokenType.EmptyElementClosing) {
							tag.EndToken = iter.Current;
							CurrentNode = tag.Parent;
							CurrentNode.RemoveChild (tag);
							CurrentNode.AddChild (new EmptyElementSyntax (tag));
						} else {
							Exceptions.Add (new SyntaxException  ("Unexpected Token", iter.Current));
							CurrentNode.EndToken = previousTok;
							CurrentNode = CurrentNode.Parent;
							continue;						
						}
					} else if (CurrentNode is ElementSyntax elt) {
						if (iter.Current.GetTokenType() == XmlTokenType.ElementOpen)
							CurrentNode = CurrentNode.AddChild (new ElementStartTagSyntax (iter.Current));
						else if (iter.Current.GetTokenType() == XmlTokenType.EndElementOpen) {						
							elt.EndTag = new ElementEndTagSyntax (iter.Current);						
							CurrentNode = elt.AddChild (elt.EndTag);
						}
					} else if (CurrentNode is AttributeSyntax attrib) {
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
							attrib.ValueCloseToken = attrib.EndToken = iter.Current;
							CurrentNode = CurrentNode.Parent;
						} else {
							Exceptions.Add (new SyntaxException  ("Unexpected Token", iter.Current));
							CurrentNode.EndToken = previousTok;
							CurrentNode = CurrentNode.Parent;
							continue;						
						}
					} else if (CurrentNode is ElementEndTagSyntax eltEndTag) {
						if (iter.Current.GetTokenType() == XmlTokenType.ElementName)
							eltEndTag.NameToken = iter.Current;
						else if (iter.Current.GetTokenType() == XmlTokenType.ClosingSign) {
							eltEndTag.EndToken = eltEndTag.Parent.EndToken = iter.Current;
							CurrentNode = eltEndTag.Parent.Parent;
						} else {
							Exceptions.Add (new SyntaxException  ("Unexpected Token", iter.Current));
							eltEndTag.EndToken = eltEndTag.Parent.EndToken = previousTok;
							CurrentNode = CurrentNode.Parent.Parent;
							continue;						
						}
					} else if (CurrentNode is IMLRootSyntax) {
						switch (iter.Current.GetTokenType()) {
							case XmlTokenType.ElementOpen:
								CurrentNode = CurrentNode.AddChild (new ElementStartTagSyntax (iter.Current));
								break;
							case XmlTokenType.PI_Start:
								CurrentNode = CurrentNode.AddChild (new ProcessingInstructionSyntax (iter.Current));
								break;
							default:
								Exceptions.Add (new SyntaxException  ("Unexpected Token", iter.Current));
								break;
						}
					} else if (CurrentNode is ProcessingInstructionSyntax pi) {
						if (iter.Current.GetTokenType() == XmlTokenType.PI_Target)
							pi.NameToken = iter.Current;
						else if (iter.Current.GetTokenType() == XmlTokenType.PI_End) {
							pi.EndToken = iter.Current;
							CurrentNode = CurrentNode.Parent;
						} else if (iter.Current.GetTokenType() == XmlTokenType.AttributeName) {
							AttributeSyntax attribute = new AttributeSyntax (iter.Current);
							attribute.NameToken = iter.Current;
							CurrentNode = CurrentNode.AddChild (attribute);
						} else {
							Exceptions.Add (new SyntaxException  ("Unexpected Token", iter.Current));
							pi.EndToken = previousTok;
							CurrentNode = CurrentNode.Parent;
							continue;						
						}
					}
				}
				
				previousTok = iter.Current;
				notEndOfSource = iter.MoveNext ();
			}
			while (CurrentNode.Parent != null) {
				if (!CurrentNode.EndToken.HasValue)
					CurrentNode.EndToken = previousTok;
				CurrentNode = CurrentNode.Parent;
			}			
		}
	}
}