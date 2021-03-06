﻿using OSGeo.GDAL;
using OSGeo.OSR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using ViewerProject.Domain;
using ViewerProject.Event;

namespace ViewerProject.Utils
{
    public class GDALReader
    {
        public string FileName { get; set; }

        private Dataset dataset;

        private List<RasterData> rasterDatas;

        private int[] levels;

        public void Open(String filePath)
        {
            GdalConfiguration.ConfigureGdal();
            GdalConfiguration.ConfigureOgr();

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

            BuildOverview();
        }

        /**
         * Downsampling 피라미드 형식으로 생성
         */
        private void BuildOverview()
        {
            levels = new int[] { 2, 4, 8, 16 };
            if (dataset.BuildOverviews("NEAREST", levels, new Gdal.GDALProgressFuncDelegate(ProgressFunc), "Sample Data") != (int)CPLErr.CE_None)
            {
                Console.WriteLine("The BuildOverviews operation doesn't work");
                Environment.Exit(-1);
            }

            /* -------------------------------------------------------------------- */
            /*      Displaying the raster parameters                                */
            /* -------------------------------------------------------------------- */
            for (int iBand = 1; iBand <= dataset.RasterCount; iBand++)
            {
                Band band = dataset.GetRasterBand(iBand);
                Console.WriteLine("Band " + iBand + " :");
                Console.WriteLine("   DataType: " + band.DataType);
                Console.WriteLine("   Size (" + band.XSize + "," + band.YSize + ")");
                Console.WriteLine("   PaletteInterp: " + band.GetRasterColorInterpretation().ToString());

                for (int iOver = 0; iOver < band.GetOverviewCount(); iOver++)
                {
                    Band over = band.GetOverview(iOver);
                    Console.WriteLine("      OverView " + iOver + " :");
                    Console.WriteLine("         DataType: " + over.DataType);
                    Console.WriteLine("         Size (" + over.XSize + "," + over.YSize + ")");
                    Console.WriteLine("         PaletteInterp: " + over.GetRasterColorInterpretation().ToString());
                }
            }
            Console.WriteLine("Completed.");
        }

        public int ProgressFunc(double Complete, IntPtr Message, IntPtr Data)
	    {
            EventAggregator.ProgressEvent.Publish((int)(Complete * 100));
            //Console.Write("Processing ... " + Complete * 100 + "% Completed.");
            //if (Message != IntPtr.Zero)
            //    Console.Write(" Message:" + Marshal.PtrToStringAnsi(Message));
            //if (Data != IntPtr.Zero)
            //    Console.Write(" Data:" + Marshal.PtrToStringAnsi(Data));

            //Console.WriteLine("");
            return 1;
	    }

        /**
        * 헤더 생성
        */
        public ImageInfo GetImageInfo()
        {
            ImageInfo imageInfo = new ImageInfo();
            imageInfo.FileName = Path.GetFileName(FileName);
            imageInfo.FileType = dataset.GetDriver().ShortName + "/" + dataset.GetDriver().LongName;
            imageInfo.Band = dataset.RasterCount;

            imageInfo.DataType = Gdal.GetDataTypeName(dataset.GetRasterBand(1).DataType);
            imageInfo.Description = dataset.GetDescription();
            imageInfo.ImageWidth = dataset.RasterXSize;
            imageInfo.ImageHeight = dataset.RasterYSize;
            imageInfo.ViewerWidth = dataset.RasterXSize;
            imageInfo.ViewerHeight = dataset.RasterYSize;
            imageInfo.Interleave = GdalUtil.ReportImageStructureMetadata(dataset);

            Boundary ImageBoundary = null;
            if (GeometryControl.GetImageBoundary(dataset, out double minX, out double minY, out double maxX, out double maxY))
                ImageBoundary = new Boundary(minX, minY, maxX, maxY, 0, 0);

            imageInfo.ImageBoundary = ImageBoundary;

            ImageFormat imageFormat = ImageFormat.Bmp;
            if (FileName.ToLower().Contains(".png") || FileName.ToLower().Contains(".tif"))
                imageFormat = ImageFormat.Png;
            else if (FileName.ToLower().Contains(".jpg") || FileName.ToLower().Contains(".jpeg"))
                imageFormat = ImageFormat.Jpeg;

            imageInfo.ImageFormat = imageFormat;

            SetMapInfo(imageInfo);

            return imageInfo;
        }

        private void SetMapInfo(ImageInfo imageInfo)
        {
            string projection = dataset.GetProjectionRef();
            if (!string.IsNullOrEmpty(projection))
            {
                SpatialReference sr = new SpatialReference(projection);

                MapInfo mapInfo = new MapInfo();
                mapInfo.Projcs = sr.GetAttrValue("PROJCS", 0);
                mapInfo.Unit = sr.GetAttrValue("UNIT", 0);

                imageInfo.MapInfo = mapInfo;
            }
        }

        public Bitmap GetBitmap(int xOff, int yOff, int xSize, int ySize, int overview)
        {
            if (rasterDatas.Count == 1)
                return ReadGrayBitmap(xOff, yOff, xSize, ySize, overview);
            else
                return ReadRgbBitmap(xOff, yOff, xSize, ySize, xSize, ySize);
        }

        private Bitmap ReadRgbBitmap(int xOff, int yOff, int width, int height, int imageWidth, int imageHeight)
        {
            int[] bandMap = new int[4] { 0, 0, 0, 0 };
            int channelCount = 1;
            bool hasAlpha = false;
            bool isIndexed = false;
            int channelSize = 8;
            ColorTable colorTable = null;

            // Evaluate the bands and find out a proper image transfer format
            for (int i = 0; i < dataset.RasterCount; i++)
            {
                Band band = dataset.GetRasterBand(i + 1);
                if (Gdal.GetDataTypeSize(band.DataType) > 8)
                    channelSize = 16;

                switch (band.GetRasterColorInterpretation())
                {
                    case ColorInterp.GCI_AlphaBand:
                        channelCount = 4;
                        hasAlpha = true;
                        bandMap[3] = i + 1;
                        break;
                    case ColorInterp.GCI_BlueBand:
                        if (channelCount < 3)
                            channelCount = 3;
                        bandMap[0] = i + 1;
                        break;
                    case ColorInterp.GCI_RedBand:
                        if (channelCount < 3)
                            channelCount = 3;
                        bandMap[2] = i + 1;
                        break;
                    case ColorInterp.GCI_GreenBand:
                        if (channelCount < 3)
                            channelCount = 3;
                        bandMap[1] = i + 1;
                        break;
                    case ColorInterp.GCI_PaletteIndex:
                        colorTable = band.GetRasterColorTable();
                        isIndexed = true;
                        bandMap[0] = i + 1;
                        break;
                    case ColorInterp.GCI_GrayIndex:
                        isIndexed = true;
                        bandMap[0] = i + 1;
                        break;
                    default:
                        // we create the bandmap using the dataset ordering by default
                        if (i < 4 && bandMap[i] == 0)
                        {
                            if (channelCount < i)
                                channelCount = i;
                            bandMap[i] = i + 1;
                        }
                        break;
                }
            }

            // find out the pixel format based on the gathered information
            PixelFormat pixelFormat;
            DataType dataType;
            int pixelSpace;

            if (isIndexed)
            {
                pixelFormat = PixelFormat.Format8bppIndexed;
                dataType = DataType.GDT_Byte;
                pixelSpace = 1;
            }
            else
            {
                if (channelCount == 1)
                {
                    if (channelSize > 8)
                    {
                        pixelFormat = PixelFormat.Format16bppGrayScale;
                        dataType = DataType.GDT_Int16;
                        pixelSpace = 2;
                    }
                    else
                    {
                        pixelFormat = PixelFormat.Format24bppRgb;
                        channelCount = 3;
                        dataType = DataType.GDT_Byte;
                        pixelSpace = 3;
                    }
                }
                else
                {
                    if (hasAlpha)
                    {
                        if (channelSize > 8)
                        {
                            pixelFormat = PixelFormat.Format64bppArgb;
                            dataType = DataType.GDT_UInt16;
                            pixelSpace = 8;
                        }
                        else
                        {
                            pixelFormat = PixelFormat.Format32bppArgb;
                            dataType = DataType.GDT_Byte;
                            pixelSpace = 4;
                        }
                        channelCount = 4;
                    }
                    else
                    {
                        if (channelSize > 8)
                        {
                            pixelFormat = PixelFormat.Format48bppRgb;
                            dataType = DataType.GDT_UInt16;
                            pixelSpace = 6;
                        }
                        else
                        {
                            pixelFormat = PixelFormat.Format24bppRgb;
                            dataType = DataType.GDT_Byte;
                            pixelSpace = 3;
                        }
                        channelCount = 3;
                    }
                }
            }

            Bitmap bitmap = new Bitmap(imageWidth, imageHeight, pixelFormat);

            if (isIndexed)
            {
                if (colorTable != null)
                {
                    ColorPalette pal = bitmap.Palette;
                    for (int i = 0; i < colorTable.GetCount(); i++)
                    {
                        ColorEntry ce = colorTable.GetColorEntry(i);
                        pal.Entries[i] = Color.FromArgb(ce.c4, ce.c1, ce.c2, ce.c3);
                    }
                    bitmap.Palette = pal;
                }
                else
                {
                    ColorPalette pal = bitmap.Palette;
                    for (int i = 0; i < 255; i++)
                    {
                        pal.Entries[i] = Color.FromArgb(255, i, i, i);
                    }
                    bitmap.Palette = pal;
                }
            }

            // Use GDAL raster reading methods to read the image data directly into the Bitmap
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, imageWidth, imageHeight), ImageLockMode.ReadWrite, pixelFormat);

            try
            {
                int stride = bitmapData.Stride;
                IntPtr buf = bitmapData.Scan0;
                dataset.ReadRaster(xOff, yOff, width, height, buf, imageWidth, imageHeight, dataType, channelCount, bandMap, pixelSpace, stride, 1);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            return bitmap;
        }

        private Bitmap GetGrayBitmap(int xOff, int yOff, int xSize, int ySize)
        {
            Rect rect = new Rect(xOff, yOff, xSize, ySize);

            RasterData rasterData = rasterDatas[0];
            double[] data = rasterData.ReadRasterData((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);

            //float min = data.Min();
            //float max = data.Max();
            //double denominator = max - min;
            double min = rasterData.MinMax[0];
            double max = rasterData.MinMax[1];
            double denominator = max - min;

            const int bpp = 4;
            byte[] bytes = new byte[xSize * ySize * bpp];
            for (int row = 0; row < ySize; row++)
            {
                for (int col = 0; col < xSize; col++)
                {
                    double result = data[row * xSize + col];
                    byte value = (byte)(int)((result - min) * 255 / denominator);

                    bytes[row * xSize * bpp + col * bpp] = value;
                    bytes[row * xSize * bpp + col * bpp + 1] = value;
                    bytes[row * xSize * bpp + col * bpp + 2] = value;
                    bytes[row * xSize * bpp + col * bpp + 3] = 255;
                }
            }

            return ImageControl.GetBitmap(bytes, xSize, ySize, PixelFormat.Format32bppRgb);
        }

        private Bitmap ReadGrayBitmap(int xOffset, int yOffset, int xSize, int ySize, int overview)
        {
            Band band = dataset.GetRasterBand(1);
            if (overview > 0)
            {
                band = band.GetOverview(overview - 1);
            }

            int level = levels[overview - 1];
            int width = (xSize - xOffset) / level;
            int height = (ySize - yOffset) / level;
            int xOff = xOffset / level;
            int yOff = yOffset / level;

            //int width = band.XSize;
            //int height = band.YSize;

            //if (xOffset + width > band.XSize)
            //{
            //    width = band.XSize - xOffset;
            //}

            //if (yOffset + height > band.YSize)
            //{
            //    height = band.YSize - yOffset;
            //}

            double[] minmax = new double[2];
            band.ComputeRasterMinMax(minmax, 0);
            double min = minmax[0];
            double max = minmax[1];
            double stretchRate = 255 / (max - min);

            int[] data = new int[width * height];
            band.ReadRaster(xOff, yOff, width, height, data, width, height, 0, 0);

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap.PixelFormat);

            int stride = Math.Abs(bitmapData.Stride);
            byte[] bytes = new byte[height * stride];

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    int num = data[row * width + col];
                    byte value = (byte)((num - min) * stretchRate);
                    bytes[row * stride + col * 3] = value;
                    bytes[row * stride + col * 3 + 1] = value;
                    bytes[row * stride + col * 3 + 2] = value;
                    //bytes[row * stride + col * 4 + 3] = 255;
                }
            }

            Marshal.Copy(bytes, 0, bitmapData.Scan0, bytes.Length);
            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }

        private Bitmap GetRgbBitmap(int xOff, int yOff, int xSize, int ySize)
        {
            Rect rect = new Rect();
            rect.X = xOff;
            rect.Y = yOff;
            rect.Width = xSize;
            rect.Height = ySize;

            //byte[] R = new byte[xSize * ySize];
            //byte[] G = new byte[xSize * ySize];
            //byte[] B = new byte[xSize * ySize];

            double[] R = new double[xSize * ySize];
            double[] G = new double[xSize * ySize];
            double[] B = new double[xSize * ySize];

            for (int i = 0; i < rasterDatas.Count; i++)
            {
                if (rasterDatas[i].Band.GetRasterColorInterpretation() == ColorInterp.GCI_RedBand)
                {
                    R = rasterDatas[i].GetRaster(rect);
                    //R = GetByteValue(data, xSize, ySize, rasterDatas[i].Data.Min(), rasterDatas[i].Data.Max());
                }
                else if (rasterDatas[i].Band.GetRasterColorInterpretation() == ColorInterp.GCI_GreenBand)
                {
                    G = rasterDatas[i].GetRaster(rect);
                    //G = GetByteValue(data, xSize, ySize, rasterDatas[i].Data.Min(), rasterDatas[i].Data.Max());
                }
                else if (rasterDatas[i].Band.GetRasterColorInterpretation() == ColorInterp.GCI_BlueBand)
                {
                    B = rasterDatas[i].GetRaster(rect);
                    //B = GetByteValue(data, xSize, ySize, rasterDatas[i].Data.Min(), rasterDatas[i].Data.Max());
                }
            }

            double min = rasterDatas[0].Data.Min();
            double max = rasterDatas[0].Data.Max();
            double denominator = max - min;

            const int bpp = 4;
            byte[] bytes = new byte[xSize * ySize * bpp];
            for (int row = 0; row < ySize; row++)
            {
                for (int col = 0; col < xSize; col++)
                {
                    var red = R[row * xSize + col];
                    var green = G[row * xSize + col];
                    var blue = B[row * xSize + col];

                    byte redByte = (byte)(int)((red - min) * 255 / denominator);
                    byte greenByte = (byte)(int)((green - min) * 255 / denominator);
                    byte blueByte = (byte)(int)((blue - min) * 255 / denominator);

                    bytes[row * xSize * bpp + col * bpp] = blueByte;
                    bytes[row * xSize * bpp + col * bpp + 1] = greenByte;
                    bytes[row * xSize * bpp + col * bpp + 2] = redByte;

                    if (R[row * xSize + col] + G[row * xSize + col] + B[row * xSize + col] == 0)
                        bytes[row * xSize * bpp + col * bpp + 3] = 0;
                    else
                        bytes[row * xSize * bpp + col * bpp + 3] = 255;
                }
            }

            return ImageControl.GetBitmap(bytes, xSize, ySize, PixelFormat.Format32bppArgb);
        }

        public System.Windows.Point ImageToWorld(double x, double y)
        {
            double[] adfGeoTransform = new double[6];
            double[] p = new double[3];

            dataset.GetGeoTransform(adfGeoTransform);
            p[0] = adfGeoTransform[0] + adfGeoTransform[1] * x + adfGeoTransform[2] * y;
            p[1] = adfGeoTransform[3] + adfGeoTransform[4] * x + adfGeoTransform[5] * y;

            SpatialReference src = new SpatialReference("");
            string s = dataset.GetProjectionRef();
            src.ImportFromWkt(ref s);

            SpatialReference wgs84 = new SpatialReference("");
            wgs84.SetWellKnownGeogCS("WGS84");

            CoordinateTransformation ct = new CoordinateTransformation(src, wgs84);
            ct.TransformPoint(p);

            ct.Dispose();
            wgs84.Dispose();
            src.Dispose();

            return new System.Windows.Point(p[0], p[1]);
        }
    }
}
