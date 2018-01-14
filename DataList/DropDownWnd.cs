using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace DataList
{
  internal sealed class DropDownWnd : ToolStripDropDown
  {
    DataList m_Parent;
    ListWnd m_List;
    ToolStripControlHost m_host;

    public new Size Size
    {
      get { return m_host.Size; }
      set { m_host.Size = value; }
    }

    internal DropDownWnd(DataList parent, ListWnd control)
    {
      m_Parent = parent;
      m_List = control;

      m_host = new ToolStripControlHost(control);
			m_host.Font = m_Parent.Font;

      Padding = new Padding(0);
      Margin = new Padding(0);
      AutoSize = true;

      this.DropShadowEnabled = false;
      this.Items.Add(m_host);
    }

		protected override void OnVisibleChanged(EventArgs e)
		{
			m_Parent.Invalidate(true);

			m_host.Size = new Size(m_Parent.Width, 300);
			m_host.Padding = new Padding(0);
			m_host.Margin = new Padding(1);
			m_host.AutoSize = false;

			base.OnVisibleChanged(e);
		}
  }
}
