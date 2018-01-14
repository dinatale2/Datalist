using System;
using System.Collections.Generic;
using System.Drawing;

namespace DataList
{
  internal class ColumnColorManager : ColorManager<int>
  {
    private ListWnd m_Parent;

    internal ColumnColorManager(ListWnd Parent) : base()
    {
      m_Parent = Parent;
    }
  }
}
