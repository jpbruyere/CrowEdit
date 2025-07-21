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
using System.Diagnostics;
using Crow.IML;
using System.Runtime.Loader;
using Glfw;

namespace CECrowPlugin
{
	public class DebugInterface : Interface {
		static DebugInterface() {
			DbgLogger.ConsoleOutput = false;
		}
		public DebugInterface (IntPtr hWin) : base (100, 100, hWin)
		{
			fiWidget_design_id = typeof(Widget).GetField("design_id");
			fiPrivateContainer_child = typeof(PrivateContainer).GetField("child", BindingFlags.Instance | BindingFlags.NonPublic);
			clientRectangle = new Rectangle (0, 0, 100, 100);
		}
		protected override void initBackend()
		{
			if (!tryFindBackend (out Type backendType))
				throw new Exception ("No backend found.");
			backend = (CrowBackend)Activator.CreateInstance (backendType, new object[] {clientRectangle.Width, clientRectangle.Height});
			//hWin = backend.hWin;
			ownWindow = false;
			clipping = Backend.CreateRegion ();
		}
		public override void Run()
		{
			initBackend ();
			try {
				Init();
			} catch (Exception e) {
				Debug.WriteLine(e.Message);
				Debug.WriteLine(e.StackTrace);
			}
			
			Thread t = new Thread (interfaceThread) {
				IsBackground = true
			};
			t.Start ();
		}
		public bool Terminate = false;
        public bool Edition = true;
        public bool FirstRenderingFinished = false;
		

		bool checkEditHoverWidget() {
			if (lastEditHoverWidget != editHoverWidget) {
				if (editHoverWidget == null)
					delCrowServiceSetHoverDesignId(null);
				else {
					string id = (string)fiWidget_design_id?.GetValue(editHoverWidget);
					delCrowServiceSetHoverDesignId(id);
				}
				lastEditHoverWidget = editHoverWidget;
				return true;
			} else
				return false;
		}
		void interfaceThread () {
			while (!Terminate) {
				try
				{
					if (Edition) {
						if (FirstRenderingFinished) {
							Thread.Sleep(100);
							checkEditHoverWidget();
							continue;
						}

						int lqiCount;
						lock(LayoutMutex)
							lqiCount = LayoutingQueue.Count;
						while(lqiCount > 0) {
							Update();
							lock(LayoutMutex)
								lqiCount = LayoutingQueue.Count;
						}
						FirstRenderingFinished = true;
					} else {
						Update();
					}
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
					Thread.Sleep(2000);
				}

				/*if (IsDirty)
					delRegisterForRepaint();*/

				Thread.Sleep (UPDATE_INTERVAL);
			}
			Dispose();
		}
        string source;
		//Action delRegisterForRepaint;//call RegisterForRepaint in the container widget (DebugInterfaceWidget)
		Action<Exception> delCrowServiceSetCurrentException;
		Action<Type,object> delCrowServiceUpdateRootWidget;
		Action<string> delCrowServiceSetCurrentDesignId, delCrowServiceSetHoverDesignId;
		Action delCrowServiceForceMousePosition;

		delegate void GetScreenCoordinateDelegateType(out int x, out int y);
		//GetScreenCoordinateDelegateType delCrowServiceGetScreenCoordinate;
		Func<IEnumerable<object>> delCrowServiceGetStyling;
		Func<string, Stream> delCrowServiceGetStreamFromPath;
		FieldInfo fiWidget_design_id, fiPrivateContainer_child;

		
		public void RegisterDebugInterfaceCallback (object crowService){
			Type t = crowService.GetType();
			delCrowServiceForceMousePosition = (Action)Delegate.CreateDelegate(typeof(Action), crowService,
				t.GetMethod("ForceMousePosition"));
			delCrowServiceSetCurrentException = (Action<Exception>)Delegate.CreateDelegate(typeof(Action<Exception>), crowService,
				t.GetProperty("CurrentException").GetSetMethod(true));
			delCrowServiceSetCurrentDesignId = (Action<string>)Delegate.CreateDelegate(typeof(Action<string>), crowService,
				t.GetProperty("CurrentWidgetDesignId").GetSetMethod(true));
			delCrowServiceSetHoverDesignId = (Action<string>)Delegate.CreateDelegate(typeof(Action<string>), crowService,
				t.GetProperty("HoverWidgetDesignId").GetSetMethod(true));
			
			delCrowServiceUpdateRootWidget = (Action<Type,object>)Delegate.CreateDelegate(typeof(Action<Type,object>), crowService,
				t.GetMethod("UpdateRootWidget"));
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
						FirstRenderingFinished = false;
						Widget tmp = CreateITorFromIMLFragment (source).CreateInstance();
						AddWidget (tmp);
						tmp.DataSource = this;
					}
					delCrowServiceUpdateRootWidget(GraphicTree[0].GetType(), (object)GraphicTree[0]);
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
				if (style is string stylePath) {
					LoadStyle (stylePath);
				} else if (style is Assembly styleAssembly) {
					loadStylingFromAssembly (styleAssembly);
				}
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
			FirstRenderingFinished = false;
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

