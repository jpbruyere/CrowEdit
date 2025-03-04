// Copyright (c) 2013-2025  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Glfw;
using Drawing2D;
using System.Diagnostics;
using System.Collections.Generic;
using CrowEditBase;
using System.Threading;
using Crow.Text;

using static CrowEditBase.CrowEditBase;
using Crow;

namespace CECrowPlugin
{
	public class DebugInterfaceWidget : Widget {
		CrowService crowIFaceService;
		public CrowService CrowIFaceService {
			get => crowIFaceService;
			set {
				if (crowIFaceService == value)
					return;
				crowIFaceService = value;
				NotifyValueChangedAuto (crowIFaceService);
			}
		}
		Command CMDRefresh, CMDZoomIn, CMDZoomOut;
		public DebugInterfaceWidget () : base () {
			CMDRefresh = new ActionCommand (this, "Refresh",
				() => {
					crowIFaceService?.LoadIML ("");
					crowIFaceService?.LoadIML (imlSource);
					RegisterForGraphicUpdate ();
				},
				"#icons.refresh.svg",
				new KeyBinding (Key.F3));
			CMDZoomIn = new ActionCommand ("Zoom in",
				() => {
					if (crowIFaceService != null) {
						crowIFaceService.ZoomFactor *= 2.0;
						RegisterForGraphicUpdate ();
					}
				}, "#icons.zoom-in.svg");
			CMDZoomOut = new ActionCommand ("Zoom out",
				() => {
					if (crowIFaceService != null) {
						crowIFaceService.ZoomFactor /= 2.0;
						RegisterForGraphicUpdate ();
					}
				}, "#icons.zoom-out.svg");

			Thread t = new Thread (backgroundThreadFunc);
			t.IsBackground = true;
			t.Start ();
		}
		protected void backgroundThreadFunc () {
			Stopwatch sw = Stopwatch.StartNew ();
			int refreshRate = crowIFaceService == null ? 10 : crowIFaceService.RefreshRate;
			while (true) {
				if (sw.ElapsedMilliseconds > 200) {
					if (Document != null && document.TryGetState (this, out List<TextChange> changes)) {
						foreach (TextChange tc in changes)
							updateIMLSource (tc);
					}
					refreshRate = crowIFaceService == null ? 10 : crowIFaceService.RefreshRate;
					sw.Restart ();
				}
				if (crowIFaceService != null && crowIFaceService.GetDirtyState)
					RegisterForRepaint ();
				Thread.Sleep (refreshRate);
			}
		}
		void updateIMLSource (TextChange change) {
			ReadOnlySpan<char> src = imlSource.AsSpan ();
			Span<char> tmp = stackalloc char[src.Length + (change.ChangedText.Length - change.Length)];
			//Console.WriteLine ($"{Text.Length,-4} {change.Start,-4} {change.Length,-4} {change.ChangedText.Length,-4} tmp:{tmp.Length,-4}");
			src.Slice (0, change.Start).CopyTo (tmp);
			change.ChangedText.AsSpan ().CopyTo (tmp.Slice (change.Start));
			src.Slice (change.End).CopyTo (tmp.Slice (change.Start + change.ChangedText.Length));

			imlSource = tmp.ToString ();

			crowIFaceService?.LoadIML (imlSource);

			RegisterForRedraw ();
		}
		string imlSource;

		ImlDocument document;
		ForeignWidgetContainer currentWidget;
		public TextDocument Document {
			get => document;
			set {
				if (document == value)
					return;

				if (value is ImlDocument imlDoc) {
					document?.UnregisterClient (this);
					document = imlDoc;
					imlSource = default;
					document?.RegisterClient (this, true);

					NotifyValueChangedAuto (document);
					RegisterForGraphicUpdate ();
				}
			}
		}

		
		public ForeignWidgetContainer CurrentWidget {
			get => currentWidget;
			set {
				if (currentWidget == value)
					return;
				currentWidget = value;
				NotifyValueChanged("CurrentWidget",currentWidget);
				RegisterForRepaint ();
			}
		}


		protected override void onInitialized(object sender, EventArgs e)
		{
			base.onInitialized(sender, e);


			CrowIFaceService = App.GetService<CrowService> ();
			crowIFaceService?.Start ();
		}
		/*public CommandGroup LoggerCommands =>
			new CommandGroup(
				new Command("Get logs", () => getLog ()),
				//new Command("Reset logs", () => delResetDebugger ()),
				new Command("Save to file", () => saveLogToDebugLogFilePath ()),
				new Command("Load from file", () => loadLogFromDebugLogFilePath ())
			);*/
		public CommandGroup WindowCommands => new CommandGroup (
			CMDRefresh, //CMDZoomIn, CMDZoomOut,
			crowIFaceService.CMDStartRecording,
			crowIFaceService.CMDStopRecording,
			crowIFaceService.CMDOpenConfig,
			(Parent.LogicalParent as DockWindow).CMDClose
		);

		protected override void onDraw(IContext gr)
		{
			//crowIFaceService.Log(LogType.Error, "onDraw");
			gr.SetSource(Colors.RoyalBlue);
			gr.Paint();
		}
		public override bool CacheEnabled { get => true; set => base.CacheEnabled = true; }

		public override void onKeyDown(object sender, KeyEventArgs e) => crowIFaceService?.onKeyDown(e);
		public override void onKeyUp(object sender, KeyEventArgs e) => crowIFaceService?.onKeyUp(e);
		public override void onKeyPress(object sender, KeyPressEventArgs e) => crowIFaceService?.onKeyPress(e);
		public override void onMouseMove(object sender, MouseMoveEventArgs e) {
			Point m = ScreenPointToLocal (e.Position);
			crowIFaceService?.onMouseMove(e.Position, new MouseMoveEventArgs(m.X,m.Y, e.XDelta, e.YDelta));
		}
		public override void onMouseDown(object sender, MouseButtonEventArgs e) => crowIFaceService?.onMouseDown(e);
		public override void onMouseUp(object sender, MouseButtonEventArgs e) => crowIFaceService?.onMouseUp(e);
		public override void onMouseWheel(object sender, MouseWheelEventArgs e) => crowIFaceService?.onMouseWheel(e);

		public override bool Paint(IContext ctx)
		{
			return base.Paint(ctx);
			/*crowIFaceService.LockRenderMutex();
			try {
				return base.Paint(ctx);				
			} finally {
				crowIFaceService.UnlockRenderMutex();
			}*/
		}
		protected override void RecreateCache()
		{
			if (crowIFaceService != null && crowIFaceService.IsRunning) {
				bmp = crowIFaceService.MainSurface;
			} else
				base.RecreateCache ();

			IsDirty = false;
		}
		protected override void UpdateCache(IContext ctx)
		{
			if (crowIFaceService != null && crowIFaceService.IsRunning && bmp != null) {
				//crowIFaceService.LockRenderMutex();
				paintCache (ctx, Slot + Parent.ClientRectangle.Position);
				//crowIFaceService.UnlockRenderMutex();
				crowIFaceService.ResetDirtyState ();
			} 
				
		}
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);
			switch (layoutType) {
			case LayoutingType.Width:
				//DesignWidth = Slot.Width * 100 / zoom;
				crowIFaceService.Resize (Slot.Width, Slot.Height);
				break;
			case LayoutingType.Height:
				//DesignHeight = Slot.Height * 100 / zoom;
				crowIFaceService.Resize (Slot.Width, Slot.Height);
				break;
			}
		}

		protected override void Dispose(bool disposing)
		{
			CMDRefresh?.Dispose ();
			//crowIFaceService?.Stop ();
			base.Dispose(disposing);
		}
	}
}