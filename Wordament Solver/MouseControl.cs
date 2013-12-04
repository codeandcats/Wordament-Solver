using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Wordament
{
	public static class MouseControl
	{
		[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
		private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

		private const int leftDown = 0x02;
		private const int leftUp = 0x04;

		public static void SetMousePos(int x, int y)
		{
			Cursor.Position = new Point(x, y);
		}

		public static void LeftButtonDown(int x, int y)
		{
			//Cursor.Position = new Point(x + 25, y + 25);
			mouse_event(leftDown, (uint)x, (uint)y, 0, 0);
		}

		public static void LeftButtonUp(int x, int y)
		{
			//Cursor.Position = new Point(x + 25, y + 25);
			mouse_event(leftUp, (uint)x, (uint)y, 0, 0);
		}
	}
}
