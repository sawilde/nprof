using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;
using System.Resources;
using System.Drawing.Text;
using System.Reflection;
using System.Collections;
using UtilityLibrary.Win32;
using UtilityLibrary.General;

namespace UtilityLibrary.WinControls
{
	/// <summary>
	/// Summary description for SortableListView.
	/// </summary>
	public class SortedListView : ListView
	{
		
		// Keeps track of the header control handle
		// so that we can distinguish between notification and
		// reflected messages
		IntPtr hHeader = IntPtr.Zero;

		// Keep track of what column was sorted last
		int lastSortedColumn = 0;
		bool setInitialSortColumn = false;

		HeaderHook headerHook = null;
		bool tracking = false;
		int pressedHeaderItem = -1;

		// To keep track if the cursor
		// hit a header divider
		Point lastMousePosition;

		// ImageList for the check boxes
		// in case user decide to use checkboxes
		ImageList checkBoxesImageList;

		// Header Icons
		ImageList headerImageList;
		ArrayList headerIconsList = new ArrayList();

		// Sorting helper 
		ArrayList rowSorterList = new ArrayList();
		
		// We only support small 16x16 icons
		const int IMAGE_WIDTH = 16;
		const int TEXT_TO_ARROW_GAP = 15;
		const int ARROW_WIDTH = 12;

		// Default Constructor
		public SortedListView()
		{
		
			SetStyle(ControlStyles.UserPaint, false);
				
			// Control needs to have full row select and detail
			// view enable otherwise it won't behave as intended
			FullRowSelect = true;
			View = View.Details;
			HeaderStyle = ColumnHeaderStyle.Nonclickable;
			InitializeCheckBoxesImageList();
			ListViewItemSorter = new CompareListItems(this);
		}

		private void InitializeCheckBoxesImageList()
		{
			checkBoxesImageList = new ImageList();
			checkBoxesImageList.ImageSize = new Size(16, 16);
			Assembly thisAssembly = Assembly.GetAssembly(Type.GetType("UtilityLibrary.WinControls.SortedListView"));
			ResourceManager rm = new ResourceManager("Resources.SortedListView", thisAssembly);
			Bitmap checkBox = (Bitmap)rm.GetObject("CheckBox");
			checkBox.MakeTransparent(Color.FromArgb(0, 128, 128));
			checkBoxesImageList.Images.AddStrip(checkBox);
		}

		internal int LastSortedColumn
		{
			set { lastSortedColumn = value; Invalidate(); }
			get { return lastSortedColumn; }
		}

		public int InitialSortedColumn
		{
			set 
			{ 
				if ( setInitialSortColumn == false )
				{	
					setInitialSortColumn = true;
					lastSortedColumn = value; 
				}
			}
		}

		internal bool Tracking
		{
			set { tracking = value; }
			get { return tracking; }
		}

		internal int PressedHeaderItem
		{
			set { pressedHeaderItem = value; }
			get { return pressedHeaderItem; }
		}
        
		internal Point LastMousePosition
		{
			set { lastMousePosition = value; }
			get { return lastMousePosition; }
		}

		public ImageList HeaderImageList
		{
			set { headerImageList = value; }
			get { return headerImageList; }
		}

		public void SetHeaderIcon(int headerIndex, int iconIndex)
		{
			// Associate an specific header with an specific image index
			// in the headerImageList
			headerIconsList.Add(new HeaderIconHelper(headerIndex, iconIndex));
		}

		public void SetColumnSortFormat(int columnIndex, SortedListViewFormatType format)
		{
			rowSorterList.Add(new RowSorterHelper(columnIndex, format));
		}

		public void SetColumnSortFormat(int columnIndex, SortedListViewFormatType format, ListSortEvent callBack)
		{
			rowSorterList.Add(new RowSorterHelper(columnIndex, format, callBack));
		}
		
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
		
