// Copyright (c) 2021-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;
using Crow.Text;
using Crow;
using System.Diagnostics;

namespace CrowEditBase
{
	public abstract class MultiNodeSyntax : SyntaxNode {
		#region CTOR
		public MultiNodeSyntax() { }
		#endregion

		internal bool isFolded;
		bool _isExpanded;

        internal List<SyntaxNode> children = new List<SyntaxNode> ();
		public IEnumerable<SyntaxNode> Children => children;
		public SyntaxNode AddChild (SyntaxNode child) {
			children.Add (child);
			child.Parent = this;
			return child;
		}
		public void RemoveChild (SyntaxNode child) {
			children.Remove (child);
			child.Parent = null;
		}
		public IEnumerable<T> GetChilds<T> () => children.OfType<T>();

		public bool ChildIs<T> (int idx) => children.Count() <= idx ? false : children[idx] is T;
		public bool ChildSequenceIs(params object[] sequ) {
			if (children.Count != sequ.Length)
				return false;
			for (int i = 0; i < sequ.Length; i++) {
				if (typeof(Type).IsAssignableFrom(sequ[i].GetType())) {
					if ((sequ[i] as Type) != children[i].GetType())
						return false;
				} else if (sequ[i].GetType().IsEnum) {
					if (!(children[i] is SingleTokenSyntax tok) || (TokenType)sequ[i] != tok.Type)
						return false;
				} else
					return false;
				if (!children[i].IsComplete)
					return false;
				
			}
			return true;
		}
        
		#region SyntaxNode implementation
		public override bool isExpanded {
			get => _isExpanded;
			set {
				bool expand = HasChilds ? value : false;
				if  (_isExpanded == expand)
					return;
				_isExpanded = value;
				if (isExpanded && Parent is SyntaxNode sn) {
					try {
						sn.isExpanded = true;
					} catch (Exception ex) {
						Debug.WriteLine($"SyntaxNode expand to the top failed:{ex.Message}");
						Debug.WriteLine(ex.StackTrace);
					}
					
				}
				NotifyValueChanged (_isExpanded);
			}
		}		
		public override bool IsComplete {
			get {
				for (int i = 0; i < children.Count(); i++) {
					if (!children[i].IsComplete)
						return false;
				}
				return true;
			}
		}
        public override bool HasChilds => children.Count > 0;
        public override int SpanStart => HasChilds ? children[0].SpanStart : 0;
		public override int SpanEnd => HasChilds ? children[children.Count - 1].SpanEnd : 0;
		public override SyntaxNode FindNodeIncludingSpan (TextSpan span) {
			foreach (SyntaxNode node in children) {
				if (node.Contains (span))
					return node.FindNodeIncludingSpan (span);
			}
			return this;
		}
		public override SyntaxNode FindNodeIncludingPosition (int pos) {
			foreach (SyntaxNode node in children) {
				if (node.Contains (pos))
					return node.FindNodeIncludingPosition (pos);
			}
			return this;
		}
		#endregion

		public virtual bool IsFoldable => IsComplete && !(Parent != Root && Parent.StartLocation.Line == StartLocation.Line) && LineCount > 1;
		public virtual void UnfoldToTheTop () {
			isFolded = false;
			Parent.UnfoldToTheTop ();
		}
		public virtual int FoldedLineCount {
			get {
				if (isFolded)
					return LineCount - 1;
				int tmp = 0;
				if (HasChilds) {
					foreach (MultiNodeSyntax n in children.OfType<MultiNodeSyntax>().Where (c => c.IsFoldable))
						tmp += n.FoldedLineCount;
				}
				return tmp;
			}
		}
		
		public void ExpandToTheTop () {
			isExpanded = true;
			Parent?.ExpandToTheTop ();
		}
		public IEnumerable<MultiNodeSyntax> VisibleFoldableNodes {
			get {
				if (IsFoldable) {
					yield return this;
				}
				if (!isFolded) {
					foreach	(MultiNodeSyntax n in Children.OfType<MultiNodeSyntax>()) {
						foreach (MultiNodeSyntax folds in n.VisibleFoldableNodes)
							yield return folds;
					}
				}
			}
		}
		
		public IEnumerable<SyntaxException> GetAllExceptions(ReadOnlyTextBuffer source) {
			foreach (SyntaxNode n in children) {
				if (n is UnexpectedTokenSyntax uts)
					yield return new SyntaxException(uts.Message, source.Lines.GetLocation(uts.SpanStart));
				else if (n is MultiNodeSyntax mns) {
					foreach (SyntaxException se in mns.GetAllExceptions(source))
						yield return se;
				}
			}
		}

   }
}