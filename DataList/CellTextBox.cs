using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Win32Lib;

namespace DataList
{
  internal class CellTextBox : TextBox
  {
    private ListWnd m_Parent;
    private Row m_CurrentRow;
    private int m_CurrentCol;
    private Timer m_PendEdit;
    //private bool m_bNumericOnly;
    private bool m_bCtrlDown;

    private bool m_bCancelOnDblClick;
    public bool CancelOnDblClick
    {
      get { return m_bCancelOnDblClick; }
    }

    internal bool HasPendingEdit
    {
      get { return m_PendEdit.Enabled; }
    }

    internal int ColumnIndex
    {
      get { return m_CurrentCol; }
    }

    internal Row Row
    {
      get { return m_CurrentRow; }
    }

    internal CellTextBox(ListWnd parent)
    {
      m_Parent = parent;
      base.BorderStyle = BorderStyle.FixedSingle;
      base.Multiline = true;
      base.WordWrap = true;

      // this is how to set margins, but you can only do it for the first time... may have to set up the datalist to destroy and recreate the handle each time
      //Win32.SendMessage(this.Handle, (uint)WindowsMessages.EM_SETMARGINS, 0x1 | 0x2, new IntPtr((m_Parent.CellPadding << 16) + m_Parent.CellPadding));

      m_CurrentRow = null;
      m_CurrentCol = -1;

      m_PendEdit = new Timer();
      m_PendEdit.Interval = SystemInformation.DoubleClickTime;
      m_PendEdit.Tick += new EventHandler(OnPendTimerTick);

      m_bCancelOnDblClick = false;
      //m_bNumericOnly = false;
      m_bCtrlDown = false;
    }

    void OnPendTimerTick(object sender, EventArgs e)
    {
      m_PendEdit.Stop();
      PostEditBoxStartPendingEdit();
    }

    internal void SetCurrentCell(int nColIndex, Row rCurrRow)
    {
      m_CurrentCol = nColIndex;
      m_CurrentRow = rCurrRow;

      if (nColIndex != -1 && rCurrRow != null)
      {
        //m_bNumericOnly = m_Parent.Columns[nColIndex].NumericSorting;
        bool bWordMulti = (m_Parent.Columns[nColIndex].Type == ColumnType.TextWrap);

        this.WordWrap = bWordMulti;
        this.Multiline = bWordMulti;
      }
      else
      {
        this.Multiline = false;
        this.WordWrap = false;
        //m_bNumericOnly = false;
      }

      this.MaximumSize = m_Parent.RowBounds.Size;
    }

    internal void ResetDblClickFlag()
    {
      m_bCancelOnDblClick = false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
      m_bCtrlDown = (e.Modifiers == Keys.Control);
      base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
      m_bCtrlDown = (e.Modifiers == Keys.Control);
      base.OnKeyUp(e);
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
      //if (m_bNumericOnly && !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
      //{
      //  e.Handled = true;
      //}
      //else
      {
        if (e.KeyChar == (char)Keys.Escape)
        {
          PostEditBoxFinishEdit(false);
          m_Parent.Focus();
          e.Handled = true;
        }
        else
        {
          if (this.Multiline && e.KeyChar == (char)13)
          {
            //Rectangle ParentRowBounds = m_Parent.RowBounds;
            //if((this.Location.Y + this.Height + this.Font.Height) < (ParentRowBounds.Y + ParentRowBounds.Height))
              //this.Height += this.Font.Height;
          }
          else
          {
            if (m_bCtrlDown && e.KeyChar == (char)10)
            {
              PostEditBoxFinishEdit(true);
              m_Parent.Focus();
              e.Handled = true;
            }
          }
        }
      }

      base.OnKeyPress(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
      // TODO: Need to not do this, editing with the editbox should be completed using mouse capture/loss of activation etc, not lostfocus
			if (Visible && m_CurrentCol != -1 && m_CurrentRow != null)
      {
        PostEditBoxFinishEdit(true);
      }

      base.OnLostFocus(e);
    }

    private void PostEditBoxFinishEdit(bool bCommit)
    {
      IntPtr pCommit;
      pCommit = GCHandle.ToIntPtr(GCHandle.Alloc(bCommit, GCHandleType.Normal));
      Win32.SendMessage(m_Parent.Handle, (uint)DatalistMessage.WM_FINISHCELLEDIT, pCommit, IntPtr.Zero);
    }

    internal void ForceFinishEdit(bool bCommit)
    {
      IntPtr pCommit;
      pCommit = GCHandle.ToIntPtr(GCHandle.Alloc(bCommit, GCHandleType.Normal));
      Win32.SendMessage(m_Parent.Handle, (uint)DatalistMessage.WM_FINISHCELLEDIT, pCommit, IntPtr.Zero);
    }

    private void PostEditBoxStartPendingEdit()
    {
      Win32.PostMessage(m_Parent.Handle, (uint)DatalistMessage.WM_STARTCELLEDIT, IntPtr.Zero, IntPtr.Zero);
    }

    internal void StartPendingEdit(int nColIndex, Row rCurrRow)
    {
      if (!m_bCancelOnDblClick)
      {
        SetCurrentCell(nColIndex, rCurrRow);
        m_PendEdit.Start();
      }

      m_bCancelOnDblClick = false;
    }

    internal void CancelPendingEdit(bool bOnDblClick)
    {
      m_bCancelOnDblClick = bOnDblClick || m_bCancelOnDblClick;
      m_PendEdit.Stop();
      ClearEditBoxCell(true);
    }

    internal void StartEditingCell(int nColIndex, Row rCurrRow)
    {
      SetCurrentCell(nColIndex, rCurrRow);
      StartEditingCell();
    }

    internal void StartEditingCell()
    {
      if (m_CurrentCol != -1 && m_CurrentRow != null)
      {
        if (m_CurrentRow.Cells[m_CurrentCol] == null)
          return;

        if (m_CurrentRow.Cells[m_CurrentCol].Value != null)
          this.Text = m_CurrentRow.Cells[m_CurrentCol].Value.ToString();
        else
          this.Text = "";

        this.Size = new Size(m_CurrentRow.Cells[m_CurrentCol].Width, m_CurrentRow.Cells[m_CurrentCol].Height);

        this.Location = m_Parent.GetCellLocation(m_CurrentRow.Cells[m_CurrentCol]);

        Show();
        Focus();
        SelectAll();
      }
    }

    internal void ClearEditBoxCell(bool bForgetCell = true)
    {
      if (bForgetCell)
        SetCurrentCell(-1, null);

      Hide();
      m_Parent.Invalidate();
    }
  }
}
