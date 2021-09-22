// Copyright (c) 2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Crow;
using Crow.Drawing;
using IML = Crow.IML;

namespace CECrowPlugin
{
	public class DebugInterface : Interface {
		static DebugInterface() {
			DbgLogger.ConsoleOutput = false;
		}
		public DebugInterface (IntPtr hWin) : base (100, 100, hWin)
		{
			SolidBackground = false;
			initBackend (true);

			clientRectangle = new Rectangle (0, 0, 100, 100);
			CreateMainSurface (ref clientRectangle);
		}

		public override void Run()
		{
			Init();

			Thread t = new Thread (interfaceThread) {
				IsBackground = true
			};
			t.Start ();
		}
		public bool Terminate;
		string source;
		Action delRegisterForRepaint;//call RegisterForRepaint in the container widget (DebugInterfaceWidget)
		Action<Exception> delSetCurrentException;

		delegate void GetScreenCoordinateDelegateType(out int x, out int y);
		GetScreenCoordinateDelegateType delGetScreenCoordinate;
		Func<IEnumerable<object>> delGetStyling;
		Func<string, Stream> delGetStreamFromPath;

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
					delSetCurrentException (ex);
					Console.WriteLine ($"[DbgIFace] {ex}");
					ClearInterface();
					Thread.Sleep(1000);
				}

				/*if (IsDirty)
					delRegisterForRepaint();				*/

				Thread.Sleep (UPDATE_INTERVAL);
			}
		}
		public IntPtr SurfacePointer {
			get {
				lock(UpdateMutex)
					return surf.Handle;
			}
		}
		public void RegisterDebugInterfaceCallback (object w){
			Type t = w.GetType();
			//delRegisterForRepaint = (Action)Delegate.CreateDelegate(typeof(Action), w, t.GetMethod("RegisterForRepaint"));
			delSetCurrentException = (Action<Exception>)Delegate.CreateDelegate(typeof(Action<Exception>), w, t.GetProperty("CurrentException").GetSetMethod());
			delGetScreenCoordinate = (GetScreenCoordinateDelegateType)Delegate.CreateDelegate(typeof(GetScreenCoordinateDelegateType), w, t.GetMethod("GetMouseScreenCoordinates"));
			delGetStyling = (Func<IEnumerable<object>>)Delegate.CreateDelegate (typeof (Func<IEnumerable<object>>), w, t.GetMethod ("GetStyling"));
			delGetStreamFromPath = (Func<string, Stream>)Delegate.CreateDelegate (typeof (Func<string, Stream>), w, t.GetMethod ("GetStreamFromPath"));
		}
		/*public void ResetDirtyState () {
			IsDirty = false;
		}*/
		public string Source {
			set {
				if (source == value)
					return;
				source = value;
				delSetCurrentException(null);
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
					delSetCurrentException(iTorEx.InnerException);
				}
				catch (System.Exception ex)
				{
					delSetCurrentException(ex);
				}
			}
		}
		void resetInterface () {
			ClearInterface();
			initDictionaries();
			foreach (object style in delGetStyling ()) {
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
			if (!HaveVkvgBackend)
				ProcessResize (new Rectangle(0,0,width, height));
		}
		public override void ProcessResize(Rectangle bounds) {
			lock (UpdateMutex) {
				clientRectangle = bounds.Size;

				CreateMainSurface (ref clientRectangle);

				foreach (Widget g in GraphicTree)
					g.RegisterForLayouting (LayoutingType.All);

				RegisterClip (clientRectangle);
			}
		}
		public override void ForceMousePosition()
		{
			delGetScreenCoordinate(out int x, out int y);
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
			Stream result = delGetStreamFromPath (path);
			if (result != null)
				return result;
			return base.GetStreamFromPath (path);
		}
		public override Type GetWidgetTypeFromName (string typeName){
			if (knownCrowWidgetTypes.ContainsKey (typeName))
				return knownCrowWidgetTypes [typeName];
			foreach (Assembly a in System.Runtime.Loader.AssemblyLoadContext.GetLoadContext (Assembly.GetExecutingAssembly ()).Assemblies) {
				foreach (Type expT in a.GetExportedTypes ()) {
					if (expT.Name != typeName)
						continue;
					knownCrowWidgetTypes.Add (typeName, expT);
					return expT;
				}
			}
			return null;
		}
	}
}