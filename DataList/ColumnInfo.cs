using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataList
{
  internal struct ColumnSortInfo
  {
    public int nIndex;
    public int nSortPriority;
    public bool bAscending;
    public DatalistDataTypes DataType;

    public ColumnSortInfo(int Index, int SortPriority, bool Ascending, DatalistDataTypes dataType)
    {
      nIndex = Index;
      nSortPriority = SortPriority;
      bAscending = Ascending;
      DataType = dataType;
    }

    public ColumnSortInfo(ColumnSortInfo csi)
    {
      nIndex = csi.nIndex;
      nSortPriority = csi.nSortPriority;
      bAscending = csi.bAscending;
      DataType = csi.DataType;
    }
  }
}
