using JetBrains.Annotations;
using Microsoft.Win32;
using Remembrance.Contracts;
using Remembrance.Resources;

namespace Remembrance.ViewModel
{
    internal static class DialogProviderConstants
    {
        [NotNull]
        public const string JsonFilesFilter = "Json files (*.json)|*.json;";

        [NotNull]
        public static readonly string DefaultFilePattern = $"{nameof(Remembrance)}.json";
    }

    internal sealed class SaveFileDialogProvider : ISaveFileDialogProvider
    {

        [NotNull]
        public string FileName => _dialog.FileName;

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
    internal sealed class OpenFileDialogProvider : IOpenFileDialogProvider
    {
        [NotNull]
        public string FileName => _dialog.FileName;

        [NotNull]
        private readonly SaveFileDialog _dialog = new SaveFileDialog
        {
            FileName = DefaultFilePattern,
            Filter = JsonFilesFilter,
            RestoreDirectory = true,
            Title = $"{Texts.Title}: {Texts.Export}"
        };

        public bool? ShowDialog()
        {
            return _dialog.ShowDialog();
        }
    }
}
