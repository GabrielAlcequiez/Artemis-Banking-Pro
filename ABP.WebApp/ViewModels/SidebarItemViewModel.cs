namespace ABP.WebApp.ViewModels;

public class SidebarItemViewModel
{
    public required string Text { get; init; }
    public required string Icon { get; init; }
    public string? Area { get; init; }
    public required string Controller { get; init; }
    public required string Action { get; init; }
}
