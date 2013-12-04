using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Wordament
{
	public class FastBitmapWrapper
	{
		public FastBitmapWrapper(Bitmap bmp)
		{
			// Lock the bitmap's bits.  
			Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
			BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

			byte[] rgbValues;

			try
			{
				Width = bmp.Width;
				Height = bmp.Height;

				pixels = new Color[Width, Height];

				// Get the address of the first line.
				IntPtr ptr = bmpData.Scan0;

				// Declare an array to hold the bytes of the bitmap.
				int bytes = bmpData.Stride * bmp.Height;
				rgbValues = new byte[bytes];

				// Copy the RGB values into the array.
				Marshal.Copy(ptr, rgbValues, 0, bytes);
			}
			finally
			{
				bmp.UnlockBits(bmpData);
			}

			int stride = bmpData.Stride;

			for (int y = 0; y < bmpData.Height; y++)
			{
				for (int x = 0; x < bmpData.Width; x++)
				{
					byte r = (byte)(rgbValues[(y * stride) + (x * 3) + 2]);
					byte g = (byte)(rgbValues[(y * stride) + (x * 3) + 1]);
					byte b = (byte)(rgbValues[(y * stride) + (x * 3)]);
					
					pixels[x, y] = Color.FromArgb(r, g, b);
				}
			}


		}

		public int Width { get; private set; }

		public int Height { get; private set; }

		private Color[,] pixels;

		public Color GetPixel(int x, int y)
		{
			return pixels[x, y];
		}
	}
}
