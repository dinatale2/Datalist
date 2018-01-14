using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataList
{
  internal class RowNode
  {
    private Row m_Row;
    public Row InternalRow
    {
      get { return m_Row; }
      set { m_Row = value; }
    }

    private ListWnd m_Parent;
    public ListWnd Parent
    {
      get { return m_Parent; }
      set { m_Parent = value; }
    }

    private RowNode m_PreviousNode;
    public RowNode PreviousNode
    {
      get { return m_PreviousNode; }
      set { m_PreviousNode = value; }
    }

    private RowNode m_NextNode;
    public RowNode NextNode
    {
      get { return m_NextNode; }
      set { m_NextNode = value; }
    }

    public RowNode(ListWnd parent, Row r)
    {
      m_Parent = parent;
      m_Row = r;
      m_Row.ParentNode = this;

      m_PreviousNode = null;
      m_NextNode = null;
    }

    public RowNode(ListWnd parent)
    {
      m_Parent = parent;
      m_Row = m_Parent.GetNewRow();

      m_PreviousNode = null;
      m_NextNode = null;
    }

    ~RowNode()
    {
      m_Parent = null;
      m_PreviousNode = null;
      m_NextNode = null;
      m_Row = null;
    }

    public RowNode GetPreviousNode()
    {
      return m_PreviousNode;
    }

    public RowNode GetNextNode()
    {
      return m_NextNode;
    }
  }
}
