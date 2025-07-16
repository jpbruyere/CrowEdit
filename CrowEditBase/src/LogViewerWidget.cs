// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Collections;
using Drawing2D;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using CrowEditBase;
using System.Diagnostics;

namespace Crow
{
	public enum LogLevel
	{
		Minimal, Normal, Full, Debug
	}
	[Flags]
	public enum LogType {
		None		= 0,
		Low			= 0x0001,
		Normal		= 0x0002,
		High		= 0x0004,
		Message		= Low | Normal | High,
		Debug		= 0x0008,
		Warning		= 0x0010,
		Error		= 0x0020,
		WarnErr		= Warning | Error,
		Custom1		= 0x0040,
		Custom2		= 0x0080,
		Custom3		= 0x0100,
		code		= 0x1000,
		crowEdit	= 0x2000,
		Plugin		= 0x4000,

		
		Custom		= Custom1 | Custom2 | Custom3,
		all			= 0xffff
	}
	public class LogEntry {
		public LogType Type;
		public string msg;
		public LogEntry (LogType type, string message) {
			Type = type;
			msg = message;
		}
		public override string ToString() => msg;
	}
	public class LogViewerWidget : ScrollingObject
	{
		LogItem logger;
		IEnumerable<LogEntry> filteredLines;
		object filteredLinesMutex = new object ();
		bool updateFilteredLinesRequest = true;
		bool scrollOnOutput, caseSensitiveSearch, allWordSearch;
		int visibleLines = 1;
		FontExtents fe;

		int hoverEntryIdx = -1, curEntryIdx;
		string searchString;
		LogType filter;
		public CommandGroup SearchCommands => new CommandGroup(
			new ActionCommand("Prev", () => performSearch(searchString, true, true)),
			new ActionCommand("Next", () => performSearch(searchString, true))
		);

		[DefaultValue(true)]
		public virtual bool ScrollOnOutput {
			get => scrollOnOutput;
			set {
				if (scrollOnOutput == value)
					return;
				scrollOnOutput = value;
				NotifyValueChanged ("ScrollOnOutput", scrollOnOutput);
			}
		}
		public LogItem Logger {
			get => logger;
			set {
				if (logger == value)
					return;
				if (logger != null) {
					lock(logger.LogMutext) {
						logger.log.ListAdd -= Lines_ListAdd;
						logger.log.ListRemove -= Lines_ListRemove;
						logger.log.ListClear -= Lines_ListClear;
					}
				}
				logger = value;
				if (logger != null) {
					lock(logger.LogMutext) {
						logger.log.ListAdd += Lines_ListAdd;
						logger.log.ListRemove += Lines_ListRemove;
						logger.log.ListClear += Lines_ListClear;
					}
					updateFilteredLinesRequest = true;
				}
				NotifyValueChanged("Logger", logger);
				if (IsVisible)
					RegisterForRedraw();
			}
		}
		[DefaultValue(LogType.all)]
		public LogType Filter {
			get => filter;
			set {
				if (filter == value)
					return;
				filter = value;
				NotifyValueChangedAuto (filter);
				updateFilteredLines ();
				RegisterForRedraw ();
			}
		}
		public int CurrentEntryIndex {
			get => curEntryIdx;
			set {
				if (curEntryIdx == value)
					return;
				curEntryIdx = value;
				NotifyValueChangedAuto (curEntryIdx);
				if (curEntryIdx >= 0) {
					if (curEntryIdx < ScrollY || (curEntryIdx > ScrollY + visibleLines))
						ScrollY = curEntryIdx - visibleLines / 2;
				}
				RegisterForRedraw();
			}
		}
		
