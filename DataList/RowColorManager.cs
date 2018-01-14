using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace DataList
{
  internal class RowColorManager : ColorManager<Row>
  {
    private ListWnd m_Parent;

    internal RowColorManager(ListWnd Parent) : base()
    {
      m_Parent = Parent;
    }
  }
}
