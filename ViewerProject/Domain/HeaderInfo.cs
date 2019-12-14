using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewerProject.Domain
{
    public class HeaderInfo
    {
        /** 영상 파일 이름 */
        public string FileName { get; set; }

        public string FileType { get; set; }

        /** 밴드 수 */
        public int Band { get; set; }

        public int ImageWidth { get; set; }

        public int ImageHeight { get; set; }

        public string DataType { get; set; }

        public string Description { get; set; }
    }
}
