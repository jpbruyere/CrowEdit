// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Glfw;
using Crow.Text;
using System.Collections.Generic;
using Crow.Drawing;
using System.Linq;
using CrowEditBase;
using System.Threading;
using System.ComponentModel;
using static CrowEditBase.CrowEditBase;

namespace Crow
{
	public interface IDocumentClient {

	}
	public class Editor : ScrollingObject, IEditableTextWidget {
		#region CTOR
		protected Editor () : base () {
			KeyEventsOverrides = true;//prevent scrollingObject moves by keyboard

			initCommands ();

			Thread t = new Thread (backgroundThreadFunc);
			t.IsBackground = true;
			t.Start ();
		}
		#endregion
		TextDocument document;
		protected bool disableTextChangedEvent;

		public Command CMDCut, CMDCopy, CMDPaste;
		void initCommands () {
			CMDCut = new ActionCommand ("Cut", Cut, "#icons.scissors.svg",  false);
			CMDCopy = new ActionCommand ("Copy", Copy, "#icons.copy-file.svg",  false);
			CMDPaste = new ActionCommand ("Paste", Paste, "#icons.paste-on-document.svg",  true);

			ContextCommands = new CommandGroup (CMDCut, CMDCopy, CMDPaste);
		}

		public TextDocument Document {
			get => document;
			set {
				if (document == value)
					return;

				document?.UnregisterClient (this);
				document = value;
				document?.RegisterClient (this);

				NotifyValueChangedAuto (document);
				RegisterForGraphicUpdate ();
			}
		}
		public event EventHandler<TextChangeEventArgs> TextChanged;
		public void OnTextChanged(object sender, TextChangeEventArgs e)
		{
			if (disableTextChangedEvent)
				return;
			TextChanged.Raise (this, e);
		}
		protected void backgroundThreadFunc () {
			while (true) {
				if (Document != null && document.TryGetState (this, out List<TextChange> changes)) {
					disableTextChangedEvent = true;
					foreach (TextChange tc in changes)
						update (tc);
					disableTextChangedEvent = false;
				}
				Thread.Sleep (200);
			}
		}

		#region Label

		int targetColumn = -1;//handle line changes with long->short->long line length sequence.

		protected CharLocation? hoverLoc = null;
		protected CharLocation? currentLoc = null;
		protected CharLocation? selectionStart = null;  //selection start (row,column)

		protected virtual CharLocation? CurrentLoc {
			get => currentLoc;
			set {
				if (currentLoc == value)
					return;
				currentLoc = value;
				NotifyValueChanged ("CurrentLine", CurrentLine);
				NotifyValueChanged ("CurrentColumn", CurrentColumn);

				CMDCopy.CanExecute = CMDCut.CanExecute = !SelectionIsEmpty;
			}
		}
		public int CurrentLine {
			get => currentLoc.HasValue ? currentLoc.Value.Line : 0;
			set {
				if (currentLoc?.Line == value)
					return;
				currentLoc = new CharLocation (value, currentLoc.Value.Column, currentLoc.Value.VisualCharXPosition);
				NotifyValueChanged ("CurrentLine", CurrentLine);

				CMDCopy.CanExecute = CMDCut.CanExecute = !SelectionIsEmpty;
			}
		}
		public int CurrentColumn {
			get => currentLoc.HasValue ? currentLoc.Value.Column < 0 ? 0 : currentLoc.Value.Column : 0;
			set {
				if (CurrentColumn == value)
					return;
				currentLoc = new CharLocation (currentLoc.Value.Line, value);
				NotifyValueChanged ("CurrentColumn", CurrentColumn);

				CMDCopy.CanExecute = CMDCut.CanExecute = !SelectionIsEmpty;
			}
		}
		/// <summary>
		/// Set current cursor position in label.
		/// </summary>
		/// <param name="position">Absolute character position in text.</param>
		public void SetCursorPosition (int position) {
			CharLocation loc = document.GetLocation (position);
			loc.Column = Math.Min (loc.Column, document.GetLine (loc.Line).Length);
			CurrentLoc = loc;
		}

