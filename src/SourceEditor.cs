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

namespace Crow
{
	/// <summary>
	/// Scrolling text box optimized for monospace fonts, for coding
	/// </summary>
	public class SourceEditor : ScrollingObject
	{
		#region CTOR
		public SourceEditor ():base()
		{


		}
		#endregion

		public event EventHandler TextChanged;

		public virtual void OnTextChanged(Object sender, EventArgs e)
		{
			TextChanged.Raise (this, e);
		}

		#region private and protected fields
		int visibleLines = 1;
		int visibleColumns = 1;
		CodeTextBuffer buffer;
		Color selBackground;
		Color selForeground;
		int _currentCol;        //0 based cursor position in string
		int _currentLine;
		Point _selBegin = -1;	//selection start (row,column)
		Point _selRelease = -1;	//selection end (row,column)

		protected Rectangle rText;
		protected FontExtents fe;
		protected TextExtents te;
		#endregion

		[XmlAttributeAttribute][DefaultValue("label")]
		public string Text
		{
			get {
				return buffer == null ? "" : buffer.FullText;
			}
			set
			{
				if (string.Equals (value, buffer?.FullText, StringComparison.Ordinal))
					return;

				buffer = new CodeTextBuffer (value);
				MaxScrollY = Math.Max (0, buffer.Count - visibleLines);
				MaxScrollX = Math.Max (0, buffer.longestLineCharCount - visibleColumns);

				OnTextChanged (this, null);
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
				else if (value > buffer [_currentLine].Length)
					_currentCol = buffer [_currentLine].Length;
				else
					_currentCol = value;

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
				if (value >= buffer.Count)
					_currentLine = buffer.Count-1;
				else if (value < 0)
					_currentLine = 0;
				else
					_currentLine = value;
				//force recheck of currentCol for bounding
				int cc = _currentCol;
				_currentCol = 0;
				CurrentColumn = cc;
				//System.Diagnostics.Debug.WriteLine ("Scroll:{0} visibleLines:{1} CurLine:{2}", ScrollY, visibleLines, CurrentLine);
				if (_currentLine < ScrollY)
					ScrollY = _currentLine;
				else if (_currentLine >= ScrollY + visibleLines)
					ScrollY = _currentLine - visibleLines + 1;
				NotifyValueChanged ("CurrentLine", _currentLine);
			}
		}
		[XmlIgnore]public Point CurrentPosition {
			get { return new Point(CurrentColumn, CurrentLine); }
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
				NotifyValueChanged ("SelectedText", SelectedText);
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
				NotifyValueChanged ("SelRelease", _selRelease);
				NotifyValueChanged ("SelectedText", SelectedText);
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

				if (SelRelease < 0 || SelBegin < 0)
					return "";
				if (selectionStart.Y == selectionEnd.Y)
					return buffer [selectionStart.Y].Substring (selectionStart.X, selectionEnd.X - selectionStart.X);
				string tmp = "";
				tmp = buffer [selectionStart.Y].Substring (selectionStart.X);
				for (int l = selectionStart.Y + 1; l < selectionEnd.Y; l++) {
					tmp += Interface.LineBreak + buffer [l];
				}
				tmp += Interface.LineBreak + buffer [selectionEnd.Y].Substring (0, selectionEnd.X);
				return tmp;
			}
		}
		[XmlIgnore]public bool selectionIsEmpty
		{ get { return SelRelease == SelBegin; } }

