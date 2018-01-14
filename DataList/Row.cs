using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace DataList
{
  public class Row
  {
    public bool Highlighted
    {
      get { return (m_Parent.CurrHighlight == this) || (m_Parent.CurrHighlight == null && m_Parent.CurrSel == this); }
    }

    public bool Selected
    {
      get { return (m_Parent.CurrSel == this); }
    }

    private ListWnd m_Parent;
    internal ListWnd Parent
    {
      get { return m_Parent; }
      set { m_Parent = value; }
    }

    private CellCollection m_Cells;
    public CellCollection Cells
    {
      get { return m_Cells; }
    }

    private RowNode m_ParentNode;
    internal RowNode ParentNode
    {
      get { return m_ParentNode; }
      set { m_ParentNode = value; }
    }

    public int Width
    {
      get { return m_Parent.Columns.RowWidth; }
    }

    private int m_Height = 0;
    public int Height
    {
      get { return m_Height; }
      set { m_Height = value; }
    }

    internal Row(ListWnd parent)
    {
      m_Cells = new CellCollection(this);
      m_Parent = parent;

      m_Height = 2 * m_Parent.CellPadding + m_Parent.Font.Height;

      for (int i = 0; i < m_Parent.Columns.Count; i++)
      {
        Cell JustAdded = m_Cells.Add(this, i);
      }
    }

    ~Row()
    {
      m_Parent = null;
      m_Cells.Clear();
    }

    public Cell this[int CellIndex]
    {
      get
      {
        return m_Cells[CellIndex];
      }
    }

    public int RecalcHeight(Graphics GFX)
    {
      int nMaxCellHeight = m_Parent.Font.Height + 2 * m_Parent.CellPadding;

      foreach (Cell c in m_Cells)
      {
        if (m_Parent.Columns[c.ColumnIndex].IsVariableHeight() && Width > 2 * m_Parent.CellPadding)
        {
          int height = c.CalcCellHeight(GFX);
          if (height > nMaxCellHeight)
            nMaxCellHeight = height;
        }
      }

      m_Height = nMaxCellHeight;
      return nMaxCellHeight;
    }

    public Row GetPreviousRow()
    {
      if (m_ParentNode != null)
        if (m_ParentNode.PreviousNode != null)
          return m_ParentNode.PreviousNode.InternalRow;

      return null;
    }

    public Row GetNextRow()
    {
      if (m_ParentNode != null)
        if (m_ParentNode.NextNode != null)
          return m_ParentNode.NextNode.InternalRow;

      return null;
    }

    public int CalculateRowHeightPrior()
    {
      int TotalHeight = 0;
      Row CurrRow = GetPreviousRow();

      while (CurrRow != null)
      {
        TotalHeight += CurrRow.Height;
        CurrRow = CurrRow.GetPreviousRow();
      }

      return TotalHeight;
    }

    public void DrawRow(PaintEventArgs e, int x, int y)
    {
      int OrigX = x;
      int nHeight = m_Height;

      for (int i = 0; i < m_Cells.Count; i++)
      {
        if (e.Graphics.Clip.IsVisible(x, y, m_Cells[i].Width, nHeight))
          m_Cells[i].DrawCell(e.Graphics, x, y);

        x += m_Cells[i].Width;
      }

#if DEBUG
			Brush b = new SolidBrush(Color.Black);
			string toDraw = string.Format("Row Height: {0}", nHeight);
			e.Graphics.DrawString(toDraw, m_Parent.Font, b, OrigX + 4, y + 4);
			b.Dispose();
#endif

			if (this.Highlighted && m_Parent.ContainsFocus && !m_Parent.ReadOnly)
      {
        Pen p = new Pen(Color.DarkGray);
        p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
        Rectangle Outline = new Rectangle(OrigX, y, m_Parent.Columns.RowWidth, nHeight);
        Outline.Inflate(-2, -2);
        Outline.X = OrigX + 1;
        Outline.Y = y + 1;
        e.Graphics.DrawRectangle(p, Outline);
        p.Dispose();
      }
    }

    public void SetRowBackColor(Color BackColor)
    {
      if (m_Parent.Rows.Contains(m_ParentNode))
      {
        m_Parent.RowColors.SetColor(ColorSelection.BackColor, this, BackColor);
        m_Parent.InvalidateRow(this);
      }
    }

    public void SetRowForeColor(Color ForeColor)
    {
      if (m_Parent.Rows.Contains(m_ParentNode))
      {
        m_Parent.RowColors.SetColor(ColorSelection.ForeColor, this, ForeColor);
        m_Parent.InvalidateRow(this);
      }
    }

    public void SetRowSelBackColor(Color BackColor)
    {
      if (m_Parent.Rows.Contains(m_ParentNode))
      {
        m_Parent.RowColors.SetColor(ColorSelection.SelBackColor, this, BackColor);
        m_Parent.InvalidateRow(this);
      }
    }

    public void SetRowSelForeColor(Color ForeColor)
    {
      if (m_Parent.Rows.Contains(m_ParentNode))
      {
        m_Parent.RowColors.SetColor(ColorSelection.SelForeColor, this, ForeColor);
        m_Parent.InvalidateRow(this);
      }
    }

    public void RemoveRowColors()
    {
      if (m_Parent.Rows.Contains(m_ParentNode))
      {
        m_Parent.RowColors.RemoveAllColors(this);
        m_Parent.CellColors.RemoveAllRowColor(this);
        m_Parent.InvalidateRow(this);
      }
    }

    public override string ToString()
    {
      return ToString(null);
    }

    public string ToString(string strFormat)
    {
      if (string.IsNullOrEmpty(strFormat.Trim()))
      {
        string temp = "";

        foreach (Cell c in this.Cells)
        {
          temp += (c.Text + ", ");
        }

        return temp.TrimEnd(',');
      }
      else
      {
        return string.Format(strFormat, m_Cells.ToArray());
      }
    }
  }
}