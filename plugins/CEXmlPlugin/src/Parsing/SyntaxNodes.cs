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
		public Token PIStartToken => StartToken;
		public Token? PIEndToken => EndToken.HasValue && EndToken.Value.GetTokenType() == XmlTokenType.PI_End ? EndToken : null;
		public Token? NameToken { get; internal set; }
		public override bool IsComplete => base.IsComplete & NameToken.HasValue;
		public ProcessingInstructionSyntax (int startLine, Token startTok)
			: base (startLine, startTok) {
		}
	}

	public abstract class ElementTagSyntax : SyntaxNode {
		public Token OpenToken => StartToken;
		public Token? NameToken { get; internal set; }
		public Token? CloseToken => EndToken.HasValue && EndToken.Value.GetTokenType() == XmlTokenType.ClosingSign ? EndToken : null;
		public override bool IsComplete => base.IsComplete & NameToken.HasValue & CloseToken.HasValue;
		protected ElementTagSyntax (int startLine, Token startTok)
			: base (startLine, startTok) {
		}
	}
	public class ElementStartTagSyntax : ElementTagSyntax {
		public ElementStartTagSyntax (int startLine, Token startTok)
			: base (startLine, startTok) {
		}
	}
	public class ElementEndTagSyntax : ElementTagSyntax {
		public ElementEndTagSyntax (int startLine, Token startTok)
			: base (startLine, startTok) {
		}
	}

	public class EmptyElementSyntax : SyntaxNode {
		public readonly ElementStartTagSyntax StartTag;
		public EmptyElementSyntax (ElementStartTagSyntax startNode) : base (startNode.StartLine, startNode.StartToken, startNode.EndToken) {
			StartTag = startNode;
			AddChild (StartTag);
		}
	}

	public class ElementSyntax : SyntaxNode {
		public readonly ElementStartTagSyntax StartTag;
		public ElementEndTagSyntax EndTag { get; internal set; }

		public override bool IsComplete => base.IsComplete & StartTag.IsComplete & (EndTag != null && EndTag.IsComplete);

		public ElementSyntax (ElementStartTagSyntax startTag)
			: base (startTag.StartLine, startTag.StartToken) {
			StartTag = startTag;
			AddChild (StartTag);
		}
	}

	public class AttributeSyntax : SyntaxNode {
		public Token? NameToken { get; internal set; }
		public Token? EqualToken { get; internal set; }
		public Token? ValueOpenToken { get; internal set; }
		public Token? ValueCloseToken { get; internal set; }
		public Token? ValueToken { get; internal set; }
		public AttributeSyntax (int startLine, Token startTok) : base  (startLine, startTok) {}
		public override bool IsComplete => base.IsComplete & NameToken.HasValue & EqualToken.HasValue & ValueToken.HasValue & ValueOpenToken.HasValue & ValueCloseToken.HasValue;
	}
}