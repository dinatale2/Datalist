using System;
using System.ComponentModel;
using System.Drawing;
using Drawing.ThemeRoutines;
using System.Windows.Forms;

namespace DataList
{
    public enum ColumnType
    {
        TextNoWrap = 0,
        TextWrap = 1,
        RowBackColor,
        RowForeColor,
        RowSelBackColor,
        RowSelForeColor,
        CheckBox,
        ComboBox,
        ProgressBar,
    };

    internal enum ColumnState
    {
        Normal = 0, //<! Button is enabled, but not clicked
        Clicked = 1,    //<! Button is clicked, meaning selected as check button
        Pressed = 2,    //<! Button is pressed with the mouse right now
        Hot = 3,    //<! Button is hot, meaning mouse cursor is over button
        PressedAndHot = 4,
        ClickedAndHot = 5,
    };

    [
    DesignTimeVisible(true),
    TypeConverter("DataList.ColumnConverter")
    ]
    public class Column
    {
        private ColumnCollection m_Parent;
        internal ColumnCollection Parent
        {
            get { return m_Parent; }
            set { m_Parent = value; }
        }

        private string m_Text;
        public string Text
        {
            get { return m_Text; }
            set { m_Text = value; }
        }

        private DatalistDataTypes m_ValueType;
        public DatalistDataTypes ValueType
        {
            get { return m_ValueType; }
            set { m_ValueType = value; }
        }

        //private bool m_NumericSorting = false;
        //public bool NumericSorting
        //{
        //  get { return m_NumericSorting; }
        //  set { m_NumericSorting = value; }
        //}

        private int m_StoredWidth;
        public int StoredWidth
        {
            get { return m_StoredWidth; }
            set { m_StoredWidth = value; }
        }

        private int m_Width;
        public int Width
        {
            get
            {
                if (Visible)
                    return m_Width;
                else
                    return 0;
            }
            set { m_Width = value; }
        }

        private ColumnType m_Type = ColumnType.TextNoWrap;
        public ColumnType Type
        {
            get { return m_Type; }
            set
            {
                m_Type = value;
                ConfigureComboControl(m_Type == ColumnType.ComboBox);
            }
        }

        private ColumnComboBox m_ComboBox;
        internal ColumnComboBox ComboBox
        {
            get { return m_ComboBox; }
        }

        private string m_strComboSource;
        public string ComboSource
        {
            get { return m_strComboSource; }
            set
            {
                m_strComboSource = value;
                if (m_ComboBox != null)
                    m_ComboBox.SetComboSource(m_strComboSource);
            }
        }

        private int m_nSortPriority;
        public int SortPriority
        {
            get { return m_nSortPriority; }
            set
            {
                SetSortPriority(value, true);
            }
        }

        private bool m_AllowResize = true;
        public bool AllowResize
        {
            get { return m_AllowResize; }
            set { m_AllowResize = value; }
        }

        private bool m_AllowSort = true;
        public bool AllowSort
        {
            get { return RowSortPredicate.AllowedToSort(m_ValueType) && m_AllowSort && m_Parent.Parent.Parent.AllowSort; }
            // We want the datalist datatype of object to be a READ ONLY thing, and have it not display text for it so the user is not inclined to edit it
            set { m_AllowSort = value && RowSortPredicate.AllowedToSort(m_ValueType); }
        }

        private bool m_bIsVisible = true;
        public bool Visible
        {
            get { return m_bIsVisible; }
            set
            {
                if (!value)
                    m_Width = 0;
                else
                    m_Width = m_StoredWidth;

                m_bIsVisible = value;
            }
        }

        private bool m_bAllowUserEdit;
        public bool AllowUserEdit
        {
            get { return RowSortPredicate.AllowedToSort(m_ValueType) && m_bAllowUserEdit && m_Parent.Parent.Parent.AllowEdit; }
            // We want the datalist datatype of object to be a READ ONLY thing, and have it not display text for it so the user is not inclined to edit it
            set { m_bAllowUserEdit = (value && RowSortPredicate.AllowedToSort(m_ValueType)); }
        }

        //internal int Index
        //{
        //  get
        //  {
        //    if (m_Parent != null)
        //      return m_Parent.Parent.Columns.GetColumnIndex(this);
        //    else
        //      return -1;
        //  }
        //}

        //private bool m_Selected;
        //internal bool Selected
        //{
        //  get { return m_Selected; }
        //  set { m_Selected = value; }
        //}

        private bool m_bAscending;
        internal bool SortAscending
        {
            get { return m_bAscending; }
            set { m_bAscending = value; }
        }

        private int m_nIndex;
        internal int Index
        {
            get { return m_nIndex; }
            set { m_nIndex = value; }
        }

        public Column()
        {
            m_Text = "";
            m_ValueType = DatalistDataTypes.String;
            m_Parent = null;
            m_Type = ColumnType.TextNoWrap;

            m_bAllowUserEdit = false;
            m_bAscending = false;

            //m_Selected = false;
            m_nIndex = -1;
            m_nSortPriority = -1;

            m_StoredWidth = 100;
            m_Width = 100;
        }

        internal Column(string dispName, DatalistDataTypes valType, int width, ColumnType ColType, bool bAllowEdit)
        {
            m_Text = dispName;
            m_ValueType = valType;
            m_Type = ColType;

            m_bAllowUserEdit = bAllowEdit;
            m_bAscending = false;
            //m_Selected = false;

            m_nIndex = -1;
            m_nSortPriority = -1;

            m_StoredWidth = width;
            m_Width = width;
        }

        internal bool IsVariableHeight()
        {
            switch (m_Type)
            {
                case ColumnType.TextWrap:
                    return true;
                default:
                    return false;
            }
        }

