// Copyright (c) 2021-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow;
using Crow.Text;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using Drawing2D;

namespace CrowEditBase
{
	public abstract class SourceDocument : TextDocument {
		public SourceDocument (string fullPath, string editorPath = "#ui.sourceEditor.itmp")
			: base (fullPath, editorPath) {
		}
		protected Token[] tokens;
		protected SyntaxRootNode root;
		protected int currentTokenIndex;
		SyntaxNode currentNode;

		protected Token currentToken => currentTokenIndex < 0 ? default : tokens[currentTokenIndex];
		protected Token? previousToken {
			get {
				if (currentTokenIndex < 1) 
					return null;
				return tokens[currentTokenIndex-1];
			}
		}
		public SyntaxNode CurrentNode {
			get => currentNode;
			set {
				if (currentNode == value)
					return;
				currentNode = value;
				NotifyValueChanged ("CurrentNode", currentNode);
			}
		}
		public string CurrentTokenString => root?.GetTokenStringByIndex (currentTokenIndex);
		public Token CurrentToken => currentToken;
		public bool IsParsed => tokens.Length > 0 && root != null;
		public SyntaxRootNode Root => root;

		//public SyntaxNode EditedNode { get; protected set; }

		public Token[] Tokens => tokens;
		public IEnumerable<SyntaxNode> SyntaxRootChildNodes => root?.children;
		public LineCollection Lines => lines;
		public Token FindTokenIncludingPosition (int pos) {
			if (pos == 0 || tokens == null || tokens.Length == 0)
				return default;
			int idx = Array.BinarySearch (tokens, 0, tokens.Length, new  Token () {Start = pos});

			return idx == 0 ? tokens[0] : idx < 0 ? tokens[~idx - 1] : tokens[idx];
		}
		public int FindTokenIndexIncludingPosition (int pos) {
			if (pos == 0 || tokens == null || tokens.Length == 0)
				return default;
			int idx = Array.BinarySearch (tokens, 0, tokens.Length, new  Token () {Start = pos});

			return idx == 0 ? 0 : idx < 0 ? ~idx - 1 : idx - 1;
		}
		/// <summary>
		/// if outermost is true, return oldest ancestor exept root node, useful for folding.
		/// </summary>
		public SyntaxNode FindNodeIncludingPosition (int pos, bool outerMost = false) {
			if (root == null)
				return null;
			if (!root.Contains (pos))
				return null;
			SyntaxNode sn = root.FindNodeIncludingPosition (pos);
			if (outerMost) {
				while (sn.Parent != root && sn.Span.Start == sn.Parent.Span.Start)
					sn = sn.Parent;
			}
			return sn;
		}
		public T FindNodeIncludingPosition<T> (int pos) {
			if (root == null)
				return default;
			if (!root.Contains (pos))
				return default;
			return root.FindNodeIncludingPosition<T> (pos);
		}
		public SyntaxNode FindNodeIncludingSpan (TextSpan span) {
			if (root == null)
				return null;
			if (!root.Contains (span))
				return null;
			return root.FindNodeIncludingSpan (span);
		}
		protected override void reloadFromFile () {
			base.reloadFromFile ();
			parse ();
		}
		protected override void apply(TextChange change)
		{
			SyntaxNode editedNode = FindNodeIncludingSpan (new TextSpan (change.Start, change.End));

			base.apply(change);

			Tokenizer tokenizer = CreateTokenizer ();
			SyntaxAnalyser syntaxAnalyser = CreateSyntaxAnalyser ();
			
			if (syntaxAnalyser == null) {
				root = null;
				return;
			}

			//SyntaxNode changedNode = root.FindNodeIncludingSpan (TextSpan.FromStartAndLength (change.Start, change.ChangedText.Length));			
			
			tokens = tokenizer.Tokenize (buffer.Span);
			syntaxAnalyser.Process ();

			root = syntaxAnalyser.Root;
			NotifyValueChanged("Exceptions", syntaxAnalyser.Exceptions);
			/*
			SyntaxNode newNode = syntaxAnalyser.Root.FindNodeIncludingSpan (TextSpan.FromStartAndLength (change.Start, change.ChangedText.Length));

			if (editedNode == null) {
				//System.Diagnostics.Debugger.Break ();
				root = syntaxAnalyser.Root;
			} else if (newNode.IsSimilar (editedNode)) {
				if (!tryReplaceNode (editedNode, newNode))
					RootNode = syntaxAnalyser.Root;
			} else if (newNode.Parent != null && newNode.Parent.IsSimilar (editedNode)) {
				if (!tryReplaceNode (editedNode, newNode.Parent))
					RootNode = syntaxAnalyser.Root;
			} else if (editedNode.Parent != null && newNode.IsSimilar (editedNode.Parent)) {
				if (!tryReplaceNode (editedNode.Parent, newNode))
					RootNode = syntaxAnalyser.Root;
			} else if (newNode.Parent != null && editedNode.Parent != null && newNode.Parent.IsSimilar (editedNode.Parent)) {
				if (!tryReplaceNode (editedNode.Parent, newNode.Parent))
					RootNode = syntaxAnalyser.Root;
			} else {
				//System.Diagnostics.Debugger.Break ();
				RootNode = syntaxAnalyser.Root;
			}
			*/

			//updateCurrentTokAndNode (change.End2);
			//EditedNode = editedNode;

			//Console.WriteLine ($"CurrentToken: idx({currentTokenIndex}) {currentToken} {RootNode.Root.GetTokenStringByIndex(currentTokenIndex)}");
		}
		static bool tryReplaceNode (SyntaxNode editedNode, SyntaxNode newNode) {
			if (newNode is SyntaxRootNode || editedNode is SyntaxRootNode)
				return false;
			editedNode.Replace (newNode);
			return true;
		}

