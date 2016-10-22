using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Windows;
using Autofac;
using Common.Logging;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts;
using Remembrance.Card.View.Contracts;
using Remembrance.Card.ViewModel.Contracts;
using Remembrance.DAL.Contracts;
using Remembrance.DAL.Contracts.Model;
using Remembrance.Resources;
using Remembrance.Translate.Contracts.Interfaces;
using Scar.Common.WPF;

namespace Remembrance.Card.Management
{
    [UsedImplicitly]
    internal sealed class AssessmentCardManager : BaseCardManager, IAssessmentCardManager, IDisposable
    {
        [NotNull]
        private readonly IMessenger messenger;

        [NotNull]
        private readonly ISettingsRepository settingsRepository;

        [NotNull]
        private readonly ITranslationDetailsRepository translationDetailsRepository;

        [NotNull]
        private readonly ITranslationEntryRepository translationEntryRepository;

        private bool hasOpenWindows;

        [NotNull]
        private IDisposable interval;

        private DateTime? lastCardShowTime;

        public AssessmentCardManager([NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] ILog logger,
            [NotNull] ILifetimeScope lifetimeScope,
            [NotNull] IMessenger messenger,
            [NotNull] ITranslationDetailsRepository translationDetailsRepository)
            : base(lifetimeScope, settingsRepository, logger)
        {
            if (textToSpeechPlayer == null)
                throw new ArgumentNullException(nameof(textToSpeechPlayer));
            if (translationEntryRepository == null)
                throw new ArgumentNullException(nameof(translationEntryRepository));
            if (settingsRepository == null)
                throw new ArgumentNullException(nameof(settingsRepository));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (lifetimeScope == null)
                throw new ArgumentNullException(nameof(lifetimeScope));
            if (messenger == null)
                throw new ArgumentNullException(nameof(messenger));
            if (translationDetailsRepository == null)
                throw new ArgumentNullException(nameof(translationDetailsRepository));

            logger.Info("Starting showing cards...");

            this.translationEntryRepository = translationEntryRepository;
            this.messenger = messenger;
            this.translationDetailsRepository = translationDetailsRepository;
            this.settingsRepository = settingsRepository;
            messenger.Register<TimeSpan>(this, MessengerTokens.CardShowFrequencyToken, OnCardShowFrequencyChanged);
            var repeatTime = settingsRepository.Get().CardShowFrequency;
            interval = CreateInterval(repeatTime);
        }

        public void Dispose()
        {
            interval.Dispose();
            Logger.Info("Finished showing cards");
        }

        private void OnCardShowFrequencyChanged(TimeSpan freq)
        {
            Logger.Debug($"Recreating interval for {freq}...");
            interval.Dispose();
            interval = CreateInterval(freq);
        }

        private IDisposable CreateInterval(TimeSpan repeatTime)
        {
            TimeSpan initialDelay;
            if (lastCardShowTime.HasValue)
            {
                var diff = DateTime.Now - lastCardShowTime.Value;
                initialDelay = repeatTime - diff;
                if (initialDelay < TimeSpan.Zero)
                    initialDelay = TimeSpan.Zero;
            }
            else
            {
                initialDelay = TimeSpan.Zero;
            }
            return Observable.Timer(initialDelay, repeatTime)
                .Subscribe(x =>
                {
                    Trace.CorrelationManager.ActivityId = Guid.NewGuid();
                    var settings = settingsRepository.Get();
                    if (!settings.IsActive)
                    {
                        Logger.Debug("Skipped showing card due to inactivity");
                        return;
                    }
                    lastCardShowTime = DateTime.Now;
                    var translationEntry = translationEntryRepository.GetCurrent();
                    if (translationEntry == null)
                    {
                        Logger.Debug("Skipped showing card due to absence of suitable cards");
                        return;
                    }
                    var translationDetails = translationDetailsRepository.GetById(translationEntry.Id);
                    var translationInfo = new TranslationInfo(translationEntry, translationDetails);
                    Logger.Debug($"Trying to show {translationInfo}...");
                    ShowCard(translationInfo);
                });
        }

        protected override IWindow TryCreateWindow(TranslationInfo translationInfo)
        {
            if (hasOpenWindows)
                return null;
            translationInfo.TranslationEntry.ShowCount++; //single place to update show count - no need to synchronize
            translationInfo.TranslationEntry.LastCardShowTime = DateTime.Now;
            translationEntryRepository.Save(translationInfo.TranslationEntry);
            messenger.Send(translationInfo, MessengerTokens.TranslationInfoToken);
            var assessmentViewModel = LifetimeScope.Resolve<IAssessmentCardViewModel>(new TypedParameter(typeof(TranslationInfo), translationInfo));
            var window = LifetimeScope.Resolve<IAssessmentCardWindow>(
                new TypedParameter(typeof(IAssessmentCardViewModel), assessmentViewModel));
            window.Closed += Window_Closed;
            hasOpenWindows = true;
            return window;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            hasOpenWindows = false;
            ((Window)sender).Closed -= Window_Closed;
        }
    }
}