// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Linq;
using Crow.Text;
using CrowEditBase;

namespace CrowEdit.Xml
{

	public class XMLRootSyntax : SyntaxRootNode {
		public XMLRootSyntax (ReadOnlyMemory<char> source, Token[] tokens) : base (source, tokens) { }
	}
	public class ProcessingInstructionSyntax : SyntaxNode {
		public int? PIClose, name;
		public override bool IsComplete => base.IsComplete & name.HasValue & PIClose.HasValue;
		public ProcessingInstructionSyntax (int startLine, int tokenBase)
			: base (startLine, tokenBase) {
		}
	}

	public abstract class ElementTagSyntax : SyntaxNode {
		public int? name, close;
		public override bool IsComplete => base.IsComplete & name.HasValue & close.HasValue;
		public string Name => name.HasValue ?
				Root.GetTokenStringByIndex (TokenIndexBase + name.Value) : null;
		protected ElementTagSyntax (int startLine, int tokenBase)
			: base (startLine, tokenBase) {
		}
	}
	public class ElementStartTagSyntax : ElementTagSyntax {
		public ElementStartTagSyntax (int startLine, int tokenBase)
			: base (startLine, tokenBase) {
		}
	}
	public class ElementEndTagSyntax : ElementTagSyntax {
		public ElementEndTagSyntax (int startLine, int tokenBase)
			: base (startLine, tokenBase) {
		}
	}

	public class EmptyElementSyntax : SyntaxNode {
		public readonly ElementStartTagSyntax StartTag;
		public EmptyElementSyntax (ElementStartTagSyntax startNode) : base (startNode.StartLine, startNode.TokenIndexBase, startNode.LastTokenIndex) {
			StartTag = startNode;
			AddChild (StartTag);
		}
        public override bool IsComplete => base.IsComplete && StartTag != null;
    }

	public class ElementSyntax : SyntaxNode {
		public readonly ElementStartTagSyntax StartTag;
		public ElementEndTagSyntax EndTag { get; set; }

		public override bool IsComplete => base.IsComplete & StartTag.IsComplete & (EndTag != null && EndTag.IsComplete);

		public ElementSyntax (ElementStartTagSyntax startTag)
			: base (startTag.StartLine, startTag.TokenIndexBase) {
			StartTag = startTag;
			AddChild (StartTag);
		}
	}

	public class AttributeSyntax : SyntaxNode {
		public int? name, equal, valueOpen, valueClose, valueTok;
		public string Name => name.HasValue ? Root.GetTokenStringByIndex (TokenIndexBase + name.Value) : null;
		public string Value => valueTok.HasValue ? Root.GetTokenStringByIndex (TokenIndexBase + valueTok.Value) : null;
		public Token? ValueToken => valueTok.HasValue ? Root.GetTokenByIndex (TokenIndexBase + valueTok.Value) : null;
		public AttributeSyntax (int startLine, int tokenBase)
			: base (startLine, tokenBase) {}
		public override bool IsComplete => base.IsComplete & name.HasValue & equal.HasValue & valueTok.HasValue & valueOpen.HasValue & valueClose.HasValue;
	}
}