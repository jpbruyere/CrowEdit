// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Glfw;
using Crow.Text;
using Crow.Drawing;
using System.Collections;
using CrowEditBase;
using static CrowEditBase.CrowEditBase;
using System.Collections.Generic;
using System.Linq;

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

		const int leftMarginGap = 5;//gap between margin start and numbering
		const int leftMarginRightGap = 3;//gap between items in margin and text
		const int foldSize = 9;//folding rectangles size
		const int foldMargin = 9;

		int leftMargin;
		bool mouseIsInMargin, mouseIsInFoldRect;

		void updateMargin () {
			leftMargin = leftMarginGap;
			if (App.PrintLineNumbers)
				leftMargin += (int)Math.Ceiling((double)lines.Count.ToString().Length * fe.MaxXAdvance) + 6;
			if (App.FoldingEnabled)
				leftMargin += foldMargin;
			leftMargin += leftMarginRightGap;
			//updateVisibleColumns ();
		}
		public override int measureRawSize(LayoutingType lt)
		{
			DbgLogger.StartEvent(DbgEvtType.GOMeasure, this, lt);
			try {
				if ((bool)lines?.IsEmpty)
					getLines ();

				updateMargin ();

				if (!textMeasureIsUpToDate) {
					using (Context gr = new Context (IFace.surf)) {
						setFontForContext (gr);
						measureTextBounds (gr);
					}
				}
				return Margin * 2 + (lt == LayoutingType.Height ? cachedTextSize.Height : cachedTextSize.Width + leftMargin);
			} finally {
				DbgLogger.EndEvent(DbgEvtType.GOMeasure);
			}
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e) {
			hideOverlay ();
			if (mouseIsInMargin) {
				if (e.Button == MouseButton.Left && mouseIsInFoldRect) {
					SyntaxNode curNode = getFoldFromLine (hoverLoc.Value.Line);
					if (curNode != null) {
						curNode.isFolded = !curNode.isFolded;
						textMeasureIsUpToDate = false;
						RegisterForRedraw();
					}
				}

				e.Handled = true;
			} else if (!e.Handled && HasFocus && e.Button == MouseButton.Left)
				visualLine = hoverVisualLine;

			base.onMouseDown (sender, e);
		}
		SyntaxNode getFoldFromLine (int line) {
			if (!(Document is SourceDocument doc))
				return null;
			IEnumerable<SyntaxNode> folds = doc.SyntaxRootNode.FoldableNodes;
			if (folds == null)
				return null;
			return folds.FirstOrDefault (n => n.StartLine == line);
		}
		protected override void mouseMove (MouseEventArgs e) {
			Point mLoc = ScreenPointToLocal (e.Position);
			if (mLoc.X < leftMargin - leftMarginRightGap) {
				mouseIsInMargin = true;
				IFace.MouseCursor = MouseCursor.arrow;
			} else {
				if (mouseIsInFoldRect)
					RegisterForRedraw();
				mouseIsInMargin = mouseIsInFoldRect = false;
				IFace.MouseCursor = MouseCursor.ibeam;
			}

			updateHoverLocation (mLoc);

			if (mouseIsInMargin) {
				double lineHeight = fe.Ascent + fe.Descent;
				Rectangle rFold = new Rectangle (leftMargin - foldMargin - leftMarginRightGap,
					(int)(lineHeight * hoverVisualLine + lineHeight / 2.0 - foldSize / 2.0) - ScrollY, foldSize, foldSize);
				mouseIsInFoldRect = rFold.ContainsOrIsEqual (mLoc);
				RegisterForRedraw();
				return;
			}


			if (HasFocus && IFace.IsDown (MouseButton.Left)) {
				CurrentLoc = hoverLoc;
				autoAdjustScroll = true;
				IFace.forceTextCursor = true;
				RegisterForRedraw ();
			}
		}
		int hoverVisualLine, visualLine;
		protected override void updateHoverLocation (Point mouseLocalPos) {
			hoverVisualLine = getLineIndex (mouseLocalPos);
			int hoverLine = hoverVisualLine + countFoldedLinesUntil (hoverVisualLine);
			NotifyValueChanged("MouseY", mouseLocalPos.Y + ScrollY);
			NotifyValueChanged("ScrollY", ScrollY);
			NotifyValueChanged("VisibleLines", visibleLines);
			NotifyValueChanged("HoverLine", hoverLine);
			if (mouseIsInMargin) {
				if (hoverLoc.HasValue)
					hoverLoc = new CharLocation (hoverLine, hoverLoc.Value.Column, hoverLoc.Value.VisualCharXPosition);
				else
					hoverLoc = new CharLocation (hoverLine, 0, 0);
				return;
			}
			hoverLoc = new CharLocation (hoverLine, -1, mouseLocalPos.X + ScrollX - leftMargin);
			using (Context gr = new Context (IFace.surf)) {
				setFontForContext (gr);
				updateLocation (gr, ClientRectangle.Width, ref hoverLoc);
			}
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
			if (e.Key == Key.F3 &&  Document is SourceDocument doc) {
				doc.SyntaxRootNode?.Dump();
			}

			base.onKeyDown(sender, e);
		}



		protected override int lineCount
		{
			get {
				if (!(Document is SourceDocument doc))
					return base.lineCount;
				return lines.Count - countFoldedLinesUntil (lines.Count);
			}
		}
		protected override int visualCurrentLine => visualLine;
		protected override void updateMaxScrolls (LayoutingType layout) {
			updateMargin();
			Rectangle cb = ClientRectangle;
			cb.Width -= leftMargin;
			if (layout == LayoutingType.Width) {
				MaxScrollX = cachedTextSize.Width - cb.Width;
				NotifyValueChanged ("PageWidth", ClientRectangle.Width);
				if (cachedTextSize.Width > 0)
					NotifyValueChanged ("ChildWidthRatio", Math.Min (1.0, (double)cb.Width / cachedTextSize.Width));
			} else if (layout == LayoutingType.Height) {
				MaxScrollY = cachedTextSize.Height - cb.Height;
				NotifyValueChanged ("PageHeight", ClientRectangle.Height);
				if (cachedTextSize.Height > 0)
					NotifyValueChanged ("ChildHeightRatio", Math.Min (1.0, (double)cb.Height / cachedTextSize.Height));
			}
		}

		int countFoldedLinesUntil (int visualLine) {
			if (!(Document is SourceDocument doc))
				return 0;
			int foldedLines = 0;
			IEnumerator<SyntaxNode> nodeEnum = doc.SyntaxRootNode.FoldableNodes.GetEnumerator ();
			if (!nodeEnum.MoveNext())
				return 0;

			int l = 0;
			while (l < visualLine + foldedLines) {
				if (nodeEnum.Current.StartLine == l) {
					if (nodeEnum.Current.isFolded) {
						foldedLines += nodeEnum.Current.lineCount - 1;
						SyntaxNode nextNode = nodeEnum.Current.NextSiblingOrParentsNextSibling;
						if (nextNode == null || !nodeEnum.MoveNext())
							return foldedLines;

						while (nodeEnum.Current.StartLine < nextNode.StartLine) {
							if (!nodeEnum.MoveNext())
								return foldedLines;
						}

					} else if (!nodeEnum.MoveNext())
						return foldedLines;
				}
				l ++;
			}
			//Console.WriteLine ($"visualLine: {visualLine} foldedLines: {foldedLines}");
			return foldedLines;
		}

		protected override void drawContent (Context gr) {
			if (!(Document is SourceDocument doc)) {
				base.drawContent (gr);
				return;
			}

			doc.EnterReadLock ();
			try {
				if (doc.Tokens == null || doc.Tokens.Length == 0) {
					base.drawContent (gr);
					return;
				}

				setFontForContext (gr);

				if (!textMeasureIsUpToDate) {
					lock (linesMutex)
						measureTextBounds (gr);
				}

				double lineHeight = fe.Ascent + fe.Descent;
				updateMargin ();

				bool printLineNumbers = App.PrintLineNumbers;
				Color marginBG = App.MarginBackground;
				Color marginFG = Colors.Ivory;
				double lineNumWidth = gr.TextExtents (lines.Count.ToString()).Width;

				Rectangle cb = ClientRectangle;
				RectangleD marginRect = new RectangleD (cb.X + ScrollX, cb.Y, leftMargin - leftMarginRightGap, lineHeight);
				cb.Left += leftMargin;


				CharLocation selStart = default, selEnd = default;
				bool selectionNotEmpty = false;

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


				double spacePixelWidth = gr.TextExtents (" ").XAdvance;
				int x = 0;
				double	pixX = cb.Left,
						pixY = cb.Top;

				Foreground.SetAsSource (IFace, gr);
				gr.Translate (-ScrollX, -ScrollY);

				ReadOnlySpan<char> sourceBytes = doc.Source.AsSpan();
				Span<byte> bytes = stackalloc byte[128];
				TextExtents extents;
				int tokPtr = 0;
				Token tok = doc.Tokens[tokPtr];

				ReadOnlySpan<char> buff = sourceBytes;

				SyntaxNode curNode = null;

				IEnumerator<SyntaxNode> nodeEnum = doc.SyntaxRootNode.FoldableNodes.GetEnumerator ();
				bool notEndOfNodes = nodeEnum.MoveNext();

				int l = 0;
				while (l < lines.Count) {
					//if (!cancelLinePrint (lineHeight, lineHeight * y, cb.Height)) {

					bool foldable = false;
					if (notEndOfNodes && nodeEnum.Current.StartLine == l) {
						curNode = nodeEnum.Current;
						notEndOfNodes = nodeEnum.MoveNext();
						if (curNode.isFolded) {
							SyntaxNode nextNode = curNode.NextSiblingOrParentsNextSibling;
							if (nextNode == null)
								notEndOfNodes = false;
							else {
								while (notEndOfNodes && nodeEnum.Current.StartLine < nextNode.StartLine)
									notEndOfNodes = nodeEnum.MoveNext();
							}
						}
						foldable = true;
					}

					//buff = sourceBytes.Slice (lines[l].Start, lines[l].Length);

					while (tok.Start < lines[l].End) {
						buff = sourceBytes.Slice (tok.Start, tok.Length);
						gr.SetSource (doc.GetColorForToken (tok.Type));

						int size = buff.Length * 4 + 1;
						if (bytes.Length < size)
							bytes = size > 512 ? new byte[size] : stackalloc byte[size];

						int encodedBytes = Crow.Text.Encoding.ToUtf8 (buff, bytes);

						if (encodedBytes > 0) {
							bytes[encodedBytes++] = 0;
							gr.TextExtents (bytes.Slice (0, encodedBytes), out extents);
							gr.MoveTo (pixX, pixY + fe.Ascent);
							gr.ShowText (bytes.Slice (0, encodedBytes));
							pixX += extents.XAdvance;
							x += buff.Length;
						}

						if (++tokPtr >= doc.Tokens.Length)
							break;
						tok = doc.Tokens[tokPtr];
					}

					RectangleD lineRect = new RectangleD (cb.X, pixY, pixX - cb.X, lineHeight);
					if (selectionNotEmpty) {
						RectangleD selRect = lineRect;

						if (l >= selStart.Line && l <= selEnd.Line) {
							if (selStart.Line == selEnd.Line) {
								selRect.X += selStart.VisualCharXPosition;
								selRect.Width = selEnd.VisualCharXPosition - selStart.VisualCharXPosition;
							} else if (l == selStart.Line) {
								selRect.X += selStart.VisualCharXPosition;
								selRect.Width -= selStart.VisualCharXPosition - 10.0;
							} else if (l == selEnd.Line)
								selRect.Width = selEnd.VisualCharXPosition - selRect.X + cb.X;
							else
								selRect.Width += 10.0;

							buff = sourceBytes.Slice(lines[l].Start, lines[l].Length);
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

					//Draw line numbering
					if (printLineNumbers){
						marginRect.Y = lineRect.Y;

						string strLN = (l+1).ToString ();
						gr.SetSource (marginBG);
						gr.Rectangle (marginRect);
						gr.Fill();
						gr.SetSource (marginFG);
						gr.MoveTo (marginRect.X + leftMarginGap + lineNumWidth - gr.TextExtents (strLN).Width, marginRect.Y + fe.Ascent);
						gr.ShowText (strLN);
						gr.Fill ();
					}
					//draw fold
					if (foldable) {
						Rectangle rFld = new Rectangle (cb.X - leftMarginGap - foldMargin,
							(int)(marginRect.Y + lineHeight / 2.0 - foldSize / 2.0), foldSize, foldSize);

						gr.Rectangle (rFld);
						if (hoverLoc.HasValue && l == hoverLoc.Value.Line && mouseIsInFoldRect)
							gr.SetSource (Colors.LightBlue);
						else
							gr.SetSource (Colors.White);
						gr.Fill();
						gr.SetSource (Colors.Black);
						gr.Rectangle (rFld, 1.0);
						if (curNode.isFolded) {
							gr.MoveTo (rFld.Center.X + 0.5, rFld.Y + 2);
							gr.LineTo (rFld.Center.X + 0.5, rFld.Bottom - 2);
						}

						gr.MoveTo (rFld.Left + 2, rFld.Center.Y + 0.5);
						gr.LineTo (rFld.Right - 2, rFld.Center.Y + 0.5);
						gr.Stroke ();
					}

					if (++tokPtr >= doc.Tokens.Length)
						break;
					tok = doc.Tokens[tokPtr];

					x = 0;
					pixX = cb.Left;
					pixY += lineHeight;

					if (foldable && curNode.isFolded) {
						TextSpan ns = curNode.Span;
						l = curNode.StartLine + curNode.LineCount;
						while (tok.End <= lines[l].Start) {
							if (++tokPtr >= doc.Tokens.Length)
								break;
							tok = doc.Tokens[tokPtr];
						}
						//tokPtr = doc.FindTokenIndexIncludingPosition (lines[l].Start);
					} else
						l ++;
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
				doc.ExitReadLock ();
			}

		}
		protected override RectangleD? computeTextCursor (Rectangle cursor) {
			Rectangle cb = ClientRectangle;
			cursor -= new Point (ScrollX, ScrollY);
			cursor.X += leftMargin;

			if (autoAdjustScroll) {
				autoAdjustScroll = false;
				int goodMsrs = 0;
				if (cursor.Left < leftMargin)
					ScrollX += cursor.Left - leftMargin;
				else if (cursor.X > cb.Width)
					ScrollX += cursor.X - cb.Width + 5;
				else
					goodMsrs++;

				if (cursor.Y < 0)
					ScrollY += cursor.Y;
				else if (cursor.Bottom > cb.Height)
					ScrollY += cursor.Bottom - cb.Height;
				else
					goodMsrs++;

				if (goodMsrs < 2)
					return null;
			} else if (cursor.Right < 0 || cursor.X > cb.Width || cursor.Y < 0 || cursor.Bottom > cb.Height)
				return null;

			return cursor;
		}

	}
}