//-----------------------------------------------------------------------
// <copyright file="MouseSimulator.cs" company="WPA">
//     Copyright (c) Tudor Potecaru. All rights reserved.
// </copyright>
// <author>Tudor</author>
//-----------------------------------------------------------------------

namespace KinectWindows7Controller
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Class for simulating mouse events on a Windows PC
    /// </summary>
    public static class MouseSimulator
    {
        #region PRIVATE MEMBERS
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public SendInputEventType SendInputEventType;
            public MouseKeyboardInputUnion MouseKeyboardInputUnion;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct MouseKeyboardInputUnion
        {
            [FieldOffset(0)]
            public MouseInputData MouseInput;

            [FieldOffset(0)]
            public KEYBDINPUT KeyboardInput;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private struct MouseInputData
        {
            public int dx;
            public int dy;
            public int mouseData;
            public MouseEventFlags dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [Flags]
        private enum MouseEventFlags : uint
        {
            MOUSEEVENTF_MOVE = 0x0001,
            MOUSEEVENTF_LEFTDOWN = 0x0002,
            MOUSEEVENTF_LEFTUP = 0x0004,
            MOUSEEVENTF_RIGHTDOWN = 0x0008,
            MOUSEEVENTF_RIGHTUP = 0x0010,
            MOUSEEVENTF_MIDDLEDOWN = 0x0020,
            MOUSEEVENTF_MIDDLEUP = 0x0040,
            MOUSEEVENTF_XDOWN = 0x0080,
            MOUSEEVENTF_XUP = 0x0100,
            MOUSEEVENTF_WHEEL = 0x0800,
            MOUSEEVENTF_VIRTUALDESK = 0x4000,
            MOUSEEVENTF_ABSOLUTE = 0x8000
        }

        private enum SendInputEventType : int
        {
            InputMouse,
            InputKeyboard
        }
        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// Simulates the mouse left click.
        /// </summary>
        public static void SimulateMouseLeftClick()
        {
            var mouseDownInput = new INPUT { SendInputEventType = SendInputEventType.InputMouse };
            mouseDownInput.MouseKeyboardInputUnion.MouseInput.dwFlags = MouseEventFlags.MOUSEEVENTF_LEFTDOWN;
            SendInput(1, ref mouseDownInput, Marshal.SizeOf(new INPUT()));

            var mouseUpInput = new INPUT { SendInputEventType = SendInputEventType.InputMouse };
            mouseUpInput.MouseKeyboardInputUnion.MouseInput.dwFlags = MouseEventFlags.MOUSEEVENTF_LEFTUP;
            SendInput(1, ref mouseUpInput, Marshal.SizeOf(new INPUT()));
        }

        /// <summary>
        /// Simulates the mouse right click.
        /// </summary>
        public static void SimulateMouseRightClick()
        {
            var mouseDownInput = new INPUT { SendInputEventType = SendInputEventType.InputMouse };
            mouseDownInput.MouseKeyboardInputUnion.MouseInput.dwFlags = MouseEventFlags.MOUSEEVENTF_RIGHTDOWN;
            SendInput(1, ref mouseDownInput, Marshal.SizeOf(new INPUT()));

            var mouseUpInput = new INPUT { SendInputEventType = SendInputEventType.InputMouse };
            mouseUpInput.MouseKeyboardInputUnion.MouseInput.dwFlags = MouseEventFlags.MOUSEEVENTF_RIGHTUP;
            SendInput(1, ref mouseUpInput, Marshal.SizeOf(new INPUT()));
        }

        /// <summary>
        /// Simulates the mouse move.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        public static void SimulateMouseMove(double x, double y)
        {
            var fScreenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            var fScreenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            var fx = x * (65535.0f / fScreenWidth);
            var fy = y * (65535.0f / fScreenHeight);

            var mouseMoveInput = new INPUT { SendInputEventType = SendInputEventType.InputMouse };

            mouseMoveInput.MouseKeyboardInputUnion.MouseInput.dwFlags = MouseEventFlags.MOUSEEVENTF_MOVE | MouseEventFlags.MOUSEEVENTF_ABSOLUTE;
            mouseMoveInput.MouseKeyboardInputUnion.MouseInput.dx = (int)fx;
            mouseMoveInput.MouseKeyboardInputUnion.MouseInput.dy = (int)fy;
            SendInput(1, ref mouseMoveInput, Marshal.SizeOf(new INPUT()));
        }

        /// <summary>
        /// Simulates the mouse wheel.
        /// </summary>
        /// <param name="amount">The amount.</param>
        public static void SimulateMouseWheel(double amount)
        {
            var mouseWheelInput = new INPUT { SendInputEventType = SendInputEventType.InputMouse };
            mouseWheelInput.MouseKeyboardInputUnion.MouseInput.dwFlags = MouseEventFlags.MOUSEEVENTF_WHEEL;
            mouseWheelInput.MouseKeyboardInputUnion.MouseInput.mouseData = (int)amount;
            SendInput(1, ref mouseWheelInput, Marshal.SizeOf(new INPUT()));
        }
        #endregion
    }
}