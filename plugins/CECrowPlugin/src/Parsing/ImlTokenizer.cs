// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow.Text;
using System.Collections.Generic;
using CrowEditBase;
using CrowEdit.Xml;

namespace CECrowPlugin
{
	public class ImlTokenizer : XmlTokenizer {
		protected override void parseAttributeValue (ref SpanCharReader reader) {
			char q = reader.Read();
			addTok (ref reader, XmlTokenType.AttributeValueOpen);
			while (!reader.EndOfSpan) {
				if (reader.Eol()) {
					addTok (ref reader, XmlTokenType.AttributeValue);
					reader.ReadEol();
					addTok (ref reader, XmlTokenType.LineBreak);
					continue;
				}
				if (reader.TryPeak (q)) {
					addTok (ref reader, XmlTokenType.AttributeValue);
					reader.Advance (1);
					addTok (ref reader, XmlTokenType.AttributeValueClose);
					return;
				} else
					reader.Read ();
			}
		}
	}
}
