using JetBrains.Annotations;

namespace Remembrance.ViewModel.DialogProviders
{
    internal static class DialogProviderConstants
    {
        [NotNull]
        public const string JsonFilesFilter = "Json files (*.json)|*.json;";

        [NotNull]
        public static readonly string DefaultFilePattern = $"{nameof(Remembrance)}.json";
    }
}