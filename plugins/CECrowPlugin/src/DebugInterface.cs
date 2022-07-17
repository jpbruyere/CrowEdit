// Copyright (c) 2021-2022  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Linq;
using Crow;
using Drawing2D;
using IML = Crow.IML;

namespace CECrowPlugin
{
	public class DebugInterface : Interface {
		static DebugInterface() {
			DbgLogger.ConsoleOutput = false;
		}
		public DebugInterface (IntPtr hWin) : base (100, 100, hWin)
		{
			clientRectangle = new Rectangle (0, 0, 100, 100);
		}
		protected override void initBackend()
		{
			if (!tryFindBackendType (out Type backendType))
				throw new Exception ("No backend found.");
			backend = (CrowBackend)Activator.CreateInstance (backendType, new object[] {clientRectangle.Width, clientRectangle.Height});
			//hWin = backend.hWin;
			ownWindow = false;
			clipping = Backend.CreateRegion ();
		}
		public override void Run()
		{
			initBackend ();

			Thread t = new Thread (interfaceThread) {
				IsBackground = true
			};
			t.Start ();

			Init();

		}
		public bool Terminate;
		string source;
		Action delRegisterForRepaint;//call RegisterForRepaint in the container widget (DebugInterfaceWidget)
		Action<Exception> delCrowServiceSetCurrentException;

		delegate void GetScreenCoordinateDelegateType(out int x, out int y);
		GetScreenCoordinateDelegateType delCrowServiceGetScreenCoordinate;
		Func<IEnumerable<object>> delCrowServiceGetStyling;
		Func<string, Stream> delCrowServiceGetStreamFromPath;

