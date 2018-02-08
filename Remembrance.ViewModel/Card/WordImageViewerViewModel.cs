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
using Remembrance.ViewModel.Translation;
using Scar.Common.Async;
using Scar.Common.Events;
using Scar.Common.Messages;
using Scar.Common.WPF.Commands;

namespace Remembrance.ViewModel.Card
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class WordImageViewerViewModel : IDisposable
    {
        internal const string SearchTemplate = "{0} {1}";

        [NotNull]
        private readonly ICancellationTokenSourceProvider _cancellationTokenSourceProvider;

        [NotNull]
        private readonly IImageDownloader _imageDownloader;

        [NotNull]
        private readonly IImageSearcher _imageSearcher;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messenger;

        [NotNull]
        private readonly IWordPropertiesReveivable _word;

        [NotNull]
        private readonly IWordImagesInfoRepository _wordImagesInfoRepository;

        private bool _isInitialized;

        private bool _isReverse;

        private int?[] _nonAvailableIndexes = new int?[2];

        [NotNull]
        private string _searchText;

        [NotNull]
        private WordKey _wordKey;

        public WordImageViewerViewModel(
            [NotNull] ILog logger,
            [NotNull] IWordPropertiesReveivable word,
            [NotNull] IWordImagesInfoRepository wordImagesInfoRepository,
            [NotNull] ICancellationTokenSourceProvider cancellationTokenSourceProvider,
            [NotNull] IImageDownloader imageDownloader,
            [NotNull] IImageSearcher imageSearcher,
            [NotNull] IMessageHub messenger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _word = word ?? throw new ArgumentNullException(nameof(word));
            _wordImagesInfoRepository = wordImagesInfoRepository ?? throw new ArgumentNullException(nameof(wordImagesInfoRepository));
            _cancellationTokenSourceProvider = cancellationTokenSourceProvider ?? throw new ArgumentNullException(nameof(cancellationTokenSourceProvider));
            _imageDownloader = imageDownloader ?? throw new ArgumentNullException(nameof(imageDownloader));
            _imageSearcher = imageSearcher ?? throw new ArgumentNullException(nameof(imageSearcher));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _word.WordKeySet += Word_WordKeySet;
            _word.ParentTextSet += Word_ParentTextSet;
            SetNextImageCommand = new CorrelationCommand(SetNextImage);
            SetPreviousImageCommand = new CorrelationCommand(SetPreviousImage);
            _searchText = _word.Text;
        }

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

        public void Dispose()
        {
            _word.WordKeySet -= Word_WordKeySet;
            _word.ParentTextSet -= Word_ParentTextSet;
        }

        private async void Word_WordKeySet(object sender, [NotNull] EventArgs<WordKey> e)
        {
            _wordKey = e.Parameter;

            var wordImageInfo = _wordImagesInfoRepository.TryGetById(_wordKey);
            if (wordImageInfo != null)
            {
                await UpdateImageViewAsync(wordImageInfo).ConfigureAwait(false);
            }
            else
            {
                await SetNextImageAsync(true).ConfigureAwait(false);
            }
        }

        private void Word_ParentTextSet(object sender, [NotNull] EventArgs<string> e)
        {
            _searchText = string.Format(SearchTemplate, _word.Text, e.Parameter);
        }

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
                            _isInitialized = false;
                        }
                        else
                        {
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
                                var position = _isReverse
                                    ? 1
                                    : 0;
                                _nonAvailableIndexes[position] = index;
                            }

                            wordImageInfo = new WordImageInfo(_wordKey, index, imageInfoWithBitmap, _isReverse, _nonAvailableIndexes);
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
            _isReverse = wordImageInfo.IsReverse;
            _nonAvailableIndexes = wordImageInfo.NonAvailableIndexes;
            var imageBytes = wordImageInfo.Image?.ThumbnailBitmap;
            if (imageBytes == null || imageBytes.Length == 0)
            {
                //Try to search next image if the current image is not available, but has metadata
                await SetNextImageAsync(true).ConfigureAwait(false);
                return;
            }

            ImageName = wordImageInfo.Image.ImageInfo.Name;
            ImageUrl = wordImageInfo.Image.ImageInfo.Url;
            Image = _imageDownloader.LoadImage(imageBytes);
            IsLoading = false;
        }

        private async void SetPreviousImage()
        {
            await SetNextImageAsync(false).ConfigureAwait(false);
        }

        //todo: disable prev image if curindex =0
        //todo: show empty image
        //TODO: reload unloaded image button
        //todo:cancellationtolen for next prev (like in PhotoReviewer)
        //TODO: separate image processor
        private async void SetNextImage()
        {
            await SetNextImageAsync(true).ConfigureAwait(false);
        }

        /// <remarks>
        /// This function swaps searchText every next time until one of the texts stops to give results
        /// </remarks>
        private async Task SetNextImageAsync(bool increase)
        {
            IsLoading = true;
            var searchIndex = SearchIndex;
            if (_isInitialized)
            {
                _logger.TraceFormat(
                    "Setting {1} image for {0}...",
                    this,
                    increase
                        ? "next"
                        : "previous");
                var swappedPosition = _isReverse
                    ? 0
                    : 1;
                var position = _isReverse
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
                    _isReverse = !_isReverse;
                }
                else
                {
                    noSwap = true;
                    if (nonAvailableIndex != null && nextPossibleIndex >= nonAvailableIndex)
                    {
                        _messenger.Publish(string.Format(Errors.NoMoreImages, _wordKey).ToWarning());
                        IsLoading = false;
                        return;
                    }
                }

                if (increase)
                {
                    if (noSwap || !_isReverse)
                    {
                        searchIndex++;
                    }
                }
                else
                {
                    if (noSwap || _isReverse)
                    {
                        if (searchIndex != 0)
                        {
                            searchIndex--;
                        }
                    }
                }
            }
            else
            {
                _isInitialized = true;
            }

            //after getting isReverse
            var searchText = _isReverse
                ? _word.Text
                : _searchText;
            await SetWordImageAsync(searchIndex, searchText).ConfigureAwait(false);
            IsLoading = false;
        }

        public override string ToString()
        {
            return _word.ToString();
        }
    }
}