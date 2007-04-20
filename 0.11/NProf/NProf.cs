/***************************************************************************
                          profiler.cpp  -  description
                             -------------------
    begin                : Sat Jan 18 2003
    copyright            : (C) 2003,2004,2005,2006 by Matthew Mastracci, Christian Staudenmeyer
    email                : mmastrac@canada.com
 ***************************************************************************/

/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 ***************************************************************************/

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Data;
using Microsoft.Win32;
using System.Globalization;
using DotNetLib.Windows.Forms;

namespace NProf {
	public class NProf : Form {
		public static Font font = new Font("Courier New", 9.0f);
		public ContainerListView runs;
		private MethodView callees = new MethodView();

		private MethodView callers = new MethodView();

		private Profiler profiler;
		private TextBox findText;
		private FlowLayoutPanel findPanel;
		public static TextBox application;
		public static TextBox arguments;
		public void ShowSearch() {
			findPanel.Visible = !findPanel.Visible;
			findText.Focus();
		}
		public void Find(bool forward, bool step) {
			if (callers.SelectedItems.Count != 0) {
				callers.Find(findText.Text, forward, step);
			}
			else {
				callees.Find(findText.Text, forward, step);
			}
		}
		public string Title {
			get {
				return "NProf " + Profiler.Version;
			}
		}
		public void GoToCaller(MethodView source) {
			MoveTo(source, callers);
		}
		public void GoToCallee(MethodView source)
		{
			MoveTo(source, callees);
			//if (callers.SelectedItems.Count != 0)
			//{
			//    ContainerListViewItem item = callers.SelectedItems[0];
			//    int id = ((FunctionInfo)item.Tag).ID;
			//    if (item.ParentItem.ParentItem == null)
			//    {
			//        callees.MoveTo(id);
			//    }
			//    else
			//    {
			//        callers.MoveTo(id);
			//    }
			//    item.Collapse();

			//}
		}

