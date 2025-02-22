// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using CrowEditBase;

namespace CrowEdit.Ebnf
{
	public class EbnfSyntaxAnalyser : SyntaxAnalyser {
        public override SyntaxNode Root => currentNode;
		public EbnfSyntaxAnalyser  (EbnfDocument source) : base (source) {
			this.source = source;
		}
        public override void Process()
        {
            EbnfDocument doc = source as EbnfDocument;
			Exceptions = new List<SyntaxException>();
			currentNode = new EbnfRootSyntax (doc);
			currentLine = 0;
			source2 = doc.Source;

			Span<Token> toks = source.Tokens;
			tokIdx = 0;

			while (tokIdx < toks.Length) {
				curTok = toks[tokIdx];

				switch (curTok.GetTokenType())
				{
					case EbnfTokenType.LineBreak:
						currentLine++;
						break;
					case EbnfTokenType.SymbolName:
						currentNode = currentNode.AddChild(new ProductionSyntax(0, currentLine, tokIdx));
						break;
					

				}
				tokIdx++;
			}


			setCurrentNodeEndLine (currentLine);
        }
		
		Token[] tokens;
		Stack<object> resolveStack;//expression resolutions
		string source2;

		bool EOF => tokIdx == tokens.Length;
		bool EndOfExpression =>
			EOF || tokIdx > tokens.Length - 2 || tokens[tokIdx + 1].GetTokenType() == EbnfTokenType.SymbolAffectation;
		bool tryRead (out Token tok) {
			if (EOF) {
				tok = default;
				return false;
			}
			tok = tokens [tokIdx++];
			return true;
		}
		bool tryPeek (out Token tok) {
			if (EOF) {
				tok = default;
				return false;
			}
			tok = tokens [tokIdx];
			return true;
		}
		bool tryRead (out Token tok, EbnfTokenType expectedType) {
			if (EOF) {
				tok = default;
				return false;
			}
			tok = tokens [tokIdx++];
			return tok.GetTokenType() == expectedType;
		}		
		bool tryPeek (out Token tok, EbnfTokenType expectedType) {
			if (EOF) {
				tok = default;
				return false;
			}
			tok = tokens [tokIdx];
			return tok.GetTokenType() == expectedType;
		}
		bool resolvStackPeekIsOpenBracket =>
			resolveStack.TryPeek (out object elt) && elt is Token tok && tok.GetTokenType() == EbnfTokenType.OpenBracket;
		bool resolvStackPeekIsSequenceOperator =>
			resolveStack.TryPeek (out object elt) && elt is Expression;

		Expression resolveStackPopExpression () {
			if (resolveStack.TryPeek (out object ro)) {
				if (ro is Expression exp) {
					resolveStack.Pop ();
					return exp;
				} else
					throw new EbnfParserException ($"resolve: expecting expression, having {ro}");
			} else
				throw new EbnfParserException ($"resolve: empty stack");
		}
		Expression resolveStackPeekExpression () {
			if (resolveStack.TryPeek (out object ro)) {
				if (ro is Expression exp)
					return exp;
				else
					throw new EbnfParserException ($"resolve: expecting expression, having {ro}");
			} else
				throw new EbnfParserException ($"resolve: empty stack");
		}
		bool resolveStackTryPeek<T> (out T obj) {
			if (resolveStack.TryPeek (out object ro)) {
				if (ro is T t) {
					obj = t;
					return true;
				}
			}
			obj = default;
			return false;
		}


		Expression resolve (Expression rightOp = null) {
			CompoundExpression compExp = default;
			if (rightOp == null)
				rightOp = resolveStackPopExpression ();	
			
			if (resolveStack.TryPeek (out object obj)) {
				if (obj is Token tok) {

					if (tok.GetTokenType() == EbnfTokenType.OpenBracket)
						return rightOp;
					
					resolveStack.Pop ();
					if (tok.GetTokenType() == EbnfTokenType.ChoiceOp)
						compExp = new CompoundExpression (CompoundExpression.Type.Choice) { SecondOperand = rightOp };
					else if (tok.GetTokenType() == EbnfTokenType.ExclusionOp)
						compExp = new CompoundExpression (CompoundExpression.Type.Exclusion) { SecondOperand = rightOp };
					else
						throw new EbnfParserException ($"resolve: expecting operator, having {tok.Type}, {tok.AsString (source2)}");

					if (resolveStack.TryPop (out obj) && obj is Expression exp) 
						compExp.FirstOperand = exp;
					else
						throw new EbnfParserException ($"resolve: expecting expression, having {obj}");
				} else if (obj is Expression exp) {
					resolveStack.Pop ();
					compExp = new CompoundExpression (CompoundExpression.Type.Sequence) {
						FirstOperand = exp,
						SecondOperand = rightOp
					};
				} else
					throw new EbnfParserException ($"resolve: unexpected stack element: {obj}");

				return compExp;
			}
			return rightOp;
		}
		Token Read ()  => tokens [tokIdx++];
		Token Peek  => tokens [tokIdx];

