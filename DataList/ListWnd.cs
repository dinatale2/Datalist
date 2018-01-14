using System;
using System.Windows.Forms;
using Drawing.ThemeRoutines;
using System.Runtime.InteropServices;
using System.Drawing;
using Win32Lib;
using DebugLib;

namespace DataList
{
	internal class ListWnd : Control
	{
		#region Events
		internal event DataListRowSelected RowSelectionMade;
		internal event DataListRowClicked RowDoubleClicked;
		internal event DataListRowClicked RowClicked;
		internal event DataListCellEditStarting CellEditStarting;
		internal event DataListCellEditStarted CellEditStarted;
		internal event DataListCellEditFinishing CellEditFinishing;
		internal event DataListCellEditFinished CellEditFinished;
		internal event DataListCellEditFailed CellEditFailed;
		#endregion

		private DataList m_Parent;
		internal new DataList Parent
		{
			get { return m_Parent; }
		}

		internal int CellPadding
		{
			get { return 4; }
		}

		internal Row CurrSel
		{
			get { return m_CurrSelectedRow; }
			set
			{
				if (m_CurrSelectedRow != value)
					MakeRowSelection(value);
			}
		}

		internal Row CurrHighlight
		{
			get { return m_CurrHighlightRow; }
			set
			{
				ChangeHighlight(value);
			}
		}

		internal MultiWindowUxThemeManager Themes
		{
			get { return m_Parent.ThemeManager; }
		}

		private CellToolTip m_CellTool;
		internal CellToolTip CellTool
		{
			get { return m_CellTool; }
		}

		private ColumnHeader m_ColumnHeader;
		internal ColumnHeader ColumnHeader
		{
			get { return m_ColumnHeader; }
		}

		internal bool ReadOnly
		{
			get { return m_Parent.ReadOnly; }
			set
			{
				if (value)
				{
					CancelAllPendingEdits(false);
					ClearHighlight();

					if (!m_Parent.IsComboBox)
						ClearSelection();
					else
						InvalidateRow(m_CurrSelectedRow);
				}

				m_bMouseDownInRows = false;
				Invalidate();
			}
		}

		private CellTextBox m_CellEditBox;
		internal CellTextBox CellEditBox
		{
			get { return m_CellEditBox; }
		}

		internal bool IsEditing
		{
			get
			{
				foreach (Column c in Columns)
				{
					if (c.ComboBox != null && c.ComboBox.Visible)
						return true;
				}

				return m_CellEditBox.Visible;
			}
		}

		// get the rectangle representing the region our rows are being drawn in
		internal Rectangle RowBounds
		{
			get
			{
				return GetRowBounds(true);
			}
		}

		internal int xOffset
		{
			get { return m_HScrollBar.Visible ? -m_HScrollBar.Value : 0; }
		}

		#region Collections
		public ColumnCollection Columns
		{
			get { return m_ColumnHeader.Columns; }
		}

		private RowCollection m_Rows;
		internal RowCollection Rows
		{
			get { return m_Rows; }
		}

		private ColumnColorManager m_ColumnColors;
		internal ColumnColorManager ColumnColors
		{
			get { return m_ColumnColors; }
		}

		private RowColorManager m_RowColors;
		internal RowColorManager RowColors
		{
			get { return m_RowColors; }
		}

		private CellColorManager m_CellColors;
		internal CellColorManager CellColors
		{
			get { return m_CellColors; }
		}
		#endregion

		#region Member Variables
		private Row m_FirstVisRow;
		private int m_nTopRowY;

		private Row m_CurrHighlightRow;
		private Row m_CurrSelectedRow;
		private Cell m_CellMouseDown;

		private bool m_bSecondClickOnRow;
		private bool m_bMouseDownInRows;
		private bool m_bNeedHeightRecalc;

		// scroll bars and corner box
		private HScrollBar m_HScrollBar;
		private VScrollBar m_VScrollBar;
		private BorderObject m_CornerBox;

		private ThresholdDiag PaintFunc;
		private ThresholdDiag FindFirstVis;
		#endregion

		#region Construction and Destruction
		internal ListWnd(DataList parent)
		{
			m_Parent = parent;

			SetStyle(
				ControlStyles.AllPaintingInWmPaint |
				ControlStyles.OptimizedDoubleBuffer |
				ControlStyles.UserPaint |
				ControlStyles.UserMouse,
				true
				);

			m_CellTool = new CellToolTip(this);
			m_CellTool.ShowDelay = 800;

			// a cell edit box so the user can make changes to cells
			m_CellEditBox = new CellTextBox(this);
			this.Controls.Add(m_CellEditBox);
			m_CellEditBox.Visible = false;

			m_ColumnHeader = new ColumnHeader(this);
			m_ColumnHeader.ColumnResizing += new ColumnHeaderColumnChange(OnColumnResizing);
			m_ColumnHeader.ColumnResizeEnd += new ColumnHeaderColumnChange(OnColumnResized);
			m_ColumnHeader.ColumnLeftClicked += new DataListColumnLeftClick(OnColumnLeftClick);
			m_ColumnHeader.VisibleChanged += new EventHandler(OnColumnHeaderVisChange);
			m_ColumnHeader.ColumnAdded += new ColumnHeaderColumnChange(OnColumnAdded);
			m_ColumnHeader.ColumnRemoved += new ColumnHeaderColumnCollChange(OnColumnRemoved);

			// scroll bar initialization
			m_HScrollBar = new HScrollBar();
			m_HScrollBar.Parent = this;
			m_HScrollBar.Enabled = true;
			m_HScrollBar.Height = SystemInformation.HorizontalScrollBarHeight;
			m_HScrollBar.Scroll += new ScrollEventHandler(OnScroll);
			m_HScrollBar.MouseCaptureChanged += new EventHandler(OnScrollBarMouseCapture);
			m_HScrollBar.MouseEnter += new EventHandler(OnScrollBarMouseEnter);
			m_HScrollBar.MouseLeave += new EventHandler(OnScrollBarMouseLeave);
			this.Controls.Add(m_HScrollBar);

			m_VScrollBar = new VScrollBar();
			m_VScrollBar.Parent = this;
			m_VScrollBar.Enabled = true;
			m_VScrollBar.Width = SystemInformation.VerticalScrollBarWidth;
			m_VScrollBar.Scroll += new ScrollEventHandler(OnScroll);
			m_VScrollBar.MouseCaptureChanged += new EventHandler(OnScrollBarMouseCapture);
			m_VScrollBar.MouseEnter += new EventHandler(OnScrollBarMouseEnter);
			m_VScrollBar.MouseLeave += new EventHandler(OnScrollBarMouseLeave);
			this.Controls.Add(m_VScrollBar);

			// corner box for when both scroll bars are visible
			m_CornerBox = new BorderObject(BorderType.CornerBox);
			m_CornerBox.Enabled = true;
			m_CornerBox.Height = m_HScrollBar.Height;
			m_CornerBox.Width = m_VScrollBar.Width;
			m_CornerBox.BringToFront();
			m_CornerBox.MouseCaptureChanged += new EventHandler(OnScrollBarMouseCapture);
			m_CornerBox.MouseEnter += new EventHandler(OnScrollBarMouseEnter);
			m_CornerBox.MouseLeave += new EventHandler(OnScrollBarMouseLeave);
			this.Controls.Add(m_CornerBox);

			// scroll bars and corner box cant have tab stops and start as not visible
			m_HScrollBar.TabStop = false;
			m_VScrollBar.TabStop = false;
			m_CornerBox.TabStop = false;
			m_HScrollBar.Visible = false;
			m_VScrollBar.Visible = false;
			m_CornerBox.Visible = false;

			this.BackColor = Color.White;
			this.TabStop = false;
			m_Rows = new RowCollection(this);

			m_RowColors = new RowColorManager(this);
			m_ColumnColors = new ColumnColorManager(this);
			m_CellColors = new CellColorManager(this);

			m_FirstVisRow = null;
			m_nTopRowY = 0;
			m_CurrSelectedRow = null;

			m_bSecondClickOnRow = false;
			m_bMouseDownInRows = false;
			m_bNeedHeightRecalc = false;

			RecalculateScrollBars();

			PaintFunc = new ThresholdDiag("OnPaint", null, 100.0);
			FindFirstVis = new ThresholdDiag("FindFirstVisibleRow", null, 100.0);
		}

