using System;
using System.ComponentModel;
using System.Windows;
using KaraokeVideoCreator.Application.Services;
using KaraokeVideoCreator.Application.ViewModels;
using KaraokeVideoCreator.Infrastructure.Audio;
using KaraokeVideoCreator.Infrastructure.Storage;
using KaraokeVideoCreator.UI.Services;

namespace KaraokeVideoCreator.UI
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            var repository = new ProjectRepository();
            var audioReader = new AudioMetadataReader();
            var recentStore = new ApplicationSettingsStore();
            var projectService = new ProjectService(repository, audioReader, recentStore);
            var fileDialogService = new WpfFileDialogService();
            var messageBoxService = new WpfMessageBoxService();

            _viewModel = new MainViewModel(projectService, fileDialogService, messageBoxService, recentStore);
            DataContext = _viewModel;

            PreviewKeyDown += OnMainWindowPreviewKeyDown;
        }

        private void OnMainWindowPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;

            if (e.Key == System.Windows.Input.Key.Tab && vm.SyncService.IsSyncModeActive)
            {
                if (System.Windows.Input.Keyboard.FocusedElement is System.Windows.Controls.TextBox &&
                    !System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) &&
                    !System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl))
                {
                    return;
                }

                e.Handled = true;

                if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) ||
                    System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightShift))
                {
                    vm.SyncService.StepPrevious(vm.CurrentProject.Lyrics);
                }
                else
                {
                    vm.ProcessTabSync();
                }
            }
            else if (e.Key == System.Windows.Input.Key.Space && vm.SyncService.IsSyncModeActive)
            {
                if (System.Windows.Input.Keyboard.FocusedElement is not System.Windows.Controls.TextBox)
                {
                    e.Handled = true;
                    if (vm.PlayCommand.CanExecute(null))
                    {
                        vm.PlayCommand.Execute(null);
                    }
                }
            }
            else if (e.Key == System.Windows.Input.Key.Escape && vm.SyncService.IsSyncModeActive)
            {
                e.Handled = true;
                vm.SyncService.IsSyncModeActive = false;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (!_viewModel.ConfirmSaveIfDirty())
            {
                e.Cancel = true;
            }
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnAboutClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Karaoke Video Creator\nVersion 1.0.0 (Phase 1: Project Foundation)\n\nA professional desktop application for karaoke project creation.",
                "About Karaoke Video Creator",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}