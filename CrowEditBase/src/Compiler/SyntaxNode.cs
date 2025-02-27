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
	public class SyntaxNode : CrowEditComponent {
		internal SyntaxNode () {}
		public SyntaxNode (int startLine, int tokenBase, int? lastTokenIdx = null) {
			StartLine = startLine;
			TokenIndexBase = tokenBase;
			if (lastTokenIdx.HasValue)
				lastTokenOfset = lastTokenIdx - tokenBase;
		}
		
		bool _isExpanded;
		internal int? lastTokenOfset;
		internal bool isFolded;
		internal int lineCount;


		public bool isExpanded {
			get => _isExpanded;
			set {
				if  (_isExpanded == value)
					return;
				_isExpanded = value;
				NotifyValueChanged (_isExpanded);
			}
		}
		public void ExpandToTheTop () {
			isExpanded = true;
			Parent?.ExpandToTheTop ();
		}
		public SyntaxNode Parent { get; private set; }
		public int StartLine { get; private set; }
		public virtual int LineCount => lineCount;
		public virtual bool IsComplete => lastTokenOfset.HasValue;
		public virtual bool IsFoldable => IsComplete && !(Parent != Root && Parent.StartLine == StartLine) && lineCount > 1;
		public virtual SyntaxRootNode Root => Parent.Root;
		public virtual void UnfoldToTheTop () {
			isFolded = false;
			Parent.UnfoldToTheTop ();
		}
		protected Token getTokenByIndex (int idx) => Root.GetTokenByIndex(idx);
		internal List<SyntaxNode> children = new List<SyntaxNode> ();
		List<SyntaxException> exceptions = new List<SyntaxException>();
		public void AddException(SyntaxException e) => exceptions.Add(e);
		public void ResetExceptions(SyntaxException e) => exceptions.Clear();
		public IEnumerable<SyntaxException> Exceptions => exceptions;
		public IEnumerable<SyntaxException> GetAllExceptions() {
				foreach (SyntaxException e in exceptions)
					yield return e;
				foreach	(SyntaxNode n in Children) {
					foreach (SyntaxException ce in n.GetAllExceptions())
						yield return ce;
				}
		}

		public IEnumerable<SyntaxNode> Children => children;
		//public int IndexOf (SyntaxNode node) => children.IndexOf (node);
		public bool HasChilds => children.Count > 0;
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
		public IEnumerable<SyntaxNode> VisibleFoldableNodes {
			get {
				if (IsFoldable) {
					yield return this;
				}
				if (!isFolded) {
					foreach	(SyntaxNode n in Children) {
						foreach (SyntaxNode folds in n.VisibleFoldableNodes)
							yield return folds;
					}
				}
			}
		}
		public virtual int FoldedLineCount {
			get {
				if (isFolded)
					return lineCount;
				int tmp = 0;
				if (HasChilds) {
					foreach (SyntaxNode n in children.Where (c => c.IsFoldable))
						tmp += n.FoldedLineCount;
				}
				return tmp;
			}
		}

		public virtual int TokenIndexBase { get; private set; }
		public virtual int TokenCount => lastTokenOfset.HasValue ? lastTokenOfset.Value + 1 : 0;
		public int? LastTokenIndex =>  lastTokenOfset.HasValue ? TokenIndexBase + lastTokenOfset.Value : null;

		public int EndLine {
			internal set {
				lineCount = value - StartLine + 1;
			}
			get => StartLine + lineCount - 1;
		}
		public TextSpan Span {
			get {
				Token startTok = getTokenByIndex(TokenIndexBase);
				Token endTok = LastTokenIndex.HasValue ? getTokenByIndex (LastTokenIndex.Value) : startTok;
				return new TextSpan (startTok.Start, endTok.End);
			}
		}
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
		void offset (int tokenOffset, int lineOffset) {
			TokenIndexBase += tokenOffset;
			StartLine += lineOffset;
			foreach (SyntaxNode child in children) {
				child.offset (tokenOffset, lineOffset);
			}
		}
		public SyntaxNode FindNodeIncludingPosition (int pos) {
			foreach (SyntaxNode node in children) {
				if (node.Contains (pos))
					return node.FindNodeIncludingPosition (pos);
			}
			return this;
		}
		public T FindNodeIncludingPosition<T> (int pos) {
			foreach (SyntaxNode node in children) {
				if (node.Contains (pos))
					return node.FindNodeIncludingPosition<T> (pos);
			}

			return this is T tt ? tt : default;
		}
		public SyntaxNode FindNodeIncludingSpan (TextSpan span) {
			foreach (SyntaxNode node in children) {
				if (node.Contains (span))
					return node.FindNodeIncludingSpan (span);
			}
			return this;
		}
		public bool Contains (int pos) => Span.Contains (pos);
		public bool Contains (TextSpan span) => Span.Contains (span);
		public void Dump (int level = 0) {
			Console.WriteLine ($"{new string('\t', level)}{this}");
			foreach (SyntaxNode node in children)
				node.Dump (level + 1);
		}
		public override string ToString() => $"l:({StartLine,3},{LineCount,3}) tks:{TokenIndexBase},{TokenCount} {this.GetType().Name}";
		public string AsText() {
			return Span.Length < 0 ? "" : Root.GetText(Span).ToString();
		}
		public bool IsSimilar (SyntaxNode other) => this.GetType() == other?.GetType();


        public class CompareOnStartLine : IComparer<SyntaxNode>
        {
            public int Compare(SyntaxNode x, SyntaxNode y) => x.StartLine - y.StartLine;
        }
    }
}