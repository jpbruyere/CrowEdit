// Copyright (c) 2021-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using CrowEditBase;
using CrowEdit.Xml;

namespace CECrowPlugin
{
	public class ImlSyntaxAnalyser : XmlSyntaxAnalyser {
		public ImlSyntaxAnalyser (ReadOnlyTextBuffer document) : base (document) {}

        protected override Token[] tokenize()
        {
            Tokenizer tokenizer = new ImlTokenizer();
			return tokenizer.Tokenize(source.Source.Span);
        }
	}
}