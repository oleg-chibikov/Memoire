using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Windows;
using Autofac;
using Common.Logging;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.View.Card;
using Remembrance.Resources;
using Remembrance.ViewModel.Card;
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.Core.CardManagement
{
    [UsedImplicitly]
    internal sealed class AssessmentCardManager : BaseCardManager, IAssessmentCardManager, IDisposable
    {
        [NotNull]
        private readonly IMessenger _messenger;

        [NotNull]
        private readonly ISettingsRepository _settingsRepository;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        [NotNull]
        private readonly IWordsProcessor _wordsProcessor;

        private bool _hasOpenWindows;

        [NotNull]
        private IDisposable _interval;

        private DateTime? _lastCardShowTime;

        public AssessmentCardManager(
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] ILog logger,
            [NotNull] ILifetimeScope lifetimeScope,
            [NotNull] IMessenger messenger,
            [NotNull] IWordsProcessor wordsProcessor)
            : base(lifetimeScope, settingsRepository, logger)
        {
            logger.Info("Starting showing cards...");

            _wordsProcessor = wordsProcessor ?? throw new ArgumentNullException(nameof(wordsProcessor));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
            var repeatTime = settingsRepository.Get().CardShowFrequency;
            _interval = CreateInterval(repeatTime);
            messenger.Register<TimeSpan>(this, MessengerTokens.CardShowFrequencyToken, OnCardShowFrequencyChanged);
        }

        public void Dispose()
        {
            _interval.Dispose();
            Logger.Info("Finished showing cards");
        }

        private IDisposable CreateInterval(TimeSpan repeatTime)
        {
            TimeSpan initialDelay;
            if (_lastCardShowTime.HasValue)
            {
                var diff = DateTime.Now - _lastCardShowTime.Value;
                initialDelay = repeatTime - diff;
                if (initialDelay < TimeSpan.Zero)
                    initialDelay = TimeSpan.Zero;
            }
            else
            {
                initialDelay = TimeSpan.Zero;
            }

            return Observable.Timer(initialDelay, repeatTime)
                .Subscribe(
                    x =>
                    {
                        Trace.CorrelationManager.ActivityId = Guid.NewGuid();
                        var settings = _settingsRepository.Get();
                        if (!settings.IsActive)
                        {
                            Logger.Trace("Skipped showing card due to inactivity");
                            return;
                        }

                        _lastCardShowTime = DateTime.Now;
                        var translationEntry = _translationEntryRepository.GetCurrent();
                        if (translationEntry == null)
                        {
                            Logger.Trace("Skipped showing card due to absence of suitable cards");
                            return;
                        }

                        var translationDetails = _wordsProcessor.ReloadTranslationDetailsIfNeeded(translationEntry.Id, translationEntry.Key.Text, translationEntry.Key.SourceLanguage, translationEntry.Key.TargetLanguage);
                        var translationInfo = new TranslationInfo(translationEntry, translationDetails);
                        Logger.Trace($"Trying to show {translationInfo}...");
                        ShowCard(translationInfo, null);
                    });
        }

        private void OnCardShowFrequencyChanged(TimeSpan freq)
        {
            Logger.Trace($"Recreating interval for {freq}...");
            _interval.Dispose();
            _interval = CreateInterval(freq);
        }

        protected override IWindow TryCreateWindow(TranslationInfo translationInfo, IWindow ownerWindow)
        {
            if (_hasOpenWindows)
            {
                Logger.Trace("There is another window opened. Skipping creation...");
                return null;
            }

            Logger.Trace($"Creating window for {translationInfo}...");
            translationInfo.TranslationEntry.ShowCount++; // single place to update show count - no need to synchronize
            translationInfo.TranslationEntry.LastCardShowTime = DateTime.Now;
            _translationEntryRepository.Save(translationInfo.TranslationEntry);
            _messenger.Send(translationInfo, MessengerTokens.TranslationInfoToken);
            var assessmentViewModel = LifetimeScope.Resolve<AssessmentCardViewModel>(new TypedParameter(typeof(TranslationInfo), translationInfo));
            var window = LifetimeScope.Resolve<IAssessmentCardWindow>(new TypedParameter(typeof(AssessmentCardViewModel), assessmentViewModel), new TypedParameter(typeof(Window), ownerWindow));
            window.Closed += Window_Closed;
            _hasOpenWindows = true;
            return window;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _hasOpenWindows = false;
            ((Window) sender).Closed -= Window_Closed;
        }
    }
}