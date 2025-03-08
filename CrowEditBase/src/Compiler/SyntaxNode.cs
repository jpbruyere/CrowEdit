// Copyright (c) 2021-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;
using Crow.Text;
using Crow;

namespace CrowEditBase
{
	public class SingleTokenSyntax : SyntaxNode {
		Token token;
        public override TokenType Type => token.Type;
        public override int SpanStart => token.Start;
		public override int SpanEnd => token.End;
		public SingleTokenSyntax(Token tok) {
			token = tok;
		}
	}
	public class MultiNodeSyntax : SyntaxNode {
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

		
		public override bool HasChilds => children.Count > 0;
        public override int SpanStart => HasChilds ? children[0].SpanStart : 0;
		public override int SpanEnd => HasChilds ? children[children.Count - 1].SpanEnd : 0;

		internal bool isFolded;
		public virtual bool IsFoldable => IsComplete && !(Parent != Root && Parent.StartLocation.Line == StartLocation.Line) && LineCount > 1;
		public void ExpandToTheTop () {
			isExpanded = true;
			Parent?.ExpandToTheTop ();
		}
		public virtual void UnfoldToTheTop () {
			isFolded = false;
			Parent.UnfoldToTheTop ();
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
		public virtual int FoldedLineCount {
			get {
				if (isFolded)
					return LineCount;
				int tmp = 0;
				if (HasChilds) {
					foreach (MultiNodeSyntax n in children.OfType<MultiNodeSyntax>().Where (c => c.IsFoldable))
						tmp += n.FoldedLineCount;
				}
				return tmp;
			}
		}
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
    }
	public abstract class SyntaxNode : CrowEditComponent {


		#region  Folding and ?expand?
		bool _isExpanded;
		public bool isExpanded {
			get => _isExpanded;
			set {
				if  (_isExpanded == value)
					return;
				_isExpanded = value;
				NotifyValueChanged (_isExpanded);
			}
		}
		#endregion

		public MultiNodeSyntax Parent { get; internal set; }
		public virtual SyntaxRootNode Root => Parent.Root;
		public virtual TokenType Type => TokenType.Unknown;
		public virtual int SpanStart => 0;
		public virtual int SpanEnd => 0;

		
		public virtual bool HasChilds => false;
		public virtual bool IsComplete => false;



		/*public int StartLine { get; private set; }
		public virtual int LineCount => lineCount;*/


		/*List<SyntaxException> exceptions = new List<SyntaxException>();
		public IEnumerable<SyntaxException> Exceptions => exceptions;
		public void AddException(SyntaxException e) => exceptions.Add(e);
		public void ResetExceptions(SyntaxException e) => exceptions.Clear();
		public IEnumerable<SyntaxException> GetAllExceptions() {
				foreach (SyntaxException e in exceptions)
					yield return e;
				foreach	(SyntaxNode n in Children) {
					foreach (SyntaxException ce in n.GetAllExceptions())
						yield return ce;
				}
		}*/


		protected Token getTokenByIndex (int idx) => Root.GetTokenByIndex(idx);

		
		//public int IndexOf (SyntaxNode node) => children.IndexOf (node);
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
		public virtual SyntaxNode NextSiblingOrParentsNextSibling
			=> NextSibling ?? Parent.NextSiblingOrParentsNextSibling;

		/*public virtual int TokenIndexBase { get; private set; }
		public virtual int TokenCount => lastTokenOfset.HasValue ? lastTokenOfset.Value + 1 : 0;
		public int? LastTokenIndex =>  lastTokenOfset.HasValue ? TokenIndexBase + lastTokenOfset.Value : null;

		public int EndLine {
			set {
				lineCount = value - StartLine + 1;
			}
			get => StartLine + lineCount - 1;
		}*/
		public TextSpan Span => new TextSpan (SpanStart, SpanEnd);
		public CharLocation StartLocation => Root.GetLocation(SpanStart);
		public CharLocation EndLocation => Root.GetLocation(SpanEnd);
		public int LineCount => EndLocation.Line - StartLocation.Line + 1;

		/*
		public void Replace (SyntaxNode newNode) {
			Parent.replaceChild (this, newNode);
		}
		void replaceChild (SyntaxNode oldNode, SyntaxNode newNode) {
			int idx = children.IndexOf (oldNode);
			children[idx] = newNode;
			newNode.Parent = this;
			int tokIdxDiff = newNode.TokenCount - oldNode.TokenCount;
			int lineDiff = newNode.EndLine - oldNode.EndLine;
			if (tokIdxDiff == 0 && lineDiff == 0)
				return;

			SyntaxNode curNode = this;
			while (curNode != null) {
				curNode.lineCount += lineDiff;
				curNode.TokenCount += tokIdxDiff;
				if (curNode is SyntaxRootNode)
					break;
				while (++idx < curNode.children.Count)
					curNode.children[idx].offset (tokIdxDiff, lineDiff);
				idx = curNode.Parent.children.IndexOf (curNode);
				curNode = curNode.Parent;
			}
		}*/
		/*void offset (int tokenOffset, int lineOffset) {
			TokenIndexBase += tokenOffset;
			StartLine += lineOffset;
			foreach (SyntaxNode child in children) {
				child.offset (tokenOffset, lineOffset);
			}
		}*/
/*		
		public T FindNodeIncludingPosition<T> (int pos) {
			foreach (SyntaxNode node in children) {
				if (node.Contains (pos))
					return node.FindNodeIncludingPosition<T> (pos);
			}

			return this is T tt ? tt : default;
		}*/
		public virtual SyntaxNode FindNodeIncludingPosition (int pos) => this;
		public virtual SyntaxNode FindNodeIncludingSpan (TextSpan span) => this;
		
		public bool Contains (int pos) => Span.Contains (pos);
		public bool Contains (TextSpan span) => Span.Contains (span);
		/*public void Dump (int level = 0) {
			Console.WriteLine ($"{new string('\t', level)}{this}");
			foreach (SyntaxNode node in children)
				node.Dump (level + 1);
		}*/
		//public override string ToString() => $"l:({StartLine,3},{LineCount,3}) tks:{TokenIndexBase},{TokenCount} {this.GetType().Name}";
		public override string ToString() => $"{this.GetType().Name}";
		public string AsText() {
			return Span.Length < 0 ? "" : Root.GetText(Span).ToString();
		}
		public bool IsSimilar (SyntaxNode other) => this.GetType() == other?.GetType();


        public class CompareOnStartLine : IComparer<SyntaxNode>
        {
            public int Compare(SyntaxNode x, SyntaxNode y) => x.StartLocation.Line - y.StartLocation.Line;
        }
    }
}