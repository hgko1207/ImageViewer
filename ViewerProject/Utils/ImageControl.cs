using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace ViewerProject.Utils
{
    public static class ImageControl
    {
        public static BitmapImage Bitmap2BitmapImage(Bitmap bitmap, ImageFormat imageFormat)
        {
            BitmapImage bitmapimage = new BitmapImage();

            try
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    bitmap.Save(memory, imageFormat);
                    memory.Position = 0;
                    bitmapimage.BeginInit();
                    bitmapimage.StreamSource = memory;
                    bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapimage.EndInit();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }

            return bitmapimage;
        }

        public static Bitmap GetBitmap(byte[] bytes, int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(bytes, 0, bitmapData.Scan0, bytes.Length);
            bitmap.UnlockBits(bitmapData);

            return bitmap;
        }
    }
}
