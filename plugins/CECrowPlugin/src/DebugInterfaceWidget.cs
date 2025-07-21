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
		#region CTOR/DTOR
		public DebugInterfaceWidget () : base () {
			
			CrowIFaceService = App.GetService<CrowService> ();
			
			if (crowIFaceService != null)
				crowIFaceService.ValueChanged += service_ValueChanged;

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
        ~DebugInterfaceWidget() {
			if (crowIFaceService != null)
				crowIFaceService.ValueChanged -= service_ValueChanged;
		}
        #endregion

		void service_ValueChanged(object instance, ValueChangeEventArgs e) {
			if (e.MemberName == "CurrentWidget") {
				if (e.NewValue is ForeignWidgetContainer fwc)
					CurrentWidget = fwc;
				else
					CurrentWidget = null;
			} else	if (e.MemberName == "HoverWidget") {
				if (e.NewValue is ForeignWidgetContainer fwc)
					HoverWidget = fwc;
				else
					HoverWidget = null;
			}
		}		
		CrowService crowIFaceService;
		string imlSource;
		ImlDocument document;
		ForeignWidgetContainer currentWidget, hoverWidget;
		
		Command CMDRefresh, CMDZoomIn, CMDZoomOut;
		public CommandGroup WindowCommands => new CommandGroup (
			crowIFaceService.CMDRun,
			crowIFaceService.CMDEditMode,
			CMDRefresh, //CMDZoomIn, CMDZoomOut,
			crowIFaceService.CMDStartRecording,
			crowIFaceService.CMDStopRecording,
			crowIFaceService.CMDOpenConfig
			//(Parent.LogicalParent as DockWindow).CMDClose
		);
		public CrowService CrowIFaceService {
			get => crowIFaceService;
			set {
				if (crowIFaceService == value)
					return;
				crowIFaceService = value;
				NotifyValueChangedAuto (crowIFaceService);
			}
		}
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
				if (!(value?.GetType().Name == "ForeignWidgetContainer"))
					return;
				if (currentWidget == value)
					return;
				currentWidget = value;
				NotifyValueChanged("CurrentWidget",currentWidget);
				RegisterForRepaint ();
			}
		}
		public ForeignWidgetContainer HoverWidget {
			get => hoverWidget;
			set {
				if (!(value?.GetType().Name == "ForeignWidgetContainer"))
					return;
				if (hoverWidget == value)
					return;
				hoverWidget = value;
				NotifyValueChanged("HoverWidget",hoverWidget);
				RegisterForRepaint ();
			}
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
			if (string.IsNullOrEmpty(change.ChangedText) && change.CharDiff == 0)
				return;
			ReadOnlySpan<char> src = imlSource.AsSpan ();
			Span<char> tmp = stackalloc char[src.Length + change.CharDiff];
			//Console.WriteLine ($"{Text.Length,-4} {change.Start,-4} {change.Length,-4} {change.ChangedText.Length,-4} tmp:{tmp.Length,-4}");
			src.Slice (0, change.Start).CopyTo (tmp);

			if (!string.IsNullOrEmpty(change.ChangedText)) {
				change.ChangedText.AsSpan ().CopyTo (tmp.Slice (change.Start));
			}
			src.Slice (change.End).CopyTo (tmp.Slice (change.End2));

			imlSource = tmp.ToString ();

			if (document.EncloseInTemplatedControl && !string.IsNullOrEmpty(document.TemplateContainerSource)) {
				if (!string.IsNullOrEmpty(imlSource) && imlSource.StartsWith("<?xml")) {
					int pos = src.IndexOf('>');
					if (pos > 0)
						src = imlSource.Substring(pos + 1);
				}
				string tmpCloseTag = document.TemplateContainerSource.Split (' ', StringSplitOptions.RemoveEmptyEntries)[0].Replace ("<","").TrimEnd('/','>');
				crowIFaceService?.LoadIML ($"{document.TemplateContainerSource.TrimEnd('/','>')}><Template>{src}</Template></{tmpCloseTag}>");
			} else
				crowIFaceService?.LoadIML (imlSource);

			RegisterForRedraw ();
		}
		protected override void onInitialized(object sender, EventArgs e)
		{
			base.onInitialized(sender, e);
			crowIFaceService?.Start ();
		}
		/*public CommandGroup LoggerCommands =>
			new CommandGroup(
				new Command("Get logs", () => getLog ()),
				//new Command("Reset logs", () => delResetDebugger ()),
				new Command("Save to file", () => saveLogToDebugLogFilePath ()),
				new Command("Load from file", () => loadLogFromDebugLogFilePath ())
			);*/


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
		Point localMousePos;
		public override void onMouseMove(object sender, MouseMoveEventArgs e) {
			Point m = ScreenPointToLocal (e.Position);
			localMousePos = m;
			//Debug.WriteLine($"local mouse position: {m}");
			crowIFaceService?.onMouseMove(e.Position, new MouseMoveEventArgs (m.X, m.Y, e.XDelta, e.YDelta));
		}
		public override void onMouseDown(object sender, MouseButtonEventArgs e) => crowIFaceService?.onMouseDown(e);
		public override void onMouseUp(object sender, MouseButtonEventArgs e) => crowIFaceService?.onMouseUp(e);
		public override void onMouseWheel(object sender, MouseWheelEventArgs e) => crowIFaceService?.onMouseWheel(e);

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

				if (crowIFaceService.EditMode) {
					if (hoverWidget != null && hoverWidget != currentWidget) {
						//currentWidget.
						RectangleD r = hoverWidget.GetScreenCoordinate() + Slot.Position + Parent.ClientRectangle.Position;
						ctx.SetDash(new double[] {1,3});
						ctx.SetSource(Colors.Yellow);
						ctx.Rectangle(r, 1);
						ctx.SetDash(new double[] {});
					}				
					if (currentWidget != null) {
						//currentWidget.
						RectangleD r = currentWidget.GetScreenCoordinate() + Slot.Position + Parent.ClientRectangle.Position;
						//ctx.ResetClip();
						//ctx.SetDash([2,3]);
						ctx.SetSource(Colors.White);
						ctx.Rectangle(r.Inflated(1), 1);
	//					ctx.Stroke();
						//ctx.SetDash([0]);
					}
					/*ctx.LineWidth = 1;
					ctx.Arc(localMousePos, 3, 0, Math.PI * 2.0);
					ctx.SetSource(Colors.Yellow);
					ctx.Stroke();*/
				}

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

        public override MouseCursor MouseCursor {
			get => base.MouseCursor;
			set {
				Console.WriteLine("set mouse cursor");
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