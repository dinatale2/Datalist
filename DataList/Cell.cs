using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Drawing.ThemeRoutines;

namespace DataList
{
    public class Cell
    {
        private object m_Value;
        public object Value
        {
            get
            { return m_Value; }
            set
            { SetValue(value); }
        }

        private Row m_ParentRow;
        public Row ParentRow
        {
            get { return m_ParentRow; }
            set { m_ParentRow = value; }
        }

        private int m_ColumnIndex = -1;
        public int ColumnIndex
        {
            get { return m_ColumnIndex; }
            set { m_ColumnIndex = value; }
        }

        public int Height
        {
            get { return m_ParentRow.Height; }
        }

        public string Text
        {
            get
            {
                if (m_ParentRow.Parent.Columns[m_ColumnIndex].ValueType != DatalistDataTypes.Object)
                {
                    if (m_Value != null)
                    {
                        if (m_ParentRow.Parent.Columns[m_ColumnIndex].Type == ColumnType.ComboBox)
                            return m_ParentRow.Parent.Columns[m_ColumnIndex].ComboBox.GetValueDisplayName((long)m_Value);
                        else
                            return m_Value.ToString();
                    }
                    else
                        if (m_ParentRow.Parent.Parent.ShowNull)
                        return "NULL";
                    else
                        return "";
                }

                return "";
            }
        }

        public int Width
        {
            get { return m_ParentRow.Parent.Columns[m_ColumnIndex].Width; }
        }

        public Cell(Row parentRow, int ColIndex)
        {
            m_ParentRow = parentRow;
            m_Value = null;
            m_ColumnIndex = ColIndex;
        }

        ~Cell()
        {
            m_ParentRow = null;
            m_Value = null;
            m_ColumnIndex = -1;
        }

        internal ColumnType GetColumnType()
        {
            return m_ParentRow.Parent.Columns[m_ColumnIndex].Type;
        }

        private void SetValue(object value)
        {
            switch (m_ParentRow.Parent.Columns[m_ColumnIndex].Type)
            {
                case ColumnType.RowBackColor:
                    if (value == null)
                        m_ParentRow.Parent.RowColors.RemoveColor(ColorSelection.BackColor, m_ParentRow);
                    else
                        m_ParentRow.Parent.RowColors.SetColor(ColorSelection.BackColor, m_ParentRow, (Color)value);
                    m_ParentRow.Parent.InvalidateRow(m_ParentRow);
                    break;
                case ColumnType.RowForeColor:
                    if (value == null)
                        m_ParentRow.Parent.RowColors.RemoveColor(ColorSelection.ForeColor, m_ParentRow);
                    else
                        m_ParentRow.Parent.RowColors.SetColor(ColorSelection.ForeColor, m_ParentRow, (Color)value);
                    m_ParentRow.Parent.InvalidateRow(m_ParentRow);
                    break;
                case ColumnType.RowSelForeColor:
                    if (value == null)
                        m_ParentRow.Parent.RowColors.RemoveColor(ColorSelection.SelForeColor, m_ParentRow);
                    else
                        m_ParentRow.Parent.RowColors.SetColor(ColorSelection.SelForeColor, m_ParentRow, (Color)value);
                    m_ParentRow.Parent.InvalidateRow(m_ParentRow);
                    break;
                case ColumnType.RowSelBackColor:
                    if (value == null)
                        m_ParentRow.Parent.RowColors.RemoveColor(ColorSelection.SelBackColor, m_ParentRow);
                    else
                        m_ParentRow.Parent.RowColors.SetColor(ColorSelection.SelBackColor, m_ParentRow, (Color)value);
                    m_ParentRow.Parent.InvalidateRow(m_ParentRow);
                    break;
                case ColumnType.ProgressBar:
                    {
                        if (value != null && (typeof(double) == value.GetType() || typeof(int) == value.GetType() || typeof(float) == value.GetType()))
                        {
                            double dblVal = Convert.ToDouble(value);
                            dblVal = Math.Max(dblVal, 0.0);
                            dblVal = Math.Min(dblVal, 100.0);
                            m_Value = dblVal;
                        }
                        else
                        {
                            m_Value = null;
                        }
                        m_ParentRow.Parent.InvalidateCell(this);
                    }
                    break;
                default:
                    m_Value = value;
                    if (m_ParentRow != null && m_ParentRow.ParentNode != null)
                        m_ParentRow.Parent.RecalcRowHeightByIndex(m_ParentRow, m_ColumnIndex);
                    m_ParentRow.Parent.InvalidateCell(this);
                    break;
            }
        }

