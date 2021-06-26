// Copyright (c) 2013-2021  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Glfw;
using Crow.Text;
using System.Collections.Generic;
using Crow.Cairo;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Collections;
using CrowEditBase;
using System.Threading;

namespace Crow
{
	public interface IDocumentClient {

	}
	public class Editor : TextBox {
		#region CTOR
		protected Editor () : base () {
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
			CMDCut = new Command ("Cut", Cut, "#CrowEditBase.ui.icons.scissors.svg",  false);
			CMDCopy = new Command ("Copy", Copy, "#CrowEditBase.ui.icons.copy-file.svg",  false);
			CMDPaste = new Command ("Paste", Paste, "#CrowEditBase.ui.icons.paste-on-document.svg",  true);

			ContextCommands = new CommandGroup (CMDCut, CMDCopy, CMDPaste);
		}
		public override int CurrentColumn {
			get => base.CurrentColumn;
			set {
				if (CurrentColumn == value)
					return;
				base.CurrentColumn = value; 
				CMDCopy.CanExecute = CMDCut.CanExecute = !SelectionIsEmpty;
			}
		}
		public override int CurrentLine {
			get => base.CurrentLine;
			set {
				if (CurrentLine == value)
					return;
				base.CurrentLine = value;
				CMDCopy.CanExecute = CMDCut.CanExecute = !SelectionIsEmpty;
			}
		}
		protected override CharLocation? CurrentLoc {
			get => base.CurrentLoc;
			set {
				if (currentLoc == value)
					return;
				base.CurrentLoc = value;
				CMDCopy.CanExecute = CMDCut.CanExecute = !SelectionIsEmpty;
			}
		}
		/*protected override CharLocation? SelectionStart {
			get => base.SelectionStart;
			set {
				if (SelectionStart == value)
					return;
				base.SelectionStart = value;
				CMDCopy.CanExecute = CMDCut.CanExecute = !SelectionIsEmpty;
			}
		}*/


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
		public override void OnTextChanged(object sender, TextChangeEventArgs e)
		{
			if (disableTextChangedEvent)
				return;			
			base.OnTextChanged(sender, e);
		}
		protected override void onFocused(object sender, EventArgs e)
		{
			base.onFocused(sender, e);
			IFace.NotifyValueChanged ("CurrentEditor", this);
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
	}
}