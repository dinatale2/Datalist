using System;
using System.Collections.Generic;
using System.Text;

namespace DataList
{
  internal class RowVestibule
  {
    private int m_nMaxRowCount;
    public int MaxRowCount
    {
      get { return m_nMaxRowCount; }
    }

    private List<Row> m_Rows;
    public List<Row> Rows
    {
      get { return m_Rows; }
    }

    public RowVestibule(int nMaxRows = 200)
    {
      m_nMaxRowCount = nMaxRows;
      m_Rows = new List<Row>(m_nMaxRowCount);
    }

    public bool HasRows()
    {
      return m_Rows.Count > 0;
    }

    public bool AtCapacity()
    {
      return m_Rows.Count == m_nMaxRowCount;
    }

    public void AddRow(Row pRow)
    {
      m_Rows.Add(pRow);
    }

    public void ClearRows()
    {
      m_Rows.Clear();
    }

    public void SortRows(RowSortPredicate predicate)
    {
      if (predicate.HasValidSortPriorities())
      {
        m_Rows.Sort(delegate(Row pRow1, Row pRow2) { return predicate.CompareRows(pRow1, pRow2); });
      }
    }
  }
}
