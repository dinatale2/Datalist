using System;
using Drawing.ThemeRoutines;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;
using Win32Lib;
using System.ComponentModel;
using System.Drawing.Design;

//TODO: Go through and remove all the pinvoke send/post message stuff... should likely use invoking

namespace DataList
{
    public enum DatalistDataTypes
    {
        Boolean,
        String,
        Color,
        Object,  // Object is READ ONLY because its intended for storage of things like structs etc, it wont have any text displayed
        Short,
        Long,
        DateTime,
        Int,
        Double,
        Float,
    };

    internal enum DatalistMessage : uint
    {
        WM_FINISHCELLEDIT = WindowsMessages.WM_APP + 0,
        WM_STARTCELLEDIT = WindowsMessages.WM_APP + 1,
        WM_ENSUREROWINBOTVIEW = WindowsMessages.WM_APP + 2,
        WM_SHOWTOOLTIP = WindowsMessages.WM_APP + 3,
        WM_CLEARTOOLTIP = WindowsMessages.WM_APP + 4,
        WM_HIDETOOLTIP = WindowsMessages.WM_APP + 5,
        WM_TOOLTIPMOUSEMOVE = WindowsMessages.WM_APP + 6,
        WM_ENSUREROWINTOPVIEW = WindowsMessages.WM_APP + 7,
    };

    //Event handling for row selection
    public delegate void DataListRowSelected(Row r);
    public delegate void DataListRowClicked(Row r, int ColIndex);
    public delegate void DataListCellEditStarting(Row r, int ColIndex, ref bool bCancel);
    public delegate void DataListCellEditStarted(Row r, int ColIndex);
    public delegate void DataListCellEditFinishing(Row r, int ColIndex, object oOldVal, ref object oNewVal, ref bool bCommit, ref bool bCancel);
    public delegate void DataListCellEditFinished(Row r, int ColIndex, object oOldVal, object oNewVal);
    public delegate void DataListCellEditFailed(Row r, int ColIndex, object oOldVal, object oNewVal, ref bool bCancel);
    public delegate void DataListColumnLeftClick(int ColIndex);
    public delegate void DataListDroppingDown(ref bool bCancelDrop);
    public delegate void DataListDroppedDown();

    public class DataList : Control
    {
        public event DataListRowSelected RowSelectionMade;
        public event DataListRowClicked RowDoubleClicked;
        public event DataListRowClicked RowClicked;
        public event DataListCellEditStarting CellEditStarting;
        public event DataListCellEditStarted CellEditStarted;
        public event DataListCellEditFinishing CellEditFinishing;
        public event DataListCellEditFinished CellEditFinished;
        public event DataListCellEditFailed CellEditFailed;
        public event DataListDroppingDown DroppingDown;
        public event DataListDroppedDown DroppedDown;

        private ListWnd m_RowWnd;
        private RECT borderRect;
        private int m_nStateId;
        private int m_nComboStateId;
        private Rectangle m_rButtonArea;
        private DropDownWnd m_DropDownWnd;
        private bool m_bComboClosedButton;
        private bool m_bMouseDownInButton;

        private MultiWindowUxThemeManager m_ThemeManager;
        internal MultiWindowUxThemeManager ThemeManager
        {
            get { return m_ThemeManager; }
        }

        public Row CurrSel
        {
            get { return m_RowWnd.CurrSel; }
            set { m_RowWnd.CurrSel = value; }
        }

        private bool m_bReadOnly;
        public bool ReadOnly
        {
            get { return m_bReadOnly; }
            set
            {
                if (m_bReadOnly != value)
                {
                    m_bReadOnly = value;
                    m_RowWnd.ReadOnly = value;
                    InvalidateBorder();
                }
            }
        }

        private bool m_ShowNull;
        public bool ShowNull
        {
            get { return m_ShowNull; }
            set { m_ShowNull = value; }
        }

        private bool m_ShowGridLines;
        public bool ShowGridLines
        {
            get { return m_ShowGridLines; }
            set
            {
                m_ShowGridLines = value;
                m_RowWnd.Invalidate();
            }
        }