		public void MoveTo(MethodView source,MethodView target) {
			if (source.SelectedItems.Count != 0) {
				target.MoveTo(((FunctionInfo)source.SelectedItems[0].Tag).ID);
			}
			//if (callers.SelectedItems.Count != 0) {
			//    ContainerListViewItem item = callers.SelectedItems[0];
			//    int id = ((FunctionInfo)item.Tag).ID;
			//    if (item.ParentItem.ParentItem == null) {
			//        callees.MoveTo(id);
			//    }
			//    else {
			//        callers.MoveTo(id);
			//    }
			//    item.Collapse();

			//}
		}
		private void CallersNext() {
			if (callers.SelectedItems.Count != 0) {
				ContainerListViewItem item = callers.SelectedItems[0];

				int id = ((FunctionInfo)item.Tag).ID;
				if (item.ParentItem.ParentItem == null) {
					callees.MoveTo(id);
				}
				else {
					callers.MoveTo(id);
				}
				item.Collapse();

			}
		}
		private void CalleesNext() {
			if (callees.SelectedItems.Count != 0) {
				ContainerListViewItem item = callees.SelectedItems[0];
				if (item.ParentItem.ParentItem == null) {
					callers.MoveTo(((FunctionInfo)item.Tag).ID);
				}
				else {
					callees.MoveTo(((FunctionInfo)item.Tag).ID);
				}
				item.Collapse();
			}
		}
		private NProf() {
			WindowState = FormWindowState.Maximized;
			Icon = new Icon(this.GetType().Assembly.GetManifestResourceStream("NProf.Resources.app-icon.ico"));
			Text = Title;
			profiler = new Profiler();
			runs = new ContainerListView();
			runs.ColumnSortColor = Color.White;
			runs.Columns.Add("Program");
			runs.Columns.Add("Time");
			runs.Font = font;
			runs.AllowMultiSelect = true;
			runs.Dock = DockStyle.Left;
			runs.Width = 200;
			runs.SelectedItemsChanged += delegate {
				if (runs.SelectedItems.Count != 0) {
					ShowRun((Run)runs.SelectedItems[0].Tag);
				}
			};
			//callers.Size = new Size(100, 200);
			//callers.Dock = DockStyle.Bottom;
			//callers.Dock = DockStyle.Fill;
			//callees.Dock = DockStyle.Fill;
			callers.GotFocus += delegate {
				callees.SelectedItems.Clear();
			};
			//ListView view = new ListView();
			//ListViewItem item = new ListViewItem();
			callers.DoubleClick += delegate { CallersNext(); };
			callers.KeyDown += delegate(object sender, KeyEventArgs e) {
				if (e.KeyData == Keys.Enter) {
					CallersNext();
				}
			};
			callees.KeyDown += delegate(object sender, KeyEventArgs e) {
				if (e.KeyData == Keys.Enter) {
					CalleesNext();
				}
			};
			// TODO: inherit from base class
			callees.GotFocus += delegate {
				callers.SelectedItems.Clear();
			};
			callees.DoubleClick += delegate {
				CalleesNext();
			};
			ContextMenu callerMenu = new ContextMenu(
				new MenuItem[] {
					new MenuItem(
						"go to method",
						delegate {
							GoToCaller(callers);
						},
						Shortcut.CtrlN
					),
					new MenuItem(
						"go to callee",
						delegate {
							GoToCallee(callers);
						},
						Shortcut.CtrlN
					)
				}
			);
			ContextMenu calleesMenu = new ContextMenu(
				new MenuItem[] {
					new MenuItem(
						"go to method",
						delegate
						{
							GoToCallee(callers);
						},
						Shortcut.CtrlE
					),
					new MenuItem(
						"go to callee",
						delegate 
						{
							GoToCaller(callers);
						},
						Shortcut.CtrlN
					)
				}
			);
			callees.SelectedItemsChanged += delegate {
				if (callees.SelectedItems.Count != 0) {
					ContainerListViewItem item = callees.SelectedItems[0];
					if (item.Items.Count == 0) {
						foreach (FunctionInfo f in ((FunctionInfo)item.Tag).Children.Values) {
							callees.AddFunctionItem(item.Items, f);
						}
					}
				}
			};
			findText = new TextBox();
			findText.TextChanged += delegate {
				Find(true, false);
			};
			findText.KeyDown += delegate(object sender, KeyEventArgs e) {
				if (e.KeyCode == Keys.Enter) {
					Find(true, true);
					e.Handled = true;
				}
			};
			Menu = new MainMenu(new MenuItem[] {
				new MenuItem(
					"File",
					new MenuItem[] {
						//new MenuItem("&New",
						//    delegate {
						//        runs.Items.Clear();
						//        NProf.arguments.Text = "";
						//        NProf.application.Text = "";
						//        callees.Items.Clear();
						//        callers.Items.Clear();
						//    },
						//    Shortcut.CtrlN),
						//new MenuItem("-"),
						new MenuItem("E&xit",delegate {Close();})
					}),
				new MenuItem(
					"Project",
					new MenuItem[] {
						new MenuItem(
							"Start",
							delegate{StartRun();},
							Shortcut.F5),
						new MenuItem("-"),
						new MenuItem("Find method",delegate{ShowSearch();},Shortcut.CtrlF)
			})});
			Panel rightPanel = new Panel();
			rightPanel.Dock = DockStyle.Fill;
			SplitContainer methodPanel = new SplitContainer();
			methodPanel.Orientation = Orientation.Horizontal;
			//SplitContainer methodPanel = new SplitContainer();
			//methodPanel.FlowDirection = FlowDirection.TopDown;
			//Panel methodPanel = new Panel();
			//methodPanel.Size = new Size(100, 100);

			Splitter methodSplitter = new Splitter();
			methodSplitter.Dock = DockStyle.Bottom;
			findPanel = new FlowLayoutPanel();
			findPanel.Dock = DockStyle.Top;
			findPanel.BorderStyle = BorderStyle.FixedSingle;
			findPanel.Visible = false;
			findPanel.WrapContents = false;
			findPanel.AutoSize = true;
			findPanel.Dock = DockStyle.Top;
			Button closeFind = new Button();
			closeFind.Text = "x";
			closeFind.Width = 17;
			closeFind.Height = 20;
			closeFind.TextAlign = ContentAlignment.BottomLeft;
			closeFind.Click += delegate {findPanel.Visible = false;};

			Button findNext = new Button();
			findNext.AutoSize = true;
			findNext.Text = "Next";
			findNext.Click += new EventHandler(findNext_Click);

			Label findLabel = new Label();
			findLabel.Text = "Find:";
			findLabel.Dock = DockStyle.Fill;
			findLabel.TextAlign = ContentAlignment.MiddleLeft;
			findLabel.AutoSize = true;

			Button findPrevious = new Button();
			findPrevious.AutoSize = true;
			findPrevious.FlatAppearance.BorderSize = 0;
			findPrevious.Click += new EventHandler(findPrevious_Click);
			findPrevious.Text = "Previous";

			findPanel.Controls.AddRange(new Control[] {closeFind,findLabel,findText,findNext ,findPrevious});

			Panel calleePanel = MakePanel("Callees",callees);
			Panel callerPanel = MakePanel("Callers",callers);
			calleePanel.Dock = DockStyle.Fill;
			callerPanel.Dock = DockStyle.Fill;
			//calleePanel.Size = new Size(100, 100);
			//callerPanel.Size = new Size(100, 100);
			//calleePanel.Dock = DockStyle.Top;
			//callerPanel.Dock = DockStyle.Bottom;
			methodPanel.BackColor = Color.Blue;
			methodPanel.Panel1.Controls.Add(calleePanel);
			methodPanel.Panel2.Controls.Add(callerPanel);
			//methodPanel.Controls.AddRange(new Control[] { calleePanel, methodSplitter, callerPanel, findPanel });

			Splitter mainSplitter = new Splitter();
			findPanel.Dock = DockStyle.Top;
			mainSplitter.Dock = DockStyle.Left;
			// TODO: use SplitContainer instead of normal panel
			rightPanel.Controls.AddRange(new Control[] { methodPanel, mainSplitter, runs });
			rightPanel.Controls.Add(findPanel);
			methodPanel.Dock = DockStyle.Fill;


			TableLayoutPanel mainPanel = new TableLayoutPanel();
			application = new TextBox();
			application.Width = 300;
			arguments = new TextBox();
			arguments.Width = 300;
			mainPanel.Height = 100;
			mainPanel.AutoSize = true;
			mainPanel.Dock = DockStyle.Top;

			Label applicationLabel = new Label();
			applicationLabel.Text = "Executable:";
			applicationLabel.Dock = DockStyle.Fill;
			applicationLabel.TextAlign = ContentAlignment.MiddleLeft;
			applicationLabel.AutoSize = true;

			mainPanel.Controls.Add(applicationLabel, 0, 0);
			mainPanel.Controls.Add(application, 1, 0);
			Button browse = new Button();
			browse.Text = "&Browse...";
			browse.TabIndex = 0;
			browse.Focus();
			browse.Click += delegate {
				OpenFileDialog dialog = new OpenFileDialog();
				dialog.Filter = "Executable files (*.exe)|*.exe";
				DialogResult dr = dialog.ShowDialog();
				if (dr == DialogResult.OK) {
					application.Text = dialog.FileName;
					application.Focus();
					application.SelectAll();
				}
			};
			mainPanel.Controls.Add(browse, 2, 0);
			Label argumentLabel = new Label();
			argumentLabel.Text = "Arguments:";
			argumentLabel.Dock = DockStyle.Fill;
			argumentLabel.TextAlign = ContentAlignment.MiddleLeft;
			argumentLabel.AutoSize = true;
			mainPanel.Controls.Add(argumentLabel, 0, 1);
			mainPanel.Controls.Add(arguments, 1, 1);
			Controls.AddRange(new Control[] { rightPanel, mainPanel});
			application.TextChanged += delegate {
				string fileName = Path.GetFileNameWithoutExtension(application.Text);
				Text = fileName + " - " + Title;
			};
		}

