using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.Events;

namespace Remembrance.ViewModel.Translation
{
    [UsedImplicitly]
    public sealed class ImageViewModel : IDisposable
    {
        [NotNull]
        private readonly IWordImagesInfoRepository _imagesInfoRepository;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messenger;

        [NotNull]
        private readonly IList<Guid> _subscriptionTokens = new List<Guid>();

        [NotNull]
        private readonly PriorityWordViewModel _word;

        [NotNull]
        private readonly IEqualityComparer<IWord> _wordsEqualityComparer;

        public ImageViewModel(
            [NotNull] IEqualityComparer<IWord> wordsEqualityComparer,
            [NotNull] IMessageHub messenger,
            [NotNull] ILog logger,
            [NotNull] PriorityWordViewModel word,
            [NotNull] IWordImagesInfoRepository imagesInfoRepository)
        {
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _word = word ?? throw new ArgumentNullException(nameof(word));
            _imagesInfoRepository = imagesInfoRepository ?? throw new ArgumentNullException(nameof(imagesInfoRepository));
            _wordsEqualityComparer = wordsEqualityComparer ?? throw new ArgumentNullException(nameof(wordsEqualityComparer));
            _word.TranslationEntryIdSet += Word_TranslationEntryIdSet;
            _subscriptionTokens.Add(messenger.Subscribe<WordImagesInfo>(OnWordImagesInfoReceived));
        }

        [CanBeNull]
        public BitmapSource Image { get; private set; }

        public void Dispose()
        {
            //TODO: Make this happen (Using nested lifetimescopes)
            foreach (var subscriptionToken in _subscriptionTokens)
                _messenger.UnSubscribe(subscriptionToken);
            _word.TranslationEntryIdSet -= Word_TranslationEntryIdSet;
        }

        private void Word_TranslationEntryIdSet(object sender, EventArgs<object> e)
        {
            var wordImagesInfo = _imagesInfoRepository.GetImagesInfo(_word.TranslationEntryId, _word);
            UpdateImage(wordImagesInfo);
        }

        private void UpdateImage([CanBeNull] WordImagesInfo wordImagesInfo)
        {
            var imageBytes = wordImagesInfo?.Images.FirstOrDefault(x => x.ThumbnailBitmap != null)
                ?.ThumbnailBitmap;
            if (imageBytes == null || imageBytes.Length == 0)
                return;

            Image = LoadImage(imageBytes);
        }

        [CanBeNull]
        private static BitmapImage LoadImage([NotNull] byte[] imageBytes)
        {
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageBytes))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }

            image.Freeze();
            return image;
        }

        private void OnWordImagesInfoReceived([NotNull] WordImagesInfo wordImagesInfo)
        {
            if (!Equals(wordImagesInfo.TranslationEntryId, _word.TranslationEntryId) || !_wordsEqualityComparer.Equals(_word, wordImagesInfo))
                return;
            _logger.InfoFormat("Received image for {0}", this);
            UpdateImage(wordImagesInfo);
        }
    }
}