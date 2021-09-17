// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Glfw;
using Crow.Text;
using System.Collections.Generic;
using Crow.Drawing;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Collections;
using CrowEditBase;

namespace Crow
{
	public class SourceEditor : Editor {
		object TokenMutex = new object();




		ListBox overlay;
		IList suggestions;
		volatile bool disableSuggestions;
		public IList Suggestions {
			get => suggestions;
			set {
				suggestions = value;
				NotifyValueChangedAuto (suggestions);
				if (suggestions == null || suggestions.Count == 0)
					hideOverlay ();
				else
					showOverlay ();
			}
		}
		bool suggestionsActive => overlay != null && overlay.IsVisible;

		public override void OnTextChanged(object sender, TextChangeEventArgs e)
		{
			if (disableTextChangedEvent)
				return;

			base.OnTextChanged(sender, e);

			if (!disableSuggestions && HasFocus)
				tryGetSuggestions ();

			RegisterForGraphicUpdate();

			//Console.WriteLine ($"{pos}: {suggestionTok.AsString (_text)} {suggestionTok}");
		}



		protected void tryGetSuggestions () {
			if (currentLoc.HasValue && Document is SourceDocument srcDoc)
				Suggestions = srcDoc.GetSuggestions (lines.GetAbsolutePosition (CurrentLoc.Value));
			 else
				Suggestions = null;
		}
		void showOverlay () {
			lock (IFace.UpdateMutex) {
				if (overlay == null) {
					overlay = IFace.LoadIMLFragment<ListBox>(@"
						<ListBox Style='suggestionsListBox' Data='{Suggestions}' UseLoadingThread = 'false'>
							<ItemTemplate>
								<ListItem Height='Fit' Margin='0' Focusable='false' HorizontalAlignment='Left'
												Selected = '{Background=${ControlHighlight}}'
												Unselected = '{Background=Transparent}'>
									<Label Text='{}' HorizontalAlignment='Left' />
								</ListItem>
							</ItemTemplate>
							<ItemTemplate DataType='System.Reflection.MemberInfo'>
								<ListItem Height='Fit' Margin='0' Focusable='false' HorizontalAlignment='Left'
												Selected = '{Background=${ControlHighlight}}'
												Unselected = '{Background=Transparent}'>
									<HorizontalStack>
										<!--<Image Picture='{GetIcon}' Width='16' Height='16'/>-->
										<Label Text='{Name}' HorizontalAlignment='Left' />
									</HorizontalStack>
								</ListItem>
							</ItemTemplate>
							<ItemTemplate DataType='Crow.Colors'>
								<ListItem Height='Fit' Margin='0' Focusable='false' HorizontalAlignment='Left'
												Selected = '{Background=${ControlHighlight}}'
												Unselected = '{Background=Transparent}'>
									<HorizontalStack>
										<Widget Background='{}' Width='20' Height='14'/>
										<Label Text='{}' HorizontalAlignment='Left' />
									</HorizontalStack>
								</ListItem>
							</ItemTemplate>
						</ListBox>
					");
					overlay.DataSource = this;
					overlay.Loaded += (sender, arg) => (sender as ListBox).SelectedIndex = 0;
				} else
					overlay.IsVisible = true;
				overlay.RegisterForLayouting(LayoutingType.Sizing);
			}
		}
		void hideOverlay () {
			if (overlay == null)
				return;
			overlay.IsVisible = false;
		}
		void completeToken () {
			if (Document is SourceDocument srcDoc) {
				TextChange? change = srcDoc.GetCompletionForCurrentToken (overlay.SelectedItem, out TextSpan? nextSelection);
				if (change.HasValue)
					update (change.Value);
				if (nextSelection.HasValue)
					Selection = nextSelection.Value;
			}
			hideOverlay ();
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e) {
			hideOverlay ();
			base.onMouseDown (sender, e);
		}

		public override void onKeyDown(object sender, KeyEventArgs e)
		{
			TextSpan selection = Selection;

			if (SelectionIsEmpty) {
				if (suggestionsActive) {
					switch (e.Key) {
					case Key.Escape:
						hideOverlay ();
						return;
					case Key.Left:
					case Key.Right:
						hideOverlay ();
						break;
					case Key.End:
					case Key.Home:
					case Key.Down:
					case Key.Up:
					case Key.PageDown:
					case Key.PageUp:
						overlay.onKeyDown (this, e);
						return;
					case Key.Tab:
					case Key.Enter:
					case Key.KeypadEnter:
						completeToken ();
						return;
					}
				} else if (e.Key == Key.Space && IFace.Ctrl) {
					tryGetSuggestions ();
					return;
				}
			} else if (e.Key == Key.Tab && !selection.IsEmpty) {
				int lineStart = lines.GetLocation (selection.Start).Line;
				CharLocation locEnd = lines.GetLocation (selection.End);
				int lineEnd = locEnd.Column == 0 ? Math.Max (0, locEnd.Line - 1) : locEnd.Line;

				disableSuggestions = true;

				if (IFace.Shift) {
					for (int l = lineStart; l <= lineEnd; l++) {
						if (_text[lines[l].Start] == '\t')
							update (new TextChange (lines[l].Start, 1, ""));
						else if (Char.IsWhiteSpace (_text[lines[l].Start])) {
							int i = 1;
							while (i < lines[l].Length && i < Interface.TAB_SIZE && Char.IsWhiteSpace (_text[i]))
								i++;
							update (new TextChange (lines[l].Start, i, ""));
						}
					}

				}else{
					for (int l = lineStart; l <= lineEnd; l++)
						update (new TextChange (lines[l].Start, 0, "\t"));
				}

				selectionStart = new CharLocation (lineStart, 0);
				CurrentLoc = new CharLocation (lineEnd, lines[lineEnd].Length);

				disableSuggestions = false;

				return;
			}
			base.onKeyDown(sender, e);
		}

		protected override void drawContent (Context gr) {
			if (!(Document is SourceDocument xmlDoc)) {
				base.drawContent (gr);
				return;
			}
			//lock(TokenMutex) {
			xmlDoc.EnterReadLock ();
			try {
				if (xmlDoc.Tokens == null || xmlDoc.Tokens.Length == 0) {
					base.drawContent (gr);
					return;
				}

				Rectangle cb = ClientRectangle;
				fe = gr.FontExtents;
				double lineHeight = fe.Ascent + fe.Descent;

				CharLocation selStart = default, selEnd = default;
				bool selectionNotEmpty = false;

				if (HasFocus) {
					if (currentLoc?.Column < 0) {
						updateLocation (gr, cb.Width, ref currentLoc);
						NotifyValueChanged ("CurrentColumn", CurrentColumn);
					} else
						updateLocation (gr, cb.Width, ref currentLoc);

					if (overlay != null && overlay.IsVisible) {
						Point p = new Point((int)currentLoc.Value.VisualCharXPosition - ScrollX, (int)(lineHeight * (currentLoc.Value.Line + 1) - ScrollY));
						if (p.Y < 0 || p.X < 0)
							hideOverlay ();
						else {
							p += ScreenCoordinates (Slot).TopLeft;
							overlay.Left = p.X;
							overlay.Top = p.Y;
						}
					}
					if (selectionStart.HasValue) {
						updateLocation (gr, cb.Width, ref selectionStart);
						if (CurrentLoc.Value != selectionStart.Value)
							selectionNotEmpty = true;
					}
					if (selectionNotEmpty) {
						if (CurrentLoc.Value.Line < selectionStart.Value.Line) {
							selStart = CurrentLoc.Value;
							selEnd = selectionStart.Value;
						} else if (CurrentLoc.Value.Line > selectionStart.Value.Line) {
							selStart = selectionStart.Value;
							selEnd = CurrentLoc.Value;
						} else if (CurrentLoc.Value.Column < selectionStart.Value.Column) {
							selStart = CurrentLoc.Value;
							selEnd = selectionStart.Value;
						} else {
							selStart = selectionStart.Value;
							selEnd = CurrentLoc.Value;
						}
					} else
						IFace.forceTextCursor = true;
				}

				double spacePixelWidth = gr.TextExtents (" ").XAdvance;
				int x = 0, y = 0;
				double pixX = cb.Left;


				Foreground.SetAsSource (IFace, gr);
				gr.Translate (-ScrollX, -ScrollY);


				ReadOnlySpan<char> sourceBytes = xmlDoc.Source.AsSpan();
				Span<byte> bytes = stackalloc byte[128];
				TextExtents extents;
				int tokPtr = 0;
				Token tok = xmlDoc.Tokens[tokPtr];
				bool multilineToken = false;

				ReadOnlySpan<char> buff = sourceBytes;


				for (int i = 0; i < lines.Count; i++) {
					//if (!cancelLinePrint (lineHeight, lineHeight * y, cb.Height)) {

					if (multilineToken) {
						if (tok.End < lines[i].End) {//last incomplete line of multiline token
							buff = sourceBytes.Slice (lines[i].Start, tok.End - lines[i].Start);
						} else {//print full line
							buff = sourceBytes.Slice (lines[i].Start, lines[i].Length);
						}
					}

					while (tok.Start < lines[i].End) {
						if (!multilineToken) {
							if (tok.End > lines[i].End) {//first line of multiline
								multilineToken = true;
								buff = sourceBytes.Slice (tok.Start, lines[i].End - tok.Start);
							} else
								buff = sourceBytes.Slice (tok.Start, tok.Length);

							gr.SetSource(xmlDoc.GetColorForToken (tok.Type));
						}

						int size = buff.Length * 4 + 1;
						if (bytes.Length < size)
							bytes = size > 512 ? new byte[size] : stackalloc byte[size];

						int encodedBytes = Crow.Text.Encoding.ToUtf8 (buff, bytes);

						if (encodedBytes > 0) {
							bytes[encodedBytes++] = 0;
							gr.TextExtents (bytes.Slice (0, encodedBytes), out extents);
							gr.MoveTo (pixX, lineHeight * y + fe.Ascent);
							gr.ShowText (bytes.Slice (0, encodedBytes));
							pixX += extents.XAdvance;
							x += buff.Length;
						}

						if (multilineToken) {
							if (tok.End < lines[i].End)//last incomplete line of multiline token
								multilineToken = false;
							else
								break;
						}

						if (++tokPtr >= xmlDoc.Tokens.Length)
							break;
						tok = xmlDoc.Tokens[tokPtr];
					}

					if (HasFocus && selectionNotEmpty) {
						RectangleD lineRect = new RectangleD (cb.X,	lineHeight * y + cb.Top, pixX, lineHeight);
						RectangleD selRect = lineRect;

						if (i >= selStart.Line && i <= selEnd.Line) {
							if (selStart.Line == selEnd.Line) {
								selRect.X = selStart.VisualCharXPosition + cb.X;
								selRect.Width = selEnd.VisualCharXPosition - selStart.VisualCharXPosition;
							} else if (i == selStart.Line) {
								double newX = selStart.VisualCharXPosition + cb.X;
								selRect.Width -= (newX - selRect.X) - 10.0;
								selRect.X = newX;
							} else if (i == selEnd.Line)
								selRect.Width = selEnd.VisualCharXPosition - selRect.X + cb.X;
							else
								selRect.Width += 10.0;

							buff = sourceBytes.Slice(lines[i].Start, lines[i].Length);
							int size = buff.Length * 4 + 1;
							if (bytes.Length < size)
								bytes = size > 512 ? new byte[size] : stackalloc byte[size];

							int encodedBytes = Crow.Text.Encoding.ToUtf8 (buff, bytes);

							gr.SetSource (SelectionBackground);
							gr.Rectangle (selRect);
							if (encodedBytes < 0)
								gr.Fill ();
							else {
								gr.FillPreserve ();
								gr.Save ();
								gr.Clip ();
								gr.SetSource (SelectionForeground);
								gr.MoveTo (lineRect.X, lineRect.Y + fe.Ascent);
								gr.ShowText (bytes.Slice (0, encodedBytes));
								gr.Restore ();
							}
							Foreground.SetAsSource (IFace, gr);
						}
					}

					if (!multilineToken) {
						if (++tokPtr >= xmlDoc.Tokens.Length)
							break;
						tok = xmlDoc.Tokens[tokPtr];
					}

					x = 0;
					pixX = 0;

					y++;


						/*	} else if (tok2.Type == TokenType.Tabulation) {
								int spaceRounding = x % tabSize;
								int spaces = spaceRounding == 0 ?
									tabSize * tok2.Length :
									spaceRounding + tabSize * (tok2.Length - 1);
								x += spaces;
								pixX += spacePixelWidth * spaces;
								continue;
							} else if (tok2.Type == TokenType.WhiteSpace) {
								x += tok2.Length;
								pixX += spacePixelWidth * tok2.Length;*/
				}
				//gr.Translate (ScrollX, ScrollY);
			} finally {
				xmlDoc.ExitReadLock ();
			}

		}
	}
}