		~ListWnd()
		{
			m_RowColors.ClearColors();
			m_ColumnColors.ClearColors();
			m_CellColors.ClearColors();

			m_Rows.Clear();
			m_ColumnHeader.Columns.Clear();

			PaintFunc = null;
			FindFirstVis = null;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (!IsDisposed)
				{
					PaintFunc.Dispose();
					FindFirstVis.Dispose();

					m_CellEditBox.Dispose();
					m_CellTool.Dispose();
				}
			}

			base.Dispose(disposing);
		}
		#endregion

		#region Column Operations
		void OnColumnLeftClick(int ColIndex)
		{
			m_CellEditBox.CancelPendingEdit(false);
			m_Rows.SortRows();
			m_FirstVisRow = null;
			this.Invalidate();
		}

		// TODO: rewrite this, height recalc happens in onPaint, this forces us to refresh our control,
		// then invalidate after we recalculate our scroll bars. Need to remove the refresh call
		int m_VScrollValueLock = -1;
		int m_HScrollValueLock = -1;
		void OnColumnResizing(int ColIndex)
		{
			if (m_VScrollValueLock < 0)
				m_VScrollValueLock = m_VScrollBar.Value;

			if (m_HScrollValueLock < 0)
				m_HScrollValueLock = m_HScrollBar.Value;

			m_bNeedHeightRecalc = Columns[ColIndex].IsVariableHeight();

			if (m_bNeedHeightRecalc)
				m_FirstVisRow = null;

			RecalculateScrollBars();
			this.Invalidate();
		}

		void OnColumnResized(int ColIndex)
		{
			m_HScrollValueLock = -1;
			m_VScrollValueLock = -1;
			RecalculateScrollBars();
			this.Invalidate();
		}

		void OnColumnHeaderVisChange(object sender, EventArgs e)
		{
			RecalculateScrollBars();
			m_FirstVisRow = null;
			this.Invalidate();
		}

		internal void EnsureColumnInView(Column c, bool bFullyVisible = true)
		{
			int x = m_ColumnHeader.FindColumnXPos(c, this.xOffset);
			Rectangle CurrBounds = RowBounds;

			if (!bFullyVisible && (CurrBounds.Contains(x, m_Parent.ShowColumnHeader ? m_ColumnHeader.ColumnHeight : 0)
				|| CurrBounds.Contains(x + c.Width, m_Parent.ShowColumnHeader ? m_ColumnHeader.ColumnHeight : 0)))
				return;

			if (x < 0 || c.Width > CurrBounds.Width)
			{
				DoHScrollAdjustment(x);
			}
			else
			{
				if ((x + c.Width) > CurrBounds.Width)
				{
					DoHScrollAdjustment((x + c.Width) - CurrBounds.Width);
				}
			}
		}

		internal void AddColumn(string dispName, DatalistDataTypes valType, int width, ColumnType ColType, bool bAllowEdit)
		{
			// TODO handle adding cells etc
			m_ColumnHeader.AddColumn(dispName, valType, width, ColType, bAllowEdit);
		}

		internal void OnColumnAdded(int nIndex)
		{
			RecalculateScrollBars();
			this.Invalidate(true);
		}

		void OnColumnRemoved(Column col)
		{
			if (col.IsVariableHeight())
				m_bNeedHeightRecalc = true;

			RecalculateScrollBars();
			m_ColumnColors.RemoveAllColors(col.Index);

			this.Invalidate(true);
		}
		#endregion

		#region Invalidation
		internal void InvalidateRow(Row ToInvalidate)
		{
			if (ToInvalidate == null || ToInvalidate.Parent != this)
				return;

			if (m_FirstVisRow == null && !FindFirstVisibleRow(out m_FirstVisRow, out m_nTopRowY))
				return;

			int nHeight = m_ColumnHeader.ColumnHeight;
			int nCurrY = m_nTopRowY + (m_ColumnHeader.Visible ? nHeight : 0);
			int nCurrX = -m_HScrollBar.Value;
			Row CurRow = m_FirstVisRow;
			int nMaxY = RowBounds.Height + (m_ColumnHeader.Visible ? nHeight : 0);

			while (CurRow != null && nCurrY < nMaxY)
			{
				if (CurRow == ToInvalidate)
				{
					Rectangle CurrRowBounds = new Rectangle(nCurrX, nCurrY, Columns.RowWidth, ToInvalidate.Height);
					CurrRowBounds.Intersect(RowBounds);
					this.Invalidate(CurrRowBounds, false);

					if (m_CellTool.Visible)
						m_CellTool.InvalidateToolTip();

					return;
				}

				nCurrY += CurRow.Height;
				CurRow = CurRow.GetNextRow();
			}
		}

		internal void InvalidateCell(Cell ToInvalidate)
		{
			if (ToInvalidate.ParentRow.Parent != this || ToInvalidate.ParentRow.ParentNode == null)
				return;

			Point ptCell = GetCellLocation(ToInvalidate);

			if (!ptCell.IsEmpty)
			{
				Rectangle rectCell = new Rectangle(ptCell, new Size(ToInvalidate.Width, ToInvalidate.Height));
				if (RowBounds.IntersectsWith(rectCell))
				{
					rectCell.Intersect(RowBounds);
					this.Invalidate(rectCell, false);

					if (m_CellTool.Visible)
						m_CellTool.InvalidateToolTip();

					return;
				}
			}
		}

		internal void InvalidateColumn(int ColIndex)
		{
			if (ColIndex >= 0 && ColIndex < Columns.Count)
				InvalidateColumn(Columns[ColIndex]);
		}

		internal void InvalidateColumn(Column ToInvalidate)
		{
			if (ToInvalidate.Width <= 0 || ToInvalidate.Parent.Parent != this)
				return;

			int nColX = m_ColumnHeader.FindColumnXPos(ToInvalidate, this.xOffset);

			Rectangle BoundsToInvalidate = new Rectangle(nColX, 0, ToInvalidate.Width, this.Height);

			if (RowBounds.IntersectsWith(BoundsToInvalidate))
				this.Invalidate(BoundsToInvalidate, false);

			if (m_CellTool.Visible)
				m_CellTool.InvalidateToolTip();
		}
		#endregion

		#region Cell Operations
		internal Point GetCellLocation(Cell c)
		{
			int X = m_ColumnHeader.FindColumnXPos(c.ColumnIndex, this.xOffset);

			if (m_FirstVisRow == null)
				if (!FindFirstVisibleRow(out m_FirstVisRow, out m_nTopRowY))
					return Point.Empty;

			Row CurrRow = m_FirstVisRow;
			int nCurrY = m_nTopRowY;
			int nHeight = m_ColumnHeader.ColumnHeight;
			int nMaxY = RowBounds.Height + (m_ColumnHeader.Visible ? nHeight : 0);

			while (CurrRow != null && nCurrY < nMaxY)
			{
				if (c.ParentRow == CurrRow)
					return new Point(X, nCurrY + (m_ColumnHeader.Visible ? nHeight : 0));

				nCurrY += CurrRow.Height;

				CurrRow = CurrRow.GetNextRow();
			}

			return Point.Empty;
		}
		#endregion

		#region Row Operations
		internal Row GetNewRow()
		{
			return new Row(this);
		}

		internal Row GetFirstRow()
		{
			return m_Rows.GetFirstRow();
		}