			// Now that the list control has been created
			// get a hold of the header control so that we can
			// subclass it
			// -- Header control always has a control ID equal zero
			hHeader = WindowsAPI.GetDlgItem(Handle, 0);
			Debug.Assert(hHeader != IntPtr.Zero, "Fail to get Header Control Windows Handle...");
			headerHook = new HeaderHook(this);
			headerHook.AssignHandle(hHeader);
		}
	
		protected override  void WndProc(ref Message message)
		{
			base.WndProc(ref message);
			
			switch (message.Msg)
			{
				case (int)Msg.WM_ERASEBKGND:
					IntPtr hDC = (IntPtr)message.WParam;
					PaintBackground(hDC);
					break;
				
					//Notification messages come from the header
				case (int)Msg.WM_NOTIFY:
					NMHDR nm1 = (NMHDR) message.GetLParam(typeof(NMHDR));
				switch(nm1.code)
				{
					case (int)NotificationMessages.NM_CUSTOMDRAW:
						NotifyHeaderCustomDraw(ref message); 
						break;
					case (int)HeaderControlNotifications.HDN_BEGINTRACKW:
						Tracking = true;
						break;
					case (int)HeaderControlNotifications.HDN_ENDTRACKW:
						Tracking = false;
						break;
					default:
						break;
				}
					break;
				
					//Reflected Messages come from the list itself
				case (int)ReflectedMessages.OCM_NOTIFY:
					NMHDR nm2 = (NMHDR) message.GetLParam(typeof(NMHDR));	
				switch (nm2.code)
				{
					case (int)NotificationMessages.NM_CUSTOMDRAW:
						NotifyListCustomDraw(ref message); 
						break;
					default:
						break;
				}
					break;
				
					// Default 
				default:
					break;
			}
		}

		private void PaintBackground(IntPtr hDC)
		{
			Graphics g = Graphics.FromHdc(hDC);
		
			if ( lastSortedColumn == -1 )
				return;
			Rectangle rc = GetSubItemRect(0, lastSortedColumn);
			if ( lastSortedColumn == 0 && Columns.Count > 1)
			{
				Rectangle rcSecondItem = GetSubItemRect(0, 1);
				rc = new Rectangle(rc.Left, rc.Top, rcSecondItem.Left, rc.Height);
			}
			g.FillRectangle(new SolidBrush(Color.FromArgb(247,247,247)), rc.Left, rc.Top, rc.Width, Height);
		}

		private bool NotifyListCustomDraw(ref Message m)
		{
			m.Result = (IntPtr)CustomDrawReturnFlags.CDRF_DODEFAULT;
			NMCUSTOMDRAW nmcd = (NMCUSTOMDRAW)m.GetLParam(typeof(NMCUSTOMDRAW));
			IntPtr thisHandle = Handle;
			
			if ( nmcd.hdr.hwndFrom != Handle)
				return false;
			switch (nmcd.dwDrawStage)
			{
				case (int)CustomDrawDrawStateFlags.CDDS_PREPAINT:
					// Ask for Item painting notifications
					m.Result = (IntPtr)CustomDrawReturnFlags.CDRF_NOTIFYITEMDRAW;
					break;
				case (int)CustomDrawDrawStateFlags.CDDS_ITEMPREPAINT:
					m.Result = (IntPtr)CustomDrawReturnFlags.CDRF_NOTIFYSUBITEMDRAW;
					break;
				case (int)(CustomDrawDrawStateFlags.CDDS_ITEMPREPAINT | CustomDrawDrawStateFlags.CDDS_SUBITEM):
					// Draw background
					DoListCustomDrawing(ref m);
					break;
				default:
					break;

			}
			return false;
		}

