// Copyright (c) 2021-2025 Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;
using Crow;
using CrowEditBase;
using Drawing2D;
using static CrowEditBase.CrowEditBase;

namespace CrowEditBase
{	
		public class LogItem : CrowEditComponent {
			int selectedIndex = -1;
			bool isOpened = false;
			public Command CMDReset, CMDCopy;
			public CommandGroup Commands => new CommandGroup (CMDCopy, CMDReset);

			public int SelectedIndex {
				get => selectedIndex;
				set {
					if (selectedIndex == value)
						return;
					selectedIndex = value;					
					NotifyValueChanged(selectedIndex);
					CMDCopy.CanExecute = !(value < 0);
				}
			}
			public bool IsOpened {
				get => isOpened;
				set {
					if (isOpened == value)
						return;
					isOpened = value;
					NotifyValueChanged(isOpened);
					lock (LogMutext) {
						if (isOpened) {
							App.OpenLog(this);
						 } else
							App.CloseLog(this);
					}
					IsSelected = value;
				}
			}

			public string Name;
			public ObservableList<LogEntry> log;
			
			public object LogMutext = new object();
			public LogItem(string name) {
				Name = name;
				log = new ObservableList<LogEntry>();
				CMDReset = new ActionCommand ("Clear", ResetLog);
				CMDCopy = new ActionCommand ("Copy message", CopyMessage);
			}


			public void Add(LogType type, string message) {
				lock (LogMutext)
					log.Add (new LogEntry(type, message));
			}
			public void ResetLog () {
				lock (LogMutext)
					log.Clear ();
			}
			public void CopyMessage () {
				lock (LogMutext) {
					if (selectedIndex < log.Count && selectedIndex >= 0)
						App.Clipboard = log.ElementAt(selectedIndex).msg;
				}
			}
			public void OnQueryClose (object sender, EventArgs e){
				IsOpened = false;
			}
		}

}
