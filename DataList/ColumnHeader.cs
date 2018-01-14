using System;
using System.Windows.Forms;
using System.Drawing;
using Drawing.ThemeRoutines;
using DebugLib;

namespace DataList
{
    internal delegate void ColumnHeaderColumnChange(int nIndex);
    internal delegate void ColumnHeaderColumnCollChange(Column c);

    internal class ColumnHeader
    {
        internal event ColumnHeaderColumnChange ColumnResizing;
        internal event ColumnHeaderColumnChange ColumnResizeEnd;
        internal event DataListColumnLeftClick ColumnLeftClicked;
        internal event ColumnHeaderColumnChange ColumnAdded;
        internal event EventHandler VisibleChanged;
        internal event ColumnHeaderColumnChange ColumnRemoving;
        internal event ColumnHeaderColumnCollChange ColumnRemoved;

        private bool m_bVisible;
        public bool Visible
        {
            get { return m_bVisible; }
            set
            {
                m_bVisible = value;
                if (VisibleChanged != null)
                    VisibleChanged(this, EventArgs.Empty);
            }
        }

        private ColumnCollection m_Columns;
        internal ColumnCollection Columns
        {
            get { return m_Columns; }
        }

        internal int ColumnHeight
        {
            get { return m_Columns.ColumnHeight; }
        }

        internal Rectangle ClientRectangle
        {
            get { return new Rectangle(0, 0, m_Parent.ClientRectangle.Width, ColumnHeight); }
        }

        private ListWnd m_Parent;
        internal ListWnd Parent
        {
            get { return m_Parent; }
        }

        private Column m_MouseDownIn;
        private Column m_Resizing;
        private Column m_ResizingPrev;
        private Column m_CurrSelCol;

        private ThresholdDiag ColumnsDiag;
        private bool m_bMouseDownInHeader;
        internal bool MouseDownInHeader
        {
            get { return m_bMouseDownInHeader; }
        }

        internal ColumnHeader(ListWnd ParentWnd)
        {
            m_Parent = ParentWnd;
            m_Columns = new ColumnCollection(ParentWnd);
            m_Columns.ColumnAdded += new ColumnCollectionChanged(OnColumnAdded);
            m_Columns.ColumnRemoving += new ColumnCollectionChanged(OnColumnRemoving);
            m_Columns.ColumnRemoved += new ColumnCollectionColumnChanged(OnColumnRemoved);

            m_bVisible = false;
            m_bMouseDownInHeader = false;

            ColumnsDiag = new ThresholdDiag("ColumnHeader.OnPaint", null, 100.0);
        }

        ~ColumnHeader()
        {
            ColumnsDiag.Dispose();
        }

        internal void AddColumn(string dispName, DatalistDataTypes valType, int width, ColumnType ColType, bool bAllowEdit)
        {
            m_Columns.Add(dispName, valType, width, ColType, bAllowEdit);
        }

        internal int FindColumnXPos(Column c, int xOffset)
        {
            int nCurrX = xOffset;
            int nIndex = c.Index;
            // find the Column we are under
            for (int i = 0; i < nIndex; i++)
            {
                nCurrX += m_Columns[i].Width;
            }

            return nCurrX;
        }

        internal int FindColumnXPos(int ColIndex, int xOffset)
        {
            if (ColIndex >= 0 && ColIndex < m_Columns.Count)
                return FindColumnXPos(m_Columns[ColIndex], xOffset);
            else
                return 0;
        }

        internal Column FindColumn(int X, int xOffset)
        {
            int nCurrX = xOffset;
            // find the Column we are under
            for (int i = 0; i < m_Columns.Count; i++)
            {
                if (m_Columns[i].Width > 0)
                {
                    if (X >= nCurrX && X < nCurrX + m_Columns[i].Width)
                    {
                        return m_Columns[i];
                    }

                    nCurrX += m_Columns[i].Width;
                }
            }

            return null;
        }

