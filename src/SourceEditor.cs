//
// ScrollingTextBox.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Collections;
using Cairo;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;
using CrowEdit;
using System.IO;

namespace Crow.Coding
{
	/// <summary>
	/// Scrolling text box optimized for monospace fonts, for coding
	/// </summary>
	public class SourceEditor : ScrollingObject
	{
		#region CTOR
		public SourceEditor ():base()
		{
			formatting.Add ((int)XMLParser.TokenType.AttributeName, new TextFormatting (Color.UnitedNationsBlue, Color.Transparent));
			formatting.Add ((int)XMLParser.TokenType.ElementName, new TextFormatting (Color.DarkBlue, Color.Transparent, true));
			formatting.Add ((int)XMLParser.TokenType.ElementStart, new TextFormatting (Color.Red, Color.Transparent,true));
			formatting.Add ((int)XMLParser.TokenType.ElementEnd, new TextFormatting (Color.Red, Color.Transparent,true));
			formatting.Add ((int)XMLParser.TokenType.ElementClosing, new TextFormatting (Color.Red, Color.Transparent, true));
			formatting.Add ((int)XMLParser.TokenType.Affectation, new TextFormatting (Color.Red, Color.Transparent, true));

			formatting.Add ((int)XMLParser.TokenType.AttributeValueOpening, new TextFormatting (Color.DarkPink, Color.Transparent,true));
			formatting.Add ((int)XMLParser.TokenType.AttributeValueClosing, new TextFormatting (Color.DarkPink, Color.Transparent,true));
			formatting.Add ((int)XMLParser.TokenType.AttributeValue, new TextFormatting (Color.DarkPink, Color.Transparent, false, true));
			formatting.Add ((int)XMLParser.TokenType.XMLDecl, new TextFormatting (Color.BlueCrayola, Color.Transparent, true));
			formatting.Add ((int)XMLParser.TokenType.BlockComment, new TextFormatting (Color.Gray, Color.Transparent, false, true));

			parsing.Add (".crow", "Crow.Coding.XMLParser");

			buffer = new CodeBuffer ();
			buffer.LineUpadateEvent += Buffer_LineUpadateEvent;
			buffer.LineAdditionEvent += Buffer_LineAdditionEvent;;
			buffer.LineRemoveEvent += Buffer_LineRemoveEvent;
			buffer.BufferCleared += Buffer_BufferCleared;
		}
		#endregion

		const int leftMarginGap = 2;//gap between items in margin and text
		const int foldSize = 9;//folding rectangles size

		#region private and protected fields
		bool foldingEnabled = true;
		string filePath = "unamed.txt";
		int leftMargin = 0;	//margin used to display line numbers, folding errors,etc...
		int visibleLines = 1;
		int visibleColumns = 1;
		CodeBuffer buffer;
		Parser parser;
		Color selBackground;
		Color selForeground;
		int _currentCol;        //0 based cursor position in string
		int _currentLine;
		Point _selBegin = -1;	//selection start (row,column)
		Point _selRelease = -1;	//selection end (row,column)

		Dictionary<int, TextFormatting> formatting = new Dictionary<int, TextFormatting>();
		Dictionary<string, string> parsing = new Dictionary<string, string>();

		protected Rectangle rText;
		protected FontExtents fe;
		protected TextExtents te;
		#endregion

		void measureLeftMargin () {
			leftMargin = 0;
			if (PrintLineNumbers)
				leftMargin += (int)Math.Ceiling((double)buffer.LineCount.ToString().Length * fe.MaxXAdvance);
			if (foldingEnabled)
				leftMargin += foldSize;
			if (leftMargin > 0)
				leftMargin += leftMarginGap;
			updateVisibleColumns ();
		}
		void findLongestLineAndUpdateMaxScrollX() {
			buffer.FindLongestVisualLine ();
			MaxScrollX = Math.Max (0, buffer.longestLineCharCount - visibleColumns);
			Debug.WriteLine ("SourceEditor: Find Longest line and update maxscrollx: {0} visible cols:{1}", MaxScrollX, visibleColumns);
		}
		void updateVisibleLines(){
			visibleLines = (int)Math.Floor ((double)ClientRectangle.Height / fe.Height);
			MaxScrollY = Math.Max (0, buffer.LineCount - visibleLines);

			System.Diagnostics.Debug.WriteLine ("update visible lines: " + visibleLines);
			System.Diagnostics.Debug.WriteLine ("update MaxScrollY: " + MaxScrollY);
		}
		void updateVisibleColumns(){
			visibleColumns = (int)Math.Floor ((double)(ClientRectangle.Width - leftMargin)/ fe.MaxXAdvance);
			MaxScrollX = Math.Max (0, buffer.longestLineCharCount - visibleColumns);

			System.Diagnostics.Debug.WriteLine ("update visible columns: {0} leftMargin:{1}",visibleColumns, leftMargin);
			System.Diagnostics.Debug.WriteLine ("update MaxScrollX: " + MaxScrollX);
		}

