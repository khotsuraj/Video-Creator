using System.Windows;
using KaraokeVideoCreator.Application.Services;
using Microsoft.Win32;

namespace KaraokeVideoCreator.UI.Services
{
    public class WpfFileDialogService : IFileDialogService
    {
        public string? ShowOpenProjectDialog()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open Karaoke Project",
                Filter = "Karaoke Project (*.kproj)|*.kproj|All Files (*.*)|*.*",
                DefaultExt = ".kproj"
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? ShowSaveProjectDialog(string defaultName)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save Karaoke Project",
                Filter = "Karaoke Project (*.kproj)|*.kproj",
                DefaultExt = ".kproj",
                FileName = defaultName
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? ShowImportAudioDialog()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import Audio File",
                Filter = "Audio Files (*.mp3;*.wav;*.m4a;*.aac;*.flac;*.wma;*.ogg)|*.mp3;*.wav;*.m4a;*.aac;*.flac;*.wma;*.ogg|All Files (*.*)|*.*"
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? ShowExportVideoDialog(string defaultName)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Export Karaoke Video",
                Filter = "MP4 Video (*.mp4)|*.mp4|All Files (*.*)|*.*",
                DefaultExt = ".mp4",
                FileName = defaultName
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }

    public class WpfMessageBoxService : IMessageBoxService
    {
        public void ShowError(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowInfo(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public UnsavedChangesResult ShowUnsavedChangesPrompt(string projectName)
        {
            string msg = $"You have unsaved changes in '{projectName}'.\n\nDo you want to save changes before closing?";
            var result = MessageBox.Show(msg, "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    return UnsavedChangesResult.Save;
                case MessageBoxResult.No:
                    return UnsavedChangesResult.DontSave;
                default:
                    return UnsavedChangesResult.Cancel;
            }
        }
    }
}
