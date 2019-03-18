using JetBrains.Annotations;
using Microsoft.Win32;
using Remembrance.Contracts;
using Remembrance.Resources;

namespace Remembrance.ViewModel.DialogProviders
{
    internal sealed class SaveFileDialogProvider : ISaveFileDialogProvider
    {
        [NotNull]
        public string FileName
        {
            get => _dialog.FileName;
            set => _dialog.FileName = value;
        }

        [NotNull]
        private readonly SaveFileDialog _dialog = new SaveFileDialog
        {
            FileName = DialogProviderConstants.DefaultFilePattern,
            Filter = DialogProviderConstants.JsonFilesFilter,
            RestoreDirectory = true,
            Title = $"{Texts.Title}: {Texts.Export}"
        };

        public bool? ShowDialog()
        {
            return _dialog.ShowDialog();
        }
    }
}
