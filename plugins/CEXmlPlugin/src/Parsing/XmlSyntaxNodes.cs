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
		public XMLRootSyntax (ReadOnlyTextBuffer buff, Token[] tokens) : base (buff, tokens) { }
	}
	
	public class ProcessingInstructionSyntax : MultiNodeSyntax {
//		public override bool IsComplete => base.IsComplete & name.HasValue & PIClose.HasValue;
		public ProcessingInstructionSyntax (Token openTok){
			AddChild(new XMLSingleTokenSyntax(openTok));
		}
	}
	public class PITargetSyntax : XMLSingleTokenSyntax {
//		public override bool IsComplete => base.IsComplete & name.HasValue & PIClose.HasValue;
		public PITargetSyntax (Token target) : base(target) { }
	}

	public class XMLSingleTokenSyntax : SingleTokenSyntax {
		public XMLSingleTokenSyntax(Token tok) : base(tok) { }
        public static implicit operator XmlTokenType (XMLSingleTokenSyntax sts) => sts == null ? XmlTokenType.Unknown : (XmlTokenType)sts.Type;
	}
	public abstract class ElementTagSyntax : MultiNodeSyntax {
//		public override bool IsComplete => base.IsComplete & name.HasValue & close.HasValue;
		protected ElementTagSyntax (){}
		protected ElementTagSyntax (Token openTok) {
			AddChild(new XMLSingleTokenSyntax(openTok));
		}
        public override bool IsComplete => ChildSequenceIs();
		public string Name => Children.ElementAtOrDefault(1) is SingleTokenSyntax sts &&
							  sts.token.GetTokenType() == XmlTokenType.ElementName ? sts.AsText(): "";
		public abstract bool HasClosingToken { get; }
	}
	public class ElementStartTagSyntax : ElementTagSyntax {
		public ElementStartTagSyntax (Token openTok) : base(openTok) {}
		public override bool HasClosingToken => Children.LastOrDefault() is SingleTokenSyntax sts && sts.token.GetTokenType() == XmlTokenType.ClosingSign;

	}
	public class ElementEndTagSyntax : ElementTagSyntax {
		public ElementEndTagSyntax (Token openTok) : base(openTok) {}
        public override bool HasClosingToken => Children.LastOrDefault() is SingleTokenSyntax sts && sts.token.GetTokenType() == XmlTokenType.ClosingSign;
	}
	public class EmptyElementSyntax : ElementTagSyntax {
		public EmptyElementSyntax (ElementStartTagSyntax startNode) {
			foreach (var child in startNode.Children)
				AddChild(child);
		}
		public override bool HasClosingToken => HasChilds && Children.LastOrDefault().IsSimilar(XmlTokenType.EmptyElementClosing);
        //public override bool IsComplete => base.IsComplete && StartTag != null;
    }

	public class ElementSyntax : MultiNodeSyntax {
		public override bool IsComplete => StartTag != null && EndTag != null && StartTag.IsComplete && EndTag.IsComplete && StartTag.Name == EndTag.Name;
		public ElementSyntax (ElementStartTagSyntax startNode) {
			AddChild (startNode);
		}

		public ElementStartTagSyntax StartTag => Children.ElementAtOrDefault(0) as ElementStartTagSyntax;
		public ElementEndTagSyntax EndTag => Children.LastOrDefault() as ElementEndTagSyntax;
	}
	public class AttributeSyntax : MultiNodeSyntax {
		public override bool IsComplete => HasName && HasEquals;
		public AttributeSyntax(Token name) {
			AddChild (new XMLSingleTokenSyntax(name));
		}
		public string Name => Children.FirstOrDefault() is SingleTokenSyntax sts &&
							  sts.token.GetTokenType() == XmlTokenType.AttributeName ? sts.AsText(): "";
		public bool HasName => HasChilds && Children.First().IsSimilar(XmlTokenType.AttributeName);
		public bool HasEquals => HasChilds &&
			((HasName && Children.First().NextSiblingIs(XmlTokenType.EqualSign)) ||
			(!HasName && Children.First().IsSimilar(XmlTokenType.EqualSign)));
		
	}
	public class AttributeValueSyntax : MultiNodeSyntax {
		public AttributeValueSyntax(Token openTok) {
			AddChild(new XMLSingleTokenSyntax(openTok));
		}
	}
}