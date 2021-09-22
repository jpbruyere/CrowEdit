// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using Crow.Text;
using Crow;
using System.Collections;
using CrowEditBase;
using static CrowEditBase.CrowEditBase;

namespace CECrowPlugin
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

		public override IList GetSuggestions (int pos) {
			currentToken = FindTokenIncludingPosition (pos);
			currentNode = FindNodeIncludingPosition (pos);
			return null;
		}
		public override TextChange? GetCompletionForCurrentToken (object suggestion, out TextSpan? newSelection) {
			newSelection = null;
			return null;
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