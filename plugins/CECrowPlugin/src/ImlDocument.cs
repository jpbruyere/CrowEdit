// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Linq;
using Crow.Text;
using System.Collections.Generic;
using System.Diagnostics;
using Crow;
using IML = Crow.IML;
using System.Collections;
using System.Reflection;
using CrowEditBase;
using static CrowEditBase.CrowEditBase;

using CrowEdit.Xml;

using AttributeSyntax = CrowEdit.Xml.AttributeSyntax;
using Drawing2D;

namespace CECrowPlugin
{
	public class ImlDocument : XmlDocument {


		public ImlDocument (string fullPath, string editorPath) : base (fullPath, editorPath) {
			App.GetService<CrowService> ()?.Start ();

			/*if (project is MSBuildProject msbp) {
				if (msbp.IsCrowProject)
			}*/
		}
		protected override Tokenizer CreateTokenizer() => new ImlTokenizer ();
		protected override SyntaxAnalyser CreateSyntaxAnalyser() => new ImlSyntaxAnalyser (this);
		public override string GetTokenTypeString (TokenType tokenType) => ((ImlTokenType)tokenType).ToString();

		string[] allWidgetNames = typeof (Widget).Assembly.GetExportedTypes ().Where(t=>typeof(Widget).IsAssignableFrom (t))
					.Select (s => s.Name).ToArray ();


		IEnumerable<MemberInfo> getAllCrowTypeMembers (string crowTypeName) {
			Type crowType = IML.Instantiator.GetWidgetTypeFromName (crowTypeName);
			return crowType?.GetMembers (BindingFlags.Public | BindingFlags.Instance).
				Where (m=>((m is PropertyInfo pi && pi.CanWrite) || (m is EventInfo)) &&
						m.GetCustomAttribute<XmlIgnoreAttribute>() == null);
		}
		MemberInfo getCrowTypeMember (string crowTypeName, string memberName) {
			Type crowType = IML.Instantiator.GetWidgetTypeFromName (crowTypeName);
			return crowType.GetMember (memberName, BindingFlags.Public | BindingFlags.Instance).FirstOrDefault ();
		}
		
		protected bool previousTokHasFlag(ImlTokenType flag) => previousToken.HasValue && previousToken.Value.Type.HasFlag(flag);
		
		protected bool tryCast<TOUT> (object objectToCast, out TOUT result) {
			result = default;
			if (typeof(TOUT).IsAssignableFrom(objectToCast.GetType())) {
				result = (TOUT)objectToCast;
				return true;
			}

			return false;
		}
		public override IList GetSuggestions (CharLocation loc) {
			IList sugs = base.GetSuggestions (loc);
			if (sugs != null)
				return sugs;

			//Token tok = currentToken.Length == 0 || currentToken.Type.HasFlag(TokenType.Trivia) ? previousToken : currentToken;
			Token tok = currentToken;

			if (tok.GetTokenType() == XmlTokenType.ElementOpen)
				return new List<string> (allWidgetNames);
			if (tok.GetTokenType() == XmlTokenType.ElementName)
				return allWidgetNames.Where (s => s.StartsWith (tok.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
			if ((tok.Type.HasFlag(TokenType.WhiteSpace) || previousTokHasFlag(TokenType.WhiteSpace)) &&
						tryCast(CurrentNode, out ElementTagSyntax ets)) {
				if (ets.name.HasValue)
					return getAllCrowTypeMembers (ets.Name).ToList();
				return null;
			}
						
			if (CurrentNode is CrowEdit.Xml.AttributeSyntax attribNode) {
				if (CurrentNode.Parent is ElementTagSyntax eltTag) {
					if (!string.IsNullOrEmpty (eltTag.Name)) {
						if (tok.GetTokenType() == XmlTokenType.AttributeName) {
							return getAllCrowTypeMembers (eltTag.Name)
								.Where (s => s.Name.StartsWith (tok.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
						} else if (!string.IsNullOrEmpty (attribNode.Name)) {
							if (tok.GetTokenType() == XmlTokenType.AttributeValue) {
								MemberInfo mi = getCrowTypeMember (
									eltTag.Name, attribNode.Name);
								if (mi is PropertyInfo pi) {
									if (pi.Name == "Style")
										return App.Styling.Keys
											.Where (s => s.StartsWith (tok.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
									if (pi.PropertyType.IsEnum)
										return Enum.GetNames (pi.PropertyType)
											.Where (s => s.StartsWith (tok.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
									if (pi.PropertyType == typeof(bool))
										return  (new string[] {"true", "false"}).
											Where (s => s.StartsWith (tok.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
									if (pi.PropertyType == typeof (Measure))
										return (new string[] {"Stretched", "Fit"}).
											Where (s => s.StartsWith (tok.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
									if (pi.PropertyType == typeof (Fill))
										return  EnumsNET.Enums.GetValues<Colors> ()
											.Where (s => s.ToString().StartsWith (tok.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
								}
							} else if (tok.GetTokenType() == XmlTokenType.AttributeValueOpen) {
								MemberInfo mi = getCrowTypeMember (
									eltTag.Name, attribNode.Name);
								if (mi is PropertyInfo pi) {
									if (pi.Name == "Style")
										return App.Styling.Keys.ToList ();
									if (pi.PropertyType.IsEnum)
										return  Enum.GetNames (pi.PropertyType).ToList ();
									if (pi.PropertyType == typeof(bool))
										return  new List<string> (new string[] {"true", "false"});
									if (pi.PropertyType == typeof (Fill))
										return  EnumsNET.Enums.GetValues<Colors> ().ToList ();
									if (pi.PropertyType == typeof (Measure))
										return  new List<string> (new string[] {"Stretched", "Fit"});
								}
							}
						}
					}
				}
			} /*else if (tok.GetTokenType() != XmlTokenType.AttributeValueClose &&
					tok.GetTokenType() != XmlTokenType.EmptyElementClosing &&
					tok.GetTokenType() != XmlTokenType.ClosingSign &&
					CurrentNode is ElementStartTagSyntax eltStartTag) {
				if (tok.GetTokenType() == XmlTokenType.AttributeName)
					return getAllCrowTypeMembers (eltStartTag.Name)
						.Where (s => s.Name.StartsWith (tok.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
				//else if (tok.Type == TokenType.ElementName)
				//	Suggestions = getAllCrowTypeMembers (eltStartTag.NameToken.Value.AsString (Source)).ToList ();
			} else {
			}*/
			return null;
		}
		public override bool TryGetCompletionForCurrentToken (object suggestion, out TextChange change, out TextSpan? newSelection) {
			return base.TryGetCompletionForCurrentToken (suggestion is MemberInfo mi ? mi.Name : suggestion, out change, out newSelection);
		}

		public override Color GetColorForToken(TokenType tokType)
		{
			switch ((ImlTokenType)tokType) {
				case ImlTokenType.BindingOpen:
				case ImlTokenType.BindingClose:
					return Colors.DarkGreen;
				case ImlTokenType.BindingName: return Colors.RoyalBlue;
				case ImlTokenType.BindingDot: 
				case ImlTokenType.BindingDoubleDot: 
				case ImlTokenType.BindingLevel: 
					return Colors.MediumVioletRed;
				case ImlTokenType.ConstantName:
				case ImlTokenType.ConstantRefOpen:
				case ImlTokenType.ConstantRefClose:
					return Colors.Brown;
			}
			return base.GetColorForToken (tokType);
		}
	}
}