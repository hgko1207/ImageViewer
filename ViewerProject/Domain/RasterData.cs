using OSGeo.GDAL;
using System.Windows;

namespace ViewerProject.Domain
{
    public class RasterData
    {
        public double[] Data { get; internal set; }

        public Band Band { get; internal set; }

        private Rect rect;

        public double[] MinMax { get; internal set; }

        public RasterData(Band band)
        {
            Band = band;
            Data = null;
            rect = new Rect();
            MinMax = new double[2];
        }

        public double[] ReadRasterData(int xOff, int yOff, int xSize, int ySize)
        {
            double[] buf = new double[xSize * ySize];
            Band.ReadRaster(xOff, yOff, xSize, ySize, buf, xSize, ySize, 0, 0);
            Data = buf;

            Band.ComputeRasterMinMax(MinMax, 0);

            rect.X = xOff;
            rect.Y = yOff;
            rect.Width = xSize;
            rect.Height = ySize;

            return (double[])buf.Clone();
        }

        public byte[] ReadRasterData2(int xOff, int yOff, int xSize, int ySize)
        {
            byte[] buf = new byte[xSize * ySize];
            Band.ReadRaster(xOff, yOff, xSize, ySize, buf, xSize, ySize, 0, 0);
            //Data = (double[])buf.Clone();

            Band.ComputeRasterMinMax(MinMax, 0);

            rect.X = xOff;
            rect.Y = yOff;
            rect.Width = xSize;
            rect.Height = ySize;

            return buf;
        }

        private double[] GetContainData(int xOff, int yOff, int xSize, int ySize)
        {
            double[] buf = new double[xSize * ySize];
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

        public double[] GetRaster(Rect rect)
        {
            if (this.rect.Contains(rect))
                return GetContainData((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
            else
                return ReadRasterData((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
        }
    }
}