		#region Buffer events handlers
		void Buffer_BufferCleared (object sender, EventArgs e)
		{
			buffer.longestLineCharCount = 0;
			buffer.longestLineIdx = 0;
			measureLeftMargin ();
			MaxScrollX = MaxScrollY = 0;
			RegisterForGraphicUpdate ();
		}
		void Buffer_LineAdditionEvent (object sender, CodeBufferEventArgs e)
		{
			for (int i = 0; i < e.LineCount; i++) {
				int lptr = e.LineStart + i;
				int charCount = buffer.GetPrintableLine (lptr).Length;
				if (charCount > buffer.longestLineCharCount) {
					buffer.longestLineIdx = lptr;
					buffer.longestLineCharCount = charCount;
				}else if (lptr <= buffer.longestLineIdx)
					buffer.longestLineIdx++;
			}
			measureLeftMargin ();
			RegisterForGraphicUpdate ();
		}

		void Buffer_LineRemoveEvent (object sender, CodeBufferEventArgs e)
		{
			bool trigFindLongestLine = false;
			for (int i = 0; i < e.LineCount; i++) {
				int lptr = e.LineStart + i;
				if (lptr <= buffer.longestLineIdx)
					trigFindLongestLine = true;
			}
			if (trigFindLongestLine)
				findLongestLineAndUpdateMaxScrollX ();
			measureLeftMargin ();
			RegisterForGraphicUpdate ();
		}

		void Buffer_LineUpadateEvent (object sender, CodeBufferEventArgs e)
		{
			bool trigFindLongestLine = false;
			for (int i = 0; i < e.LineCount; i++) {
				int lptr = e.LineStart + i;
				if (lptr == buffer.longestLineIdx)
					trigFindLongestLine = true;
				else if (buffer.GetPrintableLine (lptr).Length > buffer.longestLineCharCount) {
					buffer.longestLineCharCount = buffer.GetPrintableLine (lptr).Length;
					buffer.longestLineIdx = lptr;
				}
			}
			if (trigFindLongestLine)
				findLongestLineAndUpdateMaxScrollX ();
			RegisterForGraphicUpdate ();
		}
		#endregion

		Parser getParserFromExt (string extension) {
			if (string.IsNullOrEmpty(extension))
				return null;
			if (!parsing.ContainsKey(extension))
				return null;
			Type parserType = Type.GetType (parsing [extension]);
			if (parserType == null)
				return null;
			return (Parser)Activator.CreateInstance (parserType, buffer );
		}

