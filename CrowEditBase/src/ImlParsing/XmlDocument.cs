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

namespace CrowEdit.Xml
{
	public static class Extensions {
		public static XmlTokenType GetTokenType (this Token tok) {
			return (XmlTokenType)tok.Type;
		}
		public static void SetTokenType (this Token tok, XmlTokenType type) {
			tok.Type = (TokenType)type;
		}
	}
	public class XmlDocument : SourceDocument {

		public XmlDocument (Interface iFace, string fullPath)
			: base (iFace, fullPath) {
			
		}
		protected override Tokenizer CreateTokenizer() => new XmlTokenizer ();
		protected override SyntaxAnalyser CreateSyntaxAnalyser() => new XmlSyntaxAnalyser (this);

		string[] allWidgetNames = typeof (Widget).Assembly.GetExportedTypes ().Where(t=>typeof(Widget).IsAssignableFrom (t))
					.Select (s => s.Name).ToArray ();


		IEnumerable<MemberInfo> getAllCrowTypeMembers (string crowTypeName) {
			Type crowType = IML.Instantiator.GetWidgetTypeFromName (crowTypeName);
			return crowType.GetMembers (BindingFlags.Public | BindingFlags.Instance).
				Where (m=>((m is PropertyInfo pi && pi.CanWrite) || (m is EventInfo)) &&
						m.GetCustomAttribute<XmlIgnoreAttribute>() == null);
		}
		MemberInfo getCrowTypeMember (string crowTypeName, string memberName) {
			Type crowType = IML.Instantiator.GetWidgetTypeFromName (crowTypeName);			
			return crowType.GetMember (memberName, BindingFlags.Public | BindingFlags.Instance).FirstOrDefault ();
		}
		public override IList GetSuggestions (int pos) {			
			currentToken = FindTokenIncludingPosition (pos);
			currentNode = FindNodeIncludingPosition (pos);
#if DEBUG			
			Console.WriteLine ($"Current Token: {currentToken} Current Node: {currentNode}");
#endif

			if (currentToken.GetTokenType() == XmlTokenType.ElementOpen)
				return new List<string> (allWidgetNames);
			if (currentToken.GetTokenType() == XmlTokenType.ElementName)
				return allWidgetNames.Where (s => s.StartsWith (currentToken.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
			if (currentNode is AttributeSyntax attribNode) {
				if (currentNode.Parent is ElementTagSyntax eltTag) {
					if (eltTag.NameToken.HasValue) {
						if (currentToken.GetTokenType() == XmlTokenType.AttributeName) {
							return getAllCrowTypeMembers (eltTag.NameToken.Value.AsString (Source))
								.Where (s => s.Name.StartsWith (currentToken.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
						} else if (attribNode.NameToken.HasValue) {
							if (currentToken.GetTokenType() == XmlTokenType.AttributeValue) {
								MemberInfo mi = getCrowTypeMember (
									eltTag.NameToken.Value.AsString (Source), attribNode.NameToken.Value.AsString (Source));
								if (mi is PropertyInfo pi) {
									if (pi.Name == "Style")
										return iFace.Styling.Keys
											.Where (s => s.StartsWith (currentToken.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
									if (pi.PropertyType.IsEnum)
										return Enum.GetNames (pi.PropertyType)
											.Where (s => s.StartsWith (currentToken.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
									if (pi.PropertyType == typeof(bool))
										return  (new string[] {"true", "false"}).
											Where (s => s.StartsWith (currentToken.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
									if (pi.PropertyType == typeof (Measure))
										return (new string[] {"Stretched", "Fit"}).
											Where (s => s.StartsWith (currentToken.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
									if (pi.PropertyType == typeof (Fill)) 
										return  FastEnumUtility.FastEnum.GetValues<Colors> ()
											.Where (s => s.ToString().StartsWith (currentToken.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
								}
							} else if (currentToken.GetTokenType() == XmlTokenType.AttributeValueOpen) {
								MemberInfo mi = getCrowTypeMember (
									eltTag.NameToken.Value.AsString (Source), attribNode.NameToken.Value.AsString (Source));
								if (mi is PropertyInfo pi) {
									if (pi.Name == "Style")
										return iFace.Styling.Keys.ToList ();
									if (pi.PropertyType.IsEnum)
										return  Enum.GetNames (pi.PropertyType).ToList ();
									if (pi.PropertyType == typeof(bool))
										return  new List<string> (new string[] {"true", "false"});
									if (pi.PropertyType == typeof (Fill)) 
										return  FastEnumUtility.FastEnum.GetValues<Colors> ().ToList ();
									if (pi.PropertyType == typeof (Measure))
										return  new List<string> (new string[] {"Stretched", "Fit"});
								}
							}
						}
					}
				}			
			} else if (currentToken.GetTokenType() != XmlTokenType.AttributeValueClose && 
					currentToken.GetTokenType() != XmlTokenType.EmptyElementClosing && 
					currentToken.GetTokenType() != XmlTokenType.ClosingSign && 
					currentNode is ElementStartTagSyntax eltStartTag) {
				if (currentToken.GetTokenType() == XmlTokenType.AttributeName)
					return getAllCrowTypeMembers (eltStartTag.NameToken.Value.AsString (Source))
						.Where (s => s.Name.StartsWith (currentToken.AsString (Source), StringComparison.OrdinalIgnoreCase)).ToList ();
				//else if (currentToken.Type == TokenType.ElementName)
				//	Suggestions = getAllCrowTypeMembers (eltStartTag.NameToken.Value.AsString (Source)).ToList ();
			} else {
				/*SyntaxNode curNode = source.FindNodeIncludingPosition (pos);
				Console.WriteLine ($"Current Node: {curNode}");
				if (curNode is ElementStartTagSyntax eltStartTag &&
					(currentToken.Type != TokenType.ClosingSign && currentToken.Type != TokenType.EmptyElementClosing && currentToken.Type != TokenType.Unknown)) {
					Suggestions = getAllCrowTypeMembers (eltStartTag.NameToken.Value.AsString (Source)).ToList ();
				} else*/
				
			}			
			return null;
		}
		public override TextChange? GetCompletionForCurrentToken (object suggestion, out TextSpan? newSelection) {
			newSelection = null;

			string selectedSugg = suggestion is MemberInfo mi ?
				mi.Name : suggestion?.ToString ();
			if (selectedSugg == null)
				return null;

			if (currentToken.GetTokenType() == XmlTokenType.ElementOpen ||
				currentToken.GetTokenType() == XmlTokenType.WhiteSpace ||
				currentToken.GetTokenType() == XmlTokenType.AttributeValueOpen)
				return new TextChange (currentToken.End, 0, selectedSugg);

			if (currentToken.GetTokenType() == XmlTokenType.AttributeName && currentNode is AttributeSyntax attrib) {
					if (attrib.ValueToken.HasValue) {
						TextChange tc = new TextChange (currentToken.Start, currentToken.Length, selectedSugg);						
						newSelection = new TextSpan(
							attrib.ValueToken.Value.Start + tc.CharDiff + 1,
							attrib.ValueToken.Value.End + tc.CharDiff - 1
						);
						return tc;
					} else {
						newSelection = TextSpan.FromStartAndLength (currentToken.Start + selectedSugg.Length + 2);
						return new TextChange (currentToken.Start, currentToken.Length, selectedSugg + "=\"\"");						
					}					
			}

			return new TextChange (currentToken.Start, currentToken.Length, selectedSugg);
		}

		public override Color GetColorForToken(TokenType tokType)
		{
			XmlTokenType xmlTokType = (XmlTokenType)tokType;
			if (xmlTokType.HasFlag (XmlTokenType.Punctuation))
				return Colors.DarkGrey;
			if (xmlTokType.HasFlag (XmlTokenType.Trivia))
				return Colors.DimGrey;
			else if (xmlTokType == XmlTokenType.ElementName) 
				return Colors.Green;
			if (xmlTokType == XmlTokenType.AttributeName) 
				return Colors.Blue;
			if (xmlTokType == XmlTokenType.AttributeValue) 
				return Colors.OrangeRed;
			if (xmlTokType == XmlTokenType.EqualSign) 
				return Colors.Black;
			if (xmlTokType == XmlTokenType.PI_Target) 
				return Colors.DarkSlateBlue;
			return Colors.Red;

		}
	}	
}