        internal void DrawColumnHeader(PaintEventArgs e, int x)
        {
            if (!Visible)
                return;

            ColumnsDiag.StartTiming();

            int nWidth = m_Parent.ClientRectangle.Width;
            int nHeight = ColumnHeight;

            Rectangle ClientArea = new Rectangle(0, 0, nWidth, nHeight);
            ClientArea.Width += 1;

            IntPtr hdc = e.Graphics.GetHdc();
            m_Parent.Themes[m_Parent].DrawThemeBackground(UxThemeElements.HEADER, hdc,
              (int)HeaderPart.HeaderItem, (int)HeaderItemState.Normal, ref ClientArea, ref ClientArea);
            e.Graphics.ReleaseHdc(hdc);

            for (int i = 0; i < m_Columns.Count; i++)
            {
                Column col = m_Columns[i];

                if ((x >= 0 || (x + col.Width) > 0) && (x <= nWidth))
                {
                    if (e.ClipRectangle.IntersectsWith(new Rectangle(x, 0, col.Width, nHeight)))
                    {
                        col.DrawColumn(e.Graphics, x, col == m_MouseDownIn);
                    }
                }

                x += col.Width;
            }

            //base.OnPaint(e);

            ColumnsDiag.EndTiming();
        }

        private Column FindPrevVisibleColumn(Column Col)
        {
            int nCurrIndex = Col.Index - 1;

            while (nCurrIndex >= 0 && !m_Columns[nCurrIndex].Visible)
                nCurrIndex--;

            if (nCurrIndex >= 0)
                return m_Columns[nCurrIndex];
            else
                return null;
        }

        internal void OnMouseMove(MouseEventArgs e, int xOffset)
        {
            if (m_bMouseDownInHeader)
            {
                if (m_Resizing != null)
                {
                    int X = FindColumnXPos(m_Resizing, xOffset);
                    int nWidth = m_Resizing.Width;
                    int WidthAdjust = e.X - (X + nWidth);

                    nWidth = nWidth + WidthAdjust;
                    nWidth = nWidth < 0 ? 0 : nWidth;

                    if (nWidth != m_Resizing.Width)
                    {
                        m_Resizing.Width = nWidth;

                        if (ColumnResizing != null)
                            ColumnResizing(m_Resizing.Index);
                    }
                }
                else
                {
                    if (m_ResizingPrev != null)
                    {
                        int X = FindColumnXPos(m_ResizingPrev, xOffset);
                        int nWidth = m_ResizingPrev.Width;
                        int WidthAdjust = e.X - (X + nWidth);

                        nWidth = nWidth + WidthAdjust;
                        nWidth = nWidth < 0 ? 0 : nWidth;

                        if (nWidth != m_ResizingPrev.Width)
                        {
                            m_ResizingPrev.Width = nWidth;

                            if (ColumnResizing != null)
                                ColumnResizing(m_ResizingPrev.Index);
                        }
                    }
                    else
                    {
                        if (m_MouseDownIn != null)
                        {
                            int X = FindColumnXPos(m_MouseDownIn, xOffset);
                            m_Parent.Invalidate(new Rectangle(X, 0, m_MouseDownIn.Width, ColumnHeight));
                        }
                    }
                }
            }
            else
            {
                Column MouseIn = FindColumn(e.X, xOffset);

                if (MouseIn != null)
                {
                    int X = FindColumnXPos(MouseIn, xOffset);

                    if ((e.X - X <= 4) && MouseIn.Index != 0)
                    {
                        Column PrevVis = FindPrevVisibleColumn(MouseIn);
                        if (PrevVis != null && PrevVis.AllowResize)
                            Cursor.Current = Cursors.VSplit;
                    }
                    else
                    {
                        if (((X + MouseIn.Width) - e.X) <= 4)
                        {
                            if (MouseIn.AllowResize)
                                Cursor.Current = Cursors.VSplit;
                        }
                    }
                }
                else
                {
                    if (m_Columns.Count > 0 && (e.X - m_Columns.RowWidth + xOffset) <= 4)
                    {
                        if (m_Columns[m_Columns.Count - 1].AllowResize)
                            Cursor.Current = Cursors.VSplit;
                    }
                }
            }
        }