		#region Public Crow Properties
		[XmlAttributeAttribute]
		public bool PrintLineNumbers
		{
			get { return Configuration.Get<bool> ("PrintLineNumbers");
			}
			set
			{
				if (PrintLineNumbers == value)
					return;
				Configuration.Set ("PrintLineNumbers", value);
				NotifyValueChanged ("PrintLineNumbers", PrintLineNumbers);
				RegisterForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute]
		public string FilePath
		{
			get {
				return buffer == null ? "" : buffer.FullText;
			}
			set
			{
				if (filePath == value)
					return;

				filePath = value;
				NotifyValueChanged ("FilePath", filePath);

				if (!File.Exists (filePath))
					return;

				parser = getParserFromExt (System.IO.Path.GetExtension (filePath));

				using (StreamReader sr = new StreamReader (filePath)) {
					string txt = sr.ReadToEnd ();
					buffer.Load (txt);
				}

				MaxScrollY = Math.Max (0, buffer.LineCount - visibleLines);
				MaxScrollX = Math.Max (0, buffer.longestLineCharCount - visibleColumns);

				RegisterForGraphicUpdate ();
			}
		}
		[XmlAttributeAttribute][DefaultValue("BlueGray")]
		public virtual Color SelectionBackground {
			get { return selBackground; }
			set {
				if (value == selBackground)
					return;
				selBackground = value;
				NotifyValueChanged ("SelectionBackground", selBackground);
				RegisterForRedraw ();
			}
		}
		[XmlAttributeAttribute][DefaultValue("White")]
		public virtual Color SelectionForeground {
			get { return selForeground; }
			set {
				if (value == selForeground)
					return;
				selForeground = value;
				NotifyValueChanged ("SelectionForeground", selForeground);
				RegisterForRedraw ();
			}
		}
		[XmlAttributeAttribute][DefaultValue(0)]
		public int CurrentColumn{
			get { return _currentCol; }
			set {
				if (value == _currentCol)
					return;
				if (value < 0)
					_currentCol = 0;
				else if (value > buffer.GetPrintableLine(_currentLine).Length)
					_currentCol = buffer.GetPrintableLine(_currentLine).Length;
				else
					_currentCol = value;

				buffer.SetBufferPos (CurrentPosition);

				if (_currentCol < ScrollX)
					ScrollX = _currentCol;
				else if (_currentCol >= ScrollX + visibleColumns)
					ScrollX = _currentCol - visibleColumns + 1;

				NotifyValueChanged ("CurrentColumn", _currentCol);
			}
		}
		[XmlAttributeAttribute][DefaultValue(0)]
		public int CurrentLine{
			get { return _currentLine; }
			set {
				if (value == _currentLine)
					return;
				if (value >= buffer.LineCount)
					_currentLine = buffer.LineCount-1;
				else if (value < 0)
					_currentLine = 0;
				else
					_currentLine = value;
				//force recheck of currentCol for bounding
				int cc = _currentCol;
				_currentCol = 0;
				CurrentColumn = cc;

				buffer.SetBufferPos (CurrentPosition);

				//System.Diagnostics.Debug.WriteLine ("Scroll:{0} visibleLines:{1} CurLine:{2}", ScrollY, visibleLines, CurrentLine);

				if (_currentLine < ScrollY)
					ScrollY = _currentLine;
				else if (_currentLine >= ScrollY + visibleLines)
					ScrollY = _currentLine - visibleLines + 1;
				NotifyValueChanged ("CurrentLine", _currentLine);
			}
		}
		/// <summary>
		/// Current position is in the printed coord system, tabulation chars are replaced with 4 spaces,
		/// while in the buffer, the position holds tabulations as single chars
		/// </summary>
		/// <value>The current position.</value>
		[XmlIgnore]public Point CurrentPosition {
			get { return new Point(CurrentColumn, CurrentLine); }
			set {
				_currentCol = value.X;
				_currentLine = value.Y;

				if (_currentCol < ScrollX)
					ScrollX = _currentCol;
				else if (_currentCol >= ScrollX + visibleColumns)
					ScrollX = _currentCol - visibleColumns + 1;

				if (_currentLine < ScrollY)
					ScrollY = _currentLine;
				else if (_currentLine >= ScrollY + visibleLines)
					ScrollY = _currentLine - visibleLines + 1;

				NotifyValueChanged ("CurrentColumn", _currentCol);
				NotifyValueChanged ("CurrentLine", _currentLine);
			}
		}
		//TODO:using HasFocus for drawing selection cause SelBegin and Release binding not to work
		/// <summary>
		/// Selection begin position in char units (line, column)
		/// </summary>
		[XmlAttributeAttribute][DefaultValue("-1")]
		public Point SelBegin {
			get { return _selBegin; }
			set {
				if (value == _selBegin)
					return;
				_selBegin = value;
				System.Diagnostics.Debug.WriteLine ("SelBegin=" + _selBegin);
				NotifyValueChanged ("SelBegin", _selBegin);
				//NotifyValueChanged ("SelectedText", SelectedText);
			}
		}
		/// <summary>
		/// Selection release position in char units (line, column)
		/// </summary>
		[XmlAttributeAttribute][DefaultValue("-1")]
		public Point SelRelease {
			get {
				return _selRelease;
			}
			set {
				if (value == _selRelease)
					return;
				_selRelease = value;
				System.Diagnostics.Debug.WriteLine ("SelRelease=" + _selRelease);
				NotifyValueChanged ("SelRelease", _selRelease);
				//NotifyValueChanged ("SelectedText", SelectedText);
			}
		}
		/// <summary>
		/// return char at CurrentLine, CurrentColumn
		/// </summary>
		[XmlIgnore]protected Char CurrentChar
		{
			get {
				return buffer [CurrentLine] [CurrentColumn];
			}
		}
		/// <summary>
		/// ordered selection start and end positions in char units
		/// </summary>
		[XmlIgnore]protected Point selectionStart
		{
			get {
				return SelRelease < 0 || SelBegin.Y < SelRelease.Y ? SelBegin :
					SelBegin.Y > SelRelease.Y ? SelRelease :
					SelBegin.X < SelRelease.X ? SelBegin : SelRelease;
			}
		}
		[XmlIgnore]public Point selectionEnd
		{
			get {
				return SelRelease < 0 || SelBegin.Y > SelRelease.Y ? SelBegin :
					SelBegin.Y < SelRelease.Y ? SelRelease :
					SelBegin.X > SelRelease.X ? SelBegin : SelRelease;
			}
		}
		[XmlIgnore]public string SelectedText
		{
			get {
				if (!selectionIsEmpty)
					buffer.SetSelection (selectionStart, selectionEnd);
				return buffer.SelectedText;
			}
		}
		[XmlIgnore]public bool selectionIsEmpty
		{ get { return SelRelease == SelBegin; } }
		#endregion

		#region Editing and moving cursor
		/// <summary>
		/// Moves cursor one char to the left.
		/// </summary>
		/// <returns><c>true</c> if move succeed</returns>
		public bool MoveLeft(){
			bool res = buffer.MoveLeft();
			CurrentPosition = buffer.TabulatedPosition;
			return res;
		}
		/// <summary>
		/// Moves cursor one char to the right.
		/// </summary>
		/// <returns><c>true</c> if move succeed</returns>
		public bool MoveRight(){
			bool res = buffer.MoveRight();
			CurrentPosition = buffer.TabulatedPosition;
			return res;
		}
		public void GotoWordStart(){
			buffer.GotoWordStart();
			CurrentPosition = buffer.TabulatedPosition;
		}
		public void GotoWordEnd(){
			buffer.GotoWordEnd();
			CurrentPosition = buffer.TabulatedPosition;
		}

		public void DeleteChar()
		{
			if (!selectionIsEmpty)
				buffer.SetSelection (selectionStart, selectionEnd);
			buffer.DeleteChar ();
			CurrentPosition = buffer.TabulatedPosition;
			SelBegin = -1;
			SelRelease = -1;
		}
		/// <summary>
		/// Insert new string at caret position, should be sure no line break is inside.
		/// </summary>
		/// <param name="str">String.</param>
		protected void Insert(string str)
		{
			if (!selectionIsEmpty)
				DeleteChar ();

			buffer.Insert (str);
			CurrentPosition = buffer.TabulatedPosition;

			RegisterForGraphicUpdate();
		}
		/// <summary>
		/// Insert a line break.
		/// </summary>
		protected void InsertLineBreak()
		{
			buffer.InsertLineBreak ();

			if (_currentLine == buffer.longestLineIdx)
				findLongestLineAndUpdateMaxScrollX ();

			CurrentPosition = buffer.TabulatedPosition;

			RegisterForGraphicUpdate();
		}
		#endregion

		#region Drawing
		/// <summary>
		/// Draw unparsed buffer.
		/// </summary>
		void draw(Context gr){
			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);
			gr.FontOptions = Interface.FontRenderingOptions;
			gr.Antialias = Interface.Antialias;

			Rectangle cb = ClientRectangle;

			bool selectionInProgress = false;

			Foreground.SetAsSource (gr);

			#region draw text cursor
			if (SelBegin != SelRelease)
				selectionInProgress = true;
			else if (HasFocus){
				gr.LineWidth = 1.0;
				double cursorX = cb.X + (CurrentColumn - ScrollX) * fe.MaxXAdvance + leftMargin;
				gr.MoveTo (0.5 + cursorX, cb.Y + (CurrentLine - ScrollY) * fe.Height);
				gr.LineTo (0.5 + cursorX, cb.Y + (CurrentLine + 1 - ScrollY) * fe.Height);
				gr.Stroke();
			}
			#endregion

			for (int i = 0; i < visibleLines; i++) {
				int curL = i + ScrollY;
				if (curL >= buffer.LineCount)
					break;
				string lstr = buffer.GetPrintableLine(curL);
				if (ScrollX < lstr.Length)
					lstr = lstr.Substring (ScrollX);
				else
					lstr = "";

				gr.MoveTo (cb.X, cb.Y + fe.Ascent + fe.Height * i);
				gr.ShowText (lstr);
				gr.Fill ();

				if (selectionInProgress && curL >= selectionStart.Y && curL <= selectionEnd.Y) {

					double rLineX = cb.X,
					rLineY = cb.Y + i * fe.Height,
					rLineW = lstr.Length * fe.MaxXAdvance;

					System.Diagnostics.Debug.WriteLine ("sel start: " + selectionStart + " sel end: " + selectionEnd);
					if (curL == selectionStart.Y) {
						rLineX += (selectionStart.X - ScrollX) * fe.MaxXAdvance;
						rLineW -= selectionStart.X * fe.MaxXAdvance;
					}
					if (curL == selectionEnd.Y)
						rLineW -= (lstr.Length - selectionEnd.X) * fe.MaxXAdvance;

					gr.Save ();
					gr.Operator = Operator.Source;
					gr.Rectangle (rLineX, rLineY, rLineW, fe.Height);
					gr.SetSourceColor (SelectionBackground);
					gr.FillPreserve ();
					gr.Clip ();
					gr.Operator = Operator.Over;
					gr.SetSourceColor (SelectionForeground);
					gr.MoveTo (cb.X, cb.Y + fe.Ascent + fe.Height * i);
					gr.ShowText (lstr);
					gr.Fill ();
					gr.Restore ();
				}
			}
		}
		void drawParsed(Context gr){
			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);
			gr.FontOptions = Interface.FontRenderingOptions;
			gr.Antialias = Interface.Antialias;