        public bool ShowColumnHeader
        {
            get { return m_RowWnd.ColumnHeader.Visible; }
            set { m_RowWnd.ColumnHeader.Visible = value; }
        }

        private bool m_AllowRowColorCombo;
        public bool AllowRowColorCombo
        {
            get { return m_AllowRowColorCombo; }
            set
            {
                if (m_AllowRowColorCombo != value)
                {
                    m_AllowRowColorCombo = value;
                    this.Invalidate(true);
                }
            }
        }

        internal ListWnd RowWnd
        {
            get { return m_RowWnd; }
        }

        [Browsable(false)]
        public RowCollection Rows
        {
            get { return m_RowWnd.Rows; }
        }

        [
        Category("Collections"),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Content),
        Editor(typeof(ColumnCollectionEditor), typeof(UITypeEditor)),
        Browsable(true)
        ]
        public ColumnCollection Columns
        {
            get { return m_RowWnd.Columns; }
        }

        private bool m_bAllowEdit;
        public bool AllowEdit
        {
            get { return m_bAllowEdit; }
            set
            {
                if (!value)
                    m_RowWnd.FinishEdit(true);

                m_bAllowEdit = value;
            }
        }

        private bool m_bAllowSort;
        public bool AllowSort
        {
            get { return m_bAllowSort; }
            set { m_bAllowSort = value; }
        }

        private bool m_bIsComboBox;
        public bool IsComboBox
        {
            get { return m_bIsComboBox; }
            set
            {
                if (m_RowWnd.IsEditing)
                    m_RowWnd.FinishEdit(true);

                ConfigureListForCombo(value);
            }
        }

        private string m_strComboFormat;
        public string ComboStringFormat
        {
            get { return m_strComboFormat; }
            set
            {
                m_strComboFormat = value;
                this.Invalidate();
            }
        }

        private string m_strComboText;
        public string ComboBoxText
        {
            get { return m_strComboText; }
            set
            {
                m_strComboText = value == null ? "" : value;
                this.Invalidate();
            }
        }

        private int m_nDropDownMaxHeight;
        public int DropDownHeight
        {
            get { return m_nDropDownMaxHeight; }
            set
            {
                if (m_nDropDownMaxHeight > 0)
                    m_nDropDownMaxHeight = value;
            }
        }

        private int m_nDropDownWidth;
        public int DropDownWidth
        {
            get { return m_nDropDownWidth; }
            set
            {
                m_nDropDownWidth = value;
            }
        }

        internal bool IsDroppedDown
        {
            get { return m_DropDownWnd != null && m_DropDownWnd.Visible; }
        }

