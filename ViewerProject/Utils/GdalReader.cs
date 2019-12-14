using OSGeo.GDAL;
using OSGeo.OSR;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using ViewerProject.Domain;

namespace ViewerProject.Utils
{
    public class GDALReader
    {
        private Dataset dataset;

        public string FileName { get; set; }

        private List<RasterData> rasterDatas;

        public void Open(String filePath)
        {
            GdalConfiguration.ConfigureGdal();
            dataset = Gdal.Open(filePath, Access.GA_ReadOnly);
            if (dataset == null)
            {
                Console.WriteLine("Can't open " + filePath);
                Environment.Exit(-1);
            }

            rasterDatas = new List<RasterData>();
            for (int i = 1; i <= dataset.RasterCount; i++)
            {
                Band band = dataset.GetRasterBand(i);
                rasterDatas.Add(new RasterData(band));
            }

        }

        /**
	    * 헤더 생성
	    */
        public HeaderInfo GetInfo()
        {
            HeaderInfo headerInfo = new HeaderInfo();
            headerInfo.FileName = FileName;
            headerInfo.FileType = dataset.GetDriver().ShortName + "/" + dataset.GetDriver().LongName;
            headerInfo.Band = dataset.RasterCount;

            String dataType = Gdal.GetDataTypeName(dataset.GetRasterBand(1).DataType);
            headerInfo.DataType = dataType;
            headerInfo.Description = dataset.GetDescription();
            headerInfo.ImageWidth = dataset.RasterXSize;
            headerInfo.ImageHeight = dataset.RasterYSize;

            SetMapInfo(headerInfo);

            return headerInfo;
        }

        public Bitmap GetBitmap(int xOff, int yOff, int xSize, int ySize)
        {
            return GetRgbBitmap(xOff, yOff, xSize, ySize);
        }

        public Bitmap GetRgbBitmap(int xOff, int yOff, int xSize, int ySize)
        {
            Rect rect = new Rect();
            rect.X = xOff;
            rect.Y = yOff;
            rect.Width = xSize;
            rect.Height = ySize;

            byte[] R = new byte[xSize * ySize];
            byte[] G = new byte[xSize * ySize];
            byte[] B = new byte[xSize * ySize];

            for (int i = 0; i < rasterDatas.Count; i++)
            {
                if (rasterDatas[i].Band.GetRasterColorInterpretation() == ColorInterp.GCI_RedBand)
                {
                    float[] data = rasterDatas[i].GetRaster(rect);
                    R = GetByteValue(data, xSize, ySize, rasterDatas[i].Data.Min(), rasterDatas[i].Data.Max());
                }
                else if (rasterDatas[i].Band.GetRasterColorInterpretation() == ColorInterp.GCI_GreenBand)
                {
                    float[] data = rasterDatas[i].GetRaster(rect);
                    G = GetByteValue(data, xSize, ySize, rasterDatas[i].Data.Min(), rasterDatas[i].Data.Max());
                }
                else if (rasterDatas[i].Band.GetRasterColorInterpretation() == ColorInterp.GCI_BlueBand)
                {
                    float[] data = rasterDatas[i].GetRaster(rect);
                    B = GetByteValue(data, xSize, ySize, rasterDatas[i].Data.Min(), rasterDatas[i].Data.Max());
                }
            }

            const int bpp = 4;
            byte[] bytes = new byte[xSize * ySize * bpp];
            for (int row = 0; row < ySize; row++)
            {
                for (int col = 0; col < xSize; col++)
                {
                    bytes[row * xSize * bpp + col * bpp] = B[row * xSize + col];
                    bytes[row * xSize * bpp + col * bpp + 1] = G[row * xSize + col];
                    bytes[row * xSize * bpp + col * bpp + 2] = R[row * xSize + col];
                    if (R[row * xSize + col] + G[row * xSize + col] + B[row * xSize + col] == 0)
                        bytes[row * xSize * bpp + col * bpp + 3] = 0;
                    else
                        bytes[row * xSize * bpp + col * bpp + 3] = 255;
                }
            }

            return ImageControl.GetBitmap(bytes, xSize, ySize);
        }

        private byte[] GetByteValue(float[] value, int width, int height, double min, double max)
        {
            byte[] bytes = new byte[width * height];

            double denominator = max - min;
            bytes = value.Select(d => (byte)(int)((d - min) * 255 / denominator)).ToArray();
            
            return bytes;
        }

        private void SetMapInfo(HeaderInfo headerInfo)
        {
            String projection = dataset.GetProjectionRef();
            if (String.IsNullOrEmpty(projection))
            {
                projection = dataset.GetGCPProjection();
            }

            Console.WriteLine(projection);
            

        }

        public void PointToCoordinate()
        {
            String projection = dataset.GetProjectionRef();
            if (String.IsNullOrEmpty(projection))
            {
                projection = dataset.GetGCPProjection();
            }

            Console.WriteLine(projection);

            SpatialReference src = new SpatialReference("");
            src.ImportFromWkt(ref projection);
            Console.WriteLine("SOURCE IsGeographic:" + src.IsGeographic() + " IsProjected:" + src.IsProjected());

            SpatialReference dst = new SpatialReference("");
            dst.ImportFromEPSG(4326);
            Console.WriteLine("DEST IsGeographic:" + dst.IsGeographic() + " IsProjected:" + dst.IsProjected());


            //CoordinateTransformation ct = new CoordinateTransformation(src, dst);
            //double[] p = new double[3];
            //p[0] = 100; p[1] = 100; p[2] = 0;
            //ct.TransformPoint(p);
            //Console.WriteLine("x:" + p[0] + " y:" + p[1] + " z:" + p[2]);
        }

        public void ImageToWorld(double x, double y, out double lond, out double latd)
        {
            double[] adfGeoTransform = new double[6];
            double[] p = new double[3];

            dataset.GetGeoTransform(adfGeoTransform);
            p[0] = adfGeoTransform[0] + adfGeoTransform[1] * x + adfGeoTransform[2] * y;
            p[1] = adfGeoTransform[3] + adfGeoTransform[4] * x + adfGeoTransform[5] * y;

            SpatialReference src = new SpatialReference("");
            string s = dataset.GetProjectionRef();
            src.ImportFromWkt(ref s);
            //src.SetUTM(41, 1);
            SpatialReference wgs84 = new SpatialReference("");
            wgs84.SetWellKnownGeogCS("WGS84");
            CoordinateTransformation ct = new CoordinateTransformation(src, wgs84);
            ct.TransformPoint(p);
            lond = p[0];
            latd = p[1];

            ct.Dispose();
            wgs84.Dispose();
            src.Dispose();
        }
    }
}