		private void DoListCustomDrawing(ref Message m)
		{
			NMLVCUSTOMDRAW lvcd = (NMLVCUSTOMDRAW)m.GetLParam(typeof(NMLVCUSTOMDRAW));
			int row = lvcd.nmcd.dwItemSpec;
			int col = lvcd.iSubItem;

			// If we don't have any items we must be doing something wrong
			// because the list is only going to request custom drawing of items
			// in the list, if we have items in the list, the Items cannot possibly
			// be zero
			Debug.Assert(Items.Count != 0);
            ListViewItem lvi = Items[row];
			Rectangle rc = GetSubItemRect(row, col);
			
			// Draw the item
			// We did not need to actually paint the items that are not selected
			// but doing all the painting ourselves eliminates some random bugs where
			// the list sometimes did not update a subitem  that was not selected anymore
			// leaving the subitem with a different background color 
			// than the rest of the row
			Graphics g = Graphics.FromHdc(lvcd.nmcd.hdc);
						
			// Draw Fill Rectangle
			if ( IsRowSelected(row) )
			{
				int subItemOffset = 2;
				if ( GridLines )
				{
					subItemOffset = 3;
				}
						
				g.FillRectangle(new SolidBrush(SystemColors.Highlight), rc.Left+1, rc.Top+1, rc.Width-2, rc.Height-subItemOffset);
				
				// Draw Border
				if ( col == 0 )
				{
					Color borderColor = SystemColors.Highlight;
					int heightOffset = 1;
					if ( GridLines )
						heightOffset = 2;
					g.DrawRectangle(new Pen(borderColor), rc.Left+1, rc.Top, rc.Width-2, rc.Height-heightOffset);
				}
			}
			else 
			{
				if ( col == lastSortedColumn )
				{
					if ( col == 0)
						rc = AdjustFirstItemRect(rc);
					g.FillRectangle(new SolidBrush(Color.FromArgb(247,247,247)), rc.Left, rc.Top, rc.Width, Width);
				}
				else 
					g.FillRectangle(new SolidBrush(SystemColors.Window), rc.Left, rc.Top, rc.Width, rc.Height);
			}

			// Adjust rectangle, when getting the rectangle for column zero
			// the rectangle return is the one for the whole control
			if ( col == 0)
			{
				rc = AdjustFirstItemRect(rc);
			}
							
			// Draw Text
			string text = GetSubItemText(row, col);
			Size textSize = TextUtil.GetTextSize(g, text, Font);
			int gap = 4;
			Point pos = new Point(rc.Left+gap ,rc.Top + ((rc.Height - textSize.Height) / 2));
			
			// I use the Windows API instead of the Graphics object to draw the string
			// because the Graphics object draws ellipes without living blank spaces in between
			// the DrawText API adds those blank spaces in between 
			int ellipsingTringgering = 8;
			
			if ( CheckBoxes && col == 0 )
			{
				// draw checkbox
				int checkIndex = 0;
				if ( lvi.Checked)
					checkIndex = 1;
				g.DrawImage(checkBoxesImageList.Images[checkIndex], rc.Left + gap, rc.Top);
				pos.X += IMAGE_WIDTH;
				rc.Width = rc.Width - IMAGE_WIDTH; 
			}
			else if ( col == 0 && CheckBoxes == false && lvi.ImageIndex != -1 && lvi.ImageList != null )
			{
				ImageList imageList = lvi.ImageList;
				Image image = imageList.Images[lvi.ImageIndex];
				if ( image != null )
				{
					g.DrawImage(imageList.Images[lvi.ImageIndex], rc.Left + gap, rc.Top);
					pos.X += IMAGE_WIDTH;
					rc.Width = rc.Width - IMAGE_WIDTH; 
				}
			}

			Rectangle drawRect = new Rectangle(pos.X+2, pos.Y, rc.Width-gap-ellipsingTringgering, rc.Height);
			using ( Brush b = new SolidBrush(IsRowSelected(row) ? SystemColors.HighlightText : SystemColors.WindowText) )
			{
				StringFormat sf = StringFormat.GenericTypographic;
				sf.Trimming = StringTrimming.EllipsisCharacter;
				g.DrawString( text, Font, b, drawRect, sf );
			}
			//TextUtil.DrawText(g, text, Font, drawRect);
			g.Dispose();
			
			// Put structure back in the message
			Marshal.StructureToPtr(lvcd, m.LParam, true);
			m.Result = 	(IntPtr)CustomDrawReturnFlags.CDRF_SKIPDEFAULT;

		}