		void updateFilteredLines () {
			if (logger != null) {
				lock (filteredLinesMutex) {
					//Console.WriteLine("updatefilteredlines");
					lock (logger.LogMutext)
						filteredLines = logger.log.Where (l=>((int)l.Type & (int)filter) > 0).ToArray();
					int count = filteredLines == null ? 0 : filteredLines.Count();
					MaxScrollY = count - visibleLines;
					NotifyValueChanged ("ChildHeightRatio", Math.Min (1.0, (double)visibleLines / count));
				}
				if (scrollOnOutput)
					ScrollY = MaxScrollY;
			}
			updateFilteredLinesRequest = false;
		}
		void updateHoverEntryIdx (Point mpos) {
			PointD mouseLocalPos = ScreenPointToLocal (mpos);
			lock (filteredLinesMutex) {
				if (filteredLines == null) {
					hoverEntryIdx = -1;
					return;
				}
				hoverEntryIdx = ScrollY + (int)Math.Min (Math.Max (0, Math.Floor (mouseLocalPos.Y / fe.Height)), filteredLines.Count() - 1);
			}
			RegisterForRedraw ();
		}
		
		

		#region Searching
		[DefaultValue (true)]
		public virtual bool CaseSensitiveSearch {
			get { return caseSensitiveSearch; }
			set {
				if (caseSensitiveSearch == value)
					return;
				caseSensitiveSearch = value;
				NotifyValueChanged ("CaseSensitiveSearch", caseSensitiveSearch);

			}
		}
		[DefaultValue (false)]
		public virtual bool AllWordSearch {
			get { return allWordSearch; }
			set {
				if (allWordSearch == value)
					return;
				allWordSearch = value;
				NotifyValueChangedAuto (allWordSearch);
			}
		}
		public string SearchString {
			get => searchString;
			set {
				if (searchString == value)
					return;
				searchString = value;
				NotifyValueChanged ("SearchString", searchString);

				Task.Run (() => performSearch (searchString));
			}
		}
		private void onSearch (object sender, KeyEventArgs e) {
			if (e.Key == Glfw.Key.Enter)
				performSearch (SearchString, true);
        }
		void performSearchBackward (LogEntry[] entries, string str, bool next = false) {
			int idx = CurrentEntryIndex < 0 ? entries.Length - 1 : next ? CurrentEntryIndex - 1 : CurrentEntryIndex;
			while (idx >= 0) {
				if (entries[idx].msg.Contains (str, CaseSensitiveSearch ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)) {
					CurrentEntryIndex = idx;
					return;
                }
				idx--;
            }
			if (CurrentEntryIndex <= 0)//all the list has been searched
				return;
			idx = entries.Length - 1;
			while (idx > CurrentEntryIndex) {
				if (entries[idx].msg.Contains (str, CaseSensitiveSearch ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)) {
					CurrentEntryIndex = idx;
					return;
				}
				idx--;
			}
		}
		void performSearchForward (LogEntry[] entries, string str, bool next = false) {
			int idx = CurrentEntryIndex < 0 ? 0 : next ? CurrentEntryIndex + 1 : CurrentEntryIndex;
			while (idx < entries.Length) {
				if (entries[idx].msg.Contains (str, CaseSensitiveSearch ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)) {
					CurrentEntryIndex = idx;
					return;
                }
				idx++;
            }
			if (CurrentEntryIndex <= 0)//all the list has been searched
				return;
			idx = 0;
			while (idx < CurrentEntryIndex) {
				if (entries[idx].msg.Contains (str, CaseSensitiveSearch ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)) {
					CurrentEntryIndex = idx;
					return;
				}
				idx++;
			}
		}
		void performSearch (string str, bool next = false, bool backward = false) {
			if (string.IsNullOrEmpty (str) || filteredLines == null)
				return;
			LogEntry[] entries;
			lock(filteredLinesMutex)
				entries = filteredLines.ToArray ();
			if (entries.Length == 0) {
				CurrentEntryIndex = -1;
				return;
            }
			if (backward)
				performSearchBackward (entries, str, next);
			else
				performSearchForward (entries, str, next);
		}
		#endregion

		void Lines_ListAdd (object sender, ListChangedEventArg e)
		{
			updateFilteredLinesRequest = true;
			if (IsVisible)
				RegisterForRedraw();
		}
		void Lines_ListRemove (object sender, ListChangedEventArg e)
		{
			updateFilteredLinesRequest = true;
			RegisterForRedraw ();
		}
		void Lines_ListClear (object sender, ListClearEventArg e) {
			lock (filteredLinesMutex)
				filteredLines = null;
			MaxScrollX = ScrollY = 0;
			RegisterForRedraw ();
		}

