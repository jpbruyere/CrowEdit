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
		public ProcessingInstructionSyntax (){}
	}

	public abstract class ElementTagSyntax : SyntaxNode {
//		public override bool IsComplete => base.IsComplete & name.HasValue & close.HasValue;
		protected ElementTagSyntax () {	}
	}
	public class ElementStartTagSyntax : ElementTagSyntax {
		public ElementStartTagSyntax () {}
	}
	public class ElementEndTagSyntax : ElementTagSyntax {
		public ElementEndTagSyntax () {	}
	}

	public class EmptyElementSyntax : MultiNodeSyntax {
		public EmptyElementSyntax (ElementStartTagSyntax startNode) {
			AddChild (startNode);
		}
        //public override bool IsComplete => base.IsComplete && StartTag != null;
    }

	public class ElementSyntax : MultiNodeSyntax {

		//public override bool IsComplete => base.IsComplete & StartTag.IsComplete & (EndTag != null && EndTag.IsComplete);

		public ElementSyntax (ElementStartTagSyntax startTag) {
			AddChild (startTag);
		}
	}

	public class AttributeSyntax : MultiNodeSyntax {			
		//public override bool IsComplete => base.IsComplete & name.HasValue & equal.HasValue & valueTok.HasValue & valueOpen.HasValue & valueClose.HasValue;
	}
}