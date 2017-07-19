using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Shell;
using JetBrains.Annotations;
using Remembrance.Settings.ViewModel.Contracts.Data;
using Remembrance.Translate.Contracts.Data.TextToSpeechPlayer;

namespace Remembrance.Settings.ViewModel.Contracts
{
    public interface ISettingsViewModel
    {
        [NotNull]
        IDictionary<Speaker, string> AvailableTtsSpeakers { get; }

        [NotNull]
        IDictionary<VoiceEmotion, string> AvailableVoiceEmotions { get; }

        [NotNull]
        Language[] AvailableUiLanguages { get; }

        double CardShowFrequency { get; }

        bool ReverseTranslation { get; }

        bool RandomTranslation { get; }

        [NotNull]
        ICommand SaveCommand { get; }

        [NotNull]
        ICommand ViewLogsCommand { get; }

        [NotNull]
        ICommand OpenSharedFolderCommand { get; }

        [NotNull]
        ICommand OpenSettingsFolderCommand { get; }

        [NotNull]
        ICommand ExportCommand { get; }

        [NotNull]
        ICommand ImportCommand { get; }

        [NotNull]
        ICommand WindowClosingCommand { get; }

        Speaker TtsSpeaker { get; }
        VoiceEmotion TtsVoiceEmotion { get; }

        [NotNull]
        Language UiLanguage { get; }

        int Progress { get; }

        string ProgressDescription { get; }

        TaskbarItemProgressState ProgressState { get; }
    }
}