        public int CalcCellHeight(Graphics GFX)
        {
            int HeightCalc = 0;

            // use value rect here
            if (m_ParentRow.Parent.Columns[m_ColumnIndex].IsVariableHeight())
            {
                HeightCalc = GetValueRect(GFX, GetDrawRect(0, 0)).Height;
            }
            else
                HeightCalc = m_ParentRow.Parent.Font.Height;

            return HeightCalc + 2 * m_ParentRow.Parent.CellPadding;
        }

        public void SetCellBackColor(Color BackColor)
        {
            if (m_ParentRow.Parent.Rows.Contains(m_ParentRow.ParentNode))
            {
                if (BackColor == Color.Empty)
                    m_ParentRow.Parent.CellColors.RemoveColor(ColorSelection.BackColor, this);
                else
                    m_ParentRow.Parent.CellColors.SetColor(ColorSelection.BackColor, this, BackColor);

                m_ParentRow.Parent.InvalidateCell(this);
            }
        }

        public void SetCellForeColor(Color ForeColor)
        {
            if (m_ParentRow.Parent.Rows.Contains(m_ParentRow.ParentNode))
            {
                if (ForeColor == Color.Empty)
                    m_ParentRow.Parent.CellColors.RemoveColor(ColorSelection.ForeColor, this);
                else
                    m_ParentRow.Parent.CellColors.SetColor(ColorSelection.ForeColor, this, ForeColor);

                m_ParentRow.Parent.InvalidateCell(this);
            }
        }

        public void SetCellSelBackColor(Color BackColor)
        {
            if (m_ParentRow.Parent.Rows.Contains(m_ParentRow.ParentNode))
            {
                if (BackColor == Color.Empty)
                    m_ParentRow.Parent.CellColors.RemoveColor(ColorSelection.SelBackColor, this);
                else
                    m_ParentRow.Parent.CellColors.SetColor(ColorSelection.SelBackColor, this, BackColor);

                m_ParentRow.Parent.InvalidateCell(this);
            }
        }

        public void SetCellSelForeColor(Color ForeColor)
        {
            if (m_ParentRow.Parent.Rows.Contains(m_ParentRow.ParentNode))
            {
                if (ForeColor == Color.Empty)
                    m_ParentRow.Parent.CellColors.RemoveColor(ColorSelection.SelForeColor, this);
                else
                    m_ParentRow.Parent.CellColors.SetColor(ColorSelection.SelForeColor, this, ForeColor);

                m_ParentRow.Parent.InvalidateCell(this);
            }
        }

        internal Color DetermineBackColor(bool bForToolTip = false)
        {
            Color BackColor;
            if ((m_ParentRow.Parent.Enabled && !m_ParentRow.Parent.ReadOnly) || bForToolTip)
            {
                if (m_ParentRow.Highlighted && !bForToolTip)
                {
                    if (m_ParentRow.Parent.CellColors.GetColor(ColorSelection.SelBackColor, this, out BackColor))
                        return BackColor;
                    if (m_ParentRow.Parent.ColumnColors.GetColor(ColorSelection.SelBackColor, m_ColumnIndex, out BackColor))
                        return BackColor;
                    else
                        if (m_ParentRow.Parent.RowColors.GetColor(ColorSelection.SelBackColor, m_ParentRow, out BackColor))
                        return BackColor;
                    else
                            if (m_ParentRow.Parent.ContainsFocus || m_ParentRow.Parent.Focused)
                        return SystemColors.Highlight;
                    else
                        return SystemColors.InactiveCaption;
                }
                else
                {
                    if (m_ParentRow.Parent.CellColors.GetColor(ColorSelection.BackColor, this, out BackColor))
                        return BackColor;
                    if (m_ParentRow.Parent.ColumnColors.GetColor(ColorSelection.BackColor, m_ColumnIndex, out BackColor))
                        return BackColor;
                    else
                        if (m_ParentRow.Parent.RowColors.GetColor(ColorSelection.BackColor, m_ParentRow, out BackColor))
                        return BackColor;
                    else
                        return m_ParentRow.Parent.BackColor;
                }
            }
            else
            {
                if (m_ParentRow.Parent.CellColors.GetColor(ColorSelection.BackColor, this, out BackColor))
                    return BackColor;
                if (m_ParentRow.Parent.ColumnColors.GetColor(ColorSelection.BackColor, m_ColumnIndex, out BackColor))
                    return BackColor;
                else
                    if (m_ParentRow.Parent.RowColors.GetColor(ColorSelection.BackColor, m_ParentRow, out BackColor))
                    return BackColor;
                else
                    return SystemColors.Control;
            }
        }