		Color selForeground, selBackground;

		protected bool textMeasureIsUpToDate = false;
		//protected object linesMutex = new object ();

		protected Size cachedTextSize = default (Size);


		protected FontExtents fe;
		protected TextExtents te;


		/// <summary>
		/// Background color for selected text inside this label.
		/// </summary>
		[DefaultValue ("LightSteelBlue")]
		public virtual Color SelectionBackground {
			get { return selBackground; }
			set {
				if (selBackground == value)
					return;
				selBackground = value;
				NotifyValueChangedAuto (selBackground);
				RegisterForRedraw ();
			}
		}
		/// <summary>
		/// Selected text color inside this label.
		/// </summary>
		[DefaultValue("White")]
		public virtual Color SelectionForeground {
			get { return selForeground; }
			set {
				if (selForeground == value)
					return;
				selForeground = value;
				NotifyValueChangedAuto (selForeground);
				RegisterForRedraw ();
			}
		}

		protected double lineHeight => fe.Ascent + fe.Descent;

		protected virtual int getAbsoluteLineIndexFromVisualLineMove (int startLine, int visualLineDiff)
			=> Math.Min (Math.Max (0, startLine + visualLineDiff), visualLineCount - 1);
		/// <summary>
		/// Moves cursor one char to the left.
		/// </summary>
		/// <returns><c>true</c> if move succeed</returns>
		public bool MoveLeft(){
			//targetColumn = -1;
			CharLocation loc = CurrentLoc.Value;
			if (loc.Column == 0) {
				if (loc.Line == 0)
					return false;
				int newLine = getAbsoluteLineIndexFromVisualLineMove (loc.Line, -1);
				CurrentLoc = new CharLocation (newLine, document.GetLine (newLine).Length);
			}else
				CurrentLoc = new CharLocation (loc.Line, loc.Column - 1);
			return true;
		}
		public bool MoveRight () {
			targetColumn = -1;
			CharLocation loc = CurrentLoc.Value;
			if (loc.Column == document.GetLine (loc.Line).Length) {
				if (loc.Line == document.LinesCount - 1)
					return false;
				CurrentLoc = new CharLocation (
					getAbsoluteLineIndexFromVisualLineMove (loc.Line, 1), 0);
			} else
				CurrentLoc = new CharLocation (loc.Line, loc.Column + 1);
			return true;
		}
		public bool LineMove (int lineDiff) {
			CharLocation loc = CurrentLoc.Value;
			int newLine = getAbsoluteLineIndexFromVisualLineMove (loc.Line, lineDiff);

			if (newLine == loc.Line)
				return false;

			if (loc.Column > document.GetLine (newLine).Length) {
				if (targetColumn < 0)
					targetColumn = loc.Column;
				CurrentLoc = new CharLocation (newLine, document.GetLine (newLine).Length);
			} else if (targetColumn < 0)
				CurrentLoc = new CharLocation (newLine, loc.Column);
			else if (targetColumn > document.GetLine (newLine).Length)
				CurrentLoc = new CharLocation (newLine, document.GetLine (newLine).Length);
			else
				CurrentLoc = new CharLocation (newLine, targetColumn);

			return true;
		}