		internal void RecalcRowHeightByIndex(Row r, int nColIndex)
		{
			if (Columns[nColIndex].IsVariableHeight())
			{
				int nOldHeight = r.Height;
				Graphics GFX = CreateGraphics();
				int nNewHeight = r.RecalcHeight(GFX);
				GFX.Dispose();

				int nDelta = nNewHeight - nOldHeight;
				if (nDelta != 0)
				{
					m_Rows.AdjustHeightBy(nNewHeight - nOldHeight);
					this.Invalidate();
				}
			}
		}

		internal void Sort()
		{
			m_Rows.SortRows();
			m_FirstVisRow = null;
			this.Invalidate();
		}

		internal void RemoveRow(Row r)
		{
			if (r == null)
				return;

			r.RemoveRowColors();
			m_Rows.Remove(r.ParentNode);
			if (m_CurrSelectedRow == r)
				m_CurrSelectedRow = null;
			m_FirstVisRow = null;
			RecalculateScrollBars();
			this.Invalidate();
		}

		internal void ClearRows()
		{
			m_RowColors.ClearColors();
			m_CellColors.ClearColors();
			m_CurrSelectedRow = null;
			m_FirstVisRow = null;
			m_Rows.Clear();
			RecalculateScrollBars();
			this.Invalidate();
		}

		internal void AddRowAtEnd(Row r)
		{
			m_Rows.AddAtEnd(r);
			RecalculateScrollBars();
			this.Invalidate();
		}

		internal void AddRowBefore(Row r, Row rBefore)
		{
			m_Rows.AddBefore(r, rBefore);
			m_FirstVisRow = null;
			RecalculateScrollBars();
			this.Invalidate();
		}

		private void FindTopAndBottomRows()
		{
			FindFirstVisibleRow(out m_FirstVisRow, out m_nTopRowY);
		}

		private bool FindFirstVisibleRow(out Row FirstVisible, out int Y)
		{
			if (!m_VScrollBar.Visible)
			{
				FirstVisible = GetFirstRow();
				Y = 0;
				return true;
			}

			int nVScrollValue = -m_VScrollBar.Value;
			Row r = GetFirstRow();

			while (r != null)
			{
				nVScrollValue += r.Height;

				if (nVScrollValue >= 0)
				{
					FirstVisible = r;
					Y = nVScrollValue - FirstVisible.Height;
					//FindFirstVis.EndTiming();
					return true;
				}

				r = r.GetNextRow();
			}

			FirstVisible = null;
			Y = 0;
			//FindFirstVis.EndTiming();
			return false;
		}

		private int GetYCoorOfRow(Row r, bool bIncludeHeader = true)
		{
			int nCurrY = 0;
			if (r == m_FirstVisRow)
			{
				nCurrY = m_nTopRowY;
			}
			else
				// TODO: It is possible to remove the call to CalculateRowHeightPrior if we store the Y of the first vis row
				nCurrY = -m_VScrollBar.Value + r.CalculateRowHeightPrior();

			if (bIncludeHeader && m_ColumnHeader.Visible)
				nCurrY += m_ColumnHeader.ColumnHeight;

			return nCurrY;
		}

		private bool FindSelections(Point Mouse, out Cell CellMouseOver, out Column ColumnMouseUnder, out Row RowMouseOver)
		{
			ColumnMouseUnder = null;
			RowMouseOver = null;
			CellMouseOver = null;

			ColumnMouseUnder = m_ColumnHeader.FindColumn(Mouse.X, this.xOffset);

			if (ColumnMouseUnder == null)
				return false;

			if (m_FirstVisRow != null)
			{
				Row CurrRow = m_FirstVisRow;
				int nCurrY = GetYCoorOfRow(CurrRow);
				int nCurrX = -m_HScrollBar.Value;
				int nMaxY = RowBounds.Height + (m_ColumnHeader.Visible ? m_ColumnHeader.ColumnHeight : 0);

				while (CurrRow != null && nCurrY < nMaxY)
				{
					Rectangle CurrRowBounds = new Rectangle(nCurrX, nCurrY, Columns.RowWidth, CurrRow.Height);

					if (CurrRowBounds.Contains(Mouse))
					{
						RowMouseOver = CurrRow;
						break;
					}

					nCurrY += CurrRow.Height;
					CurrRow = CurrRow.GetNextRow();
				}
			}

			if (RowMouseOver == null)
				return false;

			CellMouseOver = RowMouseOver.Cells[ColumnMouseUnder.Index];
			return true;
		}

		private void MakeRowSelection(Row RowToSelect, bool bInvalidate = true)
		{
			if (m_CurrSelectedRow == RowToSelect)
				return;

			if (IsEditing)
				FinishEdit(true);

			// and the currently selected row is not null
			if (m_CurrSelectedRow != null)
			{
				// invalidate the area of that row only
				if (bInvalidate)
					InvalidateRow(m_CurrSelectedRow);
			}

			// the current row is now selected
			m_CurrSelectedRow = RowToSelect;

			if (m_CurrSelectedRow != null)
			{
				if (bInvalidate)
					InvalidateRow(m_CurrSelectedRow);
			}

			ClearHighlight();

			if (RowSelectionMade != null)
				RowSelectionMade(RowToSelect);
		}

		private void ChangeHighlight(Row RowToHighlight, bool bInvalidate = true)
		{
			if (m_CurrHighlightRow == RowToHighlight)
				return;

			Row OldHighlight = m_CurrHighlightRow;
			// the current row is now selected
			m_CurrHighlightRow = RowToHighlight;

			if (m_CurrSelectedRow != null)
			{
				// invalidate the area of that row only
				if (bInvalidate)
					InvalidateRow(m_CurrSelectedRow);
			}

			// and the currently selected row is not null
			if (OldHighlight != null)
			{
				// invalidate the area of that row only
				if (bInvalidate)
					InvalidateRow(OldHighlight);
			}

			if (bInvalidate && m_CurrHighlightRow != null)
				InvalidateRow(m_CurrHighlightRow);
		}

		private void HandleSelection(bool bChangeHighlight, Row RowMouseOver, bool bInvalidate = true)
		{
			if (bChangeHighlight)
				ChangeHighlight(RowMouseOver, bInvalidate);
			else
				MakeRowSelection(RowMouseOver, bInvalidate);
		}

		private bool TryToMakeNewRowSelection(Point Location, out Cell CellMouseOver, out Column ColumnMouseUnder, out Row RowMouseOver,
			bool bInvalidate = true, bool bChangeHighlight = false)
		{
			ColumnMouseUnder = null;
			RowMouseOver = null;
			CellMouseOver = null;

			if (FindSelections(Location, out CellMouseOver, out ColumnMouseUnder, out RowMouseOver))
			{
				HandleSelection(bChangeHighlight, RowMouseOver, bInvalidate);
				return true;
			}

			return false;
		}

		internal void ClearHighlight()
		{
			if (m_CurrHighlightRow != null)
			{
				InvalidateRow(m_CurrHighlightRow);
				m_CurrHighlightRow = null;
			}
		}

		internal void ClearSelection()
		{
			if (m_CurrSelectedRow != null)
			{
				InvalidateRow(m_CurrSelectedRow);
				m_CurrSelectedRow = null;
			}
		}

		internal void EnsureRowAtTopOfView(Row r, bool bInvalidate = true, bool bRefresh = false)
		{
			DoVScrollTo(r, bInvalidate, bRefresh);
		}

		internal void EnsureRowAtBottomOfView(Row r)
		{
			Rectangle CurrRowBounds = this.RowBounds;
			int nYcoor = RowBounds.Height;
			Row CurrRow = r;
			Row NewFirst = null;

			while (CurrRow != null)
			{
				nYcoor -= CurrRow.Height;

				if (nYcoor <= 0 || CurrRow.GetPreviousRow() == null)
				{
					NewFirst = CurrRow;
					break;
				}

				CurrRow = CurrRow.GetPreviousRow();
			}

			DoVScrollTo(NewFirst, false, false);
			DoVScrollAdjustment(-nYcoor, false, true);
		}

