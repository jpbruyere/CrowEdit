// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow.Text;
using System.Collections.Generic;
using CrowEditBase;
using CrowEdit.Xml;
using Crow;

namespace CECrowPlugin
{
	public class ImlTokenizer : XmlTokenizer {
		enum status {
			init,
			attribute,
			BindingAdress,
			BindingName,
			bindingTarget,
			bindingValue,
			bindingSource,
			constantRef,
		};

		void parseBindingExpression (ref SpanCharReader reader, char q) {

			reader.Advance ();
			addTok (ref reader, ImlTokenType.BindingOpen);

			if (reader.TryPeek ('²')) {
				reader.Advance ();
				addTok (ref reader, ImlTokenType.TwoWayBinding);
			}

			status curState = status.BindingAdress;

			while (!reader.EndOfSpan) {
				if (curState == status.BindingAdress) {
					if (reader.TryPeek ('.')) {
						reader.Advance();
						if (reader.TryPeek ('.')) { 
							reader.Advance();
							addTok (ref reader, ImlTokenType.BindingDoubleDot);	
						} else {
							addTok (ref reader, ImlTokenType.BindingDot);	
						}
						continue;
					}
					if (reader.TryPeek ('/')) {
						reader.Advance();
						addTok (ref reader, ImlTokenType.BindingLevel);	
						continue;
					}
				}

				if (reader.TryPeek ('=')) {
					if (curState == status.BindingName) {
						addTok (ref reader, ImlTokenType.BindingName);
						reader.Advance ();
						addTok (ref reader, ImlTokenType.EqualSign);
						curState = status.BindingAdress;
						continue;
					} else
						return;
				}

				if (reader.TryPeek ('$')) {
					reader.Advance ();
					if (reader.TryPeek ('{')) {
						reader.Advance ();
						addTok (ref reader, ImlTokenType.ConstantRefOpen);
						curState = status.constantRef;
					}
					continue;
				}
				if (reader.TryPeek ('}')) {
					if (curState == status.BindingName || curState == status.bindingValue) {
						addTok (ref reader, ImlTokenType.BindingName);
						reader.Read();
						addTok (ref reader, ImlTokenType.BindingClose);
						curState = status.attribute;
					} else if (curState == status.constantRef) {
						addTok (ref reader, ImlTokenType.ConstantName);
						reader.Advance ();
						addTok (ref reader, ImlTokenType.ConstantRefClose);
						curState = status.BindingName;
						continue;
					}
					return;
				}
				if (reader.Eol() || reader.TryPeek (q)) {
					return;
				}
				if (curState == status.BindingAdress)
					curState = status.BindingName;
				reader.Read ();
			}			
		}
		
		protected override void parseAttributeValue (ref SpanCharReader reader) {
			char q = reader.Read();
			status curState = status.attribute;
			addTok (ref reader, XmlTokenType.AttributeValueOpen);
			while (!reader.EndOfSpan) {
				if (reader.TryPeek ('{')) {
					parseBindingExpression(ref reader, q);
				}
				if (reader.TryPeek ('$')) {
					reader.Advance ();
					if (reader.TryPeek ('{')) {
						reader.Advance ();
						addTok (ref reader, ImlTokenType.ConstantRefOpen);
						curState = status.constantRef;
					}
					continue;
				}
				if (reader.TryPeek ('}')) {
					if (curState == status.constantRef) {
						addTok (ref reader, ImlTokenType.ConstantName);
						reader.Advance ();
						addTok (ref reader, ImlTokenType.ConstantRefClose);
						curState = status.attribute;
						continue;					
					}
				}

				if (reader.Eol()) {
					addTok (ref reader, XmlTokenType.AttributeValue);
					reader.ReadEol();
					addTok (ref reader, XmlTokenType.LineBreak);
					continue;
				}
				if (reader.TryPeek (q)) {
					addTok (ref reader, XmlTokenType.AttributeValue);
					reader.Advance ();
					addTok (ref reader, XmlTokenType.AttributeValueClose);
					return;
				}
				reader.Read ();
			}
		}

	}
}
