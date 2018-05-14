using System;
using System.Windows.Input;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Scar.Common.WPF.Commands;

namespace Remembrance.ViewModel.Card
{
    [AddINotifyPropertyChangedInterface]
    [UsedImplicitly]
    public sealed class LearningInfoViewModel
    {
        [NotNull]
        private readonly ILearningInfoRepository _learningInfoRepository;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messageHub;

        [NotNull]
        private readonly TranslationEntryKey _translationEntryKey;

        public LearningInfoViewModel(
            [NotNull] LearningInfo learningInfo,
            [NotNull] ILearningInfoRepository learningInfoRepository,
            [NotNull] ILog logger,
            [NotNull] IMessageHub messageHub)
        {
            if (learningInfo == null)
            {
                throw new ArgumentNullException(nameof(learningInfo));
            }

            _learningInfoRepository = learningInfoRepository ?? throw new ArgumentNullException(nameof(learningInfoRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            FavoriteCommand = new CorrelationCommand(Favorite);
            DemoteCommand = new CorrelationCommand(Demote, () => CanDemote);
            _translationEntryKey = learningInfo.Id;
            UpdateLearningInfo(learningInfo);
        }

        // A hack to raise NotifyPropertyChanged for other properties
        [AlsoNotifyFor(nameof(NextCardShowTime))]
        private bool ReRenderNextCardShowTimeSwitch { get; set; }

        public DateTime LastCardShowTime { get; private set; }

        public DateTime NextCardShowTime { get; private set; }

        public int ShowCount { get; private set; }

        public bool IsFavorited { get; private set; }

        public RepeatType RepeatType { get; private set; }

        public bool CanDemote => RepeatType != RepeatType.Elementary;

        [NotNull]
        public ICommand FavoriteCommand { get; }

        [NotNull]
        public ICommand DemoteCommand { get; }

        public void ReRenderNextCardShowTime()
        {
            ReRenderNextCardShowTimeSwitch = !ReRenderNextCardShowTimeSwitch;
        }

        public void UpdateLearningInfo([NotNull] LearningInfo learningInfo)
        {
            IsFavorited = learningInfo.IsFavorited;
            LastCardShowTime = learningInfo.LastCardShowTime;
            NextCardShowTime = learningInfo.NextCardShowTime;
            RepeatType = learningInfo.RepeatType;
            ShowCount = learningInfo.ShowCount;
        }

        private void Favorite()
        {
            var learningInfo = _learningInfoRepository.GetOrInsert(_translationEntryKey);
            learningInfo.IsFavorited = IsFavorited = !learningInfo.IsFavorited;
            _learningInfoRepository.Update(learningInfo);
            RepeatType = learningInfo.RepeatType;
            _messageHub.Publish(learningInfo);
            _logger.InfoFormat("{0} {1}", IsFavorited ? "Favorited" : "Unfavorited", learningInfo);
        }

        private void Demote()
        {
            var learningInfo = _learningInfoRepository.GetOrInsert(_translationEntryKey);
            learningInfo.DecreaseRepeatType();
            RepeatType = learningInfo.RepeatType;
            _learningInfoRepository.Update(learningInfo);
            _messageHub.Publish(learningInfo);
            _logger.InfoFormat("Demoted {0}", learningInfo);
        }
    }
}