        internal Color DetermineForeColor(bool bForToolTip = false)
        {
            Color ForeColor;
            if ((m_ParentRow.Parent.Enabled && !m_ParentRow.Parent.ReadOnly) || bForToolTip)
            {
                if (m_ParentRow.Highlighted && !bForToolTip)
                {
                    if (m_ParentRow.Parent.CellColors.GetColor(ColorSelection.SelForeColor, this, out ForeColor))
                        return ForeColor;
                    if (m_ParentRow.Parent.ColumnColors.GetColor(ColorSelection.SelForeColor, m_ColumnIndex, out ForeColor))
                        return ForeColor;
                    else
                        if (m_ParentRow.Parent.RowColors.GetColor(ColorSelection.SelForeColor, m_ParentRow, out ForeColor))
                        return ForeColor;
                    else
                            if (m_ParentRow.Parent.ContainsFocus || m_ParentRow.Parent.Focused)
                        return SystemColors.HighlightText;
                    else
                        return SystemColors.InactiveCaptionText;
                }
                else
                {
                    if (m_ParentRow.Parent.CellColors.GetColor(ColorSelection.ForeColor, this, out ForeColor))
                        return ForeColor;
                    if (m_ParentRow.Parent.ColumnColors.GetColor(ColorSelection.ForeColor, m_ColumnIndex, out ForeColor))
                        return ForeColor;
                    else
                        if (m_ParentRow.Parent.RowColors.GetColor(ColorSelection.ForeColor, m_ParentRow, out ForeColor))
                        return ForeColor;
                    else
                        return m_ParentRow.Parent.Parent.ForeColor;
                }
            }
            else
            {
                if (m_ParentRow.Parent.CellColors.GetColor(ColorSelection.ForeColor, this, out ForeColor))
                    return ForeColor;
                if (m_ParentRow.Parent.ColumnColors.GetColor(ColorSelection.ForeColor, m_ColumnIndex, out ForeColor))
                    return ForeColor;
                else
                    if (m_ParentRow.Parent.RowColors.GetColor(ColorSelection.ForeColor, m_ParentRow, out ForeColor))
                    return ForeColor;
                else
                    return SystemColors.ControlText;
            }
        }

        internal Rectangle GetValueRect(Graphics GFX, Rectangle drawRect)
        {
            if (m_Value == null)
                return Rectangle.Empty;

            ColumnType colType = m_ParentRow.Parent.Columns[m_ColumnIndex].Type;

            switch (colType)
            {
                case ColumnType.TextNoWrap:
                case ColumnType.ComboBox:
                    {
                        string strText = this.Text;
                        if (!string.IsNullOrEmpty(strText))
                        {
                            StringFormat sf = new StringFormat(StringFormat.GenericTypographic);
                            sf.FormatFlags |= System.Drawing.StringFormatFlags.NoWrap;
                            SizeF txtSizeF = GFX.MeasureString(strText, m_ParentRow.Parent.Font);
                            drawRect.Width = (int)Math.Ceiling(txtSizeF.Width);
                            drawRect.Height = (int)Math.Ceiling(txtSizeF.Height);
                            sf.Dispose();
                            return drawRect;
                        }
                        break;
                    }
                case ColumnType.TextWrap:
                    {
                        string strText = this.Text;
                        if (!string.IsNullOrEmpty(strText))
                        {
                            SizeF txtSizeF = GFX.MeasureString(strText, m_ParentRow.Parent.Font, drawRect.Width);
                            drawRect.Width = (int)Math.Ceiling(txtSizeF.Width);
                            drawRect.Height = (int)Math.Ceiling(txtSizeF.Height);
                            return drawRect;
                        }
                    }
                    break;
                case ColumnType.CheckBox:
                    {
                        int boxHeight = m_ParentRow.Parent.Font.Height;
                        int x = drawRect.X + (drawRect.Width - boxHeight) / 2;
                        int y = drawRect.Y + (drawRect.Height - boxHeight) / 2;
                        return new Rectangle(x, y, boxHeight, boxHeight);
                    }
                    break;
                case ColumnType.ProgressBar:
                    {
                        int barHeight = m_ParentRow.Parent.Font.Height;
                        drawRect.Height = barHeight;
                        drawRect.Width = Math.Max(drawRect.Width, 60);
                        return drawRect;
                    }
                    break;
                // currently unsupported
                // no data to rectangle calculation is not necessary
                case ColumnType.RowBackColor:
                case ColumnType.RowForeColor:
                case ColumnType.RowSelBackColor:
                case ColumnType.RowSelForeColor:
                default:
                    return Rectangle.Empty;
            }

            return Rectangle.Empty;
        }

