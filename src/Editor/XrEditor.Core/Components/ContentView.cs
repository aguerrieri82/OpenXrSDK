namespace XrEditor
{
    public class ContentView
    {
        public string? Title { get; set; }

        public BaseView? Content { get; set; }


        public IList<ActionView>? Actions { get; set; }
    }
}