		private Rectangle AdjustFirstItemRect(Rectangle rc)
		{
			ListViewItem lvi = Items[0];
			if ( Columns.Count > 1)
			{
				Rectangle rcSecondItem = GetSubItemRect(0, 1);
				Rectangle RC = new Rectangle(rc.Left, rc.Top, rcSecondItem.Left, rc.Height);
				return RC;
			}
			else 
			{
				return rc;
			}
		}
		
		private bool NotifyHeaderCustomDraw(ref Message m)
		{
			m.Result = (IntPtr)CustomDrawReturnFlags.CDRF_DODEFAULT;
			NMCUSTOMDRAW nmcd = (NMCUSTOMDRAW)m.GetLParam(typeof(NMCUSTOMDRAW));
			if ( nmcd.hdr.hwndFrom != hHeader)
				return false;
			
			switch (nmcd.dwDrawStage)
			{
				case (int)CustomDrawDrawStateFlags.CDDS_PREPAINT:
					// Ask for Item painting notifications
					m.Result = (IntPtr)CustomDrawReturnFlags.CDRF_NOTIFYITEMDRAW;
					break;
				case (int)CustomDrawDrawStateFlags.CDDS_ITEMPREPAINT:
					DoHeaderCustomDrawing(ref m);			
					break;
				case (int)NotificationMessages.NM_NCHITTEST:
					break;
				default:
					break;

			}
			return false;
		}

		private void DoHeaderCustomDrawing(ref Message m)
		{
			NMCUSTOMDRAW nmcd = (NMCUSTOMDRAW)m.GetLParam(typeof(NMCUSTOMDRAW));
			Graphics g = Graphics.FromHdc(nmcd.hdc);
			
			Rectangle rc = GetHeaderCtrlRect();
			rc = GetHeaderItemRect(nmcd.dwItemSpec);
			int itemRight = rc.Left + rc.Width;
			g.FillRectangle(new SolidBrush(SystemColors.ScrollBar), rc.Left, rc.Top, rc.Width, rc.Height);
		
			if ( nmcd.dwItemSpec == PressedHeaderItem && !IsCursorOnDivider() && Tracking == false )
			{
				PressedHeaderItem = -1;
				rc.Inflate(-1, -1);
				g.FillRectangle(new SolidBrush(ColorUtil.VSNetPressedColor), rc.Left, rc.Top, rc.Width, rc.Height);

			}
			else
			{
				ControlPaint.DrawBorder3D(g, rc.Left, rc.Top, rc.Width, 
					rc.Height-1, Border3DStyle.RaisedInner, Border3DSide.All);
			}

			string text = GetHeaderItemText(nmcd.dwItemSpec);
			Size textSize = TextUtil.GetTextSize(g, text, Font);
			int gap = 4;
			Point pos = new Point(rc.Left+gap ,rc.Top + ((rc.Height - textSize.Height) / 2));

			int headerImageIndex;
			if ( headerIconsList != null && HasHeaderImage(nmcd.dwItemSpec, out headerImageIndex) )
			{
				if ( headerImageIndex != -1 )
				{
					Image image = headerImageList.Images[headerImageIndex];
					if ( image != null )
					{
						g.DrawImage(headerImageList.Images[headerImageIndex], rc.Left + gap, rc.Top);
						pos.X += IMAGE_WIDTH;
						rc.Width = rc.Width - IMAGE_WIDTH;
					}
				}
			}
			
			// Draw arrow glyph
			if ( nmcd.dwItemSpec == lastSortedColumn)
			{
				int Left = pos.X+2;
				Left += textSize.Width + TEXT_TO_ARROW_GAP;
				Rectangle arrowRect = new Rectangle(Left, rc.Top, ARROW_WIDTH, rc.Height); 
				if ( itemRight >= (Left + ARROW_WIDTH + 4) ) 
				{
					if ( Sorting == SortOrder.Ascending  || Sorting == SortOrder.None )
						DrawUpArrow(g, arrowRect);
					else
						DrawDownArrow(g, arrowRect);
				}
			}

            // I use the Windows API instead of the Graphics object to draw the string
			// because the Graphics object draws ellipes without living blank spaces in between
			// the DrawText API adds those blank spaces in between 
			int ellipsingTringgering = 8;
			Rectangle drawRect = new Rectangle(pos.X+2, pos.Y, rc.Width-gap-ellipsingTringgering, rc.Height);
			StringFormat sf = StringFormat.GenericTypographic;
			sf.Trimming = StringTrimming.EllipsisCharacter;
			g.DrawString( text, Font, SystemBrushes.ControlText, drawRect, sf);
			//TextUtil.DrawText(g, text, Font, drawRect);
			g.Dispose();
          				
			m.Result = 	(IntPtr)CustomDrawReturnFlags.CDRF_SKIPDEFAULT;
          
		}

