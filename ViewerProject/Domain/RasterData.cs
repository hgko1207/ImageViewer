using OSGeo.GDAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ViewerProject.Domain
{
    public class RasterData
    {
        public float[] Data { get; internal set; }

        public Band Band { get; internal set; }
        private Rect rect;


        public RasterData(Band band)
        {
            Band = band;
            Data = null;
            rect = new Rect();
        }

        private float[] ReadRasterData(int xOff, int yOff, int xSize, int ySize)
        {
            float[] buf = new float[xSize * ySize];
            Band.ReadRaster(xOff, yOff, xSize, ySize, buf, xSize, ySize, 0, 0);
            Data = buf;

            rect.X = xOff;
            rect.Y = yOff;
            rect.Width = xSize;
            rect.Height = ySize;

            return (float[])buf.Clone();
        }

        private float[] GetContainData(int xOff, int yOff, int xSize, int ySize)
        {
            float[] buf = new float[xSize * ySize];
            for (int j = 0; j < ySize; j++)
            {
                for (int i = 0; i < xSize; i++)
                {
                    int idx = (int)((yOff - rect.Y + j) * rect.Width + (xOff - rect.X + i));
                    buf[j * xSize + i] = Data[idx];
                }
            }
            return buf;
        }

        public float[] GetRaster(Rect rect)
        {
            if (this.rect.Contains(rect))
                return GetContainData((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
            else
                return ReadRasterData((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
        }
    }
}