		internal void updateCurrentTokAndNode (CharLocation loc) {
			int pos = lines.GetAbsolutePosition(loc);
			if (tokens.Length > 0) {
				currentTokenIndex = FindTokenIndexIncludingPosition (pos);
				CurrentNode = FindNodeIncludingSpan (currentToken.Span);
				NotifyValueChanged ("CurrentTokenString", (object)CurrentTokenString);
				//NotifyValueChanged ("CurrentTokenType", (uint)(currentToken.Type)>>8);
				NotifyValueChanged ("CurrentTokenType", (object)GetTokenTypeString(currentToken.Type));
			}else {
				currentTokenIndex = -1;
				CurrentNode = null;
				NotifyValueChanged ("CurrentTokenString", (object)"no token");
			}
		}

		public virtual Color GetColorForToken (TokenType tokType) {
			if (tokType.HasFlag (TokenType.Punctuation))
				return Colors.DarkGrey;
			if (tokType.HasFlag (TokenType.Trivia))
				return Colors.Silver;
			if (tokType == TokenType.Keyword)
				return Colors.DarkSlateBlue;
			return Colors.Red;
		}
		public virtual string GetTokenTypeString (TokenType tokenType) => tokenType.ToString();
		protected abstract Tokenizer CreateTokenizer ();
		protected abstract SyntaxAnalyser CreateSyntaxAnalyser ();
		public abstract IList GetSuggestions (CharLocation loc);

		/// <summary>
		/// complete current token with selected item from the suggestion overlay.
		/// It may set a new position or a new selection.
		/// </summary>
		/// <param name="suggestion">selected object of suggestion overlay</param>
		/// /// <param name="change">the text change to apply</param>
		/// <param name="newSelection">new position or selection, null if normal position after text changes</param>
		/// <returns>true if successed</returns>
		public abstract bool TryGetCompletionForCurrentToken (object suggestion, out TextChange change, out TextSpan? newSelection);
		protected bool previousTokHasFlag(TokenType flag) => previousToken.HasValue && previousToken.Value.Type.HasFlag(flag);
		void parse () {
			Tokenizer tokenizer = CreateTokenizer ();
			tokens = tokenizer?.Tokenize (source);
			SyntaxAnalyser syntaxAnalyser = CreateSyntaxAnalyser ();
			Stopwatch sw = Stopwatch.StartNew ();
			syntaxAnalyser?.Process ();
			sw.Stop();
			root = syntaxAnalyser?.Root;

			//CrowEditBase.App.Log (LogType.Low, $"Syntax Analysis done in {sw.ElapsedMilliseconds}(ms) {sw.ElapsedTicks}(ticks)");
			if (syntaxAnalyser == null)
				return;
				/*foreach (Token t in Tokens)
					Console.WriteLine ($"{t,-40} {Source.AsSpan(t.Start, t.Length).ToString()}");
				syntaxAnalyser.Root.Dump();*/
		}

	}
}