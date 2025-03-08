// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System.Linq;
using Crow.Text;
using CrowEditBase;

namespace CECrowPlugin
{



	public class AttributeSyntax : SyntaxNode {
		/*internal int? name, equal, valueOpen, valueClose, valueTok;
		public string Name => name.HasValue ? Root.GetTokenStringByIndex (TokenIndexBase + name.Value) : null;
		public string Value => valueTok.HasValue ? Root.GetTokenStringByIndex (TokenIndexBase + valueTok.Value) : null;
		public Token? ValueToken => valueTok.HasValue ? Root.GetTokenByIndex (TokenIndexBase + valueTok.Value) : null;
		public AttributeSyntax (int startLine, int tokenBase)
			: base (startLine, tokenBase) {}
		public override bool IsComplete => base.IsComplete & name.HasValue & equal.HasValue & valueTok.HasValue & valueOpen.HasValue & valueClose.HasValue;*/
	}
}