// Copyright (c) 2021-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;

namespace CrowEditBase
{
	public abstract class SyntaxAnalyser {
		//protected abstract void Parse(SyntaxNode node);
		protected SourceDocument source;
		public SyntaxRootNode Root { get; protected set; }
		public IEnumerable<SyntaxException> Exceptions => Root?.GetAllExceptions();
		public SyntaxAnalyser (SourceDocument source) {
			this.source = source;
		}
		public abstract void Process ();
		
		#region Token handling
		protected Token curTok => tokIdx < 0 ? default : tokens[tokIdx];
		protected Token[] tokens;
		protected bool EOF => tokIdx == tokens.Length;
		protected bool tryRead (out Token tok) {
			if (EOF) {
				tok = default;
				return false;
			}
			tok = tokens [tokIdx++];
			return true;
		}
		protected bool tryPeek (out Token tok) {
			if (EOF) {
				tok = default;
				return false;
			}
			tok = tokens [tokIdx];
			return true;
		}
		protected bool tryPeek (Enum expectedType)
			=> EOF ? false : Enum.Equals(tokens [tokIdx].Type, expectedType);
		
		protected bool tryRead (out Token tok, Enum expectedType) {
			if (EOF) {
				tok = default;
				return false;
			}
			tok = tokens [tokIdx++];
			return Enum.Equals(tok.Type, expectedType);
		}		
		protected bool tryPeek (out Token tok, Enum expectedType) {
			if (EOF) {
				tok = default;
				return false;
			}
			tok = tokens [tokIdx];
			return Enum.Equals(tok.Type, expectedType);
		}
		protected bool tryPeekFlag (out Token tok, Enum expectedFlag) {
			if (EOF) {
				tok = default;
				return false;
			}
			tok = tokens [tokIdx];
			return tok.Type.HasFlag(expectedFlag);
		}		

		#endregion

		#region parsing context
		protected int currentLine, tokIdx;
		protected SyntaxNode currentNode;
		#endregion

		/// <summary>
		/// set current node endToken and line count and set current to current.parent.
		/// </summary>
		/// <param name="endToken">The final token of this node</param>
		/// <param name="endLine">the endline number of this node</param>
		protected void setEndLineForCurrentNode (int endTokenOffsetFromCurrentTokIdx = 0) {
			currentNode.TokenCount = tokIdx - currentNode.TokenIndexBase + endTokenOffsetFromCurrentTokIdx;
			currentNode.EndLine = currentLine;
			currentNode = currentNode.Parent;
		}
		protected void setEndOfNode (int endTokenOffsetFromCurrentTokIdx = 0, int endLineOffsetFromCurrentLine = 0) {
			currentNode.TokenCount = tokIdx - currentNode.TokenIndexBase + endTokenOffsetFromCurrentTokIdx;
			currentNode.EndLine = currentLine + endLineOffsetFromCurrentLine;
		}
		protected void setCurrentNodeEndLine (int endLine)
			=> currentNode.EndLine = endLine;
		protected bool skipTrivia(bool skipLineBreaks = true) {
			while (tryPeekFlag(out Token tok, TokenType.Trivia)) {
				if (tok.Type == TokenType.LineBreak) {
					if (!skipLineBreaks)
						return true;
					currentLine++;
				}
				tokIdx++;
			}
			return !EOF;
		}
		protected void addException(string message) {
			currentNode.AddException(new SyntaxException(message, curTok));
		}


		//bool EOF => tokIdx == tokens.Length;
	}
}