		public Stopwatch perf = new Stopwatch ();

		#region widget overrides
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);

			if (layoutType == LayoutingType.Height) {
				using (IContext gr = IFace.Backend.CreateContext (IFace.MainSurface)) {
					gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
					gr.SetFontSize (Font.Size);
					fe = gr.FontExtents;
				}

				visibleLines = (int)Math.Floor ((double)ClientRectangle.Height / fe.Height);

				if (updateFilteredLinesRequest)
					updateFilteredLines ();
				else {
					int count = 0;
					lock (filteredLinesMutex) 
						count = filteredLines == null ? 0 : filteredLines.Count();
					MaxScrollY = count - visibleLines;
					NotifyValueChanged ("ChildHeightRatio", Math.Min (1.0, (double)visibleLines / count));
				}
			}
		}
		protected override void onDraw (IContext gr)
		{
			base.onDraw (gr);

			if (updateFilteredLinesRequest)
				updateFilteredLines ();

			if (filteredLines == null)
				return;

			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);

			Rectangle r = ClientRectangle;

			double y = ClientRectangle.Y;
			double x = ClientRectangle.X - ScrollX;

			IEnumerable<LogEntry> entries;
			lock (filteredLinesMutex) {
				entries = filteredLines.Skip(ScrollY).Take(visibleLines).ToArray();
			}

			for (int i = 0; i < entries.Count(); i++) {
				int idx = i + ScrollY;
				LogEntry le = entries.ElementAt(i);

				if (idx == curEntryIdx) {
					gr.Rectangle (x, y, r.Width, fe.Height);
					gr.SetSource (Color.Parse ("#5555ff55"));
					gr.Fill ();
				} else if (idx == hoverEntryIdx) {
					gr.Rectangle (x, y, r.Width, fe.Height);
					gr.SetSource (Color.Parse ("#8B451355"));
					gr.Fill ();
				}
				if (!string.IsNullOrEmpty(le.msg)) {
					switch (le.Type) {
						case LogType.Low:
							gr.SetSource (Colors.DimGrey);
							break;
						case LogType.Normal:
							gr.SetSource (Colors.Grey);
							break;
						case LogType.High:
							gr.SetSource (Colors.White);
							break;
						case LogType.Debug:
							gr.SetSource (Colors.Yellow);
							break;
						case LogType.Warning:
							gr.SetSource (Colors.Orange);
							break;
						case LogType.Error:
							gr.SetSource (Colors.Red);
							break;
						case LogType.Custom1:
							gr.SetSource (Colors.Cyan);
							break;
						case LogType.Custom2:
							gr.SetSource (Colors.Lime);
							break;
						case LogType.Custom3:
							gr.SetSource (Colors.LightPink);
							break;
						default:
							gr.SetSource (Colors.Grey);
							break;
					}
					gr.MoveTo (x, y + fe.Ascent);
					ReadOnlySpan<char> tmp = le.msg.AsSpan(0, Math.Min (100, le.msg.Length));
					gr.ShowText (tmp);
				}
				y += fe.Height;
			}
		}
		
		public override void onMouseLeave(object sender, MouseMoveEventArgs e)
		{
			hoverEntryIdx = -1;
			RegisterForRedraw ();
			base.onMouseLeave(sender, e);
		}
		public override void onMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.Button == Glfw.MouseButton.Left) {
				CurrentEntryIndex = hoverEntryIdx;
				e.Handled = true;
			}
			base.onMouseDown(sender, e);
		}
		public override void onMouseMove(object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove(sender, e);
			updateHoverEntryIdx (e.Position);
		}
		public override void onMouseWheel(object sender, MouseWheelEventArgs e)
		{
			base.onMouseWheel(sender, e);
			updateHoverEntryIdx (IFace.MousePosition);
		}
        protected override void Dispose(bool disposing)
        {
			if (logger != null) {
				lock(logger.LogMutext) {
					logger.log.ListAdd -= Lines_ListAdd;
					logger.log.ListRemove -= Lines_ListRemove;
					logger.log.ListClear -= Lines_ListClear;
				}
			}			
            base.Dispose(disposing);
        }
		#endregion
    }
}

