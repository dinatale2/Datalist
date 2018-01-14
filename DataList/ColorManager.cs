using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace DataList
{
  internal enum ColorSelection
  {
    BackColor = 0,
    SelBackColor = 1,
    ForeColor = 2,
    SelForeColor = 3,
    Count = 4,
  };

  public class ColorManager<T>
  {
    private Dictionary<T, Color>[] m_ColorMaps;

    public ColorManager()
    {
      m_ColorMaps = new Dictionary<T, Color>[(int)ColorSelection.Count];
      for (int i = 0; i < m_ColorMaps.Length; i++)
        m_ColorMaps[i] = new Dictionary<T, Color>();
    }

    ~ColorManager()
    {
      ClearColors();

      for (int i = 0; i < m_ColorMaps.Length; i++)
        m_ColorMaps[i] = null;
    }

    internal void ClearColors()
    {
      foreach (Dictionary<T, Color> Map in m_ColorMaps)
        Map.Clear();
    }

    internal bool GetColor(ColorSelection part, T obj, out Color ObjColor)
    {
      ObjColor = Color.Empty;

      int nPart = (int)part;

      if (nPart >= 0 && nPart < m_ColorMaps.Length)
        return m_ColorMaps[nPart].TryGetValue(obj, out ObjColor);
      else
        return false;
    }

    internal void RemoveColor(ColorSelection part, T obj)
    {
      int nPart = (int)part;
      if (nPart >= 0 && nPart < m_ColorMaps.Length)
        m_ColorMaps[nPart].Remove(obj);
    }

    internal void RemoveAllColors(T obj)
    {
      foreach (Dictionary<T, Color> Map in m_ColorMaps)
        Map.Remove(obj);
    }

    internal void SetColor(ColorSelection part, T obj, Color objColor)
    {
      int nPart = (int)part;
      if (nPart >= 0 && nPart < m_ColorMaps.Length)
      {
        m_ColorMaps[nPart].Remove(obj);

        if (!objColor.IsEmpty)
          m_ColorMaps[nPart].Add(obj, objColor);
      }
    }
  }
}
