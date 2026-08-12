namespace ABP.WebApp.Areas.Admin.ViewModels.Users;

public sealed class ChangeUserStatusViewModel
{
    public string Id { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public bool TargetIsActive { get; set; }
}
