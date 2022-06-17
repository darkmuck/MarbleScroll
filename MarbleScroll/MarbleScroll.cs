using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MarbleScroll
{

    class MarbleScroll
    {

        private IntPtr hookID = IntPtr.Zero;
        private LowLevelMouseProc hookCallback;

		// horizontal scroll
        private const int SENSITIVITY_X = 200;	// mouse move required for one scroll
        private const int DISTANCE_X = 200;		// one scroll distance

		// vertical scroll
        private const int SENSITIVITY_Y = 50;	// mouse move required for one scroll
        private const int DISTANCE_Y = 200;		// one scroll distance
        
		// are we scrolling or just pressing the button?
        private bool isScroll = false;
		// if we are scrolling, intercept back button action
        private bool disableBackButton = false;
        // when we are simulating middle click
        private bool simulatingMiddleClick = false;

        // some coordinates for detecting scroll
        private int startX;
        private int startY;
        private int dx;
        private int dy;

        public MarbleScroll()
        {
            // we need this, else gc will collect it
            hookCallback = HookCallback;
        }

        public void Start()
        {
            hookID = SetHook(hookCallback);
        }

        public void Stop()
        {
            UnhookWindowsHookEx(hookID);
        }

        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }
        
		// main code for scrolling
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            MouseMessages type = (MouseMessages)wParam;
            MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

            if (type == MouseMessages.WM_BACKBUTTONDOWN || type == MouseMessages.WM_MBUTTONDOWN && !simulatingMiddleClick)
            {
                isScroll = true;
                disableBackButton = false;
                startX = hookStruct.pt.x;
                startY = hookStruct.pt.y;
                dx = 0;
                dy = 0;

				// for scrolling in window under mouse pointer
                POINT p = new POINT();
                p.x = startX;
                p.y = startY;
                IntPtr focusWindow = WindowFromPoint(p);
                IntPtr foregroundWindow = GetForegroundWindow();
				
				// only focus is window is not already focused
                if (GetAncestor(foregroundWindow, 3) != GetAncestor(focusWindow, 3))
                    SetForegroundWindow(focusWindow);
					
                return new IntPtr(1);
            }
            else if (type == MouseMessages.WM_BACKBUTTONUP || type == MouseMessages.WM_MBUTTONUP && !simulatingMiddleClick)
            {
                isScroll = false;
                if (disableBackButton)
                {
                    return new IntPtr(1);
                }
                else
                {
                    if (type == MouseMessages.WM_MBUTTONUP)
                    {
                        simulatingMiddleClick = true;
                        Task.Factory.StartNew(() =>
                        {
                            mouse_event((uint)MouseEvents.MIDDLEDOWN, (uint)hookStruct.pt.x, (uint)hookStruct.pt.y, 0, UIntPtr.Zero);
                            mouse_event((uint)MouseEvents.MIDDLEUP, (uint)hookStruct.pt.x, (uint)hookStruct.pt.y, 0, UIntPtr.Zero);
                        });
                        return new IntPtr(1);
                    }
                }
            }
            else if(simulatingMiddleClick && type == MouseMessages.WM_MBUTTONUP)
            {
                simulatingMiddleClick = false;
            }
            else if (isScroll && type == MouseMessages.WM_MOUSEMOVE)
            {
                dx += hookStruct.pt.x - startX;
                dy += hookStruct.pt.y - startY;

				// horizontal
                if (Math.Abs(dx) > SENSITIVITY_X)
                {
                    int d = DISTANCE_X;
                    if (dx < 0)
                    {
                        d *= -1;
                        dx += SENSITIVITY_X;
                    }
                    else
                        dx -= SENSITIVITY_X;

                    // reset vertical scroll (because vertical is more sensitive)
                    dy = 0;
					
					// scroll me (:
                    // need to run in different thread, as it takes to long to execute mouse_event and windows doesn't like it
                    Task.Factory.StartNew(() =>
                    {
                        mouse_event((uint)MouseEvents.HWHEEL, 0U, 0U, d, UIntPtr.Zero);
                    });
                    disableBackButton = true;
                }
				
				// vertical
                if (Math.Abs(dy) > SENSITIVITY_Y)
                {
                    int d = DISTANCE_Y;
                    if (dy > 0)
                    {
                        d *= -1;
                        dy -= SENSITIVITY_Y;
                    }
                    else
                        dy += SENSITIVITY_Y;

					// scroll me (:
                    // need to run in different thread, as it takes to long to execute mouse_event and windows doesn't like it
                    Task.Factory.StartNew(() => { 
                        mouse_event((uint)MouseEvents.WHEEL, 0U, 0U, d, UIntPtr.Zero);
                    });
                    disableBackButton = true;
                }

                return new IntPtr(1);
            }
			
			// nothing for me? pass it to next hook
            return CallNextHookEx(hookID, nCode, wParam, lParam);
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, int dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private const int WH_MOUSE_LL = 14;

        private enum MouseMessages
        {
            WM_LBUTTONDOWN      = 0x0201,
            WM_LBUTTONUP        = 0x0202,
            WM_MOUSEMOVE        = 0x0200,
            WM_MOUSEWHEEL       = 0x020A,
            WM_RBUTTONDOWN      = 0x0204,
            WM_RBUTTONUP        = 0x0205,
            WM_BACKBUTTONDOWN   = 0x020B,
            WM_BACKBUTTONUP     = 0x020C,
            WM_MBUTTONDOWN      = 0x0207,
            WM_MBUTTONUP        = 0x0208
        }

        private enum MouseEvents
        {
            WHEEL       = 0x0800,
            HWHEEL      = 0x1000,
            MOVE        = 0x0001,
            ABSMOVE     = 0x8001,
            MIDDLEDOWN  = 0x0020,
            MIDDLEUP    = 0x0040
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

    }

}
