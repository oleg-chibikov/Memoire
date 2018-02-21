using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.ImageSearch;
using Remembrance.Contracts.ImageSearch.Data;
using Remembrance.Resources;
using Scar.Common.Async;
using Scar.Common.Messages;
using Scar.Common.WPF.Commands;

namespace Remembrance.ViewModel.Card
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class WordImageViewerViewModel
    {
        internal const string DefaultSearchTextTemplate = "{0} {1}";

        [NotNull]
        private readonly ICancellationTokenSourceProvider _cancellationTokenSourceProvider;

        [NotNull]
        private readonly IImageDownloader _imageDownloader;

        [NotNull]
        private readonly IImageSearcher _imageSearcher;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messageHub;

        [NotNull]
        private readonly string _searchText;

        [NotNull]
        private readonly IWordImagesInfoRepository _wordImagesInfoRepository;

        [NotNull]
        private readonly WordKey _wordKey;

        private int?[] _nonAvailableIndexes = new int?[2];

        private bool _shoudRepeat;

        public WordImageViewerViewModel(
            [NotNull] WordKey wordKey,
            [NotNull] string parentText,
            [NotNull] ILog logger,
            [NotNull] IWordImagesInfoRepository wordImagesInfoRepository,
            [NotNull] ICancellationTokenSourceProvider cancellationTokenSourceProvider,
            [NotNull] IImageDownloader imageDownloader,
            [NotNull] IImageSearcher imageSearcher,
            [NotNull] IMessageHub messageHub)
        {
            _wordKey = wordKey ?? throw new ArgumentNullException(nameof(wordKey));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _wordImagesInfoRepository = wordImagesInfoRepository ?? throw new ArgumentNullException(nameof(wordImagesInfoRepository));
            _cancellationTokenSourceProvider = cancellationTokenSourceProvider ?? throw new ArgumentNullException(nameof(cancellationTokenSourceProvider));
            _imageDownloader = imageDownloader ?? throw new ArgumentNullException(nameof(imageDownloader));
            _imageSearcher = imageSearcher ?? throw new ArgumentNullException(nameof(imageSearcher));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            SetNextImageCommand = new AsyncCorrelationCommand(SetNextImageAsync);
            SetPreviousImageCommand = new AsyncCorrelationCommand(SetPreviousImageAsync);

            _searchText = string.Format(DefaultSearchTextTemplate, wordKey.Word.Text, parentText);

            var wordImageInfo = _wordImagesInfoRepository.TryGetById(_wordKey);
            if (wordImageInfo != null)
            {
                ConstructionTask = UpdateImageViewAsync(wordImageInfo);
            }
            else
            {
                _shoudRepeat = true;
                ConstructionTask = SetNextOrPreviousImageAsync(true);
            }
        }

        public bool IsReverse { get; private set; }

        public int SearchIndex { get; private set; }

        [CanBeNull]
        public string ImageName { get; private set; }

        [CanBeNull]
        public string ImageUrl { get; private set; }

        [DependsOn(nameof(ImageName), nameof(ImageUrl), nameof(SearchIndex))]
        [CanBeNull]
        public string ToolTip => $"{SearchIndex + 1}. {ImageName} ({ImageUrl})";

        public bool IsLoading { get; private set; } = true;

        [CanBeNull]
        public BitmapSource Image { get; private set; }

        [NotNull]
        public ICommand SetPreviousImageCommand { get; }

        [NotNull]
        public ICommand SetNextImageCommand { get; }

        internal Task ConstructionTask { get; }

        private async Task SetWordImageAsync(int index, string searchText)
        {
            WordImageInfo wordImageInfo;
            _logger.TraceFormat("Setting new image for {0} at search index {1} with seachText {2}...", _wordKey, index, searchText);
            await _cancellationTokenSourceProvider.ExecuteAsyncOperation(
                    async cancellationToken =>
                    {
                        var imagesUrls = await _imageSearcher.SearchImagesAsync(searchText, cancellationToken, index).ConfigureAwait(false);
                        if (imagesUrls == null)
                        {
                            //Null means error - just displaying Error instead of image. Message is already shown to client;
                            _logger.WarnFormat("Cannot search images for {0}", _wordKey);
                            IsLoading = false;
                            _shoudRepeat = true;
                        }
                        else
                        {
                            if (imagesUrls.Count > 1)
                            {
                                throw new InvalidOperationException("Search should return only one image");
                            }

                            ImageInfoWithBitmap imageInfoWithBitmap = null;
                            if (imagesUrls.Any())
                            {
                                var imageDownloadTasks = imagesUrls.Select(
                                    async image => new ImageInfoWithBitmap
                                    {
                                        ImageBitmap = null, //images[i++],
                                        ThumbnailBitmap = await _imageDownloader.DownloadImageAsync(image.ThumbnailUrl, cancellationToken).ConfigureAwait(false),
                                        ImageInfo = image
                                    });
                                imageInfoWithBitmap = (await Task.WhenAll(imageDownloadTasks).ConfigureAwait(false)).SingleOrDefault();
                                if (imageInfoWithBitmap == null)
                                {
                                    _logger.WarnFormat("Cannot download image for {0}", _wordKey);
                                }
                            }
                            else
                            {
                                var position = IsReverse
                                    ? 1
                                    : 0;
                                _nonAvailableIndexes[position] = index;
                            }

                            wordImageInfo = new WordImageInfo(_wordKey, index, imageInfoWithBitmap, IsReverse, _nonAvailableIndexes);
                            _wordImagesInfoRepository.Upsert(wordImageInfo);
                            _logger.DebugFormat("Image for {0} at search index {1} was saved", _wordKey, index);

                            await UpdateImageViewAsync(wordImageInfo).ConfigureAwait(false);
                        }
                    })
                .ConfigureAwait(false);
        }

        private async Task UpdateImageViewAsync([NotNull] WordImageInfo wordImageInfo)
        {
            SearchIndex = wordImageInfo.SearchIndex;
            IsReverse = wordImageInfo.IsReverse;
            _nonAvailableIndexes = wordImageInfo.NonAvailableIndexes;
            var imageBytes = wordImageInfo.Image?.ThumbnailBitmap;
            if (imageBytes == null || imageBytes.Length == 0)
            {
                //Try to search next image if the current image is not available, but has metadata
                await SetNextOrPreviousImageAsync(true).ConfigureAwait(false);
                return;
            }

            ImageName = wordImageInfo.Image.ImageInfo.Name;
            ImageUrl = wordImageInfo.Image.ImageInfo.Url;
            Image = _imageDownloader.LoadImage(imageBytes);
            IsLoading = false;
        }

        internal async Task SetPreviousImageAsync()
        {
            await SetNextOrPreviousImageAsync(false).ConfigureAwait(false);
        }

        //todo: disable prev image if curindex =0
        //todo: show empty image
        //TODO: reload unloaded image button
        internal async Task SetNextImageAsync()
        {
            await SetNextOrPreviousImageAsync(true).ConfigureAwait(false);
        }

        /// <remarks>
        /// This function swaps searchText every next time until one of the texts stops to give results
        /// </remarks>
        private async Task SetNextOrPreviousImageAsync(bool increase)
        {
            if (SearchIndex == 0 && !IsReverse && !increase)
            {
                return;
            }

            IsLoading = true;
            var searchIndex = SearchIndex;
            if (!_shoudRepeat)
            {
                _logger.TraceFormat(
                    "Setting {1} image for {0}...",
                    this,
                    increase
                        ? "next"
                        : "previous");
                var swappedPosition = IsReverse
                    ? 0
                    : 1;
                var position = IsReverse
                    ? 1
                    : 0;

                var nonAvailableIndex = _nonAvailableIndexes[position];
                var nonAvailableIndexForSwapped = _nonAvailableIndexes[swappedPosition];
                var nextPossibleIndex = increase
                    ? searchIndex + 1
                    : searchIndex - 1;
                var noSwap = false;
                if (nonAvailableIndexForSwapped == null || nextPossibleIndex < nonAvailableIndexForSwapped)
                {
                    IsReverse = !IsReverse;
                }
                else
                {
                    noSwap = true;
                    if (nonAvailableIndex != null && nextPossibleIndex >= nonAvailableIndex)
                    {
                        _messageHub.Publish(string.Format(Errors.NoMoreImages, _wordKey).ToWarning());
                        IsLoading = false;
                        return;
                    }
                }

                if (increase)
                {
                    if (noSwap || !IsReverse)
                    {
                        searchIndex++;
                    }
                }
                else
                {
                    if (noSwap || IsReverse)
                    {
                        searchIndex--;
                    }
                }
            }

            _shoudRepeat = false;

            //after getting isReverse
            var searchText = IsReverse
                ? _wordKey.Word.Text
                : _searchText;
            await SetWordImageAsync(searchIndex, searchText).ConfigureAwait(false);
            IsLoading = false;
        }

        public override string ToString()
        {
            //TODO:
            return _wordKey.Word.ToString();
        }
    }
}