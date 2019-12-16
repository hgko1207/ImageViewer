namespace ViewerProject.Event
{
    public static class EventAggregator
    {
        public static EventManager<string> MouseMoveEvent { set; get; } = new EventManager<string>();
    }
}