		private bool IsRowPartiallyVisible(int nYcoor, Row r)
		{
			Rectangle CurrRowBounds = RowBounds;
			return (nYcoor >= CurrRowBounds.Y || (nYcoor + r.Height) <= (CurrRowBounds.Y + CurrRowBounds.Height));
		}

		private bool IsRowFullyVisible(int nYcoor, Row r)
		{
			Rectangle CurrRowBounds = RowBounds;
			return (nYcoor >= CurrRowBounds.Y && (nYcoor + r.Height) <= (CurrRowBounds.Y + CurrRowBounds.Height));
		}

		internal void EnsureRowInView(Row r, bool bFullyVisible = true, bool bTop = true)
		{
			IntPtr pRow;
			pRow = GCHandle.ToIntPtr(GCHandle.Alloc(r, GCHandleType.Normal));

			IntPtr pFullVis;
			pFullVis = GCHandle.ToIntPtr(GCHandle.Alloc(bFullyVisible, GCHandleType.Normal));

			Win32.PostMessage(this.Handle, (bTop ? (uint)DatalistMessage.WM_ENSUREROWINTOPVIEW : (uint)DatalistMessage.WM_ENSUREROWINBOTVIEW), pRow, pFullVis);
		}

		private void EnsureRowInView_Internal(Row r, bool bFullyVisible = true, bool bTop = true)
		{
			if (r != null && r.Parent == this && m_VScrollBar.Visible && m_Rows.Contains(r.ParentNode))
			{
				FinishEdit(true);

				int nYcoor = GetYCoorOfRow(r);

				if ((bFullyVisible && !IsRowFullyVisible(nYcoor, r)) || (!bFullyVisible && !IsRowPartiallyVisible(nYcoor, r)))
				{
					if (bTop)
						EnsureRowAtTopOfView(r);
					else
						EnsureRowAtBottomOfView(r);
				}
			}
		}

		internal Color GetRowColor(ColorSelection part, Row r)
		{
			Color c;
			if (!m_RowColors.GetColor(part, r, out c))
			{
				switch (part)
				{
					case ColorSelection.BackColor:
						return this.BackColor;
					case ColorSelection.SelBackColor:
						return SystemColors.Highlight;
					case ColorSelection.ForeColor:
						return this.ForeColor;
					case ColorSelection.SelForeColor:
						return SystemColors.HighlightText;
					default:
						return Color.Empty;
				}
			}

			return c;
		}
		#endregion

		#region Cell Editing
		private bool AllowedToBeEdited(int ColIndex)
		{
			return m_Parent.AllowEdit && Columns[ColIndex].AllowUserEdit && !(Columns[ColIndex].Type == ColumnType.RowBackColor ||
				Columns[ColIndex].Type == ColumnType.RowForeColor ||
				Columns[ColIndex].Type == ColumnType.RowSelBackColor ||
				Columns[ColIndex].Type == ColumnType.RowSelForeColor ||
				Columns[ColIndex].ValueType == DatalistDataTypes.Object);
		}

		private void CancelAllPendingEdits(bool bDoubleClick)
		{
			m_CellEditBox.CancelPendingEdit(bDoubleClick);

			foreach (Column c in Columns)
			{
				if (c.ComboBox != null)
					c.ComboBox.CancelPendingEdit(bDoubleClick);
			}
		}

		private void ResetEditDoubleClick()
		{
			m_CellEditBox.ResetDblClickFlag();

			foreach (Column c in Columns)
			{
				if (c.ComboBox != null)
					c.ComboBox.ResetDblClickFlag();
			}
		}

		internal void FinishEdit(bool bCommit)
		{
			if (!IsEditing)
				return;

			if (m_CellTool != null)
				m_CellTool.ClearToolTip();

			m_CellEditBox.ForceFinishEdit(bCommit);

			foreach (Column c in Columns)
			{
				if (c.ComboBox != null)
					c.ComboBox.FinishEdit();
			}
		}

		internal void StartComboEdit(int nColIndex, Row pRowToEdit)
		{
			Column ColumnToEdit = Columns[nColIndex];
			if (ColumnToEdit.ComboBox == null)
				return;

			if (!AllowedToBeEdited(nColIndex))
				return;

			bool bCancelEdit = false;

			if (CellEditStarting != null)
				CellEditStarting(pRowToEdit, nColIndex, ref bCancelEdit);

			if (bCancelEdit)
				return;

			EnsureRowInView_Internal(pRowToEdit, true, m_FirstVisRow == pRowToEdit);
			EnsureColumnInView(ColumnToEdit);
			Refresh();

			if (m_CellTool != null)
				m_CellTool.ClearToolTip();

			Point pt = GetCellLocation(pRowToEdit[nColIndex]);
			ColumnToEdit.ComboBox.SetCurrentCell(ColumnToEdit.Index, pRowToEdit);
			ColumnToEdit.ComboBox.Location = pt;
			ColumnToEdit.ComboBox.Width = ColumnToEdit.Width;
			ColumnToEdit.ComboBox.Visible = true;
			ColumnToEdit.ComboBox.DroppedDown = true;
			ColumnToEdit.ComboBox.Focus();

			if (CellEditStarted != null)
				CellEditStarted(pRowToEdit, nColIndex);
		}

		internal bool FinishComboEdit(Row r, int ColIndex, long? nNewValue)
		{
			object oNewVal = nNewValue;
			bool bCancel = false;
			bool bCommit = false;

			if (nNewValue != (long?)r[ColIndex].Value)
			{
				bCommit = true;

				if (CellEditFinishing != null)
					CellEditFinishing(r, ColIndex, r[ColIndex].Value, ref oNewVal, ref bCommit, ref bCancel);

				if (bCancel)
					return false;

				if (bCommit)
				{
					r.Cells[ColIndex].Value = oNewVal;

					if (CellEditFinished != null)
						CellEditFinished(r, ColIndex, r[ColIndex].Value, oNewVal);
				}
			}

			Column EditedColumn = Columns[ColIndex];
			EditedColumn.ComboBox.Visible = false;
			EditedColumn.ComboBox.DroppedDown = false;
			InvalidateCell(r[ColIndex]);
			Focus();

			return bCommit;
		}

		private bool EditCheckBox(Row r, int ColIndex)
		{
			bool bStartCancel = false;

			if (CellEditStarting != null)
				CellEditStarting(r, ColIndex, ref bStartCancel);

			if (bStartCancel)
				return false;

			bool bCommit = true;
			bool bFinishCancel = false;

			object bOldVal = (bool)r.Cells[ColIndex].Value;
			object bNewVal = !(bool)bOldVal;

			if (CellEditFinishing != null)
				CellEditFinishing(r, ColIndex, bOldVal, ref bNewVal, ref bCommit, ref bFinishCancel);

			if (bFinishCancel)
				return false;

			if (bCommit)
				r.Cells[ColIndex].Value = (bool)bNewVal;

			if (CellEditFinished != null)
				CellEditFinished(r, ColIndex, bOldVal, bNewVal);

			return bCommit;
		}

		private bool FinishCellEdit(Row r, int ColIndex, bool bCommit)
		{
			object oNewVal = null;
			string strToParse = m_CellEditBox.Text;
			bool bCancel = false;
			int nOldHeight = r.Height;

			if (m_CellTool != null)
				m_CellTool.ClearToolTip();

			if (!GetValueAsObject(strToParse, ref oNewVal, Columns[ColIndex].ValueType))
			{
				bCancel = true;

				if (CellEditFailed != null)
					CellEditFailed(r, ColIndex, r[ColIndex].Value, oNewVal, ref bCancel);

				if (bCancel)
					m_CellEditBox.ClearEditBoxCell();

				return false;
			}

			if (CellEditFinishing != null)
				CellEditFinishing(r, ColIndex, r[ColIndex].Value, ref oNewVal, ref bCommit, ref bCancel);

			if (bCancel)
				return false;

			m_CellEditBox.ClearEditBoxCell();

			if (!bCommit)
				return false;

			r.Cells[ColIndex].Value = oNewVal;

			if (CellEditFinished != null)
				CellEditFinished(r, ColIndex, r[ColIndex].Value, oNewVal);

			if (Columns[ColIndex].IsVariableHeight())
			{
				RecalculateScrollBars();
				this.Invalidate();
			}

			return bCommit;
		}

		private bool GetValueAsObject(string strText, ref object oNewVal, DatalistDataTypes ConvertTo)
		{
			oNewVal = null;

			if (strText == string.Empty)
				return true;

			switch (ConvertTo)
			{
				case DatalistDataTypes.Boolean:
					{
						bool bParsed;
						if (bool.TryParse(strText, out bParsed))
						{
							oNewVal = bParsed;
							return true;
						}
					}
					break;
				case DatalistDataTypes.String:
					oNewVal = strText;
					return true;
				case DatalistDataTypes.Long:
					{
						long nParsed;
						if (long.TryParse(strText, out nParsed))
						{
							oNewVal = nParsed;
							return true;
						}
					}
					break;
				case DatalistDataTypes.Short:
					{
						short nParsed;
						if (short.TryParse(strText, out nParsed))
						{
							oNewVal = nParsed;
							return true;
						}
					}
					break;
				case DatalistDataTypes.DateTime:
					{
						DateTime nParsed;
						if (DateTime.TryParse(strText, out nParsed))
						{
							oNewVal = nParsed;
							return true;
						}
					}
					break;
				case DatalistDataTypes.Int:
					{
						int nParsed;
						if (int.TryParse(strText, out nParsed))
						{
							oNewVal = nParsed;
							return true;
						}
					}
					break;
				case DatalistDataTypes.Float:
					{
						float nParsed;
						if (float.TryParse(strText, out nParsed))
						{
							oNewVal = nParsed;
							return true;
						}
					}
					break;
				case DatalistDataTypes.Double:
					{
						double nParsed;
						if (double.TryParse(strText, out nParsed))
						{
							oNewVal = nParsed;
							return true;
						}
					}
					break;
				default:
					return false;
			}

			return false;
		}
		#endregion

		#region Keyboard Operations
		protected override bool IsInputKey(Keys keyData)
		{
			switch (keyData & Keys.KeyCode)
			{
				case Keys.Up:
					return true;
				case Keys.Down:
					return true;
				case Keys.Right:
					return true;
				case Keys.Left:
					return true;
				default:
					return base.IsInputKey(keyData);
			}
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			if (m_VScrollBar.Visible)
			{
				if (e.KeyCode == Keys.End)
				{
					DoVScrollTo(m_VScrollBar.Maximum - m_VScrollBar.LargeChange + 1);
					MakeRowSelection(m_Rows.GetLastRow());
				}
				else
				{
					if (e.KeyCode == Keys.Home)
					{
						DoVScrollTo(0);
						MakeRowSelection(m_Rows.GetFirstRow());
					}
				}
			}
			base.OnKeyUp(e);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			CancelAllPendingEdits(false);

			switch (e.KeyCode)
			{
				case Keys.PageDown:
					DoVScrollTo(m_VScrollBar.Value + RowBounds.Height);
					break;
				case Keys.PageUp:
					DoVScrollTo(m_VScrollBar.Value - RowBounds.Height);
					break;
				case Keys.Down:
					{
						Row ToEnsure = null;
						if (m_Parent.IsComboBox)
						{
							if (m_CurrHighlightRow == null)
								ToEnsure = GetFirstRow();
							else
								ToEnsure = m_CurrHighlightRow.GetNextRow();
						}
						else
						{
							if (m_CurrSelectedRow == null)
								ToEnsure = GetFirstRow();
							else
								ToEnsure = m_CurrSelectedRow.GetNextRow();
						}

						if (ToEnsure != null)
						{
							HandleSelection(m_Parent.IsComboBox, ToEnsure, false);
							EnsureRowInView_Internal(ToEnsure, true, false);
						}
					}
					break;
				case Keys.Up:
					{
						Row ToEnsure = null;
						if (m_Parent.IsComboBox)
						{
							if (m_CurrHighlightRow == null)
								ToEnsure = GetFirstRow();
							else
								ToEnsure = m_CurrHighlightRow.GetPreviousRow();
						}
						else
						{
							if (m_CurrSelectedRow == null)
								ToEnsure = GetFirstRow();
							else
								ToEnsure = m_CurrSelectedRow.GetPreviousRow();
						}

						if (ToEnsure != null)
						{
							HandleSelection(m_Parent.IsComboBox, ToEnsure, false);
							EnsureRowInView_Internal(ToEnsure, true, true);
						}
					}
					break;
				case Keys.Left:
					DoHScrollAdjustment(-m_HScrollBar.LargeChange);
					break;
				case Keys.Right:
					DoHScrollAdjustment(m_HScrollBar.LargeChange);
					break;
				case Keys.Enter:
					{
						if (m_Parent.IsComboBox && m_CurrHighlightRow != null)
						{
							MakeRowSelection(m_CurrHighlightRow);
						}
					}
					break;
			}

			base.OnKeyDown(e);
		}
		#endregion

