// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System.Linq;
using Crow.Text;
using CrowEditBase;

namespace CrowEdit.Xml
{

	public class IMLRootSyntax : SyntaxRootNode {
		public IMLRootSyntax (XmlDocument source)
			: base (source) {
		}
	}
	public class ProcessingInstructionSyntax : SyntaxNode {
		internal int? PIOpen, PIClose, name;
		public override bool IsComplete => base.IsComplete & name.HasValue & PIOpen.HasValue & PIClose.HasValue;
		public ProcessingInstructionSyntax (int startLine, int tokenBase)
			: base (startLine, tokenBase) {
		}
	}

	public abstract class ElementTagSyntax : SyntaxNode {
		internal int? name, close;
		public override bool IsComplete => base.IsComplete & name.HasValue & close.HasValue;
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
		public EmptyElementSyntax (ElementStartTagSyntax startNode) : base (startNode.StartLine, startNode.TokenIndexBase, startNode.LastTokenOffset) {
			StartTag = startNode;
			AddChild (StartTag);
		}
	}

	public class ElementSyntax : SyntaxNode {
		public readonly ElementStartTagSyntax StartTag;
		public ElementEndTagSyntax EndTag { get; internal set; }

		public override bool IsComplete => base.IsComplete & StartTag.IsComplete & (EndTag != null && EndTag.IsComplete);

		public ElementSyntax (ElementStartTagSyntax startTag)
			: base (startTag.StartLine, startTag.TokenIndexBase) {
			StartTag = startTag;
			AddChild (StartTag);
		}
	}

	public class AttributeSyntax : SyntaxNode {
		internal int? name, equal, valueOpen, valueClose, valueTok;
		/*public Token? NameToken => name.HasValue ? getTokenByIndex (TokenIndexBase + name.Value) : default;
		public int? EqualToken { get; internal set; }
		public int? ValueOpenToken { get; internal set; }
		public int? ValueCloseToken { get; internal set; }
		public int? ValueToken { get; internal set; }*/
		public AttributeSyntax (int startLine, int tokenBase)
			: base (startLine, tokenBase) {}
		public override bool IsComplete => base.IsComplete & name.HasValue & equal.HasValue & valueTok.HasValue & valueOpen.HasValue & valueClose.HasValue;
	}
}