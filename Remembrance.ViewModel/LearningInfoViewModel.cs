using System;
using System.Threading;
using System.Windows.Input;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;

namespace Remembrance.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    [UsedImplicitly]
    public sealed class LearningInfoViewModel : BaseViewModel
    {
        [NotNull]
        private readonly ILearningInfoRepository _learningInfoRepository;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messageHub;

        [NotNull]
        private readonly SynchronizationContext _synchronizationContext;

        [NotNull]
        private readonly TranslationEntryKey _translationEntryKey;

        private RepeatType _repeatType;

        public LearningInfoViewModel(
            [NotNull] LearningInfo learningInfo,
            [NotNull] ILearningInfoRepository learningInfoRepository,
            [NotNull] ILog logger,
            [NotNull] IMessageHub messageHub,
            [NotNull] SynchronizationContext synchronizationContext,
            [NotNull] ICommandManager commandManager)
            : base(commandManager)
        {
            _ = learningInfo ?? throw new ArgumentNullException(nameof(learningInfo));
            _learningInfoRepository = learningInfoRepository ?? throw new ArgumentNullException(nameof(learningInfoRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            FavoriteCommand = AddCommand(Favorite);
            DemoteCommand = AddCommand(Demote, () => CanDemote);
            _translationEntryKey = learningInfo.Id;
            UpdateLearningInfo(learningInfo);
        }

        public bool CanDemote => RepeatType != RepeatType.Elementary;

        [NotNull]
        public IRefreshableCommand DemoteCommand { get; }

        [NotNull]
        public ICommand FavoriteCommand { get; }

        public bool IsFavorited { get; private set; }

        public DateTime LastCardShowTime { get; private set; }

        public DateTime ModifiedDate { get; private set; }

        public DateTime NextCardShowTime { get; private set; }

        public RepeatType RepeatType
        {
            get => _repeatType;
            private set
            {
                _repeatType = value;
                _synchronizationContext.Send(x => DemoteCommand.RaiseCanExecuteChanged(), null);
            }
        }

        public int ShowCount { get; private set; }

        // A hack to raise NotifyPropertyChanged for other properties
        [AlsoNotifyFor(nameof(NextCardShowTime))]
        private bool ReRenderNextCardShowTimeSwitch { get; set; }

        public void ReRenderNextCardShowTime()
        {
            ReRenderNextCardShowTimeSwitch = !ReRenderNextCardShowTimeSwitch;
        }

        public void UpdateLearningInfo([NotNull] LearningInfo learningInfo)
        {
            _ = learningInfo ?? throw new ArgumentNullException(nameof(learningInfo));
            IsFavorited = learningInfo.IsFavorited;
            LastCardShowTime = learningInfo.LastCardShowTime;
            NextCardShowTime = learningInfo.NextCardShowTime;
            RepeatType = learningInfo.RepeatType;
            ShowCount = learningInfo.ShowCount;
            ModifiedDate = learningInfo.ModifiedDate;
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

        private void Favorite()
        {
            var learningInfo = _learningInfoRepository.GetOrInsert(_translationEntryKey);
            learningInfo.IsFavorited = IsFavorited = !learningInfo.IsFavorited;
            _learningInfoRepository.Update(learningInfo);
            RepeatType = learningInfo.RepeatType;
            _messageHub.Publish(learningInfo);
            _logger.InfoFormat("{0} {1}", IsFavorited ? "Favorited" : "Unfavorited", learningInfo);
        }
    }
}