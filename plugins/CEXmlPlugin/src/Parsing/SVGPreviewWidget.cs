// Copyright (c) 2021-2025  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Drawing2D;
using Crow.DebugLogger;
using CrowEditBase;
using Crow;
using System.Threading;
using System.Diagnostics;
using Crow.Text;

namespace CrowEdit.Xml
{
	public class SVGPreviewWidget : Widget
	{
		#region CTOR
		protected SVGPreviewWidget () : base () {
			initCommands();
		}
		public SVGPreviewWidget(Interface iface, string style = null) : base (iface, style) { }
        #endregion

		Command CMDRefresh, CMDZoomIn, CMDZoomOut, CMDResetZoom;
		public CommandGroup Commands => new CommandGroup (
			CMDRefresh, CMDZoomIn, CMDZoomOut, CMDResetZoom
		);
		SvgPicture pic;

		void initCommands() {
			CMDRefresh = new ActionCommand (this, "Refresh",
				() => {
					RegisterForGraphicUpdate ();
				},
				"#icons.refresh.svg",
				new KeyBinding (Glfw.Key.F5));
			CMDZoomIn = new ActionCommand ("Zoom in",
				() => {
					ZoomFactor *= 2.0;
					RegisterForGraphicUpdate ();
				}, "#icons.zoom-in.svg");
			CMDZoomOut = new ActionCommand ("Zoom out",
				() => {
					ZoomFactor /= 2.0;
					RegisterForGraphicUpdate ();
				}, "#icons.zoom-out.svg");
			CMDResetZoom = new ActionCommand ("Reset Zoom",
				() => {
					ZoomFactor = 1.0;
					RegisterForGraphicUpdate ();
				}, "#icons.zoom-out.svg");
		}

		XmlDocument xmlDocument;
		double zoomFactor;

		public TextDocument XmlDocument {
			get => xmlDocument;
			set {
				if (xmlDocument == value)
					return;
				if (xmlDocument != null)
					xmlDocument.TextChanged -= onXmlChanged;

				if (value is XmlDocument xmlDoc) {
					xmlDocument = xmlDoc;
					xmlDocument.TextChanged += onXmlChanged;
				} else
					xmlDocument = null;
				pic = null;
				NotifyValueChangedAuto (xmlDocument);
				RegisterForGraphicUpdate ();
			}
		}
		void onXmlChanged(object sender, TextChangeEventArgs e) {
			RegisterForGraphicUpdate ();
		}
		[DefaultValue(1.0)]
		public double ZoomFactor {
			get => zoomFactor;
			set {
				if (zoomFactor == value)
					return;
				zoomFactor = value;
				NotifyValueChangedAuto(zoomFactor);
			}
		}
        protected override void Dispose(bool disposing)
        {
			if (xmlDocument != null)
				xmlDocument.TextChanged -= onXmlChanged;
            base.Dispose(disposing);
        }
		void load() {
			pic = new SvgPicture();
			pic.KeepProportions = true;
			pic.Scaled = true;
			pic.LoadSvgFragment(IFace, xmlDocument.source.ToString());
		}
		public override int measureRawSize (LayoutingType lt)
		{
			if (xmlDocument == null)
				return lt == LayoutingType.Width ? 2 * Margin.Width : 2 * Margin.Height;
			if (pic == null) 
				load();
			if (lt == LayoutingType.Width)
				return (int)(zoomFactor * pic.Dimensions.Width + 2 * Margin.Width);
			else
				return (int)(zoomFactor * pic.Dimensions.Height + 2 * Margin.Height);
		}
		protected override void onDraw (IContext gr)
		{
			base.onDraw (gr);

			if (xmlDocument != null && pic == null)
				load();

			Rectangle cr = ClientRectangle;
			Rectangle r = pic.Dimensions * zoomFactor;

			r.TopLeft = cr.Center - r.Center;

			pic?.Paint (IFace, gr, r);
		}
    }
}