		private static Panel MakePanel(string name, MethodView view) {
			Panel panel = new Panel();
			//panel.FlowDirection = FlowDirection.TopDown;
			panel.BackColor = Color.Green;

			Label label = new Label();
			label.TextAlign = ContentAlignment.MiddleLeft;
			label.Text = name;
			label.BackColor = Color.Red;
			label.Dock = DockStyle.Top;
			view.BackColor = Color.Yellow;
			view.Size = new Size(100, 100);
			view.Dock = DockStyle.Fill;
			panel.Controls.Add(view);
			panel.Controls.Add(label);
			return panel;
		}
		//private static FlowLayoutPanel MakePanel(string name, MethodView view) {
		//    FlowLayoutPanel panel = new FlowLayoutPanel();
		//    panel.FlowDirection = FlowDirection.TopDown;
		//    panel.BackColor = Color.Green;
		//    Label label=new Label();
		//    label.Text=name;
		//    label.BackColor = Color.Red;
		//    //label.Dock = DockStyle.Top;
		//    view.BackColor = Color.Yellow;
		//    view.Size = new Size(100, 100);
		//    view.Dock = DockStyle.Fill;
		//    panel.Controls.Add(label);
		//    panel.Controls.Add(view);
		//    return panel;
		//}
		private void StartRun() {
			Run run = new Run(profiler);
			run.profiler.completed = new EventHandler(run.Complete);
			run.Start();
		}
		public void AddRun(Run run) {
			string text = Path.GetFileNameWithoutExtension(application.Text) + " " + runs.Items.Count;
			string title = Path.GetFileNameWithoutExtension(application.Text);
			ContainerListViewItem item = new ContainerListViewItem(title);
			item.Tag = run;
			runs.Items.Add(item);
			runs.SelectedItems.Clear();
			item.SubItems[0].Text = title;
			item.SubItems[1].Text = run.Duration.TotalSeconds.ToString("0.00;-0.00;0.00") + "s";
			runs.SelectedItems.Add(item);
			ShowRun(run);
		}
		public void ShowRun(Run run) {
			Run compareRun;
			if (runs.SelectedItems.Count > 1) {
				ContainerListViewItem first = runs.SelectedItems[runs.SelectedItems.Count - 1];
				compareRun = (Run)first.Tag;
			}
			else {
				compareRun = null;
			}
			callees.Update(run, run.functions,compareRun!=null?compareRun.functions:null,compareRun);
			callers.Update(run, run.callers,compareRun!=null?compareRun.callers:null,compareRun);
		}
		private void findNext_Click(object sender, EventArgs e) {
			callees.BeginUpdate();
			ContainerListViewItem item = callees.SelectedItems[0];
			callees.Items.Remove(item);
			callees.EndUpdate(); 
		}
		private void findPrevious_Click(object sender, EventArgs e) {
			Find(false, true);
		}
		[STAThread]
		static void Main(string[] args) {
			string s = Guid.NewGuid().ToString().ToUpper();
			Application.EnableVisualStyles();
			Application.Run(form);
		}
		public static NProf form = new NProf();