		#region Mouse Events
		protected override void OnMouseEnter(EventArgs e)
		{
			m_Parent.InvalidateBorder();
			base.OnMouseEnter(e);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			if (m_CellTool != null && !RowBounds.Contains(PointToClient(Control.MousePosition)))
				m_CellTool.ClearToolTip();

			m_Parent.InvalidateBorder();

			base.OnMouseLeave(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (m_ColumnHeader.Visible && (m_ColumnHeader.MouseDownInHeader || m_ColumnHeader.ClientRectangle.Contains(e.Location)))
			{
				int x = this.xOffset;
				if (m_HScrollValueLock >= 0)
					x = -m_HScrollValueLock;
				m_ColumnHeader.OnMouseMove(e, x);

				if (m_CellTool != null)
					m_CellTool.ClearToolTip();

				base.OnMouseMove(e);
				return;
			}

			if (!RowBounds.Contains(e.Location))
			{
				if (m_CellTool != null)
					m_CellTool.ClearToolTip();

				base.OnMouseMove(e);
				return;
			}

			Column ColumnMouseUnder = null;
			Row RowMouseOver = null;
			Cell CellMouseOver = null;

			if (!FindSelections(e.Location, out CellMouseOver, out ColumnMouseUnder, out RowMouseOver))
			{
				if (m_CellTool != null)
					m_CellTool.ClearToolTip();

				base.OnMouseMove(e);
				return;
			}

			if (!ReadOnly)
			{
				if (m_Parent.IsComboBox)
					ChangeHighlight(RowMouseOver);
				else if (e.Button == System.Windows.Forms.MouseButtons.Left && m_bMouseDownInRows)
					MakeRowSelection(RowMouseOver);
			}

			m_CellTool.ParentCell = CellMouseOver;

			base.OnMouseMove(e);
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			if (!RowBounds.Contains(e.Location))
			{
				base.OnMouseWheel(e);
				return;
			}

			if (this.IsEditing)
			{
				base.OnMouseWheel(e);
				return;
			}

			CancelAllPendingEdits(false);

			if (m_VScrollBar.Visible)
			{
				if (!this.Focused)
					this.Focus();

				if (!DoVScrollAdjustment(-e.Delta, false, true))
					return;

				bool bHandleCombo = (m_Parent.IsComboBox && !m_Parent.ReadOnly);
				if (m_bMouseDownInRows || bHandleCombo)
				{
					Column ColumnMouseUnder = null;
					Row RowMouseOver = null;
					Cell CellMouseOver = null;

					TryToMakeNewRowSelection(e.Location, out CellMouseOver, out ColumnMouseUnder, out RowMouseOver, true, bHandleCombo);
				}
			}

			base.OnMouseWheel(e);
		}

		protected override void OnMouseDoubleClick(MouseEventArgs Mouse)
		{
			CancelAllPendingEdits(true);

			if (ReadOnly || !RowBounds.Contains(Mouse.Location))
			{
				base.OnMouseDoubleClick(Mouse);
				return;
			}

			if (Mouse.Button == System.Windows.Forms.MouseButtons.Left)
			{
				Column ColumnMouseUnder = null;
				Row RowMouseOver = null;
				Cell CellMouseOver = null;
				if (TryToMakeNewRowSelection(Mouse.Location, out CellMouseOver, out ColumnMouseUnder, out RowMouseOver))
				{
					if (RowDoubleClicked != null)
						RowDoubleClicked(RowMouseOver, ColumnMouseUnder.Index);
				}
			}

			base.OnMouseDoubleClick(Mouse);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			// TODO: Use modifier keys to determine if more than one selection is to be made
			if (!this.Focused)
				this.Focus();

			CancelAllPendingEdits(false);

			if (m_ColumnHeader.Visible && m_ColumnHeader.ClientRectangle.Contains(e.Location))
			{
				m_ColumnHeader.OnMouseDown(e, this.xOffset);
				ResetEditDoubleClick();
				base.OnMouseDown(e);
				return;
			}

			if (ReadOnly || !RowBounds.Contains(e.Location))
			{
				ResetEditDoubleClick();
				base.OnMouseDown(e);
				return;
			}

			Column ColumnMouseUnder = null;
			Row RowMouseOver = null;
			Cell CellMouseOver = null;
			if (!FindSelections(e.Location, out CellMouseOver, out ColumnMouseUnder, out RowMouseOver))
			{
				ResetEditDoubleClick();
				base.OnMouseDown(e);
				return;
			}

			if (e.Button == System.Windows.Forms.MouseButtons.Left)
			{
				if (m_Parent.IsComboBox)
				{
					HandleSelection(true, RowMouseOver, true);
				}
				else
				{
					if (RowMouseOver == m_CurrSelectedRow)
					{
						m_bSecondClickOnRow = true;
					}
					else
					{
						if (Columns[CellMouseOver.ColumnIndex] == null)
						{
							base.OnMouseDown(e);
							return;
						}

						m_bSecondClickOnRow = (Columns[CellMouseOver.ColumnIndex].Type == ColumnType.CheckBox);
						MakeRowSelection(RowMouseOver);
					}
				}

				m_CellMouseDown = CellMouseOver;
				m_bMouseDownInRows = true;
			}
			else
			{
				if (e.Button == System.Windows.Forms.MouseButtons.Right)
				{
					HandleSelection(m_Parent.IsComboBox, RowMouseOver, true);
					m_bMouseDownInRows = false;
					m_bSecondClickOnRow = false;
				}
			}

			ResetEditDoubleClick();
			base.OnMouseDown(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			CancelAllPendingEdits(false);

			if (e.Button != MouseButtons.Left)
			{
				base.OnMouseUp(e);
				return;
			}

			if (m_ColumnHeader.MouseDownInHeader)
			{
				m_ColumnHeader.OnMouseUp(e, this.xOffset);
				m_CellMouseDown = null;
				m_bMouseDownInRows = false;
				base.OnMouseUp(e);
				return;
			}

			if (ReadOnly || !RowBounds.Contains(e.Location))
			{
				base.OnMouseUp(e);
				m_CellMouseDown = null;
				m_bMouseDownInRows = false;
				return;
			}

			Column ColumnMouseUnder = null;
			Row RowMouseOver = null;
			Cell CellMouseOver = null;
			if (!FindSelections(e.Location, out CellMouseOver, out ColumnMouseUnder, out RowMouseOver))
			{
				base.OnMouseUp(e);
				m_CellMouseDown = null;
				m_bMouseDownInRows = false;
				return;
			}

			if (m_Parent.IsComboBox)
			{
				if (m_Parent.IsDroppedDown)
					HandleSelection(false, RowMouseOver, true);

				m_CellMouseDown = null;
				m_bMouseDownInRows = false;
				base.OnMouseUp(e);
				return;
			}

			if (ColumnMouseUnder.AllowUserEdit && CellMouseOver == m_CellMouseDown && AllowedToBeEdited(ColumnMouseUnder.Index))
			{
				if (ColumnMouseUnder.Type == ColumnType.ComboBox)
				{
					HandleSelection(false, RowMouseOver, true);
					if (ColumnMouseUnder.ComboBox != null)
						ColumnMouseUnder.ComboBox.StartPendingEdit(ColumnMouseUnder.Index, RowMouseOver);
				}
				else
				{
					if (m_bSecondClickOnRow)
					{
						if (ColumnMouseUnder.Type == ColumnType.CheckBox)
						{
							if (CellMouseOver.Value != null)
							{
								if (EditCheckBox(RowMouseOver, ColumnMouseUnder.Index))
									InvalidateCell(CellMouseOver);
							}
						}
						else
						{
							m_CellEditBox.StartPendingEdit(ColumnMouseUnder.Index, RowMouseOver);
						}
					}
				}
			}

			m_CellMouseDown = null;
			m_bMouseDownInRows = false;
			base.OnMouseUp(e);
		}
		#endregion

		#region Scrolling and Scroll Bar
		void OnScrollBarMouseEnter(object sender, EventArgs e)
		{
			if (m_CellTool != null)
				m_CellTool.ClearToolTip();

			m_Parent.InvalidateBorder();
		}

		private void OnScroll(object sender, ScrollEventArgs e)
		{
			if (e.ScrollOrientation == ScrollOrientation.VerticalScroll)
			{
				DoVScrollTo(e.NewValue, e.OldValue, false, true);
			}
			else
			{
				DoHScrollTo(e.NewValue, e.OldValue, false, true);
			}
		}

		void OnScrollBarMouseCapture(object sender, EventArgs e)
		{
			this.Focus();
		}

		void OnScrollBarMouseLeave(object sender, EventArgs e)
		{
			m_Parent.InvalidateBorder();
		}

		private bool DoVScrollAdjustment(int nAmountToScroll, bool bInvalidate = true, bool bRefresh = false)
		{
			return DoVScrollTo(m_VScrollBar.Value + nAmountToScroll, m_VScrollBar.Value, bInvalidate, bRefresh);
		}

		private bool DoHScrollAdjustment(int nAmountToScroll)
		{
			return DoHScrollTo(m_HScrollBar.Value + nAmountToScroll, m_HScrollBar.Value);
		}

		private bool DoVScrollAdjustment(int nAmountToScroll, int nOldValue, bool bInvalidate = true, bool bRefresh = false)
		{
			//TODO: Handle when the nAmountToScroll is between 0 and our current top row y
			return DoVScrollTo(m_VScrollBar.Value + nAmountToScroll, nOldValue, bInvalidate, bRefresh);
		}

		private bool DoHScrollAdjustment(int nAmountToScroll, int nOldValue)
		{
			return DoHScrollTo(m_HScrollBar.Value + nAmountToScroll, nOldValue);
		}

		private bool SetVScrollBarValue(int VScrollNewVal)
		{
			int OldScrollValue = m_VScrollBar.Value;

			if (VScrollNewVal > m_VScrollBar.Maximum - m_VScrollBar.LargeChange + 1)
				m_VScrollBar.Value = m_VScrollBar.Maximum - m_VScrollBar.LargeChange + 1;
			else
				if (VScrollNewVal < 0)
					m_VScrollBar.Value = 0;
				else
					m_VScrollBar.Value = VScrollNewVal;

			return OldScrollValue != m_VScrollBar.Value;
		}

		private bool SetHScrollBarValue(int HScrollNewVal)
		{
			int OldScrollValue = m_HScrollBar.Value;

			if (HScrollNewVal > m_HScrollBar.Maximum - m_HScrollBar.LargeChange + 1)
				m_HScrollBar.Value = m_HScrollBar.Maximum - m_HScrollBar.LargeChange + 1;
			else
				if (HScrollNewVal < 0)
					m_HScrollBar.Value = 0;
				else
					m_HScrollBar.Value = HScrollNewVal;

			return OldScrollValue != m_HScrollBar.Value;
		}

		private bool DoHScrollTo(int nNewValue, int nOldValue, bool bInvalidate = true, bool bRefresh = false)
		{
			if (SetHScrollBarValue(nNewValue))
			{
				m_CellTool.ClearToolTip();

				if (bInvalidate)
					this.Invalidate(true);

				if (bRefresh)
					this.Refresh();

				return true;
			}

			return false;
		}

		private bool DoHScrollTo(int nNewValue, bool bInvalidate = true, bool bRefresh = false)
		{
			return DoHScrollTo(nNewValue, m_HScrollBar.Value, bInvalidate, bRefresh);
		}

		private bool DoVScrollTo(int nNewValue, int nOldValue, bool bInvalidate = true, bool bRefresh = false)
		{
			if (SetVScrollBarValue(nNewValue))
			{
				m_CellTool.ClearToolTip();

				FindNewTopRow(m_VScrollBar.Value, nOldValue);

				if (bInvalidate)
					this.Invalidate(true);

				if (bRefresh)
					this.Refresh();

				return true;
			}

			return false;
		}

		private bool DoVScrollTo(Row r, bool bInvalidate, bool bRefresh)
		{
			if (r == null || r.Parent != this || !m_VScrollBar.Visible)
				return true;

			if (m_FirstVisRow == null)
			{
				if (!FindFirstVisibleRow(out m_FirstVisRow, out m_nTopRowY))
					return false;
			}

			Row TowardTop = m_FirstVisRow.GetPreviousRow();
			int TopY = m_nTopRowY;

			Row TowardBottom = m_FirstVisRow;
			int BottomY = m_nTopRowY;

			while (TowardBottom != null || TowardTop != null)
			{
				if (TowardBottom != null)
				{
					if (TowardBottom == r)
					{
						TowardTop = null;
						break;
					}

					BottomY += TowardBottom.Height;
					TowardBottom = TowardBottom.GetNextRow();
				}

				if (TowardTop != null)
				{
					TopY -= TowardTop.Height;
					if (TowardTop == r)
					{
						TowardBottom = null;
						break;
					}

					TowardTop = TowardTop.GetPreviousRow();
				}
			}

			if (TowardBottom != null)
			{
				Row PossTop = TowardBottom;
				int nHeight = RowBounds.Height;

				while (PossTop != null && nHeight > 0)
				{
					nHeight -= PossTop.Height;
					PossTop = PossTop.GetNextRow();
				}

				bool bWentUp = false;
				PossTop = TowardBottom.GetPreviousRow();
				while (PossTop != null && nHeight > 0)
				{
					bWentUp = true;
					nHeight -= PossTop.Height;
					BottomY -= PossTop.Height;
					TowardBottom = PossTop;
					PossTop = PossTop.GetPreviousRow();
				}

				if (SetVScrollBarValue(m_VScrollBar.Value + BottomY - (bWentUp ? nHeight : 0)))
				{
					m_FirstVisRow = TowardBottom;

					if (bWentUp)
						m_nTopRowY = nHeight;
					else
						m_nTopRowY = 0;
				}

				if (bInvalidate)
					this.Invalidate(false);

				if (bRefresh)
					this.Refresh();

				return true;
			}
			else
			{
				if (TowardTop != null)
				{
					if (SetVScrollBarValue(m_VScrollBar.Value + TopY))
					{
						m_FirstVisRow = TowardTop;
						m_nTopRowY = 0;
					}

					if (bInvalidate)
						this.Invalidate(false);

					if (bRefresh)
						this.Refresh();

					return true;
				}
				else
				{
					return false;
				}
			}
		}

		private void FindNewTopRow(int nNewValue, int nOldValue)
		{
			Row CurrRow = m_FirstVisRow;

			if (CurrRow == null)
			{
				m_nTopRowY = 0;
				CurrRow = GetFirstRow();
			}

			if (CurrRow != null)
			{
				int nDelta = nNewValue - nOldValue;
				int nNewY = m_nTopRowY - (nDelta);

				if (nDelta > 0)
				{
					while (CurrRow != null)
					{
						if (nNewY + CurrRow.Height <= 0)
							nNewY += CurrRow.Height;
						else
							break;

						CurrRow = CurrRow.GetNextRow();
					}

					m_nTopRowY = nNewY;
					m_FirstVisRow = CurrRow;
				}
				else
				{
					if (nDelta < 0)
					{
						while (true)
						{
							if (CurrRow == null || nNewY <= 0)
								break;

							CurrRow = CurrRow.GetPreviousRow();

							if (CurrRow != null)
								nNewY -= CurrRow.Height;
						}

						m_nTopRowY = nNewY;
						m_FirstVisRow = CurrRow;
					}
				}
			}
		}

		private bool DoVScrollTo(int nNewValue, bool bInvalidate = true, bool bRefresh = false)
		{
			return DoVScrollTo(nNewValue, m_VScrollBar.Value, bInvalidate, bRefresh);
		}

		private Rectangle GetRowBounds(bool bWithScrollBars)
		{
			int x = 0;
			int y = 0;
			int width = this.ClientRectangle.Width;
			int height = this.ClientRectangle.Height;

			if (m_ColumnHeader.Visible)
			{
				y = m_ColumnHeader.ColumnHeight;
				height -= m_ColumnHeader.ColumnHeight;
			}

			if (bWithScrollBars)
			{
				if (m_HScrollBar.Visible)
					height -= m_HScrollBar.Height;

				if (m_VScrollBar.Visible)
					width -= m_VScrollBar.Width;
			}

			return new Rectangle(x, y, width, height);
		}

		internal void RecalculateScrollBars()
		{
			if (!Visible)
				return;

			Rectangle rowBounds = GetRowBounds(false);
			bool bChange;

			m_HScrollBar.Visible = false;
			m_VScrollBar.Visible = false;
			m_CornerBox.Visible = false;

			do
			{
				bChange = false;

				if (rowBounds.Width < m_ColumnHeader.Columns.RowWidth && !m_HScrollBar.Visible)
				{
					m_HScrollBar.Visible = true;
					bChange = true;
				}

				if (rowBounds.Height < m_Rows.TotalRowHeight && !m_VScrollBar.Visible)
				{
					m_VScrollBar.Visible = true;
					bChange = true;
				}

				rowBounds = GetRowBounds(true);
			} while (bChange);

			if (m_HScrollBar.Visible)
			{
				m_HScrollBar.Location = new Point(0, rowBounds.Bottom);
				m_HScrollBar.Width = rowBounds.Width;

				int nHLargeChange = Math.Max(0, rowBounds.Width / 10);
				int maxH = Math.Max(0, Columns.RowWidth - rowBounds.Width + nHLargeChange - 1);

				m_HScrollBar.LargeChange = nHLargeChange;
				m_HScrollBar.Maximum = maxH;

				DoHScrollTo(Math.Min(m_HScrollBar.Value, maxH - nHLargeChange + 1), false, false);
				m_HScrollBar.Refresh();
			}

			if (m_VScrollBar.Visible)
			{
				m_VScrollBar.Location = new Point(rowBounds.Right, rowBounds.Top);
				m_VScrollBar.Height = rowBounds.Height;

				int nVLargeChange = Math.Max(0, rowBounds.Height / 10);
				int maxV = Math.Max(0, m_Rows.TotalRowHeight - rowBounds.Height + nVLargeChange - 1);

				m_VScrollBar.LargeChange = nVLargeChange;
				m_VScrollBar.Maximum = maxV;

				DoVScrollTo(Math.Min(m_VScrollBar.Value, maxV - nVLargeChange + 1), false, false);
				m_VScrollBar.Refresh();
			}

			if (m_HScrollBar.Visible && m_VScrollBar.Visible)
			{
				m_CornerBox.Visible = true;
				m_CornerBox.Location = new Point(rowBounds.Right, rowBounds.Bottom);
				m_CornerBox.Size = new Size(m_VScrollBar.Width, m_HScrollBar.Height);
			}
		}
		#endregion

		#region Event Handling
		protected override void OnResize(EventArgs e)
		{
			FinishEdit(true);
			m_FirstVisRow = null;
			RecalculateScrollBars();
			this.Invalidate();

			base.OnResize(e);
		}

		protected override void OnLostFocus(EventArgs e)
		{
			if (m_CurrSelectedRow != null)
				InvalidateRow(m_CurrSelectedRow);

			m_Parent.InvalidateBorder();

			if (!ContainsFocus)
			{
				CancelAllPendingEdits(false);
			}

			base.OnLostFocus(e);
		}

		protected override void OnGotFocus(EventArgs e)
		{
			if (m_CurrSelectedRow != null)
				InvalidateRow(m_CurrSelectedRow);

			m_Parent.InvalidateBorder();

			base.OnGotFocus(e);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			// TODO: Fix clipping
			//try
			{
				PaintFunc.StartTiming();

				if (m_bNeedHeightRecalc)
				{
					// TODO: Font object provides ways of potentially approximating the size 
					// enough to where MeasureString would not be needed, should probably look into approximation
					m_Rows.GetRowsHeight(e.Graphics);
					RecalculateScrollBars();
					m_bNeedHeightRecalc = false;
				}

				Rectangle Bounds = RowBounds;
				int x = this.xOffset;
				if (m_HScrollValueLock >= 0)
					x = -m_HScrollValueLock;

				if (m_ColumnHeader.Visible && m_ColumnHeader.ClientRectangle.IntersectsWith(e.ClipRectangle))
					m_ColumnHeader.DrawColumnHeader(e, x);

				e.Graphics.IntersectClip(Bounds);

				if (Enabled && !ReadOnly)
					e.Graphics.FillRectangle(new SolidBrush(BackColor), Bounds);
				else
					e.Graphics.FillRectangle(new SolidBrush(SystemColors.Control), Bounds);

				if (m_FirstVisRow == null)
					FindFirstVisibleRow(out m_FirstVisRow, out m_nTopRowY);

				int y = m_nTopRowY;

				if (m_ColumnHeader.Visible)
				{
					y += m_ColumnHeader.ColumnHeight;
				}

				if (m_FirstVisRow != null)
				{
					int InitialY = y;

					Row CurrRow = m_FirstVisRow;
					int nMaxY = Bounds.Height + (m_ColumnHeader.Visible ? m_ColumnHeader.ColumnHeight : 0);

					while (CurrRow != null && InitialY < nMaxY)
					{
						// if we are in the bounds of the client area, draw! otherwise we just want to do calculations
						if (e.Graphics.Clip.IsVisible(x, InitialY, CurrRow.Width, CurrRow.Height))
							CurrRow.DrawRow(e, x, InitialY);

						// we now know heights so change our y coordinate and height tabulation
						InitialY += CurrRow.Height;

						CurrRow = CurrRow.GetNextRow();
					}
				}

				PaintFunc.EndTiming();

#if DEBUG
				Brush b = new SolidBrush(Color.Black);
				string toDraw = string.Format(
@"Rows Total Height: {0}
Row Bounds: {1}
Columns Height: {2}
Columns Width: {3}
Vertical Scroll: Min {4} Max {5} Value {6} Small {7} Large {8}
Horizontal Scroll: Min {9} Max {10} Value {11} Small {12} Large {13}",
					this.Rows.TotalRowHeight,
					RowBounds.ToString(),
					m_ColumnHeader.ColumnHeight,
					m_ColumnHeader.Columns.RowWidth,
					m_VScrollBar.Minimum, m_VScrollBar.Maximum, m_VScrollBar.Value, m_VScrollBar.SmallChange, m_VScrollBar.LargeChange,
					m_HScrollBar.Minimum, m_HScrollBar.Maximum, m_HScrollBar.Value, m_HScrollBar.SmallChange, m_HScrollBar.LargeChange);
				e.Graphics.DrawString(toDraw, this.Font, b, 100, 20);
				b.Dispose();
#endif
			}

			base.OnPaint(e);
		}

		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case (int)DatalistMessage.WM_FINISHCELLEDIT:
					if (m_CellEditBox.Visible)
					{
						GCHandle handle = GCHandle.FromIntPtr(m.WParam);
						bool bCommit = (bool)handle.Target;
						handle.Free();

						FinishCellEdit(m_CellEditBox.Row, m_CellEditBox.ColumnIndex, bCommit);
					}
					break;
				case (int)DatalistMessage.WM_STARTCELLEDIT:
					{
						int nColIndex = m_CellEditBox.ColumnIndex;
						if (AllowedToBeEdited(nColIndex))
						{
							bool bCancelEdit = false;

							if (CellEditStarting != null)
								CellEditStarting(m_CellEditBox.Row, nColIndex, ref bCancelEdit);

							if (!bCancelEdit)
							{
								EnsureRowInView_Internal(m_CellEditBox.Row, true, m_FirstVisRow == m_CellEditBox.Row);
								EnsureColumnInView(Columns[nColIndex]);
								Refresh();

								if (m_CellTool != null)
									m_CellTool.ClearToolTip();

								m_CellEditBox.StartEditingCell();

								if (CellEditStarted != null)
									CellEditStarted(m_CellEditBox.Row, nColIndex);
							}
						}
					}
					break;
				case (int)DatalistMessage.WM_ENSUREROWINBOTVIEW:
					{
						GCHandle WParam = GCHandle.FromIntPtr(m.WParam);
						Row rToEnsure = (Row)WParam.Target;
						WParam.Free();

						GCHandle LParam = GCHandle.FromIntPtr(m.LParam);
						bool bFullyVisible = (bool)LParam.Target;
						LParam.Free();

						EnsureRowInView_Internal(rToEnsure, bFullyVisible, false);
					}
					break;
				case (int)DatalistMessage.WM_ENSUREROWINTOPVIEW:
					{
						GCHandle WParam = GCHandle.FromIntPtr(m.WParam);
						Row rToEnsure = (Row)WParam.Target;
						WParam.Free();

						GCHandle LParam = GCHandle.FromIntPtr(m.LParam);
						bool bFullyVisible = (bool)LParam.Target;
						LParam.Free();

						EnsureRowInView_Internal(rToEnsure, bFullyVisible, true);
					}
					break;
				case (int)DatalistMessage.WM_SHOWTOOLTIP:
					{
						m_CellTool.ShowToolTip();
					}
					break;
				case (int)DatalistMessage.WM_CLEARTOOLTIP:
					{
						if (m_CellTool != null && m_CellTool.Visible)
							m_CellTool.ClearToolTip();
					}
					break;
				case (int)DatalistMessage.WM_HIDETOOLTIP:
					{
						if (m_CellTool != null && m_CellTool.Visible)
							m_CellTool.HideToolTip();
					}
					break;
				case (int)DatalistMessage.WM_TOOLTIPMOUSEMOVE:
					{
						GCHandle handle = GCHandle.FromIntPtr(m.LParam);
						Point MouseLoc = (Point)handle.Target;
						handle.Free();

						MouseLoc = PointToClient(MouseLoc);

						OnMouseMove(new MouseEventArgs(System.Windows.Forms.MouseButtons.None, 1, MouseLoc.X, MouseLoc.Y, 0));
					}
					break;
				default:
					base.WndProc(ref m);
					break;
			}

			if (m_CellTool != null)
				m_CellTool.HandleMessage(ref m);
		}
		#endregion
	}
}
