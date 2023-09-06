using System;
using System.Threading;
using System.Windows.Input;
using Easy.MessageHub;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;

namespace Mémoire.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class LearningInfoViewModel : BaseViewModel
    {
        readonly ILearningInfoRepository _learningInfoRepository;
        readonly ILogger _logger;
        readonly IMessageHub _messageHub;
        readonly SynchronizationContext _synchronizationContext;
        readonly TranslationEntryKey _translationEntryKey;
        RepeatType _repeatType;

        public LearningInfoViewModel(
            LearningInfo learningInfo,
            ILearningInfoRepository learningInfoRepository,
            ILogger<LearningInfoViewModel> logger,
            IMessageHub messageHub,
            SynchronizationContext synchronizationContext,
            ICommandManager commandManager) : base(commandManager)
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

        public IRefreshableCommand DemoteCommand { get; }

        public ICommand FavoriteCommand { get; }

        public bool IsFavorited { get; private set; }

        public DateTimeOffset LastCardShowTime { get; private set; }

        public DateTimeOffset ModifiedDate { get; private set; }

        public DateTimeOffset CreatedDate { get; private set; }

        [DependsOn(nameof(CreatedDate), nameof(ModifiedDate))]
        public string DateInfo => CreatedDate == ModifiedDate ? $"{CreatedDate:dd MMM yy HH:mm}" : $"{CreatedDate:dd MMM yy HH:mm}->{ModifiedDate:dd MMM yy HH:mm}";

        public DateTimeOffset NextCardShowTime { get; private set; }

        public RepeatType RepeatType
        {
            get => _repeatType;
            private set
            {
                _repeatType = value;
                _synchronizationContext.Send(_ => DemoteCommand.RaiseCanExecuteChanged(), null);
            }
        }

        public int ShowCount { get; private set; }

        // A hack to raise NotifyPropertyChanged for other properties
        [AlsoNotifyFor(nameof(NextCardShowTime))]
        bool ReRenderNextCardShowTimeSwitch { get; set; }

        public void ReRenderNextCardShowTime()
        {
            ReRenderNextCardShowTimeSwitch = !ReRenderNextCardShowTimeSwitch;
        }

        public void UpdateLearningInfo(LearningInfo learningInfo)
        {
            _ = learningInfo ?? throw new ArgumentNullException(nameof(learningInfo));
            IsFavorited = learningInfo.IsFavorited;
            LastCardShowTime = learningInfo.LastCardShowTime;
            NextCardShowTime = learningInfo.NextCardShowTime;
            RepeatType = learningInfo.RepeatType;
            ShowCount = learningInfo.ShowCount;
            ModifiedDate = learningInfo.ModifiedDate;
            CreatedDate = learningInfo.CreatedDate;
        }

        void Demote()
        {
            var learningInfo = _learningInfoRepository.GetOrInsert(_translationEntryKey);
            learningInfo.DecreaseRepeatType();
            RepeatType = learningInfo.RepeatType;
            _learningInfoRepository.Update(learningInfo);
            _messageHub.Publish(learningInfo);
            _logger.LogInformation("Demoted {LearningInfo}", learningInfo);
        }

        void Favorite()
        {
            var learningInfo = _learningInfoRepository.GetOrInsert(_translationEntryKey);
            learningInfo.IsFavorited = IsFavorited = !learningInfo.IsFavorited;
            _learningInfoRepository.Update(learningInfo);
            RepeatType = learningInfo.RepeatType;
            _messageHub.Publish(learningInfo);
            _logger.LogInformation("{Favorited} {LearningInfo}", IsFavorited ? "Favorited" : "Unfavorited", learningInfo);
        }
    }
}
