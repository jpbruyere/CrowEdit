// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Glfw;
using Crow;
using Crow.Text;
using Drawing2D;
using System.Collections;
using static CrowEditBase.CrowEditBase;
using System.Collections.Generic;
using System.Linq;
using System.Text.Unicode;
using System.Text;
using System.Threading.Tasks;

namespace CrowEditBase
{
	public class SourceEditor : Editor {
		SourceDocument sourceDocument;
        public override TextDocument Document {
			 get => base.Document;
			 set {
				base.Document = value;
				sourceDocument = Document as SourceDocument;
			 }
		}
        int currentTokenIndex = -1;
		SyntaxNode currentNode;
/*#if DEBUG_NODE
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
#endif*/
		public SyntaxNode CurrentNode {
			get => currentNode;
			set {
				if (currentNode == value)
					return;
				if (currentNode != null)
					currentNode.IsSelected = false;
				currentNode = value;
				if (currentNode != null) {
					currentNode.IsSelected = true;
					if (currentNode.Parent is SyntaxNode sn)
						sn.isExpanded = true;
				}
					
				NotifyValueChanged ("CurrentNode", currentNode);
			}
		}
		
		bool parsingOk => sourceDocument != null && sourceDocument.IsParsed;

		public Token CurrentToken => parsingOk ? sourceDocument.GetTokenByIndex(currentTokenIndex) : default;
#if DEBUG
		public string CurrentTokenString {
			get {
				try
				{
					return parsingOk ? CurrentToken.AsString(Document.source) : null;					
				}
				catch (System.Exception e)
				{
					Console.WriteLine($"{CurrentToken}:{e}");
				}
				return null;
			}
			 
		}
		public string CurrentTokenType {
			get {
				try
				{
					return parsingOk && CurrentToken != null ? 
						sourceDocument.GetTokenTypeString(CurrentToken.Type) : default;
				}
				catch (System.Exception e)
				{
					Console.WriteLine($"{CurrentToken}:{e}");
				}
				return default;
			}
			 
		}		
		public int AbsoluteCharLoc {
			get {
				try
				{
					return currentLoc.HasValue ?
						Document.GetAbsolutePosition(currentLoc.Value) : 0;
				}
				catch (System.Exception e)
				{
					Console.WriteLine (e);
				}
				return 0;
			}
		}
#endif

		#region suggestions and autocomplete
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

