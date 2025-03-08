// Copyright (c) 2021-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Crow.Text;

namespace CrowEditBase
{
	public abstract class SyntaxAnalyser {
		protected SourceDocument document;
		protected SyntaxRootNode Root;
		public IEnumerable<SyntaxException> Exceptions => null;// Root?.GetAllExceptions();
		public SyntaxAnalyser (SourceDocument document) {
			this.document = document;
		}
		public abstract Task<SyntaxRootNode> Process ();
		
		#region Token handling
		protected Token curTok => tokIdx < 0 ? default : tokens[tokIdx];
		
		protected ReadOnlySpan<Token> tokens => Root.Tokens;
		protected bool EOF => tokIdx >= tokens.Length;
		protected Token Read() => tokens [tokIdx++];
		protected Token Peek() => tokens [tokIdx];
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
		
		protected bool tryRead (out Token tok) {
			if (EOF) {
				tok = default;
				return false;
			}
			tok = tokens [tokIdx++];
			return true;
		}		
		protected bool tryRead (out Token tok, Enum expectedType) {
			if (EOF) {
				tok = default;
				return false;
			}
			tok = tokens [tokIdx++];
			return Enum.Equals(tok.Type, expectedType);
		}


		#endregion

		#region parsing context
		protected int currentLine = 0, tokIdx = 0;
		//protected MultiNodeSyntax currentNode;
		#endregion

		/// <summary>
		/// set current node endToken and line count and set current to current.parent.
		/// </summary>
		/// <param name="endToken">The final token of this node</param>
		/// <param name="endLine">the endline number of this node</param>
		/*protected void finishCurrentNode (int endTokenOffsetFromCurrentTokIdx = 0) {
			int lastTokOffset = tokIdx - currentNode.TokenIndexBase + endTokenOffsetFromCurrentTokIdx;
			currentNode.lastTokenOfset = lastTokOffset < 0 ? null : lastTokOffset;
			if (endTokenOffsetFromCurrentTokIdx < 0) {
				Token lastTok = currentNode.LastTokenIndex.HasValue ?
					Root.GetTokenByIndex(currentNode.LastTokenIndex.Value) :
					Root.GetTokenByIndex(currentNode.TokenIndexBase);
				
				currentNode.EndLine = lines.GetLocation(lastTok.End).Line;
			}else{
				currentNode.EndLine = currentLine;
			}
			currentNode = currentNode.Parent;
		}
		protected void setCurrentNodeEndLine (int endLine)
			=> currentNode.EndLine = endLine;*/
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
		protected bool skipWhiteSpaces(bool skipLineBreaks = true) {
			while (tryPeekFlag(out Token tok, TokenType.WhiteSpace)) {
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
			/*CharLocation loc = lines.GetLocation(curTok.Start);
			currentNode.AddException(new SyntaxException(message, loc, curTok));*/
		}


		//bool EOF => tokIdx == tokens.Length;
	}
}