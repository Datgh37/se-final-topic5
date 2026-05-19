namespace WebUITopic5_Team4.Models.ViewModels
{
    public class BreadcrumbViewModel
    {
        public string Title { get; set; } = string.Empty;

        public string BackgroundImage { get; set; } = "~/images/breadcrumb.jpg";

        public List<BreadcrumbItem> Items { get; set; } = [];
    }

    public class BreadcrumbItem
    {
        /// <summary>Display text for this breadcrumb step.</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>URL for this step. Leave null for the current/active page (renders as plain text).</summary>
        public string? Url { get; set; }

        public BreadcrumbItem() { }

        public BreadcrumbItem(string text, string? url = null)
        {
            Text = text;
            Url = url;
        }
    }
}