		/// <summary>
		/// Moves cursor one char to the left.
		/// </summary>
		/// <returns><c>true</c> if move succeed</returns>
		public bool MoveLeft(){
			int tmp = _currentCol - 1;
			if (tmp < 0) {
				if (_currentLine == 0)
					return false;
				CurrentLine--;
				CurrentColumn = int.MaxValue;
			} else
				CurrentColumn = tmp;
			return true;
		}
		/// <summary>
		/// Moves cursor one char to the right.
		/// </summary>
		/// <returns><c>true</c> if move succeed</returns>
		public bool MoveRight(){
			int tmp = _currentCol + 1;
			if (tmp > buffer [_currentLine].Length){
				if (CurrentLine == buffer.Count - 1)
					return false;
				CurrentLine++;
				CurrentColumn = 0;
			} else
				CurrentColumn = tmp;
			return true;
		}
		public void GotoWordStart(){
			if (buffer[CurrentLine].Length == 0)
				return;
			CurrentColumn--;
			//skip white spaces
			while (!char.IsLetterOrDigit (this.CurrentChar) && CurrentColumn > 0)
				CurrentColumn--;
			while (char.IsLetterOrDigit (buffer [CurrentLine] [CurrentColumn]) && CurrentColumn > 0)
				CurrentColumn--;
			if (!char.IsLetterOrDigit (this.CurrentChar))
				CurrentColumn++;
		}
		public void GotoWordEnd(){
			//skip white spaces
			if (CurrentColumn >= buffer [CurrentLine].Length - 1)
				return;
			while (!char.IsLetterOrDigit (this.CurrentChar) && CurrentColumn < buffer [CurrentLine].Length-1)
				CurrentColumn++;
			while (char.IsLetterOrDigit (this.CurrentChar) && CurrentColumn < buffer [CurrentLine].Length-1)
				CurrentColumn++;
			if (char.IsLetterOrDigit (this.CurrentChar))
				CurrentColumn++;
		}
		public void DeleteChar()
		{
			if (selectionIsEmpty) {
				if (CurrentColumn == 0) {
					if (CurrentLine == 0 && buffer.Count == 1)
						return;
					CurrentLine--;
					CurrentColumn = buffer [CurrentLine].Length;
					buffer [CurrentLine] += buffer [CurrentLine + 1];
					buffer.RemoveAt (CurrentLine + 1);
					OnTextChanged (this, null);
					return;
				}
				CurrentColumn--;
				buffer [CurrentLine] = buffer [CurrentLine].Remove (CurrentColumn, 1);
			} else {
				int linesToRemove = selectionEnd.Y - selectionStart.Y + 1;
				int l = selectionStart.Y;

				if (linesToRemove > 0) {
					buffer [l] = buffer [l].Remove (selectionStart.X, buffer [l].Length - selectionStart.X) +
						buffer [selectionEnd.Y].Substring (selectionEnd.X, buffer [selectionEnd.Y].Length - selectionEnd.X);
					l++;
					for (int c = 0; c < linesToRemove-1; c++)
						buffer.RemoveAt (l);
					CurrentLine = selectionStart.Y;
					CurrentColumn = selectionStart.X;
				} else
					buffer [l] = buffer [l].Remove (selectionStart.X, selectionEnd.X - selectionStart.X);
				CurrentColumn = selectionStart.X;
				SelBegin = -1;
				SelRelease = -1;
			}
			OnTextChanged (this, null);
		}

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
				return (int)Math.Ceiling(fe.Height * buffer.Count) + Margin * 2;