		Widget lastEditHoverWidget, editHoverWidget, editActiveWidget;
		bool checkHoverWidget() {
			while (true) {
				bool mouseInChildren = false;
				foreach(Widget child in GetWidgetChilren(editHoverWidget)) {
					if (child.MouseIsIn(MousePosition)) {
						editHoverWidget = child;
						mouseInChildren = true;
						break;
					}
				}
				if (!mouseInChildren)
					return true;
				/*if (typeof(TemplatedControl).IsAssignableFrom(editHoverWidget.GetType())) {
					return true;
				} else if (typeof(PrivateContainer).IsAssignableFrom(editHoverWidget.GetType())) {
					Widget child = (Widget)fiPrivateContainer_child.GetValue(editHoverWidget);
					if (child == null || !child.MouseIsIn(MousePosition))
						return true;
					editHoverWidget = child;
				} else if (typeof(GroupBase).IsAssignableFrom(editHoverWidget.GetType())) {
					bool mouseInChildren = false;
					foreach (Widget w in ((GroupBase)editHoverWidget).Children)	{
						if (w.MouseIsIn(MousePosition)) {
							editHoverWidget = w;
							mouseInChildren = true;
							break;
						}
					}
					if (!mouseInChildren)
						return true;
				} else
					return true;*/
			}			
		}
        public override bool OnMouseMove(int x, int y)
        {
			if (Edition) {
				int deltaX = x - base.MousePosition.X;
				int deltaY = y - base.MousePosition.Y;

				MousePosition = new Point(x,y);
				MouseMoveEventArgs e = new MouseMoveEventArgs (x, y, deltaX, deltaY);

				if (editHoverWidget != null) {
					//check topmost graphicobject first
					Widget topContainer = editHoverWidget;
					while (topContainer.LogicalParent is Widget w)
						topContainer = w;

					int indexOfTopContainer = GraphicTree.IndexOf (topContainer);
					if (indexOfTopContainer != 0) {//0 is topMost
						for (int i = 0; i < indexOfTopContainer; i++) {//check all top containers that are at a higher level
							//if logical parent of top container is the Interface, that's not a popup.
							if (typeof(Interface).IsAssignableFrom(GraphicTree [i].LogicalParent.GetType()) ) {
								if (GraphicTree [i].MouseIsIn (MousePosition)) {
									editHoverWidget = GraphicTree [i];
									if (checkHoverWidget())
										return true;
								}
							}
						}
					}

					if (editHoverWidget.MouseIsIn (MousePosition)) {
						return checkHoverWidget();
					} else {
						while (editHoverWidget.Parent is Widget parent) {
							editHoverWidget = parent;
							if (editHoverWidget.MouseIsIn (e.Position)) {
								return checkHoverWidget();
							}
						}
					}
				}

				//top level graphic obj's parsing
				lock (GraphicTree) {
					for (int i = 0; i < GraphicTree.Count; i++) {
						Widget g = GraphicTree [i];
						if (g.MouseIsIn (e.Position)) {
							editHoverWidget = g;
							return checkHoverWidget();
						}
					}
				}
				editHoverWidget = null;
				return false;
			}
            return base.OnMouseMove(x, y);
        }
        public override bool OnMouseButtonDown(MouseButton button)
        {
			if (Edition) {
				/*if (editHoverWidget != null) {
					editActiveWidget = editHoverWidget;
					string id = (string)fiWidget_design_id?.GetValue(editHoverWidget);
					delCrowServiceSetCurrentDesignId(id);
					return true;
				}*/
				return false;
			} else
            	return base.OnMouseButtonDown(button);
        }