		void interfaceThread () {
			while (!Terminate) {
				try
				{
					Update();
				}
				catch (System.Exception ex)
				{
					while (Monitor.IsEntered(LayoutMutex)) {
						Console.WriteLine ($"[DebugIFace] trying to exit LayoutMutex on error");
						Monitor.Exit (LayoutMutex);
					}
					while (Monitor.IsEntered(UpdateMutex)) {
						Console.WriteLine ($"[DebugIFace] trying to exit UpdateMutex on error");
						Monitor.Exit (UpdateMutex);
					}
					while (Monitor.IsEntered(ClippingMutex)) {
						Console.WriteLine ($"[DebugIFace] trying to exit ClippingMutex on error");
						Monitor.Exit (ClippingMutex);
					}
					/*while (Monitor.IsEntered(LayoutMutex))
						Monitor.Exit (LayoutMutex);
					while (Monitor.IsEntered(UpdateMutex))
						Monitor.Exit (UpdateMutex);
					while (Monitor.IsEntered(ClippingMutex))
						Monitor.Exit (ClippingMutex);*/
					delCrowServiceSetCurrentException (ex);
					Console.WriteLine ($"[DbgIFace] {ex}");
					ClearInterface();
					Thread.Sleep(1000);
				}

				/*if (IsDirty)
					delRegisterForRepaint();				*/

				Thread.Sleep (UPDATE_INTERVAL);
			}
			Dispose();
		}
		public void RegisterDebugInterfaceCallback (object crowService){
			Type t = crowService.GetType();
			//delRegisterForRepaint = (Action)Delegate.CreateDelegate(typeof(Action), w, t.GetMethod("RegisterForRepaint"));
			delCrowServiceSetCurrentException = (Action<Exception>)Delegate.CreateDelegate(typeof(Action<Exception>), crowService,
				t.GetProperty("CurrentException").GetSetMethod(true));
			delCrowServiceGetScreenCoordinate = (GetScreenCoordinateDelegateType)Delegate.CreateDelegate(typeof(GetScreenCoordinateDelegateType), crowService,
				t.GetMethod("getMouseScreenCoordinates", BindingFlags.Instance | BindingFlags.NonPublic));
			delCrowServiceGetStyling = (Func<IEnumerable<object>>)Delegate.CreateDelegate (typeof (Func<IEnumerable<object>>), crowService,
				t.GetMethod ("getStyling", BindingFlags.Instance | BindingFlags.NonPublic));
			delCrowServiceGetStreamFromPath = (Func<string, Stream>)Delegate.CreateDelegate (typeof (Func<string, Stream>), crowService,
				t.GetMethod ("getStreamFromPath", BindingFlags.Instance | BindingFlags.NonPublic));
		}
		/*public void ResetDirtyState () {
			IsDirty = false;
		}*/
		public string Source {
			set {
				if (source == value)
					return;
				source = value;
				delCrowServiceSetCurrentException(null);
				try
				{
					lock (UpdateMutex) {
						resetInterface ();
						if (string.IsNullOrEmpty(source))
							return;
						Widget tmp = CreateITorFromIMLFragment (source).CreateInstance();
						AddWidget (tmp);
						tmp.DataSource = this;
					}
				}
				catch (IML.InstantiatorException iTorEx)
				{
					delCrowServiceSetCurrentException(iTorEx.InnerException);
				}
				catch (System.Exception ex)
				{
					delCrowServiceSetCurrentException(ex);
				}
			}
		}
		void resetInterface () {
			ClearInterface();
			initDictionaries();
			foreach (object style in delCrowServiceGetStyling ()) {
				if (style is string stylePath)
					LoadStyle (stylePath);
				else if (style is Assembly styleAssembly)
					loadStylingFromAssembly (styleAssembly);
			}
		}
		public void ReloadIml () {
			if (string.IsNullOrEmpty (source))
				return;
			string src = source;
			Source = null;
			Source = src;
		}
		public void Resize (int width, int height) {
			ProcessResize (new Rectangle(0, 0, width, height));
		}
		/*public override void ProcessResize(Rectangle bounds) {
			lock (UpdateMutex) {
				clientRectangle = bounds.Size;

				CreateMainSurface (ref clientRectangle);

				foreach (Widget g in GraphicTree)
					g.RegisterForLayouting (LayoutingType.All);

				RegisterClip (clientRectangle);
			}
		}*/
		public override void ForceMousePosition()
		{
			delCrowServiceGetScreenCoordinate(out int x, out int y);
			Glfw.Glfw3.SetCursorPosition (WindowHandle, x, y);
		}

		public bool OnKeyDown (Glfw.Key key, int scancode, Glfw.Modifier modifiers) {
			return base.OnKeyDown (new KeyEventArgs (key, scancode, modifiers));
		}
		public bool OnKeyUp (Glfw.Key key, int scancode, Glfw.Modifier modifiers) {
			return base.OnKeyDown (new KeyEventArgs (key, scancode, modifiers));
		}


		public override Stream GetStreamFromPath(string path)
		{
			Stream result = delCrowServiceGetStreamFromPath (path);
			if (result != null)
				return result;
			return base.GetStreamFromPath (path);
		}
		public override Type GetWidgetTypeFromName (string typeName){
			if (knownCrowWidgetTypes.ContainsKey (typeName))
				return knownCrowWidgetTypes [typeName];
			System.Runtime.Loader.AssemblyLoadContext dbgLoadCtx =
				System.Runtime.Loader.AssemblyLoadContext.All.FirstOrDefault (ctx=>ctx.Name == "CrowDebuggerLoadContext");
			foreach (Assembly a in dbgLoadCtx.Assemblies) {
				try {
					foreach (Type expT in a.GetExportedTypes ()) {
						if (expT.Name != typeName)
							continue;
						knownCrowWidgetTypes.Add (typeName, expT);
						return expT;
					}
				} catch (Exception ex) {
					Console.WriteLine ($"[CECrowPlugin]Error: GetWidgetTypeFromName failed for {typeName} in {a}.\n{ex}");
				}
			}
			return null;
		}
	}
}