using System;
using System.Collections.Generic;
using System.Drawing;

namespace DataList
{
  internal class CellColorManager : ColorManager<Cell>
  {
    private ListWnd m_Parent;

    internal CellColorManager(ListWnd Parent)
    {
      m_Parent = Parent;
    }

    internal void RemoveAllRowColor(Row r)
    {
      foreach (Cell c in r.Cells)
        base.RemoveAllColors(c);
    }
  }
}