		bool HasHeaderImage(int headerIndex, out int imageIndex)
		{
			imageIndex = -1;
			for ( int i = 0; i < headerIconsList.Count; i++ )
			{
				HeaderIconHelper hih = (HeaderIconHelper)headerIconsList[i];
				if ( hih != null && hih.HeaderIndex == headerIndex )
				{
					imageIndex = hih.IconIndex;
					return true;
				}
			}

			return false;

		}

		void DrawUpArrow(Graphics g, Rectangle rc)
		{
			int xTop = rc.Left + rc.Width/2;
			int yTop = (rc.Height - 6)/2;
            
			int xLeft = xTop - 4;
			int yLeft = yTop + 6;
            
			int xRight = xTop + 4;
			int yRight = yTop + 6;

			g.DrawLine(new Pen(SystemColors.ControlDarkDark), xLeft, yLeft, xTop, yTop);
			g.DrawLine(new Pen(Color.White), xRight, yRight, xTop, yTop);
			g.DrawLine(new Pen(Color.White), xLeft, yLeft, xRight, yRight);

		}

		void DrawDownArrow(Graphics g, Rectangle rc)
		{
			int xBottom = rc.Left + rc.Width/2;
			            
			int xLeft = xBottom - 4;
			int yLeft = (rc.Height - 6)/2;;
            
			int xRight = xBottom + 4;
			int yRight = (rc.Height - 6)/2;

			int yBottom = yRight + 6;

			g.DrawLine(new Pen(SystemColors.ControlDarkDark), xLeft, yLeft, xBottom, yBottom);
			g.DrawLine(new Pen(Color.White), xRight, yRight, xBottom, yBottom);
			g.DrawLine(new Pen(SystemColors.ControlDarkDark), xLeft, yLeft, xRight, yRight);

		}
       
		private string GetSubItemText(int row, int col)
		{
			// Make sure we are not out of bounds
			ListViewItem lvi;
			ListViewItem.ListViewSubItem livsi = null;
			if ( row >= 0 && row < Items.Count)
			{
				lvi = Items[row];
				if ( col >= 0 && col < lvi.SubItems.Count )
					livsi = lvi.SubItems[col];
			}
			
			if ( livsi != null )
				return livsi.Text;
			else
				return "";

		}

		private Rectangle GetSubItemRect(int row, int col)
		{
			RECT rc = new RECT();
			rc.top = col;
			rc.left = (int)SubItemPortion.LVIR_BOUNDS;
			WindowsAPI.SendMessage(Handle, (int)ListViewMessages.LVM_GETSUBITEMRECT,  row, ref rc);
			return new Rectangle(rc.left, rc.top, rc.right-rc.left, rc.bottom-rc.top);
		}

		bool IsRowSelected(int row)
		{
			Debug.Assert(row >= 0 && row < Items.Count);
			ListViewItem lvi = Items[row];
			return lvi.Selected;
		}

		private Rectangle GetHeaderCtrlRect()
		{
			RECT rc = new RECT();
			WindowsAPI.GetClientRect(hHeader, ref rc);
			return new Rectangle(rc.left, rc.top, rc.right-rc.left,rc.bottom-rc.top);
		
		}