		int operatorPrecedance (Token tok) {
			if (tok.GetTokenType().HasFlag (EbnfTokenType.WhiteSpace))//sequence operator
				return 3;
			if (tok.GetTokenType() == EbnfTokenType.ExclusionOp)
				return 2;
			if (tok.GetTokenType() == EbnfTokenType.ChoiceOp)
				return 4;
			throw new EbnfParserException ($"unknow operator: {tok}");
		}

		void checkCardinalityAndPushNewExpression (Expression exp) {
			if (tryPeek (out Token tok, EbnfTokenType.CardinalityOp)) {
				Read ();
				switch (tok.AsString (source2)) {
					case "?":
						exp.Optional = exp.Single = true;
						break;
					case "+":
						exp.Optional = exp.Single = false;
						break;
					case "*":
						exp.Optional = true;
						exp.Single = false;
						break;
				}
			}
			if (resolveStackTryPeek<Expression> (out Expression leftOp)) {//if exp on the stack, the implicit operator is sequence with precedence=3
				resolveStack.Pop ();
				if (resolveStackTryPeek<Token> (out tok)) {
					if (tok.GetTokenType().HasFlag (EbnfTokenType.Operator)) {
						if (operatorPrecedance (tok) <= 3)
							resolveStack.Push (resolve (leftOp));
						else
							resolveStack.Push (leftOp);
					} else if (tok.GetTokenType() == EbnfTokenType.OpenBracket)
						resolveStack.Push (leftOp);
				} else //so theres an expression on the stack, the operator is sequenceOp (whitespace) with precedence = 3
					resolveStack.Push (resolve (leftOp));
			}
			resolveStack.Push (exp);
		}
		void storeSymbol () {
			Expression rightOp = resolve ();
			while (resolveStack.Count > 0)
				rightOp = resolve (rightOp);
			curSymbol.Expression = rightOp;
			symbols.Add (curSymbol);
			curSymbol = null;
			resolveStack = null;
		}
		List<SymbolDecl> symbols;
		SymbolDecl curSymbol;
		public SymbolDecl[] GetSymbols () {
			tokIdx = 0;
			resolveStack = null;

			symbols = new List<SymbolDecl> (100);
			curSymbol = null;
			Token tok = default;

			while (!EOF) {
				if (Peek.GetTokenType() == EbnfTokenType.TokenSectionStart) {
					Read ();
					continue;
				}

				if (resolveStack == null) {
					//no current symbol
					if (Peek.GetTokenType() != EbnfTokenType.SymbolName)
						throw new EbnfParserException ($"expecing symbol name, having {Peek.GetTokenType()}, {Peek.AsString (source2)}");
					curSymbol = new SymbolDecl (Read ().AsString (source2));
					if (!tryRead (out tok, EbnfTokenType.SymbolAffectation))
						throw new EbnfParserException ($"expecing '::='");
					resolveStack = new Stack<object> (16);
				} else if (Peek.GetTokenType() == EbnfTokenType.OpenBracket) {
					tok = Read ();
					resolveStack.Push (tok);
				} else if (Peek.GetTokenType() == EbnfTokenType.ClosingBracket) {
					tok = Read ();
					Expression rightOp = resolve ();
					while (!resolvStackPeekIsOpenBracket) 
						rightOp = resolve (rightOp);
					if (resolveStack.TryPop (out object obj) && obj is Token tk && tk.GetTokenType() == EbnfTokenType.OpenBracket)
						checkCardinalityAndPushNewExpression (rightOp);
					else
						throw new EbnfParserException ($"expecing open bracket.");
				} else if (Peek.GetTokenType().HasFlag (EbnfTokenType.Punctuation)) {
					tok = Read ();
					Expression te = default;
					if (tok.GetTokenType() == EbnfTokenType.CharMatchOpen) {
						bool negative = false;
						if (tryPeek (out tok, EbnfTokenType.CharMatchNegation)) {
							Read ();
							negative = true;
						}
						List<CharRangeElement> elts = new List<CharRangeElement> ();
						CharRangeElement.SingleChar leftOp = null;
						while (tryRead (out tok)) {
							if (tok.GetTokenType() == EbnfTokenType.CharMatchClose) {
								if (leftOp != null)
									elts.Add (leftOp);
								if (elts.Count == 0)
									throw new EbnfParserException ($"empty character range match");
								te = new CharMatchExpression (negative, elts.ToArray());
								break;
							}
							if (tok.GetTokenType() == EbnfTokenType.CharMatchRangeOperator) {
								if (leftOp == null) {
									elts.Add (new CharRangeElement.SingleChar ('-'));
									continue;
								}
								if (tryRead (out tok)) {
									if (tok.GetTokenType() == EbnfTokenType.CharMatch)
										elts.Add (new CharRangeElement.CharRange (leftOp,
											new CharRangeElement.SingleChar(tok.GetChar (source2))));
									else if (tok.GetTokenType() == EbnfTokenType.CodePointMatch)
										elts.Add (new CharRangeElement.CharRange (leftOp,
											new CharRangeElement.SingleChar(tok.AsString (source2))));
									else
										throw new EbnfParserException ($"malformed character range match, expecting end range after '-'");
									leftOp = null;
									continue;
								}
								throw new EbnfParserException ($"malformed character range match");

							} else if (leftOp != null)
								elts.Add (leftOp);
							
							if (tok.GetTokenType() == EbnfTokenType.CharMatch)
								leftOp = new CharRangeElement.SingleChar(tok.GetChar (source2));
							else if (tok.GetTokenType() == EbnfTokenType.CodePointMatch)
								leftOp = new CharRangeElement.SingleChar(tok.AsString (source2));
							else
								throw new EbnfParserException ($"malformed character range match");
						}
					}
					if (tok.GetTokenType() == EbnfTokenType.StringMatchOpen) {
						if (tryRead (out tok, EbnfTokenType.StringMatch)) {
							te = new StringMatch (tok.AsString (source2));
							if (!tryRead (out tok, EbnfTokenType.StringMatchClose))
								throw new EbnfParserException ($"malformed string match");
						} else
							throw new EbnfParserException ($"malformed string match");
					}
					checkCardinalityAndPushNewExpression (te);
				} else if (Peek.GetTokenType() == EbnfTokenType.CodePointMatch) {
					tok = Read ();
					checkCardinalityAndPushNewExpression (
						new CharMatchExpression (false, new CharRangeElement.SingleChar(tok.AsString (source2))));
				} else if (Peek.GetTokenType() == EbnfTokenType.SymbolName) {
					if (EndOfExpression) {
						storeSymbol ();
						continue;
					}
					tok = Read ();
					checkCardinalityAndPushNewExpression (new SymbolMatch (tok.AsString (source2)));
				} else if (Peek.GetTokenType().HasFlag (EbnfTokenType.Operator)) {
					Token newOp = Read ();					
					if (newOp.GetTokenType() == EbnfTokenType.SymbolAffectation || newOp.GetTokenType() == EbnfTokenType.CardinalityOp)
						System.Diagnostics.Debugger.Break ();
					
					if (resolveStackTryPeek<Expression> (out Expression exp)) {
						if (resolveStack.Count > 1) {
							resolveStack.Pop ();
							if (resolveStackTryPeek<Token> (out tok)) {
								if (tok.GetTokenType().HasFlag (EbnfTokenType.Operator)) {
									if (operatorPrecedance (tok) <= operatorPrecedance (newOp))
										resolveStack.Push (resolve (exp));
									else
										resolveStack.Push (exp);
								} else if (tok.GetTokenType() == EbnfTokenType.OpenBracket)
									resolveStack.Push (exp);
							} else if (3 <= operatorPrecedance (newOp)) { //so theres an expression on the stack, the operator is sequenceOp (whitespace) with precedence = 3
								resolveStack.Push (resolve (exp));
							} else
								resolveStack.Push (exp);
						}
						
						resolveStack.Push (newOp);
					} else
						System.Diagnostics.Debugger.Break ();
				} else
					System.Diagnostics.Debugger.Break ();
			}

			if (resolveStack != null && resolveStack.Count > 0) 
				storeSymbol ();

			return symbols.ToArray ();
		}		
		
	}
}