        internal void DrawCheckBox(Graphics GFX, Rectangle drawRect)
        {
            if (drawRect.Width <= 0)
                return;

            if (m_Value == null)
                return;

            Rectangle valRect = GetValueRect(GFX, drawRect);

            if (UxThemeManager.VisualStylesEnabled())
            {
                Rectangle clipRect = Rectangle.Intersect(Rectangle.Round(GFX.ClipBounds), drawRect);
                IntPtr hdc = GFX.GetHdc();

                m_ParentRow.Parent.Themes[m_ParentRow.Parent].DrawThemeBackground(UxThemeElements.BUTTON, hdc, (int)ButtonPart.Checkbox,
                    (bool)m_Value ? (int)CheckBoxState.CheckedNormal : (int)CheckBoxState.UncheckedNormal, ref valRect, ref clipRect);

                GFX.ReleaseHdc(hdc);
            }
            else
            {
                // TODO: Do this with the Graphics object, not with an image
                Bitmap ChkBox;
                System.Reflection.Assembly thisExe;
                thisExe = System.Reflection.Assembly.GetExecutingAssembly();

                if ((bool)m_Value)
                    ChkBox = new Bitmap(thisExe.GetManifestResourceStream("DataList2.Checked.bmp"));
                else
                    ChkBox = new Bitmap(thisExe.GetManifestResourceStream("DataList2.Unchecked.bmp"));

                GFX.DrawImage(ChkBox, new Point(drawRect.X + (Width - ChkBox.Width) / 2,
                     drawRect.Y + (Height - ChkBox.Width) / 2));
            }

#if DEBUG
            Brush drawRectColor = new SolidBrush(Color.Green);
            GFX.FillRectangle(drawRectColor, valRect);
            drawRectColor.Dispose();
#endif
        }

        internal void DrawProgressBar(Graphics GFX, Rectangle drawRect)
        {
            if (drawRect.Width <= 0)
                return;

            if (m_Value == null)
                return;

            Rectangle clipRect = Rectangle.Ceiling(GFX.ClipBounds);
            clipRect.Intersect(drawRect);

            if (clipRect.IsEmpty)
                return;

            RectangleF oldClip = GFX.ClipBounds;
            GFX.SetClip(clipRect);

            double dblPercent = (double)Convert.ToDouble(m_Value) / 100.0;
            Rectangle valRect = GetValueRect(GFX, drawRect);

            if (UxThemeManager.VisualStylesEnabled())
            {
                int oldWidth = valRect.Width;

                IntPtr hdc = GFX.GetHdc();
                UxThemeManager themeManager = m_ParentRow.Parent.Themes[m_ParentRow.Parent];
                themeManager.DrawThemeBackground(UxThemeElements.PROGRESS, hdc, 1, 1, ref valRect, ref clipRect);

                valRect.Width = (int)(dblPercent * valRect.Width);
                themeManager.DrawThemeBackground(UxThemeElements.PROGRESS, hdc, 3, 1, ref valRect, ref clipRect);
                GFX.ReleaseHdc(hdc);

#if DEBUG
                valRect.Width = oldWidth;
                Brush drawRectColor = new SolidBrush(Color.Green);
                GFX.FillRectangle(drawRectColor, valRect);
                drawRectColor.Dispose();
#endif
            }
            else
            {
                Brush bordercolor = new SolidBrush(Color.DarkGray);
                Brush backcolor = new SolidBrush(Color.LightGray);
                Brush b = new SolidBrush(Color.Green);
                LinearGradientBrush lb = new LinearGradientBrush(valRect, Color.FromArgb(225, Color.White), Color.FromArgb(75, Color.Transparent), LinearGradientMode.Vertical);

                GFX.FillRectangle(bordercolor, valRect);
                valRect.Inflate(-1, -1);
                GFX.FillRectangle(backcolor, valRect);
                int oldWidth = valRect.Width;

                valRect.Width = (int)(dblPercent * valRect.Width);
                GFX.FillRectangle(b, valRect);
                valRect.Width = oldWidth;
                GFX.FillRectangle(lb, valRect);

#if DEBUG
                Brush drawRectColor = new SolidBrush(Color.Green);
                valRect.Inflate(1, 1);
                GFX.FillRectangle(drawRectColor, valRect);
                drawRectColor.Dispose();
#endif

                backcolor.Dispose();
                bordercolor.Dispose();
                b.Dispose();
                lb.Dispose();
            }

            GFX.SetClip(oldClip);
        }