		public Rectangle GetHeaderItemRect(int index)
		{
			RECT rc = new RECT();
			WindowsAPI.SendMessage(hHeader, (int)HeaderControlMessages.HDM_GETITEMRECT, index, ref rc);
			return new Rectangle(rc.left, rc.top, rc.right-rc.left,rc.bottom-rc.top);
		}

		protected bool IsCursorOnDivider()
		{
			HD_HITTESTINFO hti = new HD_HITTESTINFO();
			hti.pt.x = LastMousePosition.X;
			hti.pt.y = LastMousePosition.Y;
			WindowsAPI.SendMessage(hHeader, (int)HeaderControlMessages.HDM_HITTEST, 0, ref hti); 
			bool hit = (hti.flags == (int)HeaderControlHitTestFlags.HHT_ONDIVIDER);
			return hit;
		}

		protected string GetHeaderItemText(int index)
		{
			if ( index >= 0 && index < Columns.Count )
				return Columns[index].Text;
			return "";
		}

		internal RowSorterHelper GetRowSorterHelper()
		{
			for ( int i = 0; i < rowSorterList.Count; i++ )
			{
				RowSorterHelper rs = (RowSorterHelper)rowSorterList[i];
				if ( rs != null && rs.ColumnIndex == LastSortedColumn )
				{
					return rs;
				}
			}
			return null;
		}
	}

	public delegate int ListSortEvent(ListViewItem item1, ListViewItem item2);

	public enum SortedListViewFormatType
	{
		String,
		Numeric,
		Date,
		Custom
	}

	public enum SortedListViewSortDirection
	{
        Ascending,
		Descending
	}

	internal class HeaderIconHelper
	{
		int headerIndex;
		int iconIndex;
		
		public HeaderIconHelper(int HeaderIndex, int IconIndex)
		{
			headerIndex = HeaderIndex;
			iconIndex = IconIndex;
		}

		public int HeaderIndex
		{
			set { headerIndex = value; }
			get { return headerIndex; }
		}

		public int IconIndex
		{
			set { iconIndex = value; }
			get { return iconIndex; }
		}

	}

	internal class RowSorterHelper
	{
		int columnIndex;
		SortedListViewFormatType format;
		ListSortEvent sortEvent = null;
		
		public RowSorterHelper(int columnIndex, SortedListViewFormatType format)
		{
			this.columnIndex = columnIndex;
			this.format = format;
       
		}

		public RowSorterHelper(int columnIndex, SortedListViewFormatType format, ListSortEvent sortEvent)
		{
			this.columnIndex = columnIndex;
			this.format = format;
			this.sortEvent = sortEvent;
		}

		public int ColumnIndex
		{
			set { columnIndex = value; }
			get { return columnIndex; }
		}

		public SortedListViewFormatType Format
		{
			set { format = value; }
			get { return format; }
		}

		public ListSortEvent SortEvent
		{
			set { sortEvent = value; }
			get { return sortEvent; }
		}

	}

	// This class is the key to the sorting
	public class CompareListItems : IComparer
	{
		SortedListView listView = null;
		public CompareListItems(SortedListView lv)
		{
			listView = lv;
		}

		public int Compare(object obj1, object obj2)
		{
			ListViewItem item1 = (ListViewItem)obj1;
			ListViewItem item2 = (ListViewItem)obj2;
			RowSorterHelper rs = listView.GetRowSorterHelper();
			string string1 = item1.Text;
			string string2 = item2.Text;
			int result = 0;
												
			if ( listView.LastSortedColumn != 0 )
			{
				// adjust the objets if we have to sort subitems
                string1 = item1.SubItems[listView.LastSortedColumn].Text;
				string2 = item2.SubItems[listView.LastSortedColumn].Text;
				Debug.Assert(obj1 != null && obj2 != null);
			}

			if ( rs != null )
			{
				if ( rs.Format == SortedListViewFormatType.String)
					result = CompareStrings(string1, string2, listView.Sorting);
				else if ( rs.Format == SortedListViewFormatType.Numeric )
					result = CompareNumbers(string1, string2, listView.Sorting);
				else if ( rs.Format == SortedListViewFormatType.Date )
					result = CompareDates(string1, string2, listView.Sorting);
				else if ( rs.Format == SortedListViewFormatType.Custom)
				{
					if ( rs.SortEvent != null )
					{
						result = rs.SortEvent((ListViewItem)obj1, (ListViewItem)obj2);
						if ( listView.Sorting == SortOrder.Descending )
							result *= -1;
					}
				}
			}
			else if ( rs == null )
			{
				// Consider column as strings
				result = CompareStrings(string1, string2, listView.Sorting);
			}
			
			return result;
		}

		private int CompareStrings(string string1, string string2, SortOrder sortOrder)
		{
			int result = string.Compare(string1, string2);
			if ( sortOrder == SortOrder.Descending)
				result *= -1;
			return result;
		}

		private int CompareNumbers(string string1, string string2, SortOrder sortOrder)
		{
			// Parse the object as if the were floating number that will take
			// care of both cases: integers and floats
			// -- exceptions will be thrown if they cannot be parsed
			float float1 = float.Parse(string1);
			float float2 = float.Parse(string2);
			int result = float1.CompareTo(float2);
			if ( sortOrder == SortOrder.Descending)
				result *= -1;
			return result;
			
		}

		private int CompareDates(string string1, string string2, SortOrder sortOrder)
		{
			// Parse the object as if the were floating number that will take
			// care of both cases: integers and floats
			// -- exceptions will be thrown if they cannot be parsed
			DateTime date1 = DateTime.Parse(string1);
			DateTime date2 = DateTime.Parse(string2);
			int result = DateTime.Compare(date1, date2);
			if ( sortOrder == SortOrder.Descending)
				result *= -1;
			return result;
			
		}
	}


	internal class HeaderHook : System.Windows.Forms.NativeWindow
	{
		
		SortedListView listView = null;
		public HeaderHook(SortedListView lv)
		{
			listView = lv;
		}

		protected override  void WndProc(ref Message m)
		{
			int message = m.Msg;
			Point mousePos = new Point(0,0);
			if ( message == (int)Msg.WM_LBUTTONDOWN || 
				message == (int)Msg.WM_LBUTTONUP) 
			{
				mousePos = new Point((short)m.LParam & 0x0000FFFF, 
					(short)((uint)m.LParam & 0xFFFF0000)); 
				listView.LastMousePosition = mousePos;
			}

			if ( message == (int)Msg.WM_LBUTTONDOWN && listView.Tracking == false ) 
			{
				for (int i = 0; i < listView.Columns.Count; i++ )
				{
					Rectangle rc = listView.GetHeaderItemRect(i);
					if ( rc.Contains(mousePos))
					{
						listView.PressedHeaderItem = i;
						WindowsAPI.InvalidateRect(m.HWnd, IntPtr.Zero, 0);
						break;
					}
				}
			}
			
			if ( message == (int)Msg.WM_LBUTTONUP && listView.Tracking == false )
			{
				listView.PressedHeaderItem = -1;
								
				for (int i = 0; i < listView.Columns.Count; i++ )
				{
					Rectangle rc = listView.GetHeaderItemRect(i);
					if ( rc.Contains(mousePos))
					{
						listView.LastSortedColumn = i;
						if ( listView.Sorting == SortOrder.None)
						{
							// We will set the sorting to descending
							// because the default sorting already took place
							// and default sorting is ascending
							listView.Sorting = SortOrder.Descending;
						}
						else 
						{
							if ( listView.Sorting == SortOrder.Ascending )
								listView.Sorting = SortOrder.Descending;
							else
								listView.Sorting = SortOrder.Ascending;
						}
						break;
					}
				}

				// update item
				WindowsAPI.InvalidateRect(m.HWnd, IntPtr.Zero, 0);

			}

			base.WndProc(ref m);

		}
	}


}
