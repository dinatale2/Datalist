using System;
using Win32Lib;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DataList
{
    internal interface IWndProcProvider
    {
        void WndProc(ref Message msg, ToolTipNativeWindow wnd);
    }

    internal class ToolTipNativeWindow : NativeWindow
    {
        IWndProcProvider m_WndProcProv;

        internal ToolTipNativeWindow(IWndProcProvider prov)
            : base()
        {
            m_WndProcProv = prov;
        }

        protected override void WndProc(ref Message m)
        {
            m_WndProcProv.WndProc(ref m, this);
        }
    }

    internal class CellToolTip : Component, IWndProcProvider, IDisposable
    {
        private static int BORDERWIDTH = 1;

        private Rectangle m_ClientRectangle;
        private CreateParams m_CreateParams;

        private ListWnd m_Parent;
        public ListWnd Parent
        {
            get { return m_Parent; }
            set { m_Parent = value; }
        }

        public int ShowDelay
        {
            get { return m_ShowTimer.Interval; }
            set
            {
                if (value >= 100)
                    m_ShowTimer.Interval = value;
                else
                    m_ShowTimer.Interval = 100;
            }
        }

        private bool m_Visible;
        public bool Visible
        {
            get { return m_Visible; }
        }

        private Cell m_ParentCell;
        public Cell ParentCell
        {
            get { return m_ParentCell; }
            set
            {
                SetParentCell(value);
            }
        }

        private ToolTipNativeWindow m_ToolTipWnd;
        private Timer m_ShowTimer;
        private bool m_bDisposed;
        private bool m_bTrackingMouseLeave;

        public CellToolTip(ListWnd parent)
        {
            m_Parent = parent;

            m_bDisposed = false;
            m_ToolTipWnd = new ToolTipNativeWindow(this);

            m_Visible = false;

            m_ShowTimer = new Timer();
            m_ShowTimer.Stop();
            m_ShowTimer.Interval = SystemInformation.DoubleClickTime + 200;

            CreateToolTipWnd();

            m_ShowTimer.Tick += new EventHandler(m_ShowTimer_Tick);

            m_bTrackingMouseLeave = false;
        }

        ~CellToolTip()
        {
            Dispose(false);
        }

        public void ClearToolTip()
        {
            if (m_Visible)
                HideToolTip(true);

            m_ParentCell = null;
        }

        public void SetParentCell(Cell cell)
        {
            if (cell != null && cell == m_ParentCell)
            {
                if (!m_Visible && CanBeVisible(cell))
                {
                    m_ShowTimer.Stop();
                    m_ShowTimer.Start();
                }

                return;
            }

            m_ParentCell = cell;

            if (CanBeVisible(cell))
            {
                if (m_Visible)
                {
                    ShowToolTip();
                    return;
                }

                m_ShowTimer.Stop();
                m_ShowTimer.Start();
                return;
            }

            ClearToolTip();
        }

        internal void InvalidateToolTip()
        {
            if (m_ParentCell != null)
            {
                Win32.InvalidateRect(m_ToolTipWnd.Handle, IntPtr.Zero, false);
            }
        }

        private bool AllowedToBeVisible(Cell cell)
        {
            if (CanBeVisible(cell) && m_Visible)
                return true;
            else
                return false;
        }

        private bool CanBeVisible(Cell c)
        {
            if (c == null)
                return false;

            if (c.Value == null)
                return false;

            if ((c.GetColumnType() == ColumnType.TextNoWrap || c.GetColumnType() == ColumnType.TextWrap) && (c.Text == null || string.IsNullOrEmpty(c.Text.Trim())))
                return false;

            if (Win32.GetCapture() != IntPtr.Zero)
                return false;

            if (!UtilityFunctions.IsAncestor(m_Parent, Win32.GetForegroundWindow()))
                return false;

            if (m_Parent.IsEditing)
                return false;

            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (!m_bDisposed)
            {
                if (disposing)
                {
                    m_ToolTipWnd.DestroyHandle();

                    m_ShowTimer.Dispose();
                    m_Parent = null;
                    m_ParentCell = null;

                    m_bDisposed = true;
                }
            }

            base.Dispose(disposing);
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void CreateToolTipWnd()
        {
            m_CreateParams = new CreateParams();
            m_CreateParams.ClassStyle = (int)ClassStyles.OwnDC | (int)ClassStyles.VerticalRedraw
                | (int)ClassStyles.HorizontalRedraw | (int)ClassStyles.DropShadow | (int)ClassStyles.SaveBits;
            m_CreateParams.Caption = null;
            m_CreateParams.ExStyle = (int)WindowStylesEx.WS_EX_TOPMOST | (int)WindowStylesEx.WS_EX_NOACTIVATE;
            m_CreateParams.Parent = IntPtr.Zero;
            m_CreateParams.Height = 0;
            m_CreateParams.Width = 0;
            unchecked { m_CreateParams.Style = (int)WindowStyles.WS_POPUP; }
            m_ToolTipWnd.CreateHandle(m_CreateParams);
        }

        private Win32Lib.MouseEventFlags GetMouseEventMessage(WindowsMessages m)
        {
            switch (m)
            {
                case WindowsMessages.WM_RBUTTONUP:
                    return Win32Lib.MouseEventFlags.MOUSEEVENTF_RIGHTUP;
                case WindowsMessages.WM_LBUTTONUP:
                    return Win32Lib.MouseEventFlags.MOUSEEVENTF_LEFTUP;
                case WindowsMessages.WM_RBUTTONDOWN:
                    return Win32Lib.MouseEventFlags.MOUSEEVENTF_RIGHTDOWN;
                case WindowsMessages.WM_LBUTTONDOWN:
                    return Win32Lib.MouseEventFlags.MOUSEEVENTF_LEFTDOWN;
                case WindowsMessages.WM_MOUSEWHEEL:
                    return Win32Lib.MouseEventFlags.MOUSEEVENTF_WHEEL;
                default:
                    return Win32Lib.MouseEventFlags.MOUSEEVENTF_MOVE;
            }
        }

        void IWndProcProvider.WndProc(ref Message msg, ToolTipNativeWindow wnd)
        {
            switch ((WindowsMessages)msg.Msg)
            {
                case WindowsMessages.WM_PAINT:
                    PaintToolTip(ref msg);
                    break;
                case WindowsMessages.WM_MOUSELEAVE:
                    m_bTrackingMouseLeave = false;
                    {
                        IntPtr pMouseLoc;
                        pMouseLoc = GCHandle.ToIntPtr(GCHandle.Alloc(Control.MousePosition, GCHandleType.Normal));

                        Win32.PostMessage(m_Parent.Handle, (uint)DatalistMessage.WM_TOOLTIPMOUSEMOVE, IntPtr.Zero, pMouseLoc);
                    }
                    break;
                case WindowsMessages.WM_MOUSEMOVE:
                    if (!m_bTrackingMouseLeave)
                        StartMouseLeaveTracking();
                    {
                        IntPtr pMouseLoc;
                        pMouseLoc = GCHandle.ToIntPtr(GCHandle.Alloc(Control.MousePosition, GCHandleType.Normal));

                        Win32.PostMessage(m_Parent.Handle, (uint)DatalistMessage.WM_TOOLTIPMOUSEMOVE, IntPtr.Zero, pMouseLoc);
                    }
                    break;
                case WindowsMessages.WM_MOUSEACTIVATE:
                    msg.Result = new IntPtr((int)MouseActivate.MA_NOACTIVATE);
                    break;
                case WindowsMessages.WM_MOUSEWHEEL:
                case WindowsMessages.WM_RBUTTONUP:
                case WindowsMessages.WM_LBUTTONUP:
                case WindowsMessages.WM_RBUTTONDOWN:
                case WindowsMessages.WM_LBUTTONDOWN:
                    HideToolTip(true);
                    Win32.mouse_event((uint)GetMouseEventMessage((WindowsMessages)msg.Msg), 0, 0, 0, 0);
                    break;
                default:
                    wnd.DefWndProc(ref msg);
                    break;
            }
        }

        private bool NeedsToolTip(Rectangle drawRect, Rectangle valueRect)
        {
            switch (m_Parent.Columns[m_ParentCell.ColumnIndex].Type)
            {
                case ColumnType.TextNoWrap:
                case ColumnType.ComboBox:
                    return valueRect.Width >= drawRect.Width || !m_Parent.RowBounds.Contains(valueRect);
                case ColumnType.TextWrap:
                    return !m_Parent.RowBounds.Contains(valueRect);
                case ColumnType.CheckBox:
                    return !(drawRect.Contains(valueRect) && m_Parent.RowBounds.Contains(valueRect));
                case ColumnType.ProgressBar:
                    return !(drawRect.Contains(valueRect) && m_Parent.RowBounds.Contains(valueRect));
                case ColumnType.RowBackColor:
                case ColumnType.RowForeColor:
                case ColumnType.RowSelForeColor:
                case ColumnType.RowSelBackColor:
                default:
                    return false;
            }
        }

        private bool SetupWindow()
        {
            if (m_Parent.Parent.IsComboBox)
                return false;

            if (m_ParentCell == null)
                return false;

            // get the windows graphics object
            Graphics GFX = Graphics.FromHwnd(IntPtr.Zero);
            Rectangle rDrawRect = m_ParentCell.GetDrawRect();
            Rectangle rThrBounds = m_ParentCell.GetValueRect(GFX, rDrawRect);

            // dispose the graphics object
            GFX.Dispose();

            if (rThrBounds == Rectangle.Empty)
                return false;

            if (!NeedsToolTip(rDrawRect, rThrBounds))
                return false;

            Point p = m_Parent.PointToScreen(rThrBounds.Location);
            rThrBounds.Location = p;
            rThrBounds.Inflate(2 * BORDERWIDTH, 2 * BORDERWIDTH);

            // create the new client rectangle based on the size of the text rectangle, then inflate it by the padding
            m_ClientRectangle = rThrBounds;

            return true;
        }

        private void StartMouseLeaveTracking()
        {
            TRACKMOUSEEVENT tme = new TRACKMOUSEEVENT();
            tme.cbSize = Marshal.SizeOf(tme);
            tme.dwHoverTime = 0;
            tme.hwndTrack = m_ToolTipWnd.Handle;
            tme.dwFlags = (uint)TrackMouseEventFlag.TME_LEAVE;

            Win32.TrackMouseEvent(ref tme);

            m_bTrackingMouseLeave = true;
        }

        private void PaintToolTip(ref Message msg)
        {
            try
            {
                if (m_ParentCell == null || string.IsNullOrEmpty(m_ParentCell.Text))
                {
                    ClearToolTip();
                    return;
                }

                Graphics GFX = Graphics.FromHwnd(m_ToolTipWnd.Handle);
                Color clrBackColor = m_ParentCell.DetermineBackColor(true);

                GFX.Clear(clrBackColor);

                Pen brderPen = new Pen(UtilityFunctions.AdjustBrightness(clrBackColor, 0.8), BORDERWIDTH);
                GFX.DrawRectangle(brderPen, 0, 0, m_ClientRectangle.Width - BORDERWIDTH, m_ClientRectangle.Height - BORDERWIDTH);
                brderPen.Dispose();

                Rectangle valRect = m_ClientRectangle;
                valRect.Location = new Point(0, 0);
                valRect.Inflate(-2 * BORDERWIDTH, -2 * BORDERWIDTH);

                switch (m_ParentCell.GetColumnType())
                {
                    case ColumnType.TextNoWrap:
                    case ColumnType.TextWrap:
                    case ColumnType.ComboBox:
                        m_ParentCell.DrawText(GFX, valRect, true, true);
                        break;
                    case ColumnType.CheckBox:
                        m_ParentCell.DrawCheckBox(GFX, valRect);
                        break;
                    case ColumnType.ProgressBar:
                        m_ParentCell.DrawProgressBar(GFX, valRect);
                        break;
                    default:
                        break;
                }

                GFX.Dispose();

                msg.Result = IntPtr.Zero;
                Win32.ValidateRect(msg.HWnd, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void ShowToolTip()
        {
            if (!SetupWindow())
            {
                ClearToolTip();
                return;
            }

            if (!m_Visible)
                Win32.ShowWindow(m_ToolTipWnd.Handle, ShowWindowCommand.ShowNoActivate);

            InvalidateToolTip();

            Win32.SetWindowPos(m_ToolTipWnd.Handle, new IntPtr((int)InsertAfter.HWND_TOPMOST), m_ClientRectangle.X, m_ClientRectangle.Y,
                m_ClientRectangle.Width, m_ClientRectangle.Height, SetWindowPosFlags.DoNotActivate);
        }

        private void m_ShowTimer_Tick(object sender, EventArgs e)
        {
            ShowToolTip();
            m_ShowTimer.Stop();
            m_Visible = true;
        }

        public void HideToolTip(bool bKillTimer = false)
        {
            if (bKillTimer)
                m_ShowTimer.Stop();

            Win32.ShowWindow(m_ToolTipWnd.Handle, ShowWindowCommand.Hide);
            m_Visible = false;
        }

        public bool HandleMessage(ref Message msg)
        {
            if (m_ParentCell == null)
                return false;

            switch ((WindowsMessages)msg.Msg)
            {
                case WindowsMessages.WM_RBUTTONUP:
                case WindowsMessages.WM_LBUTTONUP:
                    if (CanBeVisible(m_ParentCell))
                        m_ShowTimer.Start();
                    break;
                case WindowsMessages.WM_RBUTTONDOWN:
                case WindowsMessages.WM_LBUTTONDOWN:
                    HideToolTip(true);
                    break;
                case WindowsMessages.WM_KILLFOCUS:
                case WindowsMessages.WM_KEYDOWN:
                case WindowsMessages.WM_KEYUP:
                case WindowsMessages.WM_MOUSEWHEEL:
                    ClearToolTip();
                    break;
                default:
                    return false;
            }

            return true;
        }
    }
}