        internal void DrawText(Graphics GFX, Rectangle drawRect, bool bWrap, bool bForToolTip = false)
        {
            if (drawRect.Width <= 0)
                return;

            string strText = this.Text;

            if (strText == "")
                return;

            StringFormat sf = new StringFormat();
            Brush ForText = new SolidBrush(DetermineForeColor(bForToolTip));

            if (bWrap)
            {
                sf.Trimming |= System.Drawing.StringTrimming.Word;
            }
            else
            {
                sf.FormatFlags |= System.Drawing.StringFormatFlags.NoWrap;
                sf.Trimming |= System.Drawing.StringTrimming.EllipsisCharacter;
            }

            GFX.DrawString(strText, m_ParentRow.Parent.Font, ForText, drawRect, sf);
            ForText.Dispose();
            sf.Dispose();

#if DEBUG
            Rectangle valueRect = GetValueRect(GFX, drawRect);
            Brush drawRectColor = new SolidBrush(Color.Green);
            GFX.FillRectangle(drawRectColor, valueRect);
            drawRectColor.Dispose();
#endif
        }

        internal Rectangle GetDrawRect()
        {
            Point p = m_ParentRow.Parent.GetCellLocation(this);
            return GetDrawRect(p.X, p.Y);
        }

        private Rectangle GetDrawRect(int x, int y)
        {
            int cellPadding = m_ParentRow.Parent.CellPadding;
            int nWidth = m_ParentRow.Parent.Columns[m_ColumnIndex].Width;
            int nHeight = m_ParentRow.Height;

            Rectangle drawArea = new Rectangle(x, y, nWidth, nHeight);
            drawArea.Inflate(-cellPadding, -cellPadding);

            return drawArea;
        }

        private void DrawBaseCell(Graphics GFX, int x, int y)
        {
            int nWidth = m_ParentRow.Parent.Columns[m_ColumnIndex].Width;
            int nHeight = m_ParentRow.Height;

            Brush BackColor = new SolidBrush(DetermineBackColor());
            GFX.FillRectangle(BackColor, x, y, nWidth, nHeight);
            BackColor.Dispose();

            if (m_ParentRow.Parent.Parent.ShowGridLines)
            {
                Pen Borders = new Pen(UtilityFunctions.AdjustBrightness(DetermineBackColor(), 0.75), 1);
                int bottom = y + nHeight - (int)Borders.Width;
                int right = x + nWidth - (int)Borders.Width;
                GFX.DrawLine(Borders, x, bottom, right, bottom);
                GFX.DrawLine(Borders, right, y, right, bottom);
                Borders.Dispose();
            }
        }

        internal void DrawCell(Graphics GFX, int x, int y)
        {
            if (!m_ParentRow.Parent.Columns[m_ColumnIndex].Visible || m_ParentRow.Parent.Columns[m_ColumnIndex].Width <= 0)
                return;

            DrawBaseCell(GFX, x, y);
            Rectangle drawRect = GetDrawRect(x, y);

            if (!GFX.ClipBounds.IntersectsWith(drawRect))
                return;

#if DEBUG
            Brush drawRectColor = new SolidBrush(Color.Red);
            GFX.FillRectangle(drawRectColor, drawRect);
            drawRectColor.Dispose();
#endif

            switch (m_ParentRow.Parent.Columns[m_ColumnIndex].Type)
            {
                case ColumnType.TextWrap:
                    DrawText(GFX, drawRect, true);
                    break;
                case ColumnType.CheckBox:
                    DrawCheckBox(GFX, drawRect);
                    break;
                case ColumnType.ProgressBar:
                    DrawProgressBar(GFX, drawRect);
                    break;
                case ColumnType.TextNoWrap:
                default:
                    DrawText(GFX, drawRect, false);
                    break;
            }
        }
    }
}
