namespace ViewerProject.Domain
{
    public class ImageInfo
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

        /** 화면에 보여지는 이미지 사이즈 */
        public double ViewerWidth { get; set; }
        public double ViewerHeight { get; set; }

        public Boundary ImageBoundary { get; internal set; }
    }
}