		private void InitializeComponent() {
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NProf));
			this.SuspendLayout();
			// 
			// NProf
			// 
			this.ClientSize = new System.Drawing.Size(292, 273);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "NProf";
			this.ResumeLayout(false);

		}
	}
	public class StackWalk {
		public StackWalk(int id, int index, List<int> frames) {
			this.id = id;
			this.frames = frames;
			this.length = index;
		}
		public readonly int length;
		public readonly int id;
		public readonly List<int> frames;
	}
	public class FunctionInfo {
		public Run run;
		public string Signature {
			get {
				return run.signatures[ID];
			}
		}
		public FunctionInfo(int ID,Run run) {
			this.run = run;
			this.ID = ID;
		}
		public readonly int ID;
		public int Samples;
		public int lastWalk;
		private Dictionary<int, FunctionInfo> children;
		public List<StackWalk> stackWalks = new List<StackWalk>();
		public Dictionary<int, FunctionInfo> Children {
			get {
				if (children == null) {
					children = new Dictionary<int, FunctionInfo>();
					foreach (StackWalk walk in stackWalks) {
						if (walk.length != 0) {
							FunctionInfo callee = Run.GetFunctionInfo(children, walk.frames[walk.length - 1],run);
							if (callee.lastWalk != walk.id) {
								callee.Samples++;
							}
							callee.stackWalks.Add(new StackWalk(walk.id, walk.length - 1, walk.frames));
						}
					}
				}
				return children;
			}
		}
	}
	public class Run {
		public static FunctionInfo GetFunctionInfo(Dictionary<int, FunctionInfo> functions, int id,Run run) {
			FunctionInfo result;
			if (!functions.TryGetValue(id, out result)) {
				result = new FunctionInfo(id,run);
				functions[id] = result;
			}
			return result;
		}
		public int maxSamples;
		public List<List<int>> stackWalks = new List<List<int>>();
		private void InterpreteData() {
			Interprete(functions,false,this);
			Interprete(callers,true,this);
			List<FunctionInfo> startFunctions = new List<FunctionInfo>();
			maxSamples = 0;
			foreach (List<int> stackWalk in stackWalks) {
				if (stackWalk.Count != 0) {
					maxSamples++;
				}
			}
			//MessageBox.Show(stackWalks.Count.ToString());
			//MessageBox.Show(maxSamples.ToString());
		}
		private void Interprete(Dictionary<int, FunctionInfo> map,bool reverse,Run run) {
			int currentWalk = 0;
			foreach (List<int> original in stackWalks) {
				currentWalk++;
				List<int> stackWalk;
				if (reverse) {
					stackWalk = new List<int>(original);
					stackWalk.Reverse();
				}
				else {
					stackWalk = original;
				}
				for (int i = 0; i < stackWalk.Count; i++) {
					FunctionInfo function = Run.GetFunctionInfo(map, stackWalk[stackWalk.Count - i - 1],run);
					if (function.lastWalk != currentWalk) {
						function.Samples++;
						function.stackWalks.Add(new StackWalk(currentWalk, stackWalk.Count - i - 1, stackWalk));
					}
					function.lastWalk = currentWalk;
				}
			}
		}
		private string ReadString(BinaryReader br) {
			int length = br.ReadInt32();
			byte[] abString = new byte[length];
			br.Read(abString, 0, length);
			return System.Text.ASCIIEncoding.ASCII.GetString(abString, 0, length);
		}
		private string FileName {
			get {
				// TODO: prefix with nprof, to make it easier to find
				return Path.Combine(Path.GetDirectoryName(Path.GetTempFileName()), Profiler.ProfilerGuid + ".nprof");
			}
		}
		public TimeSpan Duration
		{
			get
			{
				return this.end - this.start;
			}
		}
		private void ReadStackWalks() {
			using (BinaryReader r = new BinaryReader(File.Open(FileName, FileMode.Open))) {
				this.end = DateTime.Now;
				while (true) {
					int functionId = r.ReadInt32();
					if (functionId == -1) {
						break;
					}
					signatures[functionId] = ReadString(r);
				}
				while (true) {
					List<int> stackWalk = new List<int>();
					while (true) {
						int functionId = r.ReadInt32();
						if (functionId == -1) {
							break;
						}
						else if (functionId == -2) {
							return;
						}
						stackWalk.Add(functionId);
					}
					stackWalks.Add(stackWalk);
				}
			}
		}
		public void Complete(object sender, EventArgs e) {
			if (File.Exists(FileName)) {
				ReadStackWalks();
				File.Delete(FileName);
			}
			NProf.form.BeginInvoke(new EventHandler(delegate {
				InterpreteData();
				NProf.form.AddRun(this);
			}));
		}
		public Dictionary<int, string> signatures = new Dictionary<int, string>();
		public Dictionary<int, FunctionInfo> functions = new Dictionary<int, FunctionInfo>();
		public Dictionary<int, FunctionInfo> callers = new Dictionary<int, FunctionInfo>();
		private DateTime start=DateTime.MinValue;
		private DateTime end=DateTime.MinValue;
		public Run(Profiler p) {
			this.profiler = p;
		}
		public bool Start() {
			start = DateTime.Now;
			// refactor this:
			return profiler.Start(this);
		}
		public Profiler profiler;
	}
	public class View : ContainerListView {
		public View() {
			this.Font=NProf.font;
			this.SizeChanged += delegate {
				this.Columns[0].Width = this.Width - 30;
			};
		}
	}
	public class MethodView : View {
		public void MoveTo(int id) {
			SelectedItems.Clear();
			foreach (ContainerListViewItem item in Items) {
				if (((FunctionInfo)item.Tag).ID == id) {
					SelectedItems.Add(item);
					EnsureVisible(item);
					Invalidate();
					this.Focus();
					break;
				}
			}
		}
		public void Update(Run run, Dictionary<int, FunctionInfo> functions, Dictionary<int, FunctionInfo> compareFunctions,Run oldRun) {
			currentRun = run;
			currentOldRun = oldRun;
			SuspendLayout();
			Invalidate();
			BeginUpdate();
			Items.Clear();
			foreach (FunctionInfo method in SortFunctions(functions.Values)) {
				FunctionInfo oldFunction;
				if (compareFunctions != null && compareFunctions.ContainsKey(method.ID)) {
					oldFunction = compareFunctions[method.ID];
				}
				else {
					oldFunction = null;
				}
				AddItem(Items, method,oldFunction);
			}
			foreach (ContainerListViewItem item in Items) {
				MakeSureComputed(item);
			}
			EndUpdate();
			ResumeLayout();
		}
		public void Find(string text, bool forward, bool step) {
			if (text != "") {
				ContainerListViewItem item;
				if (SelectedItems.Count == 0) {
					if (Items.Count == 0) {
						item = null;
					}
					else {
						item = Items[0];
					}
				}
				else {
					if (step) {
						if (forward) {
							item = SelectedItems[0].NextVisibleItem;
						}
						else {
							item = SelectedItems[0].PreviousVisibleItem;
						}
					}
					else {
						item = SelectedItems[0];
					}
				}
				ContainerListViewItem firstItem = item;
				while (item != null) {
					if (item.Text.ToLower().Contains(text.ToLower())) {
						SelectedItems.Clear();
						item.Focused = true;
						item.Selected = true;
						item.Focused = true;
						this.Invalidate();
						break;
					}
					else {
						if (forward) {
							item = item.NextVisibleItem;
						}
						else {
							item = item.PreviousVisibleItem;
						}
						if (item == null) {
							if (forward) {
								item = Items[0];
							}
							else {
								item = Items[Items.Count - 1];
							}
						}
					}
					if (item == firstItem) {
						break;
					}
				}
				if (item != null) {
					EnsureVisible(item);
				}
			}
		}
		public Run currentRun;
		public Run currentOldRun;
		public MethodView()
			: this(" Percent  Method signature") {
		}
		public MethodView(string name) {
			Columns.Add(name);
			//Columns[0].SortDataType = SortDataType.String;
			this.ShowPlusMinus = true;
			ShowRootTreeLines = true;
			ShowTreeLines = true;
			FullItemSelect = true;
			//ColumnSortColor = Color.White;
			Font = NProf.font;
			this.BeforeExpand += delegate(object sender,ContainerListViewCancelEventArgs e) {
				MakeSureComputed(e.Item);
			};
		}
		void MakeSureComputed(ContainerListViewItem item) {
			MakeSureComputed(item, true);
		}
		public List<FunctionInfo> SortFunctions(IEnumerable<FunctionInfo> f) {
			List<FunctionInfo> functions = new List<FunctionInfo>(f);
			functions.Sort(delegate(FunctionInfo a, FunctionInfo b) {
				return b.Samples.CompareTo(a.Samples);
			});
			return functions;
		}
		void MakeSureComputed(ContainerListViewItem item, bool parentExpanded) {
			if (item.Items.Count == 0) {
				foreach (FunctionInfo function in SortFunctions(((FunctionInfo)item.Tag).Children.Values)) {
					AddFunctionItem(item.Items, function);
				}
			}
			if (item.Expanded || parentExpanded) {
				foreach (ContainerListViewItem subItem in item.Items) {
					MakeSureComputed(subItem, item.Expanded);
				}
			}
		}
		private ContainerListViewItem AddItem(ContainerListViewItemCollection parent, FunctionInfo function,FunctionInfo oldFunction) {
			ContainerListViewItem item = parent.Add(currentRun.signatures[function.ID]);
			double fraction = ((double)function.Samples) / (double)currentRun.maxSamples;
    		double percent=(100.0*((double)function.Samples / (double)function.run.maxSamples));
			item.Text = " " + percent.ToString("0.00;-0.00;0.00").PadLeft(5, ' ') + "  " + currentRun.signatures[function.ID].Trim();
			item.Tag = function;
			return item;
		}
		void label_Paint(object sender, PaintEventArgs e) {
			e.Graphics.DrawRectangle(Pens.Red,new Rectangle(10,5,100,10));
		}
		public void AddFunctionItem(ContainerListViewItemCollection parent, FunctionInfo function) {
			ContainerListViewItem item = AddItem(parent, function,null);
			foreach (FunctionInfo callee in SortFunctions(function.Children.Values)) {
				AddItem(item.Items, callee,null);
			}
		}
		public void Add(FunctionInfo function) {
			AddFunctionItem(Items, function);
		}
	}
	public class Profiler {
		public const string ProfilerGuid = "107F578A-E019-4BAF-86A1-7128A749DB05";
		public const string Version = "0.11"; 
		public EventHandler completed;
		public bool Start(Run run) {
			this.run = run;
			process = new Process();
			process.StartInfo = new ProcessStartInfo(NProf.application.Text, NProf.arguments.Text);
			process.StartInfo.EnvironmentVariables["COR_ENABLE_PROFILING"] = "0x1";
			process.StartInfo.EnvironmentVariables["COR_PROFILER"] = "{" + ProfilerGuid + "}";
			process.StartInfo.UseShellExecute = false;
			process.EnableRaisingEvents = true;
			process.Exited += new EventHandler(OnProcessExited);
			return process.Start();
		}
		private void OnProcessExited(object oSender, EventArgs ea) {
			completed(null, null);
		}
		private Run run;
		private Process process;
	}
}