        internal void OnMouseUp(MouseEventArgs e, int xOffset)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (m_Resizing != null)
                {
                    m_Resizing.StoredWidth = m_Resizing.Width;
                    m_Resizing = null;
                    if (ColumnResizeEnd != null)
                        ColumnResizeEnd(-1);
                }
                else
                {
                    if (m_ResizingPrev != null)
                    {
                        m_ResizingPrev.StoredWidth = m_ResizingPrev.Width;
                        m_ResizingPrev = null;
                        if (ColumnResizeEnd != null)
                            ColumnResizeEnd(-1);
                    }
                    else
                    {
                        if (m_MouseDownIn != null)
                        {
                            int X = FindColumnXPos(m_MouseDownIn, xOffset);
                            bool bFireLeftClick = false;

                            {
                                int nHeight = ColumnHeight;
                                Rectangle Bounds = new Rectangle(X, 0, m_MouseDownIn.Width, nHeight);
                                if (Bounds.Contains(e.Location))
                                {
                                    if (m_CurrSelCol != m_MouseDownIn)
                                    {
                                        if (m_CurrSelCol != null)
                                        {
                                            int oldX = FindColumnXPos(m_CurrSelCol, xOffset);
                                            m_Parent.Invalidate(new Rectangle(oldX, 0, m_CurrSelCol.Width, nHeight));
                                        }

                                        m_CurrSelCol = m_MouseDownIn;
                                        int nNewSortPriority = 0;
                                        m_Columns.HandleSortPriorities(m_MouseDownIn, nNewSortPriority);
                                    }

                                    m_CurrSelCol.SortAscending = !m_CurrSelCol.SortAscending;
                                    bFireLeftClick = true;
                                }

                                m_Parent.Invalidate(Bounds);
                            }

                            m_MouseDownIn = null;

                            if (bFireLeftClick && ColumnLeftClicked != null)
                                ColumnLeftClicked(m_CurrSelCol.Index);
                        }
                    }
                }

                m_bMouseDownInHeader = false;
                m_Resizing = null;
                m_ResizingPrev = null;
                m_MouseDownIn = null;
            }

            //base.OnMouseUp(e);
        }

        internal void OnMouseDown(MouseEventArgs e, int xOffset)
        {
            if (e.Button == MouseButtons.Left)
            {
                Column MouseDown = FindColumn(e.X, xOffset);

                if (MouseDown != null)
                {
                    int X = FindColumnXPos(MouseDown, xOffset);

                    if ((e.X - X <= 4) && MouseDown.Index != 0)
                    {
                        Column Prev = FindPrevVisibleColumn(MouseDown);

                        if (Prev != null && Prev.AllowResize)
                        {
                            Cursor.Current = Cursors.VSplit;
                            m_ResizingPrev = Prev;
                        }
                    }
                    else
                    {
                        if ((X + MouseDown.Width) - e.X <= 4)
                        {
                            if (MouseDown.AllowResize)
                            {
                                Cursor.Current = Cursors.VSplit;
                                m_Resizing = MouseDown;
                            }
                        }
                    }

                    m_MouseDownIn = (!MouseDown.AllowSort || m_Resizing != null || m_ResizingPrev != null) ? null : MouseDown;

                    if (m_MouseDownIn != null)
                        m_Parent.Invalidate(new Rectangle(X, 0, MouseDown.Width, ColumnHeight));
                }
                else
                {
                    if (m_Columns.Count > 0 && (e.X - m_Columns.RowWidth + xOffset) <= 4)
                    {
                        if (m_Columns[m_Columns.Count - 1].AllowResize)
                        {
                            Cursor.Current = Cursors.VSplit;
                            m_ResizingPrev = m_Columns[m_Columns.Count - 1];
                        }
                    }

                    m_MouseDownIn = null;
                }

                m_bMouseDownInHeader = true;
            }
            else
            {
                if (m_bMouseDownInHeader && e.Button == MouseButtons.Right)
                {
                    if (m_Resizing != null)
                    {
                        m_Resizing.Width = m_Resizing.StoredWidth;
                        Cursor.Current = Cursors.Default;

                        if (ColumnResizing != null)
                            ColumnResizing(m_Resizing.Index);

                        m_Resizing = null;
                    }
                    else
                    {
                        if (m_ResizingPrev != null)
                        {
                            m_ResizingPrev.Width = m_ResizingPrev.StoredWidth;
                            Cursor.Current = Cursors.Default;

                            if (ColumnResizing != null)
                                ColumnResizing(m_ResizingPrev.Index);

                            m_ResizingPrev = null;
                        }
                        else
                        {
                            if (m_MouseDownIn != null)
                            {
                                int X = FindColumnXPos(m_MouseDownIn, xOffset);
                                m_Parent.Invalidate(new Rectangle(X, 0, m_MouseDownIn.Width, ColumnHeight));
                                m_MouseDownIn = null;
                            }
                        }
                    }
                }
            }
        }

        void OnColumnRemoving(int nIndex)
        {
            if (ColumnRemoving != null)
                ColumnRemoving(nIndex);
        }

        void OnColumnRemoved(Column c)
        {
            if (ColumnRemoved != null)
                ColumnRemoved(c);
        }

        private void OnColumnAdded(int nIndex)
        {
            m_Columns[nIndex].ConfigureComboControl(m_Columns[nIndex].Type == ColumnType.ComboBox);
            if (ColumnAdded != null)
                ColumnAdded(nIndex);
        }
    }
}