        public override void ForceMousePosition()
		{
			//delCrowServiceGetScreenCoordinate(out int x, out int y);
			//Debug.WriteLine($"force mouse position: {x},{y}");
			//Glfw.Glfw3.SetCursorPosition (WindowHandle, x, y);
			delCrowServiceForceMousePosition();
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
			AssemblyLoadContext dbgLoadCtx =
				AssemblyLoadContext.All.FirstOrDefault (ctx=>ctx.Name == "CrowDebuggerLoadContext");
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
		public override MethodInfo SearchExtMethod (Type t, string methodName) {
			string key = t.Name + "." + methodName;
			if (knownExtMethods.ContainsKey (key))
				return knownExtMethods [key];

			Debug.WriteLine ($"[CECrowPlugin] search extension method: {t};{methodName} => key={key}");

			MethodInfo mi = null;
			AssemblyLoadContext dbgLoadCtx =
				AssemblyLoadContext.All.FirstOrDefault (ctx=>ctx.Name == "CrowDebuggerLoadContext");
			foreach (Assembly a in dbgLoadCtx.Assemblies) {
				try {
					if (CompilerServices.TryGetExtensionMethods (a, t, methodName, out mi)) {
						break;
					}
				} catch (Exception ex) {
					Console.WriteLine ($"[CECrowPlugin]Error: SearchExtMethod failed for  {t};{methodName} => key={key}");
				}
			}

			if (mi == null) {
				Debug.WriteLine ($"[CECrowPlugin] Extension method not found: {t};{methodName} => key={key}");
				return null;
			}

			knownExtMethods.Add (key, mi);
			return mi;
		}
		public Type GetTypeFromName (string typeName) {
			AssemblyLoadContext dbgLoadCtx =
				AssemblyLoadContext.All.FirstOrDefault (ctx=>ctx.Name == "CrowDebuggerLoadContext");
			foreach (Assembly a in dbgLoadCtx.Assemblies) {
				try {
					foreach (Type expT in a.GetExportedTypes ()) {
						if (string.Equals(expT.Name,typeName,StringComparison.Ordinal))
							return expT;
					}
				} catch (Exception ex) {
					Console.WriteLine ($"[CECrowPlugin]Error: GetWidgetTypeFromName failed for {typeName} in {a}.\n{ex}");
				}
			}
			return null;
		}
		public IEnumerable<object> GetWidgetChilren(object widget) {
			Type goType = widget.GetType();
			if (typeof (Group).IsAssignableFrom (goType)) {
				foreach (Widget w in (widget as Group).Children)
					yield return w;
			} else if (typeof(Container).IsAssignableFrom (goType))
				yield return (widget as Container).Child;
			else if (typeof(TemplatedContainer).IsAssignableFrom (goType))
				yield return (widget as TemplatedContainer).Content;
			else if (typeof(TemplatedGroup).IsAssignableFrom (goType)) {
				foreach (Widget w in (widget as TemplatedGroup).Items)
					yield return w;
			}
			/* for template tree
			} else if (typeof(PrivateContainer).IsAssignableFrom (goType)) {
				FieldInfo fi = typeof(PrivateContainer).GetField("child", BindingFlags.NonPublic | BindingFlags.Instance);
				yield return fi.GetValue(widget);
			*/
		}
	
		public void LockRenderMutex() => Monitor.Enter(this.UpdateMutex);
		public void UnlockRenderMutex() => Monitor.Exit(this.UpdateMutex);


        /*protected override void processDrawing(IContext ctx)
        {
            base.processDrawing(ctx);

			ctx.Arc(MousePosition, 2, 0, Math.PI * 2.0);
			ctx.SetSource(Colors.DarkRed);
			ctx.Fill();
        }*/
	}
}