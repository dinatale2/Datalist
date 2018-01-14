using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace DataList
{
  public class RowCollection
  {
    private ListWnd m_Parent;
    internal ListWnd Parent
    {
      get { return m_Parent; }
      set { m_Parent = value; }
    }

    private int m_nCount;
    public int Count
    {
      get { return m_nCount; }
    }

    private int m_nTotalRowHeight = 0;
    internal int TotalRowHeight
    {
      get { return m_nTotalRowHeight; }
    }

    internal RowCollection(ListWnd parent)
    {
      m_Parent = parent;
      m_nCount = 0;
      m_nTotalRowHeight = 0;
    }

    private RowNode rnHead;
    private RowNode rnTail;

    internal RowNode this[int RowIndex]
    {
      get
      {
        if (RowIndex < 0 || RowIndex > (m_nCount - 1))
          return null;

        int i = 0;
        RowNode rNode = rnHead;
        while (i < RowIndex && rNode != null)
        {
          rNode = rNode.GetNextNode();
          i++;
        }

        return rNode;
      }
    }

    public bool RemoveAt(int index)
    {
      bool ToReturn = false;
      if (index >= 0 && index < (m_nCount - 1))
      {
        Remove(this[index]);
        ToReturn = true;
      }

      return ToReturn;
    }

    internal void Remove(RowNode row)
    {
      if (row != null && row.Parent == m_Parent)
      {
        if (rnHead != null && rnHead == row)
          rnHead = rnHead.NextNode;

        if (rnTail != null && rnTail == row)
          rnTail = rnTail.PreviousNode;

        if (row.PreviousNode != null)
          row.PreviousNode.NextNode = row.NextNode;

        if (row.NextNode != null)
          row.NextNode.PreviousNode = row.PreviousNode;

        row.PreviousNode = null;
        row.NextNode = null;

        m_nCount--;
        m_nTotalRowHeight -= row.InternalRow.Height;
      }
    }

    public void Clear()
    {
      m_nTotalRowHeight = 0;
      m_nCount = 0;
      rnHead = null;
      rnTail = null;
    }

    public void AddBefore(Row row, Row rowBefore)
    {
      if (row != null && row.ParentNode == null)
      {
        if (rowBefore == null)
        {
          AddAtEnd(row);
        }
        else
        {
          if (row.Parent == m_Parent && rowBefore.Parent == m_Parent)
          {
            Graphics GFX = m_Parent.CreateGraphics();
            row.RecalcHeight(GFX);

            RowNode ToAdd = new RowNode(m_Parent, row);
            ToAdd.PreviousNode = rowBefore.ParentNode.PreviousNode;
            ToAdd.NextNode = rowBefore.ParentNode;
            if (ToAdd.PreviousNode != null)
            {
              ToAdd.PreviousNode.NextNode = ToAdd;
            }
            ToAdd.NextNode.PreviousNode = ToAdd;

            if (rnHead == null || rowBefore.ParentNode == rnHead)
              rnHead = ToAdd;

            if (rnTail == null)
              rnTail = ToAdd;

            GFX.Dispose();
            m_nTotalRowHeight += row.Height;
            m_nCount++;
          }
        }
      }
    }

    public void AddAtEnd(Row row)
    {
      if (row.Parent == m_Parent)
      {
        if (row.ParentNode == null)
        {
          Graphics GFX = m_Parent.CreateGraphics();
          row.RecalcHeight(GFX);

          RowNode ToAdd = new RowNode(m_Parent, row);
          ToAdd.PreviousNode = rnTail;

          if (ToAdd.PreviousNode != null)
          {
            ToAdd.NextNode = ToAdd.PreviousNode.NextNode;
            ToAdd.PreviousNode.NextNode = ToAdd;
          }

          if (ToAdd.NextNode != null)
            ToAdd.NextNode.PreviousNode = ToAdd;

          GFX.Dispose();

          if (rnHead == null)
            rnHead = ToAdd;

          rnTail = ToAdd;
          m_nTotalRowHeight += row.Height;
          m_nCount++;
        }
      }
    }

    internal void AdjustHeightBy(int nDelta)
    {
      m_nTotalRowHeight += nDelta;
    }

    internal bool Contains(RowNode rowNode)
    {
      return (rowNode != null && rowNode.Parent == m_Parent);
    }

    internal Row GetFirstRow()
    {
      if (rnHead != null)
        return rnHead.InternalRow;

      return null;
    }

    internal Row GetLastRow()
    {
      if (rnTail != null)
        return rnTail.InternalRow;

      return null;
    }

    public int GetRowsHeight(Graphics GFX)
    {
      int height = 0;
      RowNode rNode = rnHead;

      while (rNode != null)
      {
        rNode.InternalRow.RecalcHeight(GFX);
        height += rNode.InternalRow.Height;
        rNode = rNode.NextNode;
      }

      m_nTotalRowHeight = height;
      return m_nTotalRowHeight;
    }

    public void SortRows()
    {
      if (rnHead != rnTail)
      {
        Cursor.Current = Cursors.WaitCursor;

        RowSortPredicate pred = new RowSortPredicate(m_Parent.Columns);

        if (!pred.HasValidSortPriorities())
          return;

        Row pRow = rnHead.InternalRow;
        RowVestibule rowsVest = new RowVestibule(this.Count);

        while (pRow != null)
        {
          rowsVest.AddRow(pRow);
          pRow = pRow.GetNextRow();
        }

        if (!rowsVest.HasRows())
          return;

        rowsVest.SortRows(pred);
        RowNode pRowNode = rnHead;

        foreach (Row r in rowsVest.Rows)
        {
          if (pRowNode.InternalRow != r)
          {
            pRowNode.InternalRow = r;
            pRowNode.InternalRow.ParentNode = pRowNode;
          }

          pRowNode = pRowNode.NextNode;
        }

        rowsVest.ClearRows();
      }
    }
  }
}
