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
		SyntaxNode currentNode;
#if DEBUG_NODE
		SyntaxNode _hoverNode;
		SyntaxNode hoverNode {
			get =>_hoverNode;
			set {
				if (_hoverNode == value)
					return;
				_hoverNode = value;
				RegisterForRedraw ();
			}
		}
#endif
		public SyntaxNode CurrentNode {
			get => currentNode;
			set {
				if (currentNode == value)
					return;
				currentNode = value;
				NotifyValueChanged ("CurrentNode", currentNode);
				RegisterForRedraw ();
			}
		}

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

			if (Document is SourceDocument srcdoc)
				srcdoc.updateCurrentTokAndNode (lines.GetAbsolutePosition(CurrentLoc.Value));

			if (!disableSuggestions && HasFocus)
				tryGetSuggestions ();

			RegisterForGraphicUpdate();

			lock (IFace.UpdateMutex) {
				if (Document is SourceDocument doc) {
					doc.NotifyValueChanged ("SyntaxRootChildNodes", (object)null);
					doc.NotifyValueChanged ("SyntaxRootChildNodes", doc.SyntaxRootChildNodes);
					CurrentNode?.ExpandToTheTop();
				}
			}
			//Console.WriteLine ($"{pos}: {suggestionTok.AsString (_text)} {suggestionTok}");
		}

		protected void tryGetSuggestions () {
			if (currentLoc.HasValue && Document is SourceDocument srcDoc) {
				IList suggs = srcDoc.GetSuggestions (lines.GetAbsolutePosition (CurrentLoc.Value));
				if (suggs != null && suggs.Count == 1 && (
					(suggs[0] is System.Reflection.MemberInfo mi && mi.Name == srcDoc.CurrentTokenString) ||
					(suggs[0].ToString() == srcDoc.CurrentTokenString)
				)){
					Suggestions = null;
				}else
					Suggestions = suggs;
			} else
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
				if (srcDoc.TryGetCompletionForCurrentToken (overlay.SelectedItem, out TextChange change, out TextSpan? nextSelection)) {
					update (change);
					if (nextSelection.HasValue) {
						Selection = nextSelection.Value;
					}
				}
			}
			hideOverlay ();
			tryGetSuggestions ();
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

		protected override CharLocation? CurrentLoc {
			get => currentLoc;
			set {
				if (currentLoc == value)
					return;
				currentLoc = value;
				if (currentLoc.HasValue) {
					SyntaxNode fold = getFoldContainingLine (currentLoc.Value.Line);
					while (fold != null && fold.StartLine == currentLoc.Value.Line)
						fold = fold.Parent;
					fold?.UnfoldToTheTop();
					if (Document is SourceDocument doc)
						doc.updateCurrentTokAndNode (lines.GetAbsolutePosition(currentLoc.Value));
				}
				NotifyValueChanged ("CurrentLine", CurrentLine);
				NotifyValueChanged ("CurrentColumn", CurrentColumn);
				CMDCopy.CanExecute = CMDCut.CanExecute = !SelectionIsEmpty;
			}
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
					SyntaxNode curNode = getFoldStartingAt (hoverLoc.Value.Line);
					if (curNode != null) {
						curNode.isFolded = !curNode.isFolded;
						textMeasureIsUpToDate = false;
						RegisterForRedraw();
					}
				}

				e.Handled = true;
			}

			base.onMouseDown (sender, e);
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
				Rectangle rFold = new Rectangle (leftMargin - foldMargin - leftMarginRightGap,
					(int)(lineHeight * getVisualLine(hoverLoc.Value.Line) + lineHeight / 2.0 - foldSize / 2.0) - ScrollY, foldSize, foldSize);
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
		protected override void updateHoverLocation (Point mouseLocalPos) {
			int hoverVisualLine = getLineIndexFromMousePosition (mouseLocalPos);
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
#if DEBUG_NODES
			if (Document is SourceDocument doc) {
				hoverNode = doc.FindNodeIncludingPosition (lines.GetAbsolutePosition (hoverLoc.Value));
			}
#endif
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
				} else if (e.Key == Key.Space && e.Modifiers.HasFlag (Modifier.Control)) {
					tryGetSuggestions ();
					return;
				}
			} else if (e.Key == Key.Tab && !selection.IsEmpty) {
				int lineStart = lines.GetLocation (selection.Start).Line;
				CharLocation locEnd = lines.GetLocation (selection.End);
				int lineEnd = locEnd.Column == 0 ? Math.Max (0, locEnd.Line - 1) : locEnd.Line;

				disableSuggestions = true;

				if ( e.Modifiers == Modifier.Shift) {
					for (int l = lineStart; l <= lineEnd; l++) {
						if (_text[lines[l].Start] == '\t')
							update (new TextChange (lines[l].Start, 1, ""));
						else if (Char.IsWhiteSpace (_text[lines[l].Start])) {
							int i = 1;
							while (i < lines[l].Length && i < App.TabulationSize && Char.IsWhiteSpace (_text[i]))
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
			if (Document is SourceDocument doc) {
				switch (e.Key) {
					case Key.F3:
						doc.SyntaxRootNode?.Dump();
						break;
					case Key.Enter:
					case Key.KeypadEnter:
						//doc.updateCurrentTokAndNode (Selection.Start);
						Console.WriteLine ($"*** Current Token: {doc.CurrentToken} Current Node: {doc.CurrentNode}");
						if (string.IsNullOrEmpty (LineBreak))
							detectLineBreak ();
						update (new TextChange (selection.Start, selection.Length, LineBreak));
						autoAdjustScroll = true;
						IFace.forceTextCursor = true;
						e.Handled = true;
						return;
				}
			}

			base.onKeyDown(sender, e);
		}


		SyntaxNode getFoldStartingAt (int line) {
			if (!(Document is SourceDocument doc))
				return null;
			IEnumerable<SyntaxNode> folds = doc.SyntaxRootNode.FoldableNodes;
			if (folds == null)
				return null;
			return folds.FirstOrDefault (n => n.StartLine == line);
		}
		SyntaxNode getFoldContainingLine (int line) {
			if (!(Document is SourceDocument doc))
				return null;
			doc.EnterReadLock();
			try {
				IEnumerable<SyntaxNode> folds = doc.SyntaxRootNode.FoldableNodes;
				if (folds == null)
					return null;
				return folds.LastOrDefault (n => n.StartLine <= line && n.EndLine >= line);
			} finally {
				doc.ExitReadLock ();
			}
		}

		int getVisualLine (int absoluteLine) {
			if (!(Document is SourceDocument doc))
				return absoluteLine;
			doc.EnterReadLock();
			try {
				int foldedLines = 0;
				IEnumerator<SyntaxNode> foldsEnum = doc.SyntaxRootNode.FoldableNodes.GetEnumerator();
				bool notEndOfFolds = foldsEnum.MoveNext();
				while (notEndOfFolds && foldsEnum.Current.StartLine < absoluteLine) {
					if (foldsEnum.Current.isFolded) {
						foldedLines += foldsEnum.Current.LineCount - 1;
						SyntaxNode nextNode = foldsEnum.Current.NextSiblingOrParentsNextSibling;
						if (nextNode == null)
							break;
						notEndOfFolds = foldsEnum.MoveNext();
						while (notEndOfFolds && foldsEnum.Current.StartLine < nextNode.StartLine)
							notEndOfFolds = foldsEnum.MoveNext();
					} else
						notEndOfFolds = foldsEnum.MoveNext();
				}
				return absoluteLine - foldedLines;
			} finally {
				doc.ExitReadLock ();
			}
		}
		int countFoldedLinesUntil (int visualLine) {
			if (!(Document is SourceDocument doc))
				return 0;
			doc.EnterReadLock();
			try {
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
			} finally {
				doc.ExitReadLock ();
			}
		}

		protected override int getAbsoluteLineIndexFromVisualLineMove (int startLine, int visualLineDiff) {
			int newVl = Math.Min (Math.Max (0, getVisualLine (startLine) + visualLineDiff), visualLineCount - 1);
			return newVl + countFoldedLinesUntil (newVl);
		}

		protected override int visualLineCount
		{
			get {
				if (!(Document is SourceDocument doc))
					return base.visualLineCount;
				return lines.Count - countFoldedLinesUntil (lines.Count);
			}
		}
		protected override int visualCurrentLine => CurrentLoc.HasValue ? getVisualLine (CurrentLoc.Value.Line) : 0;
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


		protected virtual void fillHighlight (Context gr, int l, CharLocation selStart, CharLocation selEnd, RectangleD selRect, Color color) {
			if (selStart.Line == selEnd.Line) {
				selRect.X += selStart.VisualCharXPosition;
				selRect.Width = selEnd.VisualCharXPosition - selStart.VisualCharXPosition;
			} else if (l == selStart.Line) {
				selRect.X += selStart.VisualCharXPosition;
				selRect.Width -= selStart.VisualCharXPosition - 10.0;
			} else if (l == selEnd.Line)
				//selRect.Width = selEnd.VisualCharXPosition - selRect.X;// + cb.X;
				selRect.Width = selEnd.VisualCharXPosition;
			else
				selRect.Width += 10.0;

			gr.Operator = Operator.DestOver;

			gr.SetSource (color);
			gr.Rectangle (selRect);
			gr.Fill ();
			Foreground.SetAsSource (IFace, gr);

			gr.Operator = Operator.Over;
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
				CharLocation? nodeStart = null, nodeEnd = null;

				CharLocation? editNodeStart = null, editNodeEnd = null;//debug
				CharLocation? hoverNodeStart = null, hoverNodeEnd = null;

				if (currentLoc?.Column < 0) {
					updateLocation (gr, cb.Width, ref currentLoc);
					NotifyValueChanged ("CurrentColumn", CurrentColumn);
				} else
					updateLocation (gr, cb.Width, ref currentLoc);

				if (CurrentNode != null) {
					TextSpan nodeSpan = CurrentNode.Span;
					nodeStart = lines.GetLocation  (nodeSpan.Start);
					updateLocation (gr, cb.Width, ref nodeStart);
					nodeEnd = lines.GetLocation  (nodeSpan.End);
					updateLocation (gr, cb.Width, ref nodeEnd);
				}
#if DEBUG_NODES
				if (doc.EditedNode != null) {
					TextSpan nodeSpan = doc.EditedNode.Span;
					editNodeStart = lines.GetLocation  (nodeSpan.Start);
					updateLocation (gr, cb.Width, ref editNodeStart);
					editNodeEnd = lines.GetLocation  (nodeSpan.End);
					updateLocation (gr, cb.Width, ref editNodeEnd);
				}
				if (hoverNode != null) {
					TextSpan nodeSpan = hoverNode.Span;
					hoverNodeStart = lines.GetLocation  (nodeSpan.Start);
					updateLocation (gr, cb.Width, ref hoverNodeStart);
					hoverNodeEnd = lines.GetLocation  (nodeSpan.End);
					updateLocation (gr, cb.Width, ref hoverNodeEnd);
				}
#endif

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
					if (CurrentNode != null && l >= nodeStart.Value.Line && l <= nodeEnd.Value.Line)
						fillHighlight (gr, l, nodeStart.Value, nodeEnd.Value, lineRect, new Color(0.0,0.1,0.0,0.1));;
#if DEBUG_NODES
					if (doc.EditedNode != null && l >= editNodeStart.Value.Line && l <= editNodeEnd.Value.Line)
						fillHighlight (gr, l, editNodeStart.Value, editNodeEnd.Value, lineRect, new Color(0,0.5,0,0.2));;
					if (hoverNode != null && l >= hoverNodeStart.Value.Line && l <= hoverNodeEnd.Value.Line)
						fillHighlight (gr, l, hoverNodeStart.Value, hoverNodeEnd.Value, lineRect, new Color(0,0,0.8,0.1));;
#endif
					if (selectionNotEmpty && l >= selStart.Line && l <= selEnd.Line)
						fillHighlight (gr, l, selStart, selEnd, lineRect, SelectionBackground);


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