		protected void tryGetSuggestions () {
			if (currentLoc.HasValue && Document is SourceDocument srcDoc && srcDoc.IsParsed) {
				int pos = srcDoc.GetAbsolutePosition(CurrentLoc.Value);
				if (pos > 0) {
					SyntaxNode node = srcDoc.FindNodeIncludingPosition(pos-1);
					if (node != null) {
						var tmp = srcDoc.GetSuggestions (pos,
									srcDoc.FindTokenIndexIncludingPosition(pos -1), node, CurrentLoc.Value);
						if (!(tmp?.Count == 1 && tmp[0].TryCast(out Suggestion sug) && sug.Change.HasNoEffect(srcDoc.source)))
							Suggestions = tmp;
						return;
					}
				}
			}
			Suggestions = null;
		}
		void showOverlay () {
			lock (IFace.UpdateMutex) {
				if (overlay == null) {
					//overlay = IFace.Load<ListBox>(@"#ui.SuggestionsOverlay.crow");
					overlay = IFace.LoadIMLFragment<ListBox>(@"
							<ListBox Style='suggestionsListBox' Data='{Suggestions}' UseLoadingThread = 'false'>
								<ItemTemplate>
									<ListItem Height='Fit' Margin='2' Focusable='false' HorizontalAlignment='Left' Width='Stretched'
																	Selected = '{Background=${ControlHighlight}}'
																	Unselected = '{Background=Transparent}'>
										<HorizontalStack Width='Stretched' Spacing='6'>
											<Image Path='{Icon}' Width='12' Height='12'/>
											<Label Text='{Caption}' HorizontalAlignment='Left' Width='Stretched'/>
										</HorizontalStack>
										<!--<Label Text='{Caption}' HorizontalAlignment='Left' Width='Stretched'/>-->
									</ListItem>
								</ItemTemplate>
								<ItemTemplate DataType='CECrowPlugin.ColorSuggestion'>
									<ListItem Height='Fit' Margin='0' Focusable='false' HorizontalAlignment='Left' Width='Stretched'
																	Selected = '{Background=${ControlHighlight}}'
																	Unselected = '{Background=Transparent}'>
										<HorizontalStack Width='Stretched' >
											<Image Margin='6' Path='{Icon}' Width='32' Height='24' Background='{Fill}' CornerRadius='2'/>
											<Label Text='{Caption}' HorizontalAlignment='Left' Width='Stretched'/>
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
			lock(App.UpdateMutex)
				overlay.IsVisible = false;
		}
		void completeToken () {
			disableSuggestions = true;
			if (Document is SourceDocument srcDoc) {
				if (overlay.SelectedItem is Suggestion sug) {	
					update (sug.Change);
					if (sug.NextSelection.HasValue) 
						Selection = sug.NextSelection.Value;
				}
			}
			hideOverlay ();
			disableSuggestions = false;
			//tryGetSuggestions ();
		}
		#endregion
		
		#region  Margin
		const int leftMarginGap = 5;//gap between margin start and numbering
		const int leftMarginRightGap = 3;//gap between items in margin and text
		const int foldSize = 9;//folding rectangles size
		const int foldMargin = 9;
		int leftMargin;
		bool mouseIsInMargin, mouseIsInFoldRect;

		void updateMargin () {
			leftMargin = leftMarginGap;
			if (App.PrintLineNumbers)
				leftMargin += (int)Math.Ceiling((double)(Document == null ? 1 : Document.LinesCount.ToString().Length) * fe.MaxXAdvance) + 6;
			if (App.FoldingEnabled)
				leftMargin += foldMargin;
			leftMargin += leftMarginRightGap;
			//updateVisibleColumns ();
		}
		#endregion

		protected override CharLocation? CurrentLoc {
			get => currentLoc;
			set {
				if (currentLoc == value)
					return;
				currentLoc = value;
				if (currentLoc.HasValue && Document is SourceDocument doc && doc.IsParsed) {
					MultiNodeSyntax fold = getFoldContainingLine (currentLoc.Value.Line);
					while (fold != null && fold.StartLocation.Line == currentLoc.Value.Line)
						fold = fold.Parent;
					fold?.UnfoldToTheTop();
					updateCurrentTokAndNode();
				}
				NotifyValueChanged ("CurrentLine", CurrentLine);
				NotifyValueChanged ("CurrentColumn", CurrentColumn);
				NotifyValueChanged ("TabulatedColumn", TabulatedColumn);
				CMDCopy.CanExecute = CMDCut.CanExecute = !SelectionIsEmpty;
			}
		}
		
		public override int measureRawSize(LayoutingType lt)
		{
			DbgLogger.StartEvent(DbgEvtType.GOMeasure, this, lt);
			try {
				updateMargin ();

				if (!textMeasureIsUpToDate) {
					using (IContext gr = IFace.Backend.CreateContext (IFace.MainSurface)) {
						gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
						gr.SetFontSize (Font.Size);

						measureTextBounds (gr);
					}
				}
				return Margin * 2 + (lt == LayoutingType.Height ? cachedTextSize.Height : cachedTextSize.Width + leftMargin);
			} finally {
				DbgLogger.EndEvent(DbgEvtType.GOMeasure);
			}
		}
		
		//Task<int> taskFirstVisibleLineIndex;

		#region Mouse & Keyboard overrides
		public override void onMouseDown (object sender, MouseButtonEventArgs e) {
			hideOverlay ();
			if (parsingOk) {
				if (mouseIsInMargin) {
					if (e.Button == MouseButton.Left && mouseIsInFoldRect) {
						MultiNodeSyntax curNode = getFoldStartingAt (hoverLoc.Value.Line);
						if (curNode != null) {
							curNode.isFolded = !curNode.isFolded;
							textMeasureIsUpToDate = false;
							RegisterForRedraw();
						}
					}

					e.Handled = true;
				}
			}
			base.onMouseDown (sender, e);
		}
		protected override void mouseMove (MouseEventArgs e) {
			if (!parsingOk) {
				base.mouseMove(e);
				return;
			}
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

			//int hoverVisualLine = getLineIndexFromMousePositionUnchecked (mLoc);
			int hoverVisualLine = getLineIndexFromMousePosition (mLoc);
			int hoverLine = getAbsoluteLineFromVisualLine (hoverVisualLine);

			/***************************/
				/*hoverLoc = new CharLocation (hoverLine, -1, mLoc.X + ScrollX - leftMargin);
				using (IContext gr = IFace.Backend.CreateContext (IFace.MainSurface)) {
					gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
					gr.SetFontSize (Font.Size);
					updateLocation (gr, ref hoverLoc);
				}*/
			/***************************/

			NotifyValueChanged("MouseY", mLoc.Y + ScrollY);
			NotifyValueChanged("hoverVisualLine", hoverVisualLine);
			NotifyValueChanged("hoverLine", hoverLine);

			
			if (mouseIsInMargin) {
				if (hoverLoc.HasValue)
					hoverLoc = new CharLocation (hoverLine, hoverLoc.Value.Column, hoverLoc.Value.VisualCharXPosition);
				else
					hoverLoc = new CharLocation (hoverLine, 0, 0);
			} else {
				hoverLoc = new CharLocation (hoverLine, -1, mLoc.X + ScrollX - leftMargin);
				using (IContext gr = IFace.Backend.CreateContext (IFace.MainSurface)) {
					gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
					gr.SetFontSize (Font.Size);
					updateLocation (gr, ref hoverLoc);
				}
			}

			if (mouseIsInMargin) {
				Rectangle rFold = new Rectangle (leftMargin - foldMargin - leftMarginRightGap,
					(int)(lineHeight * hoverVisualLine + lineHeight / 2.0 - foldSize / 2.0) - ScrollY, foldSize, foldSize);
				rFold.Inflate(2);
				mouseIsInFoldRect = rFold.ContainsOrIsEqual (mLoc);
				RegisterForRedraw();
				return;
			}


			if (HasFocus && IFace.IsDown (MouseButton.Left)) {
				CurrentLoc = hoverLoc;
				autoAdjustScroll = true;
				IFace.forceTextCursor();
				RegisterForRedraw ();
			}
		}
		protected override void updateHoverLocation (Point mouseLocalPos) {
			int hoverVisualLine = getLineIndexFromMousePositionUnchecked (mouseLocalPos);
			int hoverLine = getAbsoluteLineFromVisualLine (hoverVisualLine);
			
			if (mouseIsInMargin) {
				if (hoverLoc.HasValue)
					hoverLoc = new CharLocation (hoverLine, hoverLoc.Value.Column, hoverLoc.Value.VisualCharXPosition);
				else
					hoverLoc = new CharLocation (hoverLine, 0, 0);
				return;
			}
			hoverLoc = new CharLocation (hoverLine, -1, mouseLocalPos.X + ScrollX - leftMargin);
			using (IContext gr = IFace.Backend.CreateContext (IFace.MainSurface)) {
				gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
				gr.SetFontSize (Font.Size);
				updateLocation (gr, ref hoverLoc);
			}
/*#if DEBUG_NODES
			if (Document is SourceDocument doc) {
				hoverNode = doc.FindNodeIncludingPosition (lines.GetAbsolutePosition (hoverLoc.Value));
			}
#endif*/
		}
		public override void onKeyDown(object sender, KeyEventArgs e)
		{
			TextSpan selection = Selection;

			/*Document.EnterReadLock();
			try {*/
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
					int lineStart =  Document.GetLocation (selection.Start).Line;
					CharLocation locEnd = Document.GetLocation (selection.End);
					int lineEnd = locEnd.Column == 0 ? Math.Max (0, locEnd.Line - 1) : locEnd.Line;

					disableSuggestions = true;

					if ( e.Modifiers == Modifier.Shift) {
						for (int l = lineStart; l <= lineEnd; l++) {
							TextLine li = Document.GetLine (l);
							if (Document.GetChar (li.Start) == '\t')
								update (new TextChange (li.Start, 1, ""));
							else if (Char.IsWhiteSpace (Document.GetChar (li.Start))) {
								int i = 1;
								while (i < li.Length && i < App.TabulationSize && Char.IsWhiteSpace (Document.GetChar  (i)))
									i++;
								update (new TextChange (li.Start, i, ""));
							}
						}

					}else{
						for (int l = lineStart; l <= lineEnd; l++)
							update (new TextChange (Document.GetLine (l).Start, 0, "\t"));
					}

					selectionStart = new CharLocation (lineStart, 0);
					CurrentLoc = new CharLocation (lineEnd, Document.GetLine (lineEnd).Length);

					disableSuggestions = false;

					return;
				}
				if (Document is SourceDocument doc && doc.IsParsed) {
					switch (e.Key) {
						/*case Key.F3:
							doc.Root?.Dump();
							break;*/
						case Key.Enter:
						case Key.KeypadEnter:
							//doc.updateCurrentTokAndNode (Selection.Start);
							//Console.WriteLine ($"*** Current Token: {doc.CurrentToken} Current Node: {doc.CurrentNode}");
							if (currentLoc.HasValue) {
								TextLine tl = doc.GetLine(currentLoc.Value.Line);
								int firstTok = doc.FindTokenIndexIncludingPosition(tl.Start);
								int i = firstTok;
								Token tok = doc.GetTokenByIndex(i);
								StringBuilder sb = new StringBuilder(20);
								while (tok.End < tl.End && tok.Type.HasFlag(TokenType.WhiteSpace)) {
									if (tok.Type == TokenType.Tabulation) 
										sb.Append(new string('\t', tok.Length));
									else if (tok.Type == TokenType.WhiteSpace) 
										sb.Append(new string(' ', tok.Length));
									else
										break;
									tok = doc.GetTokenByIndex(++i);
								}
								update (new TextChange (selection.Start, selection.Length, Document.GetLineBreak () + sb.ToString()));
							} else
								update (new TextChange (selection.Start, selection.Length, Document.GetLineBreak ()));
							autoAdjustScroll = true;
							IFace.forceTextCursor();
							e.Handled = true;
							return;
					}
				}

				base.onKeyDown(sender, e);
			/*} finally {
				Document.ExitReadLock ();
			}*/
		}
		#endregion

		MultiNodeSyntax getFoldStartingAt (int line) {
			if (!(Document is SourceDocument doc))
				return null;
			IEnumerable<MultiNodeSyntax> folds = doc.Root.VisibleFoldableNodes;
			if (folds == null)
				return null;
			return folds.FirstOrDefault (n => n.StartLocation.Line == line);
		}
		MultiNodeSyntax getFoldContainingLine (int line) {
			if (!(Document is SourceDocument doc))
				return null;
			doc.EnterReadLock();
			try {
				IEnumerable<MultiNodeSyntax> folds = doc.Root.VisibleFoldableNodes;
				if (folds == null)
					return null;
				return folds.LastOrDefault (n => n.StartLocation.Line <= line && n.EndLocation.Line >= line);
			} finally {
				doc.ExitReadLock ();
			}
		}

		int getVisualLineFromAboluteLine (int absoluteLine) {
			if (!(Document is SourceDocument doc))
				return absoluteLine;
			doc.EnterReadLock();
			try {
				int foldedLines = 0;
				if (!doc.IsParsed)
					return 0;
				IEnumerator<MultiNodeSyntax> foldsEnum = doc.Root.VisibleFoldableNodes.GetEnumerator();
				bool notEndOfFolds = foldsEnum.MoveNext();
				while (notEndOfFolds && foldsEnum.Current.StartLocation.Line < absoluteLine) {
					if (foldsEnum.Current.isFolded) {
						foldedLines += foldsEnum.Current.LineCount - 1;
						SyntaxNode nextNode = foldsEnum.Current.NextSiblingOrParentsNextSibling;
						if (nextNode == null)
							break;
						notEndOfFolds = foldsEnum.MoveNext();
						while (notEndOfFolds && foldsEnum.Current.StartLocation.Line < nextNode.StartLocation.Line)
							notEndOfFolds = foldsEnum.MoveNext();
					} else
						notEndOfFolds = foldsEnum.MoveNext();
				}
				return absoluteLine - foldedLines;
			} finally {
				doc.ExitReadLock ();
			}
		}
		int getAbsoluteLineFromVisualLine (int visualLine) {
			if (!(Document is SourceDocument doc))
				return 0;
			doc.EnterReadLock();
			try {
				if (!doc.IsParsed)
					return 0;

				int linesToSkip = visualLine;
				IEnumerator<MultiNodeSyntax> foldsEnum = doc.Root.VisibleFoldableNodes.GetEnumerator ();
				bool notEndOfFolds = foldsEnum.MoveNext();

				while (notEndOfFolds && foldsEnum.Current.StartLocation.Line < linesToSkip) {
					if (foldsEnum.Current.isFolded)
						linesToSkip += foldsEnum.Current.LineCount-1;
					notEndOfFolds = foldsEnum.MoveNext();
				}
				return linesToSkip;					
			} finally {
				doc.ExitReadLock ();
			}
		}

		protected override int getAbsoluteLineIndexFromVisualLineMove (int startLine, int visualLineDiff) {
			int newVl = Math.Min (Math.Max (0, getVisualLineFromAboluteLine (startLine) + visualLineDiff), visualLineCount - 1);
			return getAbsoluteLineFromVisualLine (newVl);
		}
		protected override int visualLineCount => parsingOk ? Document.LinesCount - sourceDocument.Root.FoldedLineCount : base.visualLineCount;
		protected override int visualCurrentLine => CurrentLoc.HasValue ? getVisualLineFromAboluteLine (CurrentLoc.Value.Line) : 0;
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


		protected virtual void fillHighlight (IContext gr, int l, CharLocation selStart, CharLocation selEnd, RectangleD selRect, Color color) {
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
		protected void drawLineNumber(IContext gr, int lineIndex, double x, double y) {
			string strLN = (lineIndex+1).ToString ();
			gr.MoveTo (x - gr.TextExtents (strLN).Width, y);
			gr.ShowText (strLN);
			gr.Fill ();

		}
		protected override void drawContent (IContext gr) {
			sourceDocument.EnterReadLock ();

			double lineHeight = fe.Ascent + fe.Descent;
			updateMargin ();

			bool printLineNumbers = App.PrintLineNumbers;
			Color marginBG = App.MarginBackground;
			Color marginFG = Colors.Ivory;
			Rectangle cb = ClientRectangle;
			RectangleD marginRect = new RectangleD (cb.X, cb.Y, leftMargin - leftMarginRightGap, cb.Height);

			gr.SetSource (marginBG);
			gr.Rectangle (marginRect);
			gr.Fill();			

			if (!parsingOk) {
				base.drawContent (gr);
				sourceDocument.ExitReadLock ();
				return;
			}

			try {
				gr.Translate (-ScrollX, 0);

				double lineNumWidth = gr.TextExtents (Document.LinesCount.ToString()).Width;

				marginRect.Height = lineHeight;
				marginRect.Left += ScrollX;
				cb.Left += leftMargin;

				CharLocation selStart = default, selEnd = default;
				bool selectionNotEmpty = false;
				CharLocation? nodeStart = null, nodeEnd = null;
#if DEBUG_NODES
				CharLocation? editNodeStart = null, editNodeEnd = null;//debug
				CharLocation? hoverNodeStart = null, hoverNodeEnd = null;
#endif
				if (currentLoc?.Column < 0) {
					updateLocation (gr, ref currentLoc);
				} else
					updateLocation (gr, ref currentLoc);

				if (CurrentNode != null) {
					TextSpan nodeSpan = CurrentNode.Span;
					nodeStart = Document.GetLocation  (nodeSpan.Start);
					updateLocation (gr, ref nodeStart);
					nodeEnd = Document.GetLocation  (nodeSpan.End);
					updateLocation (gr, ref nodeEnd);
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
						p += ScreenCoordinates (Slot).TopLeft + cb.TopLeft;
						overlay.Left = p.X;
						overlay.Top = p.Y;
					}
				}
				if (selectionStart.HasValue) {
					updateLocation (gr, ref selectionStart);
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
				}

				double	pixX = cb.Left,
						pixY = cb.Top;

				Foreground.SetAsSource (IFace, gr);

				bool showWhiteSpaces = App.ShowWhiteSpace;
				string tabString = showWhiteSpaces ?
					$"{new string(' ', (App.TabulationSize - 2) / 2)} \u2192{new string(' ', (App.TabulationSize - 2) / 2)}" : default;

				ReadOnlySpan<char> sourceBytes = sourceDocument.source;
				Span<byte> bytes = stackalloc byte[128];
				TextExtents extents;
				ReadOnlySpan<char> buff = sourceBytes;
				
				int printedLines = 0;
				int linesToSkip = (int)Math.Floor(ScrollY / lineHeight);

				
				IEnumerator<MultiNodeSyntax> foldsEnum = sourceDocument.Root.VisibleFoldableNodes.GetEnumerator ();
				bool notEndOfFolds = foldsEnum.MoveNext();

				while (notEndOfFolds && foldsEnum.Current.StartLocation.Line < linesToSkip) {
					if (foldsEnum.Current.isFolded)
						linesToSkip += foldsEnum.Current.LineCount-1;
					notEndOfFolds = foldsEnum.MoveNext();
				}				

				int curLine = linesToSkip;
				pixY = -(ScrollY % lineHeight);

				/*IEnumerator<int> exceptionLines = doc.Root.GetAllExceptions()?.Select(e=>e.Location.Line).Order().GetEnumerator();
				bool hasExceptions = exceptionLines.MoveNext();*/

				while (curLine < sourceDocument.LinesCount && printedLines < Math.Min(visibleLines, sourceDocument.LinesCount)) {

					/*while (hasExceptions && exceptionLines.Current < curLine) {
						hasExceptions = exceptionLines.MoveNext();
					}*/
										
					int encodedChar = 0;
					TextLine curTxtLine = sourceDocument.GetLine (curLine);
					int tokPtr = sourceDocument.FindTokenIndexIncludingPosition(curTxtLine.Start);
					Token tok = sourceDocument.Tokens[tokPtr];

					while (tok.Start < (showWhiteSpaces ? curTxtLine.EndIncludingLineBreak : curTxtLine.End)) {
						if (showWhiteSpaces && tok.Type.HasFlag(TokenType.WhiteSpace)) {

							if(tok.Type == TokenType.WhiteSpace) {
								buff = new string('·', tok.Length);
							} if(tok.Type == TokenType.Tabulation) {
								buff = new StringBuilder(tok.Length*tabString.Length).Insert(0,tabString,tok.Length).ToString() ;
							} else if (tok.Type == TokenType.LineBreak) {
								buff = new string('\u204B', tok.Length);
							}
							/*gr.MoveTo (pixX, pixY + fe.Ascent);
							gr.ShowText (buff);*/
						} else
							buff = sourceBytes.Slice (tok.Start, tok.Length);
						gr.SetSource (sourceDocument.GetColorForToken (tok));

						int size = buff.Length * 4 + 1;
						if (bytes.Length < size)
							bytes = new byte[size];

						int encodedBytes = buff.ToUtf8 (bytes, ref encodedChar, tabSize);

						if (encodedBytes > 0) {
							bytes[encodedBytes++] = 0;
							gr.TextExtents (bytes.Slice (0, encodedBytes), out extents);
							if (extents.Width > 0) {
								gr.MoveTo (pixX, pixY + fe.Ascent);
								gr.ShowText (bytes.Slice (0, encodedBytes));
							}

							if (CurrentToken != null && CurrentToken.Equals(tok)) {
								Rectangle r = new RectangleD(pixX, pixY, extents.Width, lineHeight);
								r.Inflate(1);
								gr.Rectangle(r);
								gr.SetSource(sourceDocument.GetColorForToken (tok).AdjustAlpha(0.5));
								gr.Stroke();
							}

							pixX += extents.XAdvance;
						}

						if (++tokPtr >= sourceDocument.Tokens.Length)
							break;
						tok = sourceDocument.Tokens[tokPtr];
					}

					RectangleD lineRect = new RectangleD (cb.X, pixY, pixX - cb.X, lineHeight);
					if (CurrentNode != null && curLine >= nodeStart.Value.Line && curLine <= nodeEnd.Value.Line)
						fillHighlight (gr, curLine, nodeStart.Value, nodeEnd.Value, lineRect, new Color(0.0,0.1,0.0,0.06));;
#if DEBUG_NODES
					if (doc.EditedNode != null && curLine >= editNodeStart.Value.Line && l <= editNodeEnd.Value.Line)
						fillHighlight (gr, curLine, editNodeStart.Value, editNodeEnd.Value, lineRect, new Color(0,0.5,0,0.2));;
					if (hoverNode != null && curLine >= hoverNodeStart.Value.Line && curLine <= hoverNodeEnd.Value.Line)
						fillHighlight (gr, curLine, hoverNodeStart.Value, hoverNodeEnd.Value, lineRect, new Color(0,0,0.8,0.1));;
#endif
					if (selectionNotEmpty && curLine >= selStart.Line && curLine <= selEnd.Line)
						fillHighlight (gr, curLine, selStart, selEnd, lineRect, SelectionBackground);
					/*if (hasExceptions && exceptionLines.Current == curLine) {
						gr.Operator = Operator.DestOver;
						gr.SetSource (new Color(1.0,0,0.0,0.3));
						gr.Rectangle (lineRect);
						gr.Fill ();
						gr.Operator = Operator.Over;						
					}*/
					
					//Draw line numbering
					if (printLineNumbers){
						marginRect.Y = lineRect.Y;
						gr.SetSource (marginFG);

						drawLineNumber (gr, curLine, marginRect.X + leftMarginGap + lineNumWidth, marginRect.Y + fe.Ascent);
						if (tokPtr + 1 == sourceDocument.Tokens.Length && curLine < sourceDocument.LinesCount-1)
							drawLineNumber (gr, curLine+1, marginRect.X + leftMarginGap + lineNumWidth, marginRect.Y + lineHeight + fe.Ascent);
					}
					bool curFoldStart = notEndOfFolds && foldsEnum.Current.StartLocation.Line == curLine; 
					//draw fold
					if (curFoldStart) {
						Rectangle rFld = new Rectangle ((int)marginRect.Right - leftMarginGap - foldSize,
							(int)(marginRect.Y + lineHeight / 2.0 - foldSize / 2.0), foldSize, foldSize);

						gr.Rectangle (rFld);
						if (hoverLoc.HasValue && curLine == hoverLoc.Value.Line && mouseIsInFoldRect)
							gr.SetSource (Colors.LightBlue);
						else
							gr.SetSource (Colors.White);
						gr.Fill();
						gr.SetSource (Colors.Black);
						gr.Rectangle (rFld, 1.0);
						if (foldsEnum.Current.isFolded) {
							gr.MoveTo (rFld.Center.X + 0.5, rFld.Y + 2);
							gr.LineTo (rFld.Center.X + 0.5, rFld.Bottom - 2);
						}

						gr.MoveTo (rFld.Left + 2, rFld.Center.Y + 0.5);
						gr.LineTo (rFld.Right - 2, rFld.Center.Y + 0.5);
						gr.Stroke ();
					}

					if (++tokPtr >= sourceDocument.Tokens.Length)
						break;
					tok = sourceDocument.Tokens[tokPtr];

					pixX = cb.Left;
					pixY += lineHeight;
					printedLines++;

					if (curFoldStart && curLine == foldsEnum.Current.StartLocation.Line){
						if (foldsEnum.Current.isFolded)
							curLine += foldsEnum.Current.LineCount;
						else
							curLine++;
						notEndOfFolds = foldsEnum.MoveNext();
						while (notEndOfFolds && foldsEnum.Current.StartLocation.Line < curLine) {
							/*if (foldsEnum.Current.isFolded && curLine <= foldsEnum.Current.EndLine)
								curLine += foldsEnum.Current.LineCount;*/
							notEndOfFolds = foldsEnum.MoveNext();
						}
					} else
						curLine++;

				}
			} catch (Exception e) {
				Console.WriteLine(e.Message);
				Console.WriteLine(e.StackTrace);
			} finally {
				sourceDocument.ExitReadLock ();
			}
			gr.Translate (ScrollX, 0);
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
		protected override void update (TextChange change) {
			base.update (change);

			updateCurrentTokAndNode();

			if (!disableSuggestions &&!disableTextChangedEvent && HasFocus)
				tryGetSuggestions ();

			RegisterForGraphicUpdate();
		}
		void updateCurrentTokAndNode() {
			if (currentLoc.HasValue && Document is SourceDocument srcdoc && srcdoc.Tokens.Length > 0) {
				int pos = srcdoc.GetAbsolutePosition(currentLoc.Value);
				currentTokenIndex = srcdoc.FindTokenIndexIncludingPosition(pos);
				Token tok = srcdoc.GetTokenByIndex(currentTokenIndex);

				CurrentNode = srcdoc.Root?.FindNodeIncludingSpan(tok.Span);
				
				NotifyValueChanged("CurrentToken",tok);
#if DEBUG
				NotifyValueChanged("CurrentTokenString",CurrentTokenString);
				NotifyValueChanged("CurrentTokenType",CurrentTokenType);
				NotifyValueChanged("AbsoluteCharLoc",AbsoluteCharLoc);
				
#endif
			
				
			} else {
				currentTokenIndex = -1;
				CurrentNode = null;
				NotifyValueChanged("CurrentToken",default);
			}
		}
	}
}