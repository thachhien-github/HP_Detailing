using System.Collections.Generic;

namespace HP_Detailing.Models
{
    public class PageHeaderBreadcrumb
    {
        public string Label { get; set; } = "";
        public string? Path { get; set; }
    }

    // Used by Views/Shared/_PageHeader.cshtml
    public class PageHeaderModel
    {
        public string Title { get; set; } = "";
        public string? Subtitle { get; set; }
        public List<PageHeaderBreadcrumb>? Breadcrumbs { get; set; }

        // Allows passing pre-rendered HTML from controller (for actions buttons)
        public string? ActionsSlotHtml { get; set; }
    }
}

