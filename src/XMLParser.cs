using System;
using Crow;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CrowEdit
{
	public class XMLParser : Parser
	{

		public XMLParser (CodeTextBuffer _buffer) : base(_buffer)
		{
		}

		public new enum TokenType {
			Unknown = Parser.TokenType.Unknown,
			WhiteSpace = Parser.TokenType.WhiteSpace,
			NewLine = Parser.TokenType.NewLine,
			LineComment = Parser.TokenType.LineComment,
			BlockComment = Parser.TokenType.BlockComment,
			Affectation = Parser.TokenType.Affectation,
			XMLDecl = Parser.TokenType.Preprocessor,
			ElementStart,
			ElementEnd,
			ElementClosing = Parser.TokenType.StatementEnding,
			ElementName = Parser.TokenType.Type,
			AttributeName = Parser.TokenType.Identifier,
			AttributeValueOpening = Parser.TokenType.StringLitteralOpening,
			AttributeValueClosing = Parser.TokenType.StringLitteralClosing,
			AttributeValue = Parser.TokenType.StringLitteral,
		}
		public enum States
		{
			init,       //first statement of prolog, xmldecl should only apear in this state
			prolog,     //misc before doctypedecl
			InternalSubset,    //doctype declaration subset
			ExternalSubsetInit,
			ExternalSubset,
			DTDEnd,//doctype finished
			XML,
			StartTag,
			Content,
			EndTag,
			XMLEnd
		}
		enum Keywords
		{
			DOCTYPE,
			ELEMENT,
			ATTLIST,
			ENTITY,
			NOTATION
		}

		States curState = States.init;

		#region Regular Expression for validity checks
		//private static Regex rxValidChar = new Regex("[\u0020-\uD7FF]");
		private static Regex rxValidChar = new Regex(@"\u0009|\u000A|\u000D|[\u0020-\uD7FF]|[\uE000-\uFFFD]");   //| [\u10000-\u10FFFF] unable to set those plans
		private static Regex rxNameStartChar = new Regex(@":|[A-Z]|_|[a-z]|[\u00C0-\u00D6]|[\u00D8-\u00F6]|[\u00F8-\u02FF]|[\u0370-\u037D]|[\u037F-\u1FFF]|[\u200C-\u200D]|[\u2070-\u218F]|[\u2C00-\u2FEF]|[\u3001-\uD7FF]|[\uF900-\uFDCF]|[\uFDF0-\uFFFD]"); // | [\u10000-\uEFFFF]
		private static Regex rxNameChar = new Regex(@":|[A-Z]|_|[a-z]|[\u00C0-\u00D6]|[\u00D8-\u00F6]|[\u00F8-\u02FF]|[\u0370-\u037D]|[\u037F-\u1FFF]|[\u200C-\u200D]|[\u2070-\u218F]|[\u2C00-\u2FEF]|[\u3001-\uD7FF]|[\uF900-\uFDCF]|[\uFDF0-\uFFFD]|-|\.|[0-9]|\u00B7|[\u0300-\u036F]|[\u203F-\u2040]");//[\u10000-\uEFFFF]|
		private static Regex rxDecimal = new Regex(@"[0-9]+");
		private static Regex rxHexadecimal = new Regex(@"[0-9a-fA-F]+");
		private static Regex rxAttributeValue = new Regex(@"[^<]");
		private static Regex rxEntityValue = new Regex(@"[^<]");
		private static Regex rxPubidChar = new Regex(@"\u0020|\u000D|\u000A|[a-zA-Z0-9]|[-\(\)\+\,\./:=\?;!\*#@\$_%]");
		#endregion

		#region Character ValidityCheck
		public bool nextCharIsValidCharStartName
		{
			get { return rxNameStartChar.IsMatch(new string(new char[]{Peek()})); }
		}
		public bool nextCharIsValidCharName
		{
			get { return rxNameChar.IsMatch(new string(new char[]{Peek()})); }
		}
//		public bool NameIsValid(string name)
//		{
//			if (!rxNameStartChar.IsMatch(char.ConvertFromUtf32(((string)name)[0])))
//				return false;
//
//			return rxNameChar.IsMatch(name);
//		}
//		private bool NextCharIsValidPubidChar
//		{
//			get { return rxPubidChar.IsMatch(char.ConvertFromUtf32(Peek())); }
//		}
//		private bool AttributeValueIsValid(string name)
//		{
//			return string.IsNullOrEmpty(name) ? true : rxAttributeValue.IsMatch(name);
//		}
//		private bool NextCharIsValidEntityValue
//		{
//			get { return rxEntityValue.IsMatch(char.ConvertFromUtf32(Peek())); }
//		}
		#endregion

		public override void Parse ()
		{
			parsed = false;
			Tokens = new List<List<Token>> ();
			TokensLine = new List<Token> ();
			currentLine = currentColumn = 0;
			currentTok = default(Token);
			curState = States.init;

			string tmp = "";

			while (!eof) {
				SkipWhiteSpaces ();

				if (eof)
					break;

				switch (Peek()) {
				case '\n':
					if (currentTok != TokenType.Unknown)
						throw new ParsingException (this, "Unexpected end of line");
					Read ();
					Tokens.Add (TokensLine);
					TokensLine = new List<Token> ();
					break;
				case '<':
					readToCurrTok (true);
					switch (Peek()) {
					case '?':
						if (curState != States.init)
							throw new ParsingException (this, "prolog may appear only on first line");
						readToCurrTok ();
						currentTok += ReadUntil ("?>");
						saveAndResetCurrentTok (TokenType.XMLDecl);
						curState = States.prolog;
						break;
					case '!':
						readToCurrTok ();
						switch (Peek()) {
						case '-':
							readToCurrTok ();
							if (Peek () != '-')
								throw new ParsingException (this, "Expecting comment start tag");
							currentTok += ReadUntil ("--");
							if (Peek () != '>')
								throw new ParsingException (this, "Expecting comment closing tag");
							readAndResetCurrentTok (TokenType.BlockComment);
							break;
						default:
							throw new NotImplementedException ();
						}
						break;
					default:
						if (!(curState == States.Content || curState == States.XML || curState == States.init))
							throw new ParsingException (this, "Unexpected char: '<'");
						if (Peek () == '/') {
							curState = States.EndTag;
							readToCurrTok ();
							saveAndResetCurrentTok (TokenType.ElementEnd);
						} else {
							curState = States.StartTag;
							saveAndResetCurrentTok (TokenType.ElementStart);
						}

						if (!nextCharIsValidCharStartName)
							throw new ParsingException (this, "Expected element name");

						readToCurrTok (true);
						while (nextCharIsValidCharName)
							readToCurrTok ();

						saveAndResetCurrentTok (TokenType.ElementName);
						break;
					}
					break;
				case '/':
					if (curState != States.StartTag)
						throw new ParsingException (this, "Unexpected char: '/'");
					readToCurrTok (true);
					if (Peek () != '>')
						throw new ParsingException (this, "Expecting '>'");
					readAndResetCurrentTok (TokenType.ElementClosing);

					curState = States.XML;
					break;
				case '>':
					readAndResetCurrentTok (TokenType.ElementClosing, true);
					switch (curState) {
					case States.EndTag:
						curState = States.XML;
						break;
					case States.StartTag:
						curState = States.Content;
						break;
					default:
						throw new ParsingException (this, "Unexpected char: '>'");
					}
					break;
				default:
					switch (curState) {
					case States.StartTag:
						if (!nextCharIsValidCharStartName)
							throw new ParsingException (this, "Expected attribute name");
						readToCurrTok (true);
						while (nextCharIsValidCharName)
							readToCurrTok ();
						saveAndResetCurrentTok (TokenType.AttributeName);
						if (Peek () != '=')
							throw new ParsingException (this, "Expecting: '='");
						readAndResetCurrentTok (TokenType.Affectation, true);

						char openAttVal = Peek ();
						if (openAttVal != '"' && openAttVal != '\'')
							throw new ParsingException (this, "Expecting attribute value enclosed either in '\"' or in \"'\"");
						readAndResetCurrentTok (TokenType.AttributeValueOpening, true);

						currentTok.Start = CurrentPosition;
						currentTok.Content = ReadUntil (new string (new char[]{ openAttVal }));
						saveAndResetCurrentTok (TokenType.AttributeValue);

						if (Peek () != openAttVal)
							throw new ParsingException (this, string.Format ("Expecting {0}", openAttVal));
						readAndResetCurrentTok (TokenType.AttributeValueClosing, true);
						break;
					default:
						throw new ParsingException (this, "unexpected char: " + Peek ());
					}
					break;
				}
			}
			if (TokensLine.Count > 0)
				Tokens.Add (TokensLine);

			parsed = true;
		}
	}
}

