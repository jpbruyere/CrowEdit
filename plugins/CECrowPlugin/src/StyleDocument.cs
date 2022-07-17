// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using Crow.Text;
using Crow;
using System.Collections;
using CrowEditBase;
using static CrowEditBase.CrowEditBase;
using Drawing2D;

namespace CECrowPlugin.Style
{
	public class StyleDocument : SourceDocument {


		public StyleDocument (string fullPath, string editorPath) : base (fullPath, editorPath) {
			App.GetService<CrowService> ()?.Start ();

			/*if (project is MSBuildProject msbp) {
				if (msbp.IsCrowProject)
			}*/
		}

		protected override Tokenizer CreateTokenizer() => new StyleTokenizer ();
		protected override SyntaxAnalyser CreateSyntaxAnalyser() => new StyleSyntaxAnalyser (this);

		public override IList GetSuggestions (CharLocation loc) {
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.WriteLine ($"Tok: {this.CurrentTokenString} {((StyleTokenType)CurrentToken.Type).ToString()}");
			Console.ResetColor();
			return null;
		}
		public override bool TryGetCompletionForCurrentToken(object suggestion, out TextChange change, out TextSpan? newSelection)
		{
			change = default;
			newSelection = null;
			return false;
		}
		public override Color GetColorForToken(TokenType tokType)
		{
			StyleTokenType xmlTokType = (StyleTokenType)tokType;
			if (xmlTokType.HasFlag (StyleTokenType.Punctuation))
				return Colors.DarkGrey;
			if (xmlTokType.HasFlag (StyleTokenType.Trivia))
				return Colors.DimGrey;
			if (xmlTokType == StyleTokenType.MemberName)
				return Colors.Blue;
			if (xmlTokType == StyleTokenType.ConstantName)
				return Colors.DarkCyan;
			else if (xmlTokType.HasFlag (StyleTokenType.Name))
				return Colors.Green;
			if (xmlTokType == StyleTokenType.MemberValuePart)
				return Colors.OrangeRed;
			if (xmlTokType == StyleTokenType.EqualSign)
				return Colors.Black;
			if (xmlTokType == StyleTokenType.Unknown)
				return Colors.Red;
			return Colors.YellowGreen;
		}
	}
}