        internal void SetSortPriority(int nNewPriority, bool bFixAll)
        {
            if (bFixAll && m_Parent != null)
                m_Parent.HandleSortPriorities(this, nNewPriority);

            if (nNewPriority < 0)
                m_nSortPriority = -1;
            else
                m_nSortPriority = nNewPriority;
        }

        private ColumnState GetState(bool bIsPressed, Rectangle rArea)
        {
            bool bIsMouseOver = false;

            {
                Point p = m_Parent.Parent.PointToClient(Control.MousePosition);
                bIsMouseOver = m_Parent.Parent.ClientRectangle.Contains(p) && rArea.Contains(p);
            }

            if (AllowSort)
            {
                if (bIsPressed)
                {
                    if (bIsMouseOver)
                        return ColumnState.PressedAndHot;
                    else
                        return ColumnState.Hot;
                }
                else
                {
                    return ColumnState.Normal;
                }
            }

            return ColumnState.Normal;
        }

        internal void ConfigureComboControl(bool bIsComboCol)
        {
            if (bIsComboCol)
            {
                if (m_ComboBox == null)
                {
                    if (m_Parent != null && m_Parent.Parent != null)
                        m_ComboBox = new ColumnComboBox(m_Parent.Parent, m_strComboSource);
                }
            }
            else
            {
                if (m_ComboBox != null)
                {
                    m_ComboBox.Dispose();
                    m_ComboBox = null;
                }
            }
        }

        private void DrawHeaderBackground(Graphics GFX, Rectangle rectHeader, bool bIsPressed)
        {
            IntPtr hdc = GFX.GetHdc();
            int nHeaderItemState = (int)HeaderItemState.Normal;

            switch (GetState(bIsPressed, rectHeader))
            {
                case ColumnState.PressedAndHot:
                    nHeaderItemState = (int)HeaderItemState.Pressed;
                    break;
                case ColumnState.Hot:
                case ColumnState.ClickedAndHot:
                    nHeaderItemState = (int)HeaderItemState.Hot;
                    break;
                default:
                case ColumnState.Normal:
                case ColumnState.Pressed:
                case ColumnState.Clicked:
                    // already defaulted
                    break;
            }

            m_Parent.Parent.Themes[m_Parent.Parent].DrawThemeBackground(UxThemeElements.HEADER, hdc, (int)HeaderPart.HeaderItem, nHeaderItemState, ref rectHeader, ref rectHeader);
            GFX.ReleaseHdc(hdc);
        }

        private void DrawHiddenIndicators(Graphics GFX, Rectangle rectHeader)
        {
            Pen p = new Pen(Color.Black);
            GFX.DrawLine(p, rectHeader.Left - 1, 0, rectHeader.Left - 1, rectHeader.Height - 1);
            GFX.DrawLine(p, rectHeader.Right - 1, 0, rectHeader.Right - 1, rectHeader.Height - 1);
            p.Dispose();
        }

        private int DrawSortIndicator(Graphics GFX, Rectangle rectHeader)
        {
            if (m_nSortPriority != 0)
                return 0;

            int x = Math.Max(rectHeader.Left, rectHeader.Right - 10);
            int y = Math.Max(rectHeader.Top, rectHeader.Top + (rectHeader.Height / 2) - 5);
            Rectangle arrowRect = new Rectangle(x, y, Math.Min(rectHeader.Width, 10), Math.Min(rectHeader.Height, 10));

            IntPtr hdc = GFX.GetHdc();
            int nHeaderItemState = m_bAscending ? (int)HeaderSortArrowState.SortedUp : (int)HeaderSortArrowState.SortedDown;
            m_Parent.Parent.Themes[m_Parent.Parent].DrawThemeBackground(UxThemeElements.HEADER, hdc, (int)HeaderPart.HeaderSortArrow, nHeaderItemState, ref arrowRect, ref arrowRect);
            GFX.ReleaseHdc(hdc);

#if DEBUG
            Brush drawRectColor = new SolidBrush(Color.Green);
            GFX.FillRectangle(drawRectColor, arrowRect);
            drawRectColor.Dispose();
#endif

            return 10;
        }

        private void DrawHeaderText(Graphics GFX, Color TextColor, Font TextFont, Rectangle drawArea)
        {
            if (drawArea.Width <= 0)
                return;

            Brush TextBrush = new SolidBrush(TextColor);
            StringFormat sf = new StringFormat();
            sf.FormatFlags = System.Drawing.StringFormatFlags.NoWrap;
            sf.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
            GFX.DrawString(Text, TextFont, TextBrush, drawArea, sf);
            TextBrush.Dispose();
            sf.Dispose();
        }

        internal void DrawColumn(Graphics GFX, int x, bool bIsPressed)
        {
            if (!Visible)
                return;

            int height = m_Parent.Parent.ColumnHeader.ColumnHeight;
            int cellPadding = m_Parent.Parent.CellPadding;
            int width = Width;

            Rectangle drawRect = new Rectangle(x, 0, width, height);
            DrawHeaderBackground(GFX, drawRect, bIsPressed);

            if (width <= 2 * cellPadding)
            {
                DrawHiddenIndicators(GFX, drawRect);
                return;
            }

            drawRect.Inflate(-cellPadding, -cellPadding);

            Color TextColor = Color.Black;
            Font TextFont = m_Parent.Parent.Font;
            drawRect.Width = drawRect.Width - DrawSortIndicator(GFX, drawRect);

#if DEBUG
            Brush drawRectColor = new SolidBrush(Color.Red);
            GFX.FillRectangle(drawRectColor, drawRect);
            drawRectColor.Dispose();
#endif

            DrawHeaderText(GFX, TextColor, TextFont, drawRect);
        }
    }
}
