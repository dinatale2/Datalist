using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace DataList
{
  internal enum BorderType
  {
    CornerBox,
  };

  internal class BorderObject : Control
  {
    BorderType m_Type;
    public BorderType Type
    {
      get { return m_Type; }
    }

    public BorderObject(BorderType type)
    {
      m_Type = type;
      BackColor = SystemColors.Control;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      if (!Enabled)
        return;

      Graphics GFX = e.Graphics;

      switch (m_Type)
      {
        case BorderType.CornerBox:
          ControlPaint.DrawBorder3D(GFX, this.ClientRectangle, Border3DStyle.Sunken);
          break;
      }
    }
  }
}