			Rectangle cb = ClientRectangle;
			gr.Save ();
			CairoHelpers.CairoRectangle (gr, cb, CornerRadius);
			gr.Clip ();

			bool selectionInProgress = false;

			Foreground.SetAsSource (gr);

			#region draw text cursor
			if (SelBegin != SelRelease)
				selectionInProgress = true;
			else if (HasFocus){
				gr.LineWidth = 1.0;
				double cursorX = + leftMargin + cb.X + (CurrentColumn - ScrollX) * fe.MaxXAdvance;
				gr.MoveTo (0.5 + cursorX, cb.Y + (CurrentLine - ScrollY) * fe.Height);
				gr.LineTo (0.5 + cursorX, cb.Y + (CurrentLine + 1 - ScrollY) * fe.Height);
				gr.Stroke();
			}
			#endregion

			for (int i = 0; i < visibleLines; i++) {
				if (i + ScrollY >= parser.Tokens.Count)
					break;
				drawTokenLine (gr, i, selectionInProgress, cb);
			}

			gr.Restore ();
		}
		void drawTokenLine(Context gr, int i, bool selectionInProgress, Rectangle cb) {
			int curL = i + ScrollY;
			TokenList tokens = parser.Tokens[curL];
			int lPtr = 0;
			double y = cb.Y + fe.Height * i;

			//Draw line numbering
			Color mgFg = Color.Gray;
			Color mgBg = Color.White;
			if (PrintLineNumbers){
				Rectangle mgR = new Rectangle (cb.X, (int)y, leftMargin - leftMarginGap, (int)Math.Ceiling(fe.Height));
				if (tokens.exception != null) {
					mgBg = Color.Red;
					if (CurrentLine == curL)
						mgFg = Color.White;
					else
						mgFg = Color.LightGray;
				}else if (CurrentLine == curL) {
					mgFg = Color.Black;
				}
				string strLN = curL.ToString ();
				gr.SetSourceColor (mgBg);
				gr.Rectangle (mgR);
				gr.Fill();
				gr.SetSourceColor (mgFg);

				gr.MoveTo (cb.X + (int)(gr.TextExtents (parser.Tokens.Count.ToString()).Width - gr.TextExtents (strLN).Width), y + fe.Ascent);
				gr.ShowText (strLN);
				gr.Fill ();
			}
			//draw folding
			if (foldingEnabled){
				if (tokens.SyntacticNode != null) {
					if (tokens.SyntacticNode.StartLine < tokens.SyntacticNode.EndLine) {
						gr.SetSourceColor (Color.Black);
						Rectangle rFld = new Rectangle (cb.X + leftMargin - leftMarginGap - foldSize, (int)(y + fe.Height / 2.0 - foldSize / 2.0), foldSize, foldSize);
						gr.Rectangle (rFld, 1.0);
						if (tokens.folded) {
							gr.MoveTo (rFld.Center.X + 0.5, rFld.Y + 2);
							gr.LineTo (rFld.Center.X + 0.5, rFld.Bottom - 2);
						}
						gr.MoveTo (rFld.Left + 2, rFld.Center.Y + 0.5);
						gr.LineTo (rFld.Right - 2, rFld.Center.Y + 0.5);
						gr.Stroke ();
					}
				}
			}

			for (int t = 0; t < tokens.Count; t++) {
				string lstr = tokens [t].PrintableContent;
				if (lPtr < ScrollX) {
					if (lPtr - ScrollX + lstr.Length <= 0) {
						lPtr += lstr.Length;
						continue;
					}
					lstr = lstr.Substring (ScrollX - lPtr);
					lPtr += ScrollX - lPtr;
				}
				Color bg = this.Background;
				Color fg = this.Foreground;
				Color selbg = this.SelectionBackground;
				Color selfg = this.SelectionForeground;
				FontSlant fts = FontSlant.Normal;
				FontWeight ftw = FontWeight.Normal;

				if (formatting.ContainsKey ((int)tokens [t].Type)) {
					TextFormatting tf = formatting [(int)tokens [t].Type];
					bg = tf.Background;
					fg = tf.Foreground;
					if (tf.Bold)
						ftw = FontWeight.Bold;
					if (tf.Italic)
						fts = FontSlant.Italic;
				}

				gr.SelectFontFace (Font.Name, fts, ftw);
				gr.SetSourceColor (fg);

				int x = leftMargin + cb.X + (int)((lPtr - ScrollX) * fe.MaxXAdvance);

				gr.MoveTo (x, y + fe.Ascent);
				gr.ShowText (lstr);
				gr.Fill ();

				if (selectionInProgress && curL >= selectionStart.Y && curL <= selectionEnd.Y &&
					!(curL == selectionStart.Y && lPtr + lstr.Length <= selectionStart.X) &&
					!(curL == selectionEnd.Y && selectionEnd.X <= lPtr)) {

					double rLineX = x,
					rLineY = cb.Y + i * fe.Height,
					rLineW = lstr.Length * fe.MaxXAdvance;
					double startAdjust = 0.0;

					if ((curL == selectionStart.Y) && (selectionStart.X < lPtr + lstr.Length) && (selectionStart.X > lPtr))
						startAdjust = (selectionStart.X - lPtr) * fe.MaxXAdvance;
					rLineX += startAdjust;
					if ((curL == selectionEnd.Y) && (selectionEnd.X < lPtr + lstr.Length))
						rLineW = (selectionEnd.X - lPtr) * fe.MaxXAdvance;
					rLineW -= startAdjust;

					gr.Save ();
					gr.Operator = Operator.Source;
					gr.Rectangle (rLineX, rLineY, rLineW, fe.Height);
					gr.SetSourceColor (selbg);
					gr.FillPreserve ();
					gr.Clip ();
					gr.Operator = Operator.Over;
					gr.SetSourceColor (selfg);
					gr.MoveTo (x, cb.Y + fe.Ascent + fe.Height * i);
					gr.ShowText (lstr);
					gr.Fill ();
					gr.Restore ();
				}

				lPtr += lstr.Length;
			}
		}
		#endregion

		#region GraphicObject overrides
		public override Font Font {
			get { return base.Font; }
			set {
				base.Font = value;

				using (ImageSurface img = new ImageSurface (Format.Argb32, 1, 1)) {
					using (Context gr = new Context (img)) {
						gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
						gr.SetFontSize (Font.Size);

						fe = gr.FontExtents;
					}
				}
				MaxScrollY = 0;
				RegisterForGraphicUpdate ();
			}
		}
		protected override int measureRawSize(LayoutingType lt)
		{
			if (lt == LayoutingType.Height)
				return (int)Math.Ceiling(fe.Height * buffer.LineCount) + Margin * 2;

			return (int)(fe.MaxXAdvance * buffer.longestLineCharCount) + Margin * 2 + leftMargin;
		}
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);

			if (layoutType == LayoutingType.Height)
				updateVisibleLines ();
			else if (layoutType == LayoutingType.Width)
				updateVisibleColumns ();
		}

		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			if (parser != null)
				drawParsed (gr);
			else
				draw(gr);

		}
		#endregion

		#region Mouse handling
		void updatemouseLocalPos(Point mpos){
			Point mouseLocalPos = mpos - ScreenCoordinates(Slot).TopLeft - ClientRectangle.TopLeft;

			if (mouseLocalPos.X < 0)
				CurrentColumn--;
			else
				CurrentColumn = ScrollX +  (int)Math.Round ((mouseLocalPos.X - leftMargin) / fe.MaxXAdvance);

			if (mouseLocalPos.Y < 0)
				CurrentLine--;
			else
				CurrentLine = ScrollY + (int)Math.Floor (mouseLocalPos.Y / fe.Height);

			CurrentPosition = buffer.TabulatedPosition; //for rounding if in middle of tabs
		}
		public override void onMouseEnter (object sender, MouseMoveEventArgs e)
		{
			base.onMouseEnter (sender, e);
			if (e.X - ScreenCoordinates(Slot).X < leftMargin + ClientRectangle.X)
				currentInterface.MouseCursor = XCursor.Default;
			else
				currentInterface.MouseCursor = XCursor.Text;
		}
		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			base.onMouseLeave (sender, e);
			currentInterface.MouseCursor = XCursor.Default;
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			if (e.X - ScreenCoordinates(Slot).X < leftMargin + ClientRectangle.X)
				currentInterface.MouseCursor = XCursor.Default;
			else
				currentInterface.MouseCursor = XCursor.Text;

			if (!e.Mouse.IsButtonDown (MouseButton.Left))
				return;
			if (!HasFocus || SelBegin < 0)
				return;

			updatemouseLocalPos (e.Position);
			SelRelease = CurrentPosition;

			RegisterForRedraw();
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			//initialize cursor position if not yet focused
			if (!this.HasFocus & this.Focusable){
				updatemouseLocalPos (e.Position);
				SelBegin = SelRelease = CurrentPosition;
				RegisterForRedraw();
			}

			base.onMouseDown (sender, e);

			if (doubleClicked) {
				doubleClicked = false;
				return;
			}
			if (this.HasFocus){
				updatemouseLocalPos (e.Position);
				SelBegin = SelRelease = CurrentPosition;
				RegisterForRedraw();
			}
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			Debug.WriteLine ("MouseUp");
			base.onMouseUp (sender, e);

			if (SelBegin == SelRelease)
				SelBegin = SelRelease = -1;

			updatemouseLocalPos (e.Position);
			RegisterForRedraw ();
		}
		bool doubleClicked = false;
		public override void onMouseDoubleClick (object sender, MouseButtonEventArgs e)
		{
			doubleClicked = true;
			Debug.WriteLine ("DoubleClick");
			base.onMouseDoubleClick (sender, e);

			GotoWordStart ();
			SelBegin = CurrentPosition;
			GotoWordEnd ();
			SelRelease = CurrentPosition;
			RegisterForRedraw ();
		}
		#endregion

		#region Keyboard handling
		public override void onKeyDown (object sender, KeyboardKeyEventArgs e)
		{
			//base.onKeyDown (sender, e);

			Key key = e.Key;

			switch (key)
			{
			case Key.Back:
				if (CurrentPosition == 0)
					return;
				this.DeleteChar();
				break;
			case Key.Clear:
				break;
			case Key.Delete:
				if (selectionIsEmpty) {
					if (!MoveRight ())
						return;
				}else if (e.Shift)
					currentInterface.Clipboard = this.SelectedText;
				this.DeleteChar ();
				break;
			case Key.Enter:
			case Key.KeypadEnter:
				if (!selectionIsEmpty)
					this.DeleteChar ();
				this.InsertLineBreak ();
				break;
			case Key.Escape:
				SelRelease = SelBegin = -1;
				break;
			case Key.Home:
				if (e.Shift) {
					if (selectionIsEmpty)
						SelBegin = new Point (CurrentColumn, CurrentLine);
					if (e.Control)
						CurrentLine = 0;
					CurrentColumn = 0;
					SelRelease = new Point (CurrentColumn, CurrentLine);
					break;
				}
				SelRelease = SelBegin = -1;
				if (e.Control)
					CurrentLine = 0;
				CurrentColumn = 0;
				break;
			case Key.End:
				if (e.Shift) {
					if (selectionIsEmpty)
						SelBegin = CurrentPosition;
					if (e.Control)
						CurrentLine = int.MaxValue;
					CurrentColumn = int.MaxValue;
					SelRelease = CurrentPosition;
					break;
				}
				SelRelease = SelBegin = -1;
				if (e.Control)
					CurrentLine = int.MaxValue;
				CurrentColumn = int.MaxValue;
				break;
			case Key.Insert:
				if (e.Shift)
					this.Insert (currentInterface.Clipboard);
				else if (e.Control && !selectionIsEmpty)
					currentInterface.Clipboard = this.SelectedText;
				break;
			case Key.Left:
				if (e.Shift) {
					if (selectionIsEmpty)
						SelBegin = CurrentPosition;
					if (e.Control)
						GotoWordStart ();
					else if (!MoveLeft ())
						return;
					SelRelease = CurrentPosition;
					break;
				}
				SelRelease = SelBegin = -1;
				if (e.Control)
					GotoWordStart ();
				else
					MoveLeft();
				break;
			case Key.Right:
				if (e.Shift) {
					if (selectionIsEmpty)
						SelBegin = CurrentPosition;
					if (e.Control)
						GotoWordEnd ();
					else if (!MoveRight ())
						return;
					SelRelease = CurrentPosition;
					break;
				}
				SelRelease = SelBegin = -1;
				if (e.Control)
					GotoWordEnd ();
				else
					MoveRight ();
				break;
			case Key.Up:
				if (e.Shift) {
					if (selectionIsEmpty)
						SelBegin = CurrentPosition;
					CurrentLine--;
					SelRelease = CurrentPosition;
					break;
				}
				SelRelease = SelBegin = -1;
				CurrentLine--;
				break;
			case Key.Down:
				if (e.Shift) {
					if (selectionIsEmpty)
						SelBegin = CurrentPosition;
					CurrentLine++;
					SelRelease = CurrentPosition;
					break;
				}
				SelRelease = SelBegin = -1;
				CurrentLine++;
				break;
			case Key.Menu:
				break;
			case Key.NumLock:
				break;
			case Key.PageDown:
				if (e.Shift) {
					if (selectionIsEmpty)
						SelBegin = CurrentPosition;
					CurrentLine += visibleLines;
					SelRelease = CurrentPosition;
					break;
				}
				SelRelease = -1;
				CurrentLine += visibleLines;
				break;
			case Key.PageUp:
				if (e.Shift) {
					if (selectionIsEmpty)
						SelBegin = CurrentPosition;
					CurrentLine -= visibleLines;
					SelRelease = CurrentPosition;
					break;
				}
				CurrentLine -= visibleLines;
				break;
			case Key.RWin:
				break;
			case Key.Tab:
				this.Insert ("\t");
				break;
			case Key.F8:
//				if (parser != null)
//					reparseSource ();
				break;
			default:
				break;
			}
			RegisterForGraphicUpdate();
		}
		public override void onKeyPress (object sender, KeyPressEventArgs e)
		{
			base.onKeyPress (sender, e);

			this.Insert (e.KeyChar.ToString());

			SelRelease = -1;
			SelBegin = -1; //new Point(CurrentColumn, SelBegin.Y);

			RegisterForGraphicUpdate();
		}
		#endregion
	}
}