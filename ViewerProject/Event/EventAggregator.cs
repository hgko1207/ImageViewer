using ViewerProject.Domain;

namespace ViewerProject.Event
{
    public static class EventAggregator
    {
        public static EventManager<string> MouseMoveEvent { set; get; } = new EventManager<string>();

        public static EventManager<int> ProgressEvent { set; get; } = new EventManager<int>();

        public static EventManager<ImageInfo> ImageOpenEvent { set; get; } = new EventManager<ImageInfo>();
    }
}
