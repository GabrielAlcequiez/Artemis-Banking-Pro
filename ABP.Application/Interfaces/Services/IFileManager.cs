namespace ABP.Application.Interfaces.Services
{
    public interface IFileManager
    {
        string? Upload<TKey>(Stream? file, TKey id, string folderName, string? fileName, bool isEditMode = false, string? imagePath = "");
    }
}