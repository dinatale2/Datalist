using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;

namespace DataList
{
  internal class ColumnComboBox : ComboBox
  {
    private class ComboValue
    {
      public long nValueID;
      public string strValueName;

      public ComboValue(long nVal, string strName)
      {
        nValueID = nVal;
        strValueName = strName;
      }

      public override string ToString()
      {
        return strValueName ?? "";
      }
    }

    private bool m_bCancelOnDblClick;
    public bool CancelOnDblClick
    {
      get { return m_bCancelOnDblClick; }
    }

    private ListWnd m_ParentList;

    private Timer m_PendEdit;
    internal bool HasPendingEdit
    {
      get { return m_PendEdit.Enabled; }
    }

    private Row m_CurrentRow;
    private int m_CurrentCol;
    internal bool HasValidCell
    {
      get { return (m_CurrentRow != null && m_CurrentCol > 0); }
    }

    private Dictionary<long, ComboValue> m_IDtoComboValue;

    internal ColumnComboBox(ListWnd parent, string strComboSource)
    {
      m_ParentList = parent;
      m_ParentList.Controls.Add(this);
      m_IDtoComboValue = new Dictionary<long, ComboValue>();
      SetComboSource(strComboSource);
      this.DropDownStyle = ComboBoxStyle.DropDownList;
      this.Visible = false;
      this.DroppedDown = false;
      m_bCancelOnDblClick = false;

      m_PendEdit = new Timer();
      m_PendEdit.Interval = SystemInformation.DoubleClickTime;
      m_PendEdit.Tick += new EventHandler(OnPendTimerTick);
    }

    void OnPendTimerTick(object sender, EventArgs e)
    {
      m_PendEdit.Stop();
      m_ParentList.StartComboEdit(m_CurrentCol, m_CurrentRow);
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

    internal void SetCurrentCell(int nColIndex, Row rCurrRow)
    {
      m_CurrentCol = nColIndex;
      m_CurrentRow = rCurrRow;

      this.MaximumSize = m_ParentList.RowBounds.Size;
    }

    internal void SetComboSource(string strComboSource)
    {
      this.Items.Clear();
      m_IDtoComboValue.Clear();

      if (string.IsNullOrEmpty(strComboSource))
        return;

      string[] aryCombo = strComboSource.Split(';');

      if (aryCombo.Length % 2 != 0)
        throw new ArgumentException("SetComboSource: String split resulted in odd number of splits.");

      for (int i = 0; i < aryCombo.Length; i += 2)
      {
        string strID = aryCombo[i];
        string strName = aryCombo[i + 1];
        int nID;

        if (!int.TryParse(strID, out nID))
          throw new ArgumentException("SetComboSource: Invalid ID value found!");

        ComboValue temp = new ComboValue(nID, strName);

        if (m_IDtoComboValue.ContainsKey(nID))
          throw new InvalidOperationException("SetComboSource: Same ID encountered!");

        this.Items.Add(temp);
        m_IDtoComboValue.Add(nID, temp);
      }
    }

    internal string GetValueDisplayName(long nValueID)
    {
      ComboValue value;
      if (m_IDtoComboValue.TryGetValue(nValueID, out value))
      {
        if(value != null)
          return value.strValueName;
      }

      return "";
    }

    protected override void  OnKeyDown(KeyEventArgs e)
    {
      if ((e.KeyCode & Keys.Escape) == Keys.Escape)
        this.DroppedDown = false;

      base.OnKeyDown(e);
    }

    protected override void Dispose(bool bDisposing)
    {
      if (bDisposing)
      {
        this.Items.Clear();
        m_ParentList.Controls.Remove(this);
        m_ParentList = null;
      }

      base.Dispose(bDisposing);
    }

    internal void ResetDblClickFlag()
    {
      m_bCancelOnDblClick = false;
    }

    internal void CancelPendingEdit(bool bOnDblClick)
    {
      m_bCancelOnDblClick = bOnDblClick || m_bCancelOnDblClick;
      m_PendEdit.Stop();
      SetCurrentCell(-1, null);
    }

    protected override void OnDropDown(EventArgs e)
    {
      if (m_CurrentCol == -1 || m_CurrentRow == null)
        this.DroppedDown = false;

      if (m_CurrentRow[m_CurrentCol].Value != null)
      {
        long nID = (long)m_CurrentRow[m_CurrentCol].Value;
        ComboValue lookUp;
        if (m_IDtoComboValue.TryGetValue(nID, out lookUp))
        {
          this.SelectedItem = lookUp;
        }
      }
      else
      {
        this.SelectedItem = null;
      }

      base.OnDropDown(e);
    }

    internal void FinishEdit()
    {
      if (!HasValidCell)
        return;

      long? nID = null;
      if (this.SelectedItem != null)
      {
        ComboValue currSel = (ComboValue)this.SelectedItem;
        nID = currSel.nValueID;
      }

      m_ParentList.FinishComboEdit(m_CurrentRow, m_CurrentCol, nID);

      m_CurrentCol = -1;
      m_CurrentRow = null;
    }

    protected override void OnDropDownClosed(EventArgs e)
    {
      FinishEdit();
      base.OnDropDownClosed(e);
    }
  }
}
