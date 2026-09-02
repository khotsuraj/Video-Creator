namespace KaraokeVideoCreator.Application.Services
{
    public interface IFileDialogService
    {
        string? ShowOpenProjectDialog();
        string? ShowSaveProjectDialog(string defaultName);
        string? ShowImportAudioDialog();
        string? ShowExportVideoDialog(string defaultName);
    }

    public enum UnsavedChangesResult
    {
        Save,
        DontSave,
        Cancel
    }

    public interface IMessageBoxService
    {
        void ShowError(string title, string message);
        void ShowInfo(string title, string message);
        UnsavedChangesResult ShowUnsavedChangesPrompt(string projectName);
    }
}
