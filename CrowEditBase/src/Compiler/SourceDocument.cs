// Copyright (c) 2021-2025  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow;
using Crow.Text;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using Drawing2D;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace CrowEditBase
{
	public abstract class SourceDocument : TextDocument {
		#region CTOR
		public SourceDocument (string fullPath, string editorPath = "#ui.sourceEditor.itmp")
			: base (fullPath, editorPath) {
		}
		#endregion

		protected SyntaxRootNode root;
		CancellationTokenSource cancelSource;
		Task backgroundCompilationTask;

        public SyntaxRootNode Root {
			get => root;
		}
		public bool IsParsed => root != null && Tokens.Length > 0;
		public ReadOnlySpan<Token> Tokens => root.Tokens;
		public IEnumerable<SyntaxNode> SyntaxRootChildNodes => root?.children;
		public SyntaxException CurrentException {
			get => CrowEditBase.App.CurrentException;
			set {
				CrowEditBase.App.CurrentException = value;
				SetLocation(value.Location);
			} 
		}
		
		#region commands
		public Command CMDRefreshSyntaxTree;
        protected override void initCommands()
        {
            base.initCommands();

			CMDRefreshSyntaxTree = new ActionCommand ("Reparse", parse, "#icons.refresh.svg", true);
        }
		#endregion

		#region token & node searching
		public Token GetTokenByIndex(int tokIdx) => IsParsed && tokIdx >= 0 ?
						Tokens[Math.Min(Tokens.Length - 1, tokIdx)] : default;
		public Token FindTokenIncludingPosition (int pos) {
			if (!IsParsed || pos == 0 || Tokens.Length == 0)
				return default;
			int idx = Tokens.BinarySearch(new  Token (pos));
			return idx == 0 ? Tokens[0] : idx < 0 ? Tokens[~idx - 1] : Tokens[idx];
		}
		public int FindTokenIndexIncludingPosition (int pos) {
			if (!IsParsed || pos == 0 || Tokens.Length == 0)
				return default;
			int idx = Tokens.BinarySearch(new  Token (pos));
			return idx == 0 ? 0 : idx < 0 ? ~idx - 1 : idx;
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
		#endregion
		
		#region TextDocument implementation
		protected override void reloadFromFile () {
			base.reloadFromFile ();
			parse ();
		}
		protected override void apply(TextChange change)
		{
			SyntaxNode editedNode = root?.FindNodeIncludingSpan (new TextSpan (change.Start, change.End));
			base.apply(change);
			parse ();
			

			//SyntaxNode changedNode = root.FindNodeIncludingSpan (TextSpan.FromStartAndLength (change.Start, change.ChangedText.Length));			
			
			
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
		#endregion

		public virtual Color GetColorForToken (Token token)
		{
			TokenType tokType = token.Type;
			if (tokType.HasFlag (TokenType.Punctuation))
				return Colors.DarkGrey;
			if (tokType.HasFlag (TokenType.WhiteSpace))
				return Colors.Gainsboro;
			if (tokType.HasFlag (TokenType.Trivia))
				return Colors.Silver;
			if (tokType == TokenType.Keyword)
				return Colors.DarkSlateBlue;
			return Colors.Red;
		}
		public virtual string GetTokenTypeString (TokenType tokenType) => tokenType.ToString();
		//protected abstract Tokenizer CreateTokenizer ();
		public abstract IList GetSuggestions (int absoluteTextPos, int currentTokenIndex, SyntaxNode currentNode, CharLocation loc);
		protected virtual async void parse () {
			if (backgroundCompilationTask != null && !backgroundCompilationTask.IsCompleted) {
				cancelSource.Cancel();
				await backgroundCompilationTask;
			}

			cancelSource = new CancellationTokenSource();
			backgroundCompilationTask = Task.Run(()=>parseAssync(cancelSource.Token));
			
			//CurrentNode?.ExpandToTheTop();
			
			//CrowEditBase.App.Log (LogType.Low, $"Syntax Analysis done in {sw.ElapsedMilliseconds}(ms) {sw.ElapsedTicks}(ticks)");
		}
		protected abstract SyntaxAnalyser CreateSyntaxAnalyser ();

		async void parseAssync(CancellationToken cancel) {
			SyntaxAnalyser syntaxAnalyser = CreateSyntaxAnalyser ();
			root = await syntaxAnalyser.Process (cancel);
			if (cancel.IsCancellationRequested)
				return;

			NotifyValueChanged("Exceptions", syntaxAnalyser?.Exceptions);
			NotifyValueChanged ("SyntaxRootChildNodes", (object)null);
			NotifyValueChanged ("SyntaxRootChildNodes", SyntaxRootChildNodes);
		}


	}
}