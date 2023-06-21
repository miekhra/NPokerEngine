using Microsoft.AspNetCore.Components;

namespace NPokerEngine.Demo.Shared
{
    public class NavMenuItem
    {
        public string Icon { get; set; }
        public string Title { get; set; }
        public EventCallback OnClick { get; set; }
        public bool Visible { get; set; }
    }
}
