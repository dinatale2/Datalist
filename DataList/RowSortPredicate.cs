using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace DataList
{
  internal class RowSortPredicate
  {
    private Dictionary<int, ColumnSortInfo> m_mapSortPriority;

    internal RowSortPredicate(ColumnCollection columns)
    {
      m_mapSortPriority = new Dictionary<int, ColumnSortInfo>();
      GetSortPriorities(columns);
    }

    internal static bool AllowedToSort(DatalistDataTypes dataType)
    {
      return (dataType != DatalistDataTypes.Color && dataType != DatalistDataTypes.Object);
    }

    public void GetSortPriorities(ColumnCollection columns)
    {
      if (columns == null)
        return;

      m_mapSortPriority.Clear();

      foreach (Column col in columns)
      {
        AddSortPriority(col.Index, col.ValueType, col.SortPriority, col.SortAscending);
      }
    }

    public void AddSortPriority(int nIndex, DatalistDataTypes dataType, int nSortPriority, bool bAscending)
    {
      if (nIndex >= 0 && nSortPriority >= 0 && AllowedToSort(dataType))
      {
        if (m_mapSortPriority.ContainsKey(nSortPriority))
          throw new InvalidOperationException("Column already in sort order mapping!");

        m_mapSortPriority.Add(nSortPriority, new ColumnSortInfo(nIndex, nSortPriority, bAscending, dataType));
      }
    }

    public bool HasValidSortPriorities()
    {
      return m_mapSortPriority.Count > 0;
    }

    public int CompareRows(Row pRow1, Row pRow2)
    {
      return CompareCells(0, pRow1, pRow2);
    }

    private int CompareCells(int nSortPriority, Row pRow1, Row pRow2)
    {
      ColumnSortInfo CurrColInfo;

      if (m_mapSortPriority.TryGetValue(nSortPriority, out CurrColInfo))
      {
        // TODO: Maybe if the datatype is a string, then we should pass in the text param instead?
        int nCompValue = CompareValues(CurrColInfo.bAscending, CurrColInfo.DataType, pRow1[CurrColInfo.nIndex].Value, pRow2[CurrColInfo.nIndex].Value);

        if (nCompValue == 0)
          return CompareCells(nSortPriority + 1, pRow1, pRow2);

        return nCompValue;
      }

      return 0;
    }

    private int CompareValues(bool bAscending, DatalistDataTypes dataType, object oValue1, object oValue2)
    {
      int nCompValue = 0;
      if (oValue1 == null && oValue2 == null)
      {
        nCompValue = 0;
      }
      else
      {
        if (oValue1 == null)
        {
          nCompValue = -1;
        }
        else
        {
          if (oValue2 == null)
          {
            nCompValue = 1;
          }
          else
          {
            switch (dataType)
            {
              case DatalistDataTypes.Boolean:
                nCompValue = CompareObjects<bool>(oValue1, oValue2);
                break;
              case DatalistDataTypes.String:
                nCompValue = CompareObjects<string>(oValue1, oValue2);
                break;
              case DatalistDataTypes.Short:
                nCompValue = CompareObjects<short>(oValue1, oValue2);
                break;
              case DatalistDataTypes.Long:
                nCompValue = CompareObjects<long>(oValue1, oValue2);
                break;
              case DatalistDataTypes.DateTime:
                nCompValue = CompareObjects<DateTime>(oValue1, oValue2);
                break;
              case DatalistDataTypes.Int:
                nCompValue = CompareObjects<int>(oValue1, oValue2);
                break;
              case DatalistDataTypes.Double:
                nCompValue = CompareObjects<double>(oValue1, oValue2);
                break;
              case DatalistDataTypes.Float:
                nCompValue = CompareObjects<float>(oValue1, oValue2);
                break;
              case DatalistDataTypes.Color:
              case DatalistDataTypes.Object:
              default:
                throw new Exception("Could not sort column because sorting is not supported for the data type");
            }
          }
        }
      }

      return bAscending ? -1 * nCompValue : nCompValue;
    }

    private int CompareObjects<T>(object oValue1, object oValue2) where T : IConvertible, IComparable
    {
      Type typeParameterType = typeof(T);
      T obj1 = (T)Convert.ChangeType(oValue1, typeParameterType);
      T obj2 = (T)Convert.ChangeType(oValue2, typeParameterType);

      return (obj1).CompareTo(obj2);
    }
  }
}
