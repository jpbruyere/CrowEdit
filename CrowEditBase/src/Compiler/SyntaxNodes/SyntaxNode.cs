// Copyright (c) 2021-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System.Collections.Generic;
using System.ComponentModel;
using Crow.Text;

namespace CrowEditBase
{
	public class UnexpectedTokenSyntax : SingleTokenSyntax {
		public string Message;
		public UnexpectedTokenSyntax(Token tok, string message = "Unexpected token") : base (tok) { 
			Message = message;
		}
	}
	public class CommentTriviaSyntax : MultiNodeSyntax {
		bool block;
		public CommentTriviaSyntax(bool block) {
			this.block = block;
		}
        public override bool IsComplete => true;
			
    }
	
	public abstract class SyntaxNode : CrowEditComponent {

		public MultiNodeSyntax Parent { get; internal set; }

		#region  Folding and ?expand?
		public virtual bool isExpanded {
			get => false;
			set {
				if (value && Parent is SyntaxNode sn) {
					sn.isExpanded = true;					
				}
			}
		}
		#endregion
		
		public virtual SyntaxRootNode Root => Parent.Root;
		public virtual TokenType Type => TokenType.Unknown;
		public virtual int SpanStart => 0;
		public virtual int SpanEnd => 0;
		public virtual bool HasChilds => false;
		public virtual bool IsComplete => false;
		public virtual SyntaxNode FindNodeIncludingPosition (int pos) => this;
		public virtual SyntaxNode FindNodeIncludingSpan (TextSpan span) => this;
		public virtual SyntaxNode NextSiblingOrParentsNextSibling
			=> NextSibling ?? Parent.NextSiblingOrParentsNextSibling;


		public SyntaxNode NextSibling {
			get {
				if (Parent != null) {
					int idx  = Parent.children.IndexOf (this);
					if (idx < Parent.children.Count - 1)
						return Parent.children[idx + 1];
				}
				return null;
			}
		}
		public SyntaxNode PreviousSibling {
			get {
				if (Parent != null) {
					int idx  = Parent.children.IndexOf (this);
					if (idx > 0)
						return Parent.children[idx - 1];
				}
				return null;
			}
		}
		public bool NextSiblingIs(TokenType tokType)
			=> NextSibling is SingleTokenSyntax sts && sts.token.Type == tokType;
		public bool PreviousSiblingIs(TokenType tokType) 
			=> PreviousSibling is SingleTokenSyntax sts && sts.token.Type == tokType;

		protected Token getTokenByIndex (int idx) => Root.GetTokenByIndex(idx);


		public TextSpan Span => new TextSpan (SpanStart, SpanEnd);
		public CharLocation StartLocation => Root.GetLocation(SpanStart);
		public CharLocation EndLocation => Root.GetLocation(SpanEnd);
		public int LineCount => EndLocation.Line - StartLocation.Line + 1;

		public bool Contains (int pos) => Span.Contains (pos);
		public bool Contains (TextSpan span) => Span.Contains (span);
		public string AsText() {
			return Span.Length < 0 ? "" : Root.GetText(Span).ToString();
		}
		public virtual bool IsSimilar (object other) => this.GetType() == other?.GetType();

        public class CompareOnStartLine : IComparer<SyntaxNode>
        {
            public int Compare(SyntaxNode x, SyntaxNode y) => x.StartLocation.Line - y.StartLocation.Line;
        }
		public override string ToString() => $"{this.GetType().Name}";
    }
}