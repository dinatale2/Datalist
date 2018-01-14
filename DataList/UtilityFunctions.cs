using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;
using System.Drawing;

namespace DataList
{
  internal class UtilityFunctions
  {
    internal static bool IsAncestor(Control cControl, IntPtr pForeGround)
    {
      if (pForeGround == null || cControl == null)
      {
        return false;
      }
      else
      {
        if (cControl.Handle == pForeGround)
        {
          return true;
        }
        else
        {
          return IsAncestor(cControl.Parent, pForeGround);
        }
      }
    }

    // allows for the adjustment of the brightness of a given color by some factor m
    internal static Color AdjustBrightness(Color color, double m)
    {
      // adjust the red, green, and blue by the given factor
      int r = (int)Math.Max(0, Math.Min(255, Math.Round((double)color.R * m)));
      int g = (int)Math.Max(0, Math.Min(255, Math.Round((double)color.G * m)));
      int b = (int)Math.Max(0, Math.Min(255, Math.Round((double)color.B * m)));

      return Color.FromArgb(r, g, b);
    }
  }
}