		/// <summary>
		/// Current Selected text span. May be used to set current position, or current selection.
		/// </summary>
		public TextSpan Selection {
			set {
				if (value.IsEmpty)
					selectionStart = null;
				else
					selectionStart = document.GetLocation (value.Start);
				CurrentLoc = document.GetLocation (value.End);
			}
			get {
				if (CurrentLoc == null)
					return default;
				CharLocation selStart = CurrentLoc.Value, selEnd = CurrentLoc.Value;
				if (selectionStart.HasValue) {
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
				return new TextSpan (document.GetAbsolutePosition (selStart), document.GetAbsolutePosition (selEnd));
			}
		}
		public string SelectedText {
			get {
				TextSpan selection = Selection;
				return selection.IsEmpty ? "" : document.GetText (selection).ToString ();
			}
		}
		public bool SelectionIsEmpty => selectionStart.HasValue ? Selection.IsEmpty : true;
		/// <summary>
		/// on screen visible line bounded by the client rectangle
		/// </summary>
		protected int visibleLines => (int)((double)ClientRectangle.Height / lineHeight);
		/// <summary>
		/// total line count
		/// </summary>
		protected virtual int visualLineCount => document.LinesCount;

		protected virtual void measureTextBounds (Context gr) {
			fe = gr.FontExtents;
			te = new TextExtents ();

			document.EnterReadLock();
			try {

				cachedTextSize.Height = (int)Math.Ceiling (lineHeight * Math.Max (1, visualLineCount));

				TextExtents tmp = default;
				int longestLine = 0;
				for (int i = 0; i < document.LinesCount; i++) {
					TextLine l = document.GetLine (i);
					if (l.LengthInPixel < 0) {
						if (l.Length == 0)
							l.LengthInPixel = 0;// (int)Math.Ceiling (fe.MaxXAdvance);
						else {
							gr.TextExtents (document.GetText (l), App.TabulationSize, out tmp);
							l.LengthInPixel = (int)Math.Ceiling (tmp.XAdvance);
						}
					}
					if (l.LengthInPixel > document.GetLine (longestLine).LengthInPixel)
						longestLine = i;
				}
				cachedTextSize.Width = document.GetLine (longestLine).LengthInPixel;
				textMeasureIsUpToDate = true;

				updateMaxScrolls (LayoutingType.Height);
				updateMaxScrolls (LayoutingType.Width);
			} finally {
				document.ExitReadLock ();
			}
		}
		protected virtual void drawContent (Context gr) {
			gr.Translate (-ScrollX, -ScrollY);

			Rectangle cb = ClientRectangle;
			fe = gr.FontExtents;
			double lineHeight = fe.Ascent + fe.Descent;

			CharLocation selStart = default, selEnd = default;
			bool selectionNotEmpty = false;

			document.EnterReadLock();
			try {
				//if (HasFocus) {
					if (currentLoc?.Column < 0) {
						updateLocation (gr, cb.Width, ref currentLoc);
						NotifyValueChanged ("CurrentColumn", CurrentColumn);
					} else
						updateLocation (gr, cb.Width, ref currentLoc);
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
				//}

				if (document.Lenght > 0) {
					Foreground?.SetAsSource (IFace, gr);

					TextExtents extents;
					Span<byte> bytes = stackalloc byte[128];
					double y = 0;

					for (int i = 0; i < document.LinesCount; i++) {
						if (!cancelLinePrint (lineHeight, y, cb.Height)) {
							int encodedBytes = -1;
							TextLine l = document.GetLine (i);
							if (l.Length > 0) {
								int size = l.Length * 4 + 1;
								if (bytes.Length < size)
									bytes = size > 512 ? new byte[size] : stackalloc byte[size];

								encodedBytes = Crow.Text.Encoding.ToUtf8 (document.GetText (l), bytes);
								bytes[encodedBytes++] = 0;

								if (l.LengthInPixel < 0) {
									gr.TextExtents (bytes.Slice (0, encodedBytes), out extents);
									l.LengthInPixel = (int)extents.XAdvance;
								}
							}

							RectangleD lineRect = new RectangleD (
								(int)cb.X,
								y + cb.Top, l.LengthInPixel, lineHeight);

							if (encodedBytes > 0) {
								gr.MoveTo (lineRect.X, lineRect.Y + fe.Ascent);
								gr.ShowText (bytes.Slice (0, encodedBytes));
							}
							/********** DEBUG TextLineCollection *************
							gr.SetSource (Colors.Red);
							gr.SetFontSize (9);
							gr.MoveTo (700, lineRect.Y + fe.Ascent);
							gr.ShowText ($"({lines[i].Start}, {lines[i].End}, {lines[i].EndIncludingLineBreak})");
							gr.SetFontSize (Font.Size);
							Foreground.SetAsSource (IFace, gr);
							********** DEBUG TextLineCollection *************/

							if (selectionNotEmpty) {
								RectangleD selRect = lineRect;

								if (i >= selStart.Line && i <= selEnd.Line) {
									if (selStart.Line == selEnd.Line) {
										selRect.X = selStart.VisualCharXPosition + cb.X;
										selRect.Width = selEnd.VisualCharXPosition - selStart.VisualCharXPosition;
									} else if (i == selStart.Line) {
										double newX = selStart.VisualCharXPosition + cb.X;
										selRect.Width -= (newX - selRect.X) - 10.0;
										selRect.X = newX;
									} else if (i == selEnd.Line) {
										selRect.Width = selEnd.VisualCharXPosition - selRect.X + cb.X;
									} else
										selRect.Width += 10.0;
								} else {
									y += lineHeight;
									continue;
								}

								gr.SetSource (selBackground);
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
						y += lineHeight;
					}
				}
			} finally {
				document.ExitReadLock ();
			}

			gr.Translate (ScrollX, ScrollY);
		}
		protected int getLineIndexFromMousePosition (Point mouseLocalPos) =>
			(int)Math.Min (Math.Max (0, Math.Floor ((mouseLocalPos.Y + ScrollY)/ lineHeight)), visualLineCount - 1);
		protected int getVisualLineIndex (Point mouseLocalPos) =>
			(int)Math.Min (Math.Max (0, Math.Floor (mouseLocalPos.Y / lineHeight)), visibleLines - 1);
		protected virtual int visualCurrentLine => CurrentLoc.HasValue ? CurrentLoc.Value.Line : 0;

		protected virtual void updateHoverLocation (Point mouseLocalPos) {
			int hoverLine = getLineIndexFromMousePosition (mouseLocalPos);
			NotifyValueChanged("MouseY", mouseLocalPos.Y + ScrollY);
			NotifyValueChanged("ScrollY", ScrollY);
			NotifyValueChanged("VisibleLines", visibleLines);
			NotifyValueChanged("HoverLine", hoverLine);
			hoverLoc = new CharLocation (hoverLine, -1, mouseLocalPos.X + ScrollX);
			using (Context gr = new Context (IFace.surf)) {
				setFontForContext (gr);
				updateLocation (gr, ClientRectangle.Width, ref hoverLoc);
			}
		}
		protected virtual bool cancelLinePrint (double lineHeght, double y, int clientHeight) => false;
		RectangleD? textCursor = null;

		public virtual bool DrawCursor (Context ctx, out Rectangle rect) {
			if (CurrentLoc == null) {
				rect = default;
				return false;
			}
			if (!CurrentLoc.Value.HasVisualX) {
				setFontForContext (ctx);

				if (currentLoc?.Column < 0) {
					updateLocation (ctx, ClientRectangle.Width, ref currentLoc);
					NotifyValueChanged ("CurrentColumn", CurrentColumn);
				} else
					updateLocation (ctx, ClientRectangle.Width, ref currentLoc);

				textCursor = null;
			}

			textCursor = computeTextCursor (new RectangleD (CurrentLoc.Value.VisualCharXPosition, lineHeight * visualCurrentLine, 1.0, lineHeight));

			if (textCursor == null) {
				rect = default;
				return false;
			}
			//}

			Rectangle c = ScreenCoordinates (textCursor.Value + Slot.Position + ClientRectangle.Position);
			ctx.ResetClip ();
			Foreground.SetAsSource (IFace, ctx, c);
			ctx.LineWidth = 1.0;
			ctx.MoveTo (0.5 + c.X, c.Y);
			ctx.LineTo (0.5 + c.X, c.Bottom);
			ctx.Stroke ();
			rect = c;
			return true;
		}

		protected void updateLocation (Context gr, int clientWidth, ref CharLocation? location) {
			if (location == null)
				return;
			CharLocation loc = location.Value;
			//Console.WriteLine ($"updateLocation: {loc} text:{_text.Length}");
			if (loc.HasVisualX)
				return;
			TextLine ls = document.GetLine (loc.Line);
			ReadOnlySpan<char> curLine = document.GetText (ls);
			double cPos = 0;

			if (loc.Column >= 0) {
				//int encodedBytes = Crow.Text.Encoding2.ToUtf8 (curLine.Slice (0, loc.Column), bytes);
#if DEBUG
				if (loc.Column > curLine.Length) {
					System.Diagnostics.Debug.WriteLine ($"loc.Column: {loc.Column} curLine.Length:{curLine.Length}");
					loc.Column = curLine.Length;
				}
#endif
				loc.VisualCharXPosition = gr.TextExtents (curLine.Slice (0, loc.Column), App.TabulationSize).XAdvance + cPos;
				location = loc;
			} else {
				TextExtents te;
				Span<byte> bytes = stackalloc byte[5];//utf8 single char buffer + '\0'

				for (int i = 0; i < ls.Length; i++) {
					int encodedBytes = Crow.Text.Encoding.ToUtf8 (curLine.Slice (i, 1), bytes);
					bytes[encodedBytes] = 0;

					gr.TextExtents (bytes, out te);
					double halfWidth = te.XAdvance / 2;

					if (loc.VisualCharXPosition <= cPos + halfWidth) {
						loc.Column = i;
						loc.VisualCharXPosition = cPos;
						location = loc;
						return;
					}

					cPos += te.XAdvance;
				}
				loc.Column = ls.Length;
				loc.VisualCharXPosition = cPos;
				location = loc;
			}
		}

		protected void checkShift (KeyEventArgs e) {
			if (e.Modifiers.HasFlag (Modifier.Shift)) {
				if (!selectionStart.HasValue)
					selectionStart = CurrentLoc;
			} else
				selectionStart = null;
		}

		#region GraphicObject overrides
		public override void OnLayoutChanges (LayoutingType layoutType) {
			base.OnLayoutChanges (layoutType);
			updateMaxScrolls (layoutType);
		}
		/*public override bool UpdateLayout (LayoutingType layoutType) {
			if ((LayoutingType.Sizing | layoutType) != LayoutingType.None) {
				if (!System.Threading.Monitor.TryEnter (linesMutex))
					return false;
			}
			try {
				bool result = base.UpdateLayout (layoutType);
				return result;
			} finally {
				System.Threading.Monitor.Exit (linesMutex);
			}
		}*/
		public override int measureRawSize(LayoutingType lt)
		{
			DbgLogger.StartEvent(DbgEvtType.GOMeasure, this, lt);
			try {
				if (!textMeasureIsUpToDate) {
					using (Context gr = new Context (IFace.surf)) {
						setFontForContext (gr);
						measureTextBounds (gr);
					}
				}
				return Margin * 2 + (lt == LayoutingType.Height ? cachedTextSize.Height : cachedTextSize.Width);
			} finally {
				DbgLogger.EndEvent(DbgEvtType.GOMeasure);
			}
		}
		public override void Paint (Context ctx) {
			base.Paint (ctx);
			IFace.forceTextCursor = true;
		}
		protected override void onDraw (Context gr)
		{
			//base.onDraw (gr);

			setFontForContext (gr);

			if (!textMeasureIsUpToDate) {
				measureTextBounds (gr);
			}

			if (ClipToClientRect) {
				gr.Save ();
				CairoHelpers.CairoRectangle (gr, ClientRectangle, CornerRadius);
				gr.Clip ();
			}

			drawContent (gr);

			if (ClipToClientRect)
				gr.Restore ();
		}
		#endregion

		#region Mouse handling
		protected override void onFocused (object sender, EventArgs e)
		{
			base.onFocused (sender, e);

			if (CurrentLoc == null)
				CurrentLoc = new CharLocation (0, 0);

			RegisterForRedraw ();

			(IFace as CrowEditBase.CrowEditBase).CurrentEditor = this;
		}
		public override void onMouseEnter (object sender, MouseMoveEventArgs e) {
			base.onMouseEnter (sender, e);
			if (!Focusable)
				return;
			HasFocus = true;
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);
			mouseMove (e);
		}
		public override void onMouseWheel(object sender, MouseWheelEventArgs e)
		{
			base.onMouseWheel(sender, e);
			mouseMove (e);
		}
		protected virtual void mouseMove (MouseEventArgs e) {
			updateHoverLocation (ScreenPointToLocal (e.Position));

			if (HasFocus && IFace.IsDown (MouseButton.Left)) {
				CurrentLoc = hoverLoc;
				autoAdjustScroll = true;
				IFace.forceTextCursor = true;
				RegisterForRedraw ();
			}
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			if (!e.Handled && e.Button == Glfw.MouseButton.Left) {
				targetColumn = -1;
				if (HasFocus) {
					if (!IFace.Shift)
						selectionStart = hoverLoc;
					else if (!selectionStart.HasValue)
						selectionStart = CurrentLoc;
					CurrentLoc = hoverLoc;
					IFace.forceTextCursor = true;
					RegisterForRedraw ();
					e.Handled = true;
				}
			}
			base.onMouseDown (sender, e);

			//done at the end to set 'hasFocus' value after testing it
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			base.onMouseUp (sender, e);
			if (e.Button != MouseButton.Left || !HasFocus || !selectionStart.HasValue)
				return;
			if (selectionStart.Value == CurrentLoc.Value)
				selectionStart = null;
		}
		public override void onMouseDoubleClick (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDoubleClick (sender, e);
			if (e.Button != MouseButton.Left || !HasFocus)
				return;

			selectionStart = document.GetWordStart (CurrentLoc.Value);
			CurrentLoc = document.GetWordEnd (CurrentLoc.Value);
			RegisterForRedraw ();
		}
		#endregion

		#region Keyboard handling
		public override void onKeyPress (object sender, KeyPressEventArgs e) {
			base.onKeyPress (sender, e);

			TextSpan selection = Selection;
			update (new TextChange (selection.Start, selection.Length, e.KeyChar.ToString ()));

			/*Insert (e.KeyChar.ToString());

			SelRelease = -1;
			SelBegin = new Point(CurrentColumn, SelBegin.Y);

			RegisterForGraphicUpdate();*/
		}
		public override void onKeyDown (object sender, KeyEventArgs e) {
			Key key = e.Key;
			TextSpan selection = Selection;

			/*document.EnterReadLock();
			try {*/
				switch (key) {
				case Key.Backspace:
					if (selection.IsEmpty) {
						if (selection.Start == 0)
							return;
						if (CurrentLoc.Value.Column == 0) {
							int lbLength = document.GetLine (CurrentLoc.Value.Line - 1).LineBreakLength;
							update (new TextChange (selection.Start - lbLength, lbLength, ""));
						}else
							update (new TextChange (selection.Start - 1, 1, ""));
					} else
						update (new TextChange (selection.Start, selection.Length, ""));
					break;
				case Key.Delete:
					if (selection.IsEmpty) {
						if (selection.Start == document.Lenght)
							return;
						if (CurrentLoc.Value.Column >= document.GetLine (CurrentLoc.Value.Line).Length)
							update (new TextChange (selection.Start, document.GetLine (CurrentLoc.Value.Line).LineBreakLength, ""));
						else
							update (new TextChange (selection.Start, 1, ""));
					} else {
						if (e.Modifiers == Modifier.Shift)
							IFace.Clipboard = SelectedText;
						update (new TextChange (selection.Start, selection.Length, ""));
					}
					break;
				case Key.Insert:
					if (e.Modifiers.HasFlag (Modifier.Shift))
						Paste ();
					else if (e.Modifiers.HasFlag (Modifier.Control))
						Copy ();
					break;
				case Key.KeypadEnter:
				case Key.Enter:
					update (new TextChange (selection.Start, selection.Length, document.GetLineBreak ()));
					break;
				case Key.Escape:
					selectionStart = null;
					CurrentLoc = document.GetLocation (selection.Start);
					RegisterForRedraw ();
					break;
				case Key.Tab:
					update (new TextChange (selection.Start, selection.Length, App.IndentWithSpace ? new string(' ', App.TabulationSize) : "\t"));
					break;
				case Key.PageUp:
					checkShift (e);
					LineMove (-visibleLines);
					RegisterForRedraw ();
					break;
				case Key.PageDown:
					checkShift (e);
					LineMove (visibleLines);
					RegisterForRedraw ();
					break;
				case Key.Home:
					targetColumn = -1;
					checkShift (e);
					if (e.Modifiers.HasFlag (Modifier.Control))
						CurrentLoc = new CharLocation (0, 0);
					else
						CurrentLoc = new CharLocation (CurrentLoc.Value.Line, 0);
					RegisterForRedraw ();
					break;
				case Key.End:
					checkShift (e);
					int l = e.Modifiers.HasFlag (Modifier.Control) ? document.LinesCount - 1 : CurrentLoc.Value.Line;
					CurrentLoc = new CharLocation (l, document.GetLine (l).Length);
					RegisterForRedraw ();
					break;
				case Key.Left:
					checkShift (e);
					if (e.Modifiers.HasFlag (Modifier.Control))
						CurrentLoc = document.GetWordStart (CurrentLoc.Value);
					else
						MoveLeft ();
					RegisterForRedraw ();
					break;
				case Key.Right:
					checkShift (e);
					if (e.Modifiers.HasFlag (Modifier.Control))
						CurrentLoc = document.GetWordEnd (CurrentLoc.Value);
					else
						MoveRight ();
					RegisterForRedraw ();
					break;
				case Key.Up:
					checkShift (e);
					LineMove (-1);
					RegisterForRedraw ();
					break;
				case Key.Down:
					checkShift (e);
					LineMove (1);
					RegisterForRedraw ();
					break;
				case Key.A:
					if (e.Modifiers.HasFlag (Modifier.Control)) {
						selectionStart = new CharLocation (0, 0);
						CurrentLoc = document.EndLocation;
					}
					break;
				default:
					base.onKeyDown (sender, e);
					return;
				}
				autoAdjustScroll = true;
				IFace.forceTextCursor = true;
				e.Handled = true;
			/*} finally {
				document.ExitReadLock ();
			}*/
		}
		#endregion
		#endregion


		#region textBox
		protected bool autoAdjustScroll = false;//if scrollXY is changed directly, dont try adjust scroll to cursor
		protected virtual RectangleD? computeTextCursor (Rectangle cursor) {
			Rectangle cb = ClientRectangle;
			cursor -= new Point (ScrollX, ScrollY);

			if (autoAdjustScroll) {
				autoAdjustScroll = false;
				int goodMsrs = 0;
				if (cursor.Left < 0)
					ScrollX += cursor.Left;
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

		protected virtual void updateMaxScrolls (LayoutingType layout) {
			Rectangle cb = ClientRectangle;
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
		public virtual void Cut () {
			TextSpan selection = Selection;
			if (selection.IsEmpty)
				return;
			IFace.Clipboard = SelectedText;
			update (new TextChange (selection.Start, selection.Length, ""));
		}
		public virtual void Copy () {
			TextSpan selection = Selection;
			if (selection.IsEmpty)
				return;
			IFace.Clipboard = SelectedText;
		}
		public virtual void Paste () {
			TextSpan selection = Selection;
			update (new TextChange (selection.Start, selection.Length, IFace.Clipboard));
		}

		protected virtual void update (TextChange change) {

			OnTextChanged (this, new TextChangeEventArgs (change));

			selectionStart = null;
			CurrentLoc = document.GetLocation (change.Start + change.ChangedText.Length);

			textMeasureIsUpToDate = false;
			IFace.forceTextCursor = true;
			autoAdjustScroll = true;

			RegisterForGraphicUpdate ();
		}

		#endregion
	}
}