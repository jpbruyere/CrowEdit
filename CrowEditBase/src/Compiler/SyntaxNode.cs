// Copyright (c) 2021-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;
using Crow.Text;

namespace CrowEditBase
{
	public abstract class SyntaxRootNode : SyntaxNode {
		protected readonly SourceDocument source;
		public SyntaxRootNode (SourceDocument source)
			: base (0, source.Tokens.FirstOrDefault (), source.Tokens.LastOrDefault ()) {
			this.source = source;
		}
		public override SyntaxRootNode Root => this;
		public override bool IsFoldable => false;
		public override SyntaxNode NextSiblingOrParentsNextSibling => null;
		public override void UnfoldToTheTop() {}
	}
	public class SyntaxNode {
		public SyntaxNode Parent { get; private set; }
		public int StartLine { get; private set; }
		public virtual int LineCount => lineCount;
		public virtual bool IsComplete => EndToken.HasValue;
		public virtual bool IsFoldable => Parent.StartLine != StartLine && lineCount > 1;
		public virtual SyntaxRootNode Root => Parent.Root;
		public virtual void UnfoldToTheTop () {
			isFolded = false;
			Parent.UnfoldToTheTop ();
		}

		List<SyntaxNode> children = new List<SyntaxNode> ();
		public IEnumerable<SyntaxNode> Children => children;
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
		public IEnumerable<SyntaxNode> FoldableNodes {
			get {
				if (IsFoldable)
					yield return this;
				foreach	(SyntaxNode n in Children) {
					foreach (SyntaxNode folds in n.FoldableNodes)
						yield return folds;
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

		public readonly Token StartToken;
		public Token? EndToken { get; set; }
		public SyntaxNode (int startLine, Token tokStart, Token? tokEnd = null) {
			StartLine = startLine;
			StartToken = tokStart;
			EndToken = tokEnd;
		}
		internal bool isFolded;
		internal int lineCount;
		public int EndLine {
			internal set {
				lineCount = value - StartLine + 1;
			}
			get => StartLine + lineCount - 1;
		}
		public TextSpan Span {
			get {
				/*if (HasChilds) {
					return new TextSpan (children.First().Span.Start, children.Last().Span.End)
				}*/
				return new TextSpan (StartToken.Start, EndToken.HasValue ? EndToken.Value.End : StartToken.End);

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
		public T GetChild<T> () => children.OfType<T> ().FirstOrDefault ();
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
		public bool Contains (int pos) =>
			EndToken.HasValue ?
				StartToken.Start <= pos && EndToken.Value.End >= pos : false;
		public void Dump (int level = 0) {
			Console.WriteLine ($"{new string('\t', level)}{this}");
			foreach (SyntaxNode node in children)
				node.Dump (level + 1);
		}
		public override string ToString() => $"{this.GetType().Name}: lines:({StartLine},{LineCount}) tokens:{StartToken} -> {EndToken}";
	}
}