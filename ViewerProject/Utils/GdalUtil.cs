using OSGeo.GDAL;
using System;

namespace ViewerProject.Utils
{
    public class GdalUtil
    {
        /* -------------------------------------------------------------------- */
        /*      Report "IMAGE_STRUCTURE" metadata.                              */
        /* -------------------------------------------------------------------- */
        public static string ReportImageStructureMetadata(Dataset dataset)
        {
            String interleave = "";

            string[] metadata = dataset.GetMetadata("IMAGE_STRUCTURE");
            if (metadata.Length > 0)
            {
                for (int iMeta = 0; iMeta < metadata.Length; iMeta++)
                {
                    interleave = metadata[iMeta];
                }
            }

            return interleave;
        }
    }
}