        public DataList()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint |
                ControlStyles.Selectable |
                ControlStyles.UserMouse,
                true);

            m_nStateId = (int)ComboBoxState.Normal;
            m_nComboStateId = (int)ComboBoxState.Normal;
            m_bReadOnly = false;

            m_bMouseDownInButton = false;
            m_nDropDownMaxHeight = 300;
            m_nDropDownWidth = -1;
            m_strComboText = "";

            m_ThemeManager = new MultiWindowUxThemeManager(this);

            m_ShowNull = false;
            m_ShowGridLines = true;
            m_bAllowEdit = true;
            m_bIsComboBox = false;
            m_bComboClosedButton = false;
            m_rButtonArea = Rectangle.Empty;
            m_AllowRowColorCombo = false;

            m_RowWnd = new ListWnd(this);
            m_RowWnd.Size = this.ClientRectangle.Size;
            this.Controls.Add(m_RowWnd);

            m_RowWnd.CellEditFinishing += new DataListCellEditFinishing(FireCellEditFinishing);
            m_RowWnd.CellEditStarting += new DataListCellEditStarting(FireCellEditStarting);
            m_RowWnd.RowSelectionMade += new DataListRowSelected(FireRowSelected);
            m_RowWnd.RowDoubleClicked += new DataListRowClicked(FireRowDoubleClicked);
            m_RowWnd.RowClicked += new DataListRowClicked(FireRowClicked);
            m_RowWnd.CellEditFinished += new DataListCellEditFinished(FireCellEditFinished);
            m_RowWnd.CellEditStarted += new DataListCellEditStarted(FireCellEditStarted);
            m_RowWnd.CellEditFailed += new DataListCellEditFailed(FireCellEditFailed);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!IsDisposed)
                {
                    m_RowWnd.Dispose();
                    m_ThemeManager.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        public void SetColumnBackColor(int ColIndex, Color BackColor)
        {
            if (BackColor == Color.Empty)
                m_RowWnd.ColumnColors.RemoveColor(ColorSelection.BackColor, ColIndex);
            else
                m_RowWnd.ColumnColors.SetColor(ColorSelection.BackColor, ColIndex, BackColor);

            m_RowWnd.InvalidateColumn(ColIndex);
        }

        public void SetColumnForeColor(int ColIndex, Color ForeColor)
        {
            if (ForeColor == Color.Empty)
                m_RowWnd.ColumnColors.RemoveColor(ColorSelection.ForeColor, ColIndex);
            else
                m_RowWnd.ColumnColors.SetColor(ColorSelection.ForeColor, ColIndex, ForeColor);

            m_RowWnd.InvalidateColumn(ColIndex);
        }

        public void SetColumnSelBackColor(int ColIndex, Color BackColor)
        {
            if (BackColor == Color.Empty)
                m_RowWnd.ColumnColors.RemoveColor(ColorSelection.SelBackColor, ColIndex);
            else
                m_RowWnd.ColumnColors.SetColor(ColorSelection.SelBackColor, ColIndex, BackColor);

            m_RowWnd.InvalidateColumn(ColIndex);
        }

        public void SetColumnSelForeColor(int ColIndex, Color ForeColor)
        {
            if (ForeColor == Color.Empty)
                m_RowWnd.ColumnColors.RemoveColor(ColorSelection.SelForeColor, ColIndex);
            else
                m_RowWnd.ColumnColors.SetColor(ColorSelection.SelForeColor, ColIndex, ForeColor);

            m_RowWnd.InvalidateColumn(ColIndex);
        }

        #region Event Passing
        private void FireRowSelected(Row r)
        {
            if (m_bIsComboBox)
            {
                m_DropDownWnd.Close();
                this.Invalidate();
            }

            if (RowSelectionMade != null)
                RowSelectionMade(r);
        }

        private void FireRowDoubleClicked(Row r, int ColIndex)
        {
            if (RowDoubleClicked != null)
                RowDoubleClicked(r, ColIndex);
        }

        private void FireRowClicked(Row r, int ColIndex)
        {
            if (RowClicked != null)
                RowClicked(r, ColIndex);
        }

        private void FireCellEditStarting(Row r, int ColIndex, ref bool bCancel)
        {
            if (CellEditStarting != null)
                CellEditStarting(r, ColIndex, ref bCancel);
        }

        private void FireCellEditStarted(Row r, int ColIndex)
        {
            if (CellEditStarted != null)
                CellEditStarted(r, ColIndex);
        }

        private void FireCellEditFinishing(Row r, int ColIndex, object oOldVal, ref object oNewVal, ref bool bCommit, ref bool bCancel)
        {
            if (CellEditFinishing != null)
                CellEditFinishing(r, ColIndex, oOldVal, ref oNewVal, ref bCommit, ref bCancel);
        }

        private void FireCellEditFinished(Row r, int ColIndex, object oOldVal, object oNewVal)
        {
            if (CellEditFinished != null)
                CellEditFinished(r, ColIndex, oOldVal, oNewVal);
        }

        void FireCellEditFailed(Row r, int ColIndex, object oOldVal, object oNewVal, ref bool bCancel)
        {
            if (CellEditFailed != null)
                CellEditFailed(r, ColIndex, oOldVal, oNewVal, ref bCancel);
        }
        #endregion

        protected override void OnResize(EventArgs e)
        {
            if (!m_bIsComboBox)
                m_RowWnd.Size = this.ClientRectangle.Size;
            else
            {
                m_rButtonArea = Rectangle.Empty;
                this.Invalidate();
            }

            base.OnResize(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            InvalidateBorder();
            InvalidateCombo();
            base.OnEnabledChanged(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            InvalidateBorder();
            InvalidateCombo();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            InvalidateBorder();
            InvalidateCombo();

            base.OnLostFocus(e);
        }

        public Row GetNewRow()
        {
            return m_RowWnd.GetNewRow();
        }

        public void RemoveRow(Row r)
        {
            m_RowWnd.RemoveRow(r);
            InvalidateBorder(true);
            InvalidateCombo(true);
        }

        public void ClearRows()
        {
            m_RowWnd.ClearRows();
            InvalidateBorder(true);
            InvalidateCombo(true);
        }

        public Row GetFirstRow()
        {
            return m_RowWnd.GetFirstRow();
        }

        public void Sort()
        {
            m_RowWnd.Sort();
        }

        public void AddRowAtEnd(Row r)
        {
            m_RowWnd.AddRowAtEnd(r);
        }

        public void AddRowBefore(Row r, Row rBefore)
        {
            m_RowWnd.AddRowBefore(r, rBefore);
        }

        public void AddColumn(string dispName, DatalistDataTypes valType, int width, ColumnType ColType, bool bAllowEdit)
        {
            m_RowWnd.AddColumn(dispName, valType, width, ColType, bAllowEdit);
        }

        public void EnsureRowInView(Row r, bool bFullyVisible = true, bool bTop = true)
        {
            m_RowWnd.EnsureRowInView(r, bFullyVisible, bTop);
        }

        private int GetState()
        {
            // set the state id of the current TextBox
            int stateId;
            if (this.Enabled && !m_bReadOnly)
                if (ContainsFocus || (m_bIsComboBox && m_DropDownWnd.Visible))
                    stateId = (int)ComboBoxState.Pressed;
                else
                    if (ClientRectangle.Contains(PointToClient(Control.MousePosition)))
                    stateId = (int)ComboBoxState.Hot;
                else
                    stateId = (int)ComboBoxState.Normal;
            else
                stateId = (int)ComboBoxState.Disabled;

            return stateId;
        }

        #region Border Drawing and Calculation
        internal void InvalidateBorder(bool bForceInvalidate = false)
        {
            int stateId = GetState();

            if (m_nStateId != stateId || bForceInvalidate)
            {
                m_nStateId = stateId;
                Win32.RedrawWindow(this.Handle, IntPtr.Zero, IntPtr.Zero, (int)(RedrawWindowFlags.Frame | RedrawWindowFlags.Invalidate));
            }
        }

        private void WmNccalcsize(ref Message m)
        {
            // we visual styles are not enabled and BorderStyle is not Fixed3D then we have nothing more to do.
            if (!UxThemeManager.VisualStylesEnabled())
                return;

            // contains detailed information about WM_NCCALCSIZE message
            NCCALCSIZE_PARAMS par = new NCCALCSIZE_PARAMS();

            // contains the window frame RECT
            RECT windowRect;

            if (m.WParam == IntPtr.Zero) // LParam points to a RECT struct
            {
                windowRect = (RECT)Marshal.PtrToStructure(m.LParam, typeof(RECT));
            }
            else // LParam points to a NCCALCSIZE_PARAMS struct
            {
                par = (NCCALCSIZE_PARAMS)Marshal.PtrToStructure(m.LParam, typeof(NCCALCSIZE_PARAMS));
                windowRect = par.rgrc0;
            }

            // contains the client area of the control
            RECT contentRect;

            // get the DC
            IntPtr hDC = Win32.GetWindowDC(this.Handle);

            // find out how much space the borders needs
            if (m_ThemeManager[this].GetThemeBackgroundContentRect(UxThemeElements.COMBOBOX, hDC, (int)ComboBoxPart.Border, (int)ComboBoxState.Normal, ref windowRect, out contentRect))
            {
                // shrink the client area the make more space for containing text.
                contentRect.Inflate(-1, -1);

                // remember the space of the borders
                this.borderRect = new RECT(contentRect.Left - windowRect.Left
                    , contentRect.Top - windowRect.Top
                    , windowRect.Right - contentRect.Right
                    , windowRect.Bottom - contentRect.Bottom);

                // update LParam of the message with the new client area
                if (m.WParam == IntPtr.Zero)
                {
                    Marshal.StructureToPtr(contentRect, m.LParam, false);
                }
                else
                {
                    par.rgrc0 = contentRect;
                    Marshal.StructureToPtr(par, m.LParam, false);
                }

                // force the control to redraw it´s client area
                m.Result = new IntPtr(0x200 | 0x100);
            }

            // release DC
            Win32.ReleaseDC(this.Handle, hDC);

            base.WndProc(ref m);
        }

        void WmNcpaint(ref Message m)
        {
            if (!UxThemeManager.VisualStylesEnabled())
                return;

            /////////////////////////////////////////////////////////////////////////////
            // Get the DC of the window frame and paint the border using uxTheme API´s
            /////////////////////////////////////////////////////////////////////////////

            // set the part id to TextBox
            int partId = (int)ComboBoxPart.Border;

            // define the windows frame rectangle of the TextBox
            RECT windowRect;
            Win32.GetWindowRect(this.Handle, out windowRect);
            windowRect.Right -= windowRect.Left; windowRect.Bottom -= windowRect.Top;
            windowRect.Top = windowRect.Left = 0;

            // get the device context of the window frame
            IntPtr hDC = Win32.GetWindowDC(this.Handle);

            // define a rectangle inside the borders and exclude it from the DC
            RECT clientRect = windowRect;
            clientRect.Left += this.borderRect.Left;
            clientRect.Top += this.borderRect.Top;
            clientRect.Right -= this.borderRect.Right;
            clientRect.Bottom -= this.borderRect.Bottom;
            Win32.ExcludeClipRect(hDC, clientRect.Left, clientRect.Top, clientRect.Right, clientRect.Bottom);

            // make sure the background is updated when transparent background is used.
            if (m_ThemeManager[this].IsThemeBackgroundPartiallyTransparent(UxThemeElements.COMBOBOX, partId, m_nStateId))
            {
                m_ThemeManager[this].DrawThemeParentBackground(hDC, ref windowRect);
            }

            // draw background
            m_ThemeManager[this].DrawThemeBackground(UxThemeElements.COMBOBOX, hDC, partId, m_nStateId, ref windowRect, IntPtr.Zero);

            // release dc
            Win32.ReleaseDC(this.Handle, hDC);

            // we have processed the message so set the result to zero
            m.Result = IntPtr.Zero;

            base.WndProc(ref m);
        }
        #endregion

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case (int)WindowsMessages.WM_NCCALCSIZE:
                    WmNccalcsize(ref m);
                    break;
                case (int)WindowsMessages.WM_NCPAINT:
                    WmNcpaint(ref m);
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        private void ConfigureListForCombo(bool bIsCombo)
        {
            m_bIsComboBox = bIsCombo;
            m_nComboStateId = (int)ComboBoxState.Normal;
            m_rButtonArea = Rectangle.Empty;
            m_bComboClosedButton = false;
            m_bMouseDownInButton = false;
            m_RowWnd.ClearHighlight();

            if (!bIsCombo)
            {
                this.Controls.Add(m_RowWnd);
                m_RowWnd.Size = this.ClientRectangle.Size;
                m_DropDownWnd = null;
            }
            else
            {
                //m_RowWnd.Size = new Size(this.Width, 500);
                this.Controls.Remove(m_RowWnd);
                m_DropDownWnd = new DropDownWnd(this, m_RowWnd);
                m_DropDownWnd.Closed += new ToolStripDropDownClosedEventHandler(OnDropDownClosed);
                m_DropDownWnd.Opened += new EventHandler(OnDropDownOpened);
            }

            InvalidateBorder(true);
        }

        void OnDropDownOpened(object sender, EventArgs e)
        {
            m_RowWnd.ClearHighlight();
            Row ToEnsure = null;

            if (m_RowWnd.CurrSel != null)
            {
                m_RowWnd.CurrHighlight = m_RowWnd.CurrSel;
                ToEnsure = m_RowWnd.CurrSel;
            }
            else
            {
                ToEnsure = m_RowWnd.GetFirstRow();
            }

            m_RowWnd.RecalculateScrollBars();
            m_RowWnd.EnsureRowAtTopOfView(ToEnsure, true, true);
        }

        void OnDropDownClosed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            m_bComboClosedButton = e.CloseReason == ToolStripDropDownCloseReason.AppClicked &&
                GetButtonRect().Contains(PointToClient(Control.MousePosition));
            InvalidateBorder();
            InvalidateCombo();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            InvalidateBorder();
            InvalidateCombo();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            InvalidateBorder();
            InvalidateCombo();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            InvalidateCombo();
            base.OnMouseMove(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (e.Delta == 0)
                return;

            if (this.ReadOnly)
                return;

            if (m_bIsComboBox)
            {
                Row CurSel = m_RowWnd.CurrSel;

                if (CurSel != null)
                {
                    if (e.Delta < 0)
                    {
                        if (CurSel.GetNextRow() != null)
                            m_RowWnd.CurrSel = CurSel.GetNextRow();
                    }
                    else
                    {
                        if (CurSel.GetPreviousRow() != null)
                            m_RowWnd.CurrSel = CurSel.GetPreviousRow();
                    }
                }
                else
                {
                    m_RowWnd.CurrSel = m_RowWnd.GetFirstRow();
                }

                this.Invalidate();
            }

            base.OnMouseWheel(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (m_bIsComboBox)
            {
                m_bMouseDownInButton = GetButtonRect().Contains(e.Location) && !m_bComboClosedButton;
                this.Invalidate();
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (m_bIsComboBox)
            {
                if (!m_bComboClosedButton && GetButtonRect().Contains(e.Location))
                {
                    if (m_DropDownWnd.Visible)
                    {
                        m_DropDownWnd.Close();
                    }
                    else
                    {
                        ShowDropDown();
                    }
                }

                m_bComboClosedButton = false;
                InvalidateCombo();

                m_bMouseDownInButton = false;
            }

            base.OnMouseDown(e);
        }

        private void ShowDropDown()
        {
            if (m_bIsComboBox)
            {
                bool bCancel = false;

                if (DroppingDown != null)
                    DroppingDown(ref bCancel);

                if (!bCancel)
                {
                    int nCurrListHeight = m_RowWnd.Rows.TotalRowHeight
                        + (m_RowWnd.ColumnHeader.Visible ? m_RowWnd.ColumnHeader.ColumnHeight : 0);

                    m_DropDownWnd.Size = new Size(
                        m_nDropDownWidth < 0 ? this.Width : m_nDropDownWidth,
                        nCurrListHeight > m_nDropDownMaxHeight ? m_nDropDownMaxHeight : nCurrListHeight);

                    m_DropDownWnd.Show(Parent.PointToScreen(new Point(this.Location.X, this.Location.Y + this.Height)));
                    m_RowWnd.Focus();

                    if (DroppedDown != null)
                        DroppedDown();
                }
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Down:
                    return true;
                default:
                    return base.IsInputKey(keyData);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (m_bIsComboBox && e.KeyCode == Keys.Down)
            {
                if (!m_DropDownWnd.Visible)
                {
                    ShowDropDown();
                }
            }

            base.OnKeyDown(e);
        }

        private Rectangle GetButtonRect()
        {
            if (m_rButtonArea.IsEmpty)
            {
                int nButtonWidth = SystemInformation.VerticalScrollBarWidth;
                m_rButtonArea = new Rectangle(this.Width - nButtonWidth - 2, 0, nButtonWidth, this.Height - 2);
            }

            return m_rButtonArea;
        }

        private int GetComboButtonState()
        {
            // set the state id of the current TextBox
            int stateId;
            bool bInButton = GetButtonRect().Contains(PointToClient(Control.MousePosition));

            if (this.Enabled)
                if ((m_DropDownWnd != null && m_DropDownWnd.Visible) || (m_bMouseDownInButton && bInButton))
                    stateId = (int)ComboBoxState.Pressed;
                else
                    if (bInButton)
                    stateId = (int)ComboBoxState.Hot;
                else
                    stateId = (int)ComboBoxState.Normal;
            else
                stateId = (int)ComboBoxState.Disabled;

            return stateId;
        }

        private void InvalidateCombo(bool bForceInvalidate = false)
        {
            if (m_bIsComboBox)
            {
                int stateId = GetComboButtonState();

                if (m_nComboStateId != stateId || bForceInvalidate)
                {
                    m_nComboStateId = stateId;
                    this.Invalidate();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                if (m_bIsComboBox)
                {
                    Row CurSel = m_RowWnd.CurrSel;
                    bool bHaveRow = m_RowWnd.CurrSel != null;
                    SolidBrush Backcolor;

                    if (m_AllowRowColorCombo && bHaveRow)
                        Backcolor = new SolidBrush(m_RowWnd.GetRowColor(ColorSelection.BackColor, m_RowWnd.CurrSel));
                    else
                        Backcolor = new SolidBrush(m_RowWnd.BackColor);

                    e.Graphics.FillRectangle(Backcolor, this.ClientRectangle);
                    Backcolor.Dispose();

                    Rectangle BtnRect = GetButtonRect();

                    if (UxThemeManager.VisualStylesEnabled())
                    {
                        IntPtr hDC = e.Graphics.GetHdc();
                        m_ThemeManager[this].DrawThemeBackground(UxThemeElements.COMBOBOX, hDC, (int)ComboBoxPart.DropDownButtonRight,
                            GetComboButtonState(), ref BtnRect, IntPtr.Zero);
                        e.Graphics.ReleaseHdc(hDC);
                    }

                    int nWidth = this.ClientRectangle.Width - BtnRect.Width - 2;
                    if (nWidth > 0)
                    {
                        int nHeight = this.ClientRectangle.Height - 2;

                        if (nHeight > 0)
                        {
                            bool bNeedSelColor = (IsDroppedDown || ContainsFocus) && !ReadOnly;
                            if (bNeedSelColor)
                            {

                                SolidBrush SelBackcolor;
                                if (m_AllowRowColorCombo && bHaveRow)
                                    SelBackcolor = new SolidBrush(m_RowWnd.GetRowColor(ColorSelection.SelBackColor, CurSel));
                                else
                                    SelBackcolor = new SolidBrush(SystemColors.Highlight);

                                e.Graphics.FillRectangle(SelBackcolor, 1, 1, nWidth, nHeight);
                                SelBackcolor.Dispose();

                                Pen p = new Pen(Color.DarkGray);
                                p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                                e.Graphics.DrawRectangle(p, 1, 1, nWidth, nHeight);
                                p.Dispose();
                            }

                            nWidth -= 4;
                            nHeight -= 8;
                            if (nWidth > 0 && nHeight > 0)
                            {
                                StringFormat sf = new StringFormat();
                                sf.FormatFlags = System.Drawing.StringFormatFlags.NoWrap;
                                sf.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
                                sf.Alignment = StringAlignment.Near;
                                sf.LineAlignment = StringAlignment.Center;
                                Brush ForText;

                                if (bHaveRow)
                                {
                                    ForText = new SolidBrush(m_RowWnd.GetRowColor(bNeedSelColor ? ColorSelection.SelForeColor : ColorSelection.ForeColor, CurSel));
                                }
                                else
                                {
                                    ForText = new SolidBrush(bNeedSelColor ? SystemColors.HighlightText : this.ForeColor);
                                }

                                e.Graphics.DrawString(bHaveRow ? CurSel.ToString(m_strComboFormat) : m_strComboText,
                                    this.Font, ForText, new Rectangle(5, 5, nWidth, nHeight), sf);
                                ForText.Dispose();
                                sf.Dispose();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            base.OnPaint(e);
        }
    }
}