			return (int)(fe.MaxXAdvance * buffer.longestLineCharCount) + Margin * 2;
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

			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);
			gr.FontOptions = Interface.FontRenderingOptions;
			gr.Antialias = Interface.Antialias;

			Rectangle cb = ClientRectangle;

			Foreground.SetAsSource (gr);

			bool selectionInProgress = false;

			Foreground.SetAsSource (gr);

			#region draw text cursor
			if (SelBegin != SelRelease)
				selectionInProgress = true;
			else if (HasFocus){
				gr.LineWidth = 1.0;
				double cursorX = cb.X + (CurrentColumn - ScrollX) * fe.MaxXAdvance;
				gr.MoveTo (0.5 + cursorX, cb.Y + (CurrentLine - ScrollY) * fe.Height);
				gr.LineTo (0.5 + cursorX, cb.Y + (CurrentLine + 1 - ScrollY) * fe.Height);
				gr.Stroke();
			}
			#endregion

			for (int i = 0; i < visibleLines; i++) {
				int curL = i + ScrollY;
				if (curL >= buffer.Count)
					break;
				string lstr = buffer[curL];
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
		#endregion

		#region Mouse handling
		void updatemouseLocalPos(Point mpos){
			Point mouseLocalPos = mpos - ScreenCoordinates(Slot).TopLeft - ClientRectangle.TopLeft;
			if (mouseLocalPos.X < 0)
				CurrentColumn--;
			else
				CurrentColumn = ScrollX +  (int)Math.Round (mouseLocalPos.X / fe.MaxXAdvance);

			if (mouseLocalPos.Y < 0)
				CurrentLine--;
			else
				CurrentLine = ScrollY + (int)Math.Floor (mouseLocalPos.Y / fe.Height);
		}
		public override void onMouseEnter (object sender, MouseMoveEventArgs e)
		{
			base.onMouseEnter (sender, e);
			currentInterface.MouseCursor = XCursor.Text;
		}
		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			base.onMouseLeave (sender, e);
			currentInterface.MouseCursor = XCursor.Default;
		}
		protected override void onFocused (object sender, EventArgs e)
		{
			base.onFocused (sender, e);

			//			SelBegin = new Point(0,0);
			//			SelRelease = new Point (lines.LastOrDefault ().Length, lines.Count-1);
			RegisterForRedraw ();
		}
		protected override void onUnfocused (object sender, EventArgs e)
		{
			base.onUnfocused (sender, e);

			//			SelBegin = -1;
			//			SelRelease = -1;
			RegisterForRedraw ();
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

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
			if (this.HasFocus){
				updatemouseLocalPos (e.Position);
				SelBegin = SelRelease = CurrentPosition;
				RegisterForRedraw();//TODO:should put it in properties
			}

			//done at the end to set 'hasFocus' value after testing it
			base.onMouseDown (sender, e);
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			base.onMouseUp (sender, e);

			if (SelBegin == SelRelease)
				SelBegin = SelRelease = -1;

			updatemouseLocalPos (e.Position);
			RegisterForRedraw ();
		}
		public override void onMouseDoubleClick (object sender, MouseButtonEventArgs e)
		{
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
				Text = "";
				CurrentColumn = 0;
				SelRelease = -1;
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
				SelRelease = -1;
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
				SelRelease = -1;
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
						SelBegin = new Point(CurrentColumn, CurrentLine);
					if (e.Control)
						GotoWordStart ();
					else if (!MoveLeft ())
						return;
					SelRelease = CurrentPosition;
					break;
				}
				SelRelease = -1;
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
				SelRelease = -1;
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
				SelRelease = -1;
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
				SelRelease = -1;
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
				try {
					CrowEdit.XMLParser parser = new CrowEdit.XMLParser (buffer);
					parser.Parse ();
				}catch(Exception ee){
					Debug.WriteLine (ee.ToString ());
				}
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


		/// <summary> Compute x offset in cairo unit from text position </summary>
		double GetXFromTextPointer(Context gr, Point pos)
		{
			try {
				string l = buffer [pos.Y].Substring (0, pos.X).
					Replace ("\t", new String (' ', Interface.TabSize));
				return gr.TextExtents (l).XAdvance;
			} catch{
				return -1;
			}
		}

		void updateVisibleLines(){
			visibleLines = (int)Math.Floor ((double)ClientRectangle.Height / fe.Height);
			MaxScrollY = Math.Max (0, buffer.Count - visibleLines);

			System.Diagnostics.Debug.WriteLine ("update visible lines: " + visibleLines);
			System.Diagnostics.Debug.WriteLine ("update MaxScrollY: " + MaxScrollY);
		}
		void updateVisibleColumns(){
			visibleColumns = (int)Math.Floor ((double)ClientRectangle.Width / fe.MaxXAdvance);
			MaxScrollX = Math.Max (0, buffer.longestLineCharCount - visibleColumns);

			System.Diagnostics.Debug.WriteLine ("update visible columns: " + visibleColumns);
			System.Diagnostics.Debug.WriteLine ("update MaxScrollX: " + MaxScrollX);
		}



		/// <summary>
		/// Insert new string at caret position, should be sure no line break is inside.
		/// </summary>
		/// <param name="str">String.</param>
		protected void Insert(string str)
		{
			if (!selectionIsEmpty)
				this.DeleteChar ();
			string[] strLines = Regex.Split (str, "\r\n|\r|\n|" + @"\\n").ToArray();
			buffer [CurrentLine] = buffer [CurrentLine].Insert (CurrentColumn, strLines[0]);
			CurrentColumn += strLines[0].Length;
			for (int i = 1; i < strLines.Length; i++) {
				InsertLineBreak ();
				buffer [CurrentLine] = buffer [CurrentLine].Insert (CurrentColumn, strLines[i]);
				CurrentColumn += strLines[i].Length;
			}
			OnTextChanged (this, null);
			RegisterForGraphicUpdate();
		}

		/// <summary>
		/// Insert a line break.
		/// </summary>
		protected void InsertLineBreak()
		{
			buffer.Insert(CurrentLine + 1, buffer[CurrentLine].Substring(CurrentColumn));
			buffer [CurrentLine] = buffer [CurrentLine].Substring (0, CurrentColumn);
			CurrentLine++;
			CurrentColumn = 0;
			OnTextChanged (this, null);
		}
	}
}