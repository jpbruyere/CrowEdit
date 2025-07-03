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
			AddChild(new SingleTokenSyntax(openTok));
		}
	}
	public class PITargetSyntax : SingleTokenSyntax {
//		public override bool IsComplete => base.IsComplete & name.HasValue & PIClose.HasValue;
		public PITargetSyntax (Token target) : base(target) { }
	}


	public abstract class ElementTagSyntax : MultiNodeSyntax {
//		public override bool IsComplete => base.IsComplete & name.HasValue & close.HasValue;
		protected ElementTagSyntax (Token openTok) {
			AddChild(new SingleTokenSyntax(openTok));
		}
	}
	/*public class ElementNameSyntax : SingleTokenSyntax {
		public ElementNameSyntax(Token name) : base(name) {}
	}*/
	public class ElementStartTagSyntax : ElementTagSyntax {
		public ElementStartTagSyntax (Token openTok) : base(openTok) {}
	}
	public class ElementEndTagSyntax : ElementTagSyntax {
		public ElementEndTagSyntax (Token openTok) : base(openTok) {}
	}

	public class EmptyElementSyntax : MultiNodeSyntax {
		public EmptyElementSyntax (ElementStartTagSyntax startNode) {
			AddChild (startNode);
		}
        //public override bool IsComplete => base.IsComplete && StartTag != null;
    }

	public class ElementSyntax : MultiNodeSyntax {

		public override bool IsComplete => base.IsComplete;// & StartTag.IsComplete & (EndTag != null && EndTag.IsComplete);

		public ElementSyntax (ElementStartTagSyntax startNode) {
			AddChild (startNode);
		}
	}
	public class AttributeSyntax : MultiNodeSyntax {			
		//public override bool IsComplete => base.IsComplete & name.HasValue & equal.HasValue & valueTok.HasValue & valueOpen.HasValue & valueClose.HasValue;
		public AttributeSyntax(Token name) {
			AddChild (new SingleTokenSyntax(name));
		}
	}
}