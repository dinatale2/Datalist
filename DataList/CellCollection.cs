using System.Collections;


namespace DataList
{
    // TODO: next collection to be re-written
    public class CellCollection : CollectionBase
    {
        /// <summary>
        /// Row of which the cell belongs.
        /// </summary>
        private Row m_ParentRow;
        public Row ParentRow
        {
            get { return m_ParentRow; }
            set { m_ParentRow = value; }
        }

        /// <summary>
        /// Base constructor which creates an empty object.
        /// </summary>
        public CellCollection()
          : base()
        {
        }

        /// <summary>
        /// Initialize the object with the List and Row the Cell belongs to.
        /// </summary>
        /// <param name="parentList"></param>
        /// <param name="parentRow"></param>
        public CellCollection(Row parentRow)
          : base()
        {
            m_ParentRow = parentRow;
        }

        /// <summary>
        /// Returns the Cell at the specified index.
        /// </summary>
        /// <param name="CellIndex"></param>
        /// <returns></returns>
        public Cell this[int CellIndex]
        {
            get
            {
                if (CellIndex < List.Count && CellIndex >= 0)
                    return List[CellIndex] as Cell;
                else
                    return null;
            }
        }

        public new void RemoveAt(int index)
        {
            lock (List.SyncRoot)
            {
                if (index >= 0 && index < List.Count)
                {
                    List.RemoveAt(index);
                }
            }
        }

        public void Remove(Cell cell)
        {
            lock (List.SyncRoot)
            {
                List.Remove(cell);
            }
        }

        public new void Clear()
        {
            lock (List.SyncRoot)
            {
                List.Clear();
            }
        }

        public int Add(Cell cell)
        {
            int ToReturn = -1;

            lock (List.SyncRoot)
            {
                ToReturn = List.Add(cell);
            }

            return ToReturn;
        }

        public Cell Add(Row parentRow, int ColIndex)
        {
            Cell ToAdd = new Cell(parentRow, ColIndex);
            Add(ToAdd);
            return ToAdd;
        }

        public int Add(object Value, Row parentRow, int ColIndex)
        {
            Cell ToAdd = new Cell(parentRow, ColIndex);
            ToAdd.Value = Value;

            return Add(ToAdd);
        }

        public string[] ToArray()
        {
            string[] cells = new string[List.Count];
            int nCount = 0;

            foreach (Cell c in List)
            {
                cells[nCount] = c.Text;
                nCount++;
            }

            return cells;
        }
    }
}
