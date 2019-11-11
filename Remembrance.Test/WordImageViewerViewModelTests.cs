using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.Moq;
using Easy.MessageHub;
using JetBrains.Annotations;
using Moq;
using NUnit.Framework;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.ImageSearch;
using Remembrance.Contracts.ImageSearch.Data;
using Remembrance.ViewModel;
using Scar.Common.Async;
using Scar.Common.Messages;

namespace Remembrance.Test
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class WordImageViewerViewModelTests
    {
        [SetUp]
        public void SetUp()
        {
            _autoMock = AutoMock.GetLoose();
            _autoMock.Mock<ICancellationTokenSourceProvider>()
                .Setup(x => x.ExecuteAsyncOperation(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<bool>()))
                .Returns((Func<CancellationToken, Task> f, bool b) => f(CancellationToken.None));
            _key = _autoMock.Create<WordKey>();
            var word = _key.Word;
            word.Text = FallbackSearchText;
            _imageInfoWithBitmap = _autoMock.Create<ImageInfoWithBitmap>();
            _imageInfoWithBitmap.ThumbnailBitmap = new byte[1];
            _imageInfoWithBitmap.ImageInfo = _autoMock.Create<ImageInfo>();
            _autoMock.Mock<IImageDownloader>().Setup(x => x.DownloadImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new byte[1]);
        }

        [TearDown]
        public void TearDown()
        {
            Assert.That(_sut.IsLoading, Is.False, "IsLoading should be false after each kind of processing");
            _autoMock.Dispose();
        }

        private const string FallbackSearchText = "foo";

        private const string ParentText = "bar";

        private static readonly string DefaultSearchText = string.Format(WordImageViewerViewModel.DefaultSearchTextTemplate, FallbackSearchText, ParentText);

        private AutoMock _autoMock;

        private ImageInfoWithBitmap _imageInfoWithBitmap;

        private WordKey _key;

        private WordImageViewerViewModel _sut;

        [ItemNotNull]
        [NotNull]
        private async Task<WordImageViewerViewModel> CreateViewModelAsync(bool reset = true, bool wait = true)
        {
            _sut = _autoMock.Create<WordImageViewerViewModel>(new TypedParameter(typeof(string), ParentText), new TypedParameter(typeof(WordKey), _key));
            if (wait)
            {
                await _sut.ConstructionTask.ConfigureAwait(false);
            }

            if (reset)
            {
                Reset();
            }

            return _sut;
        }

        private void Reset()
        {
            _autoMock.Mock<IImageSearcher>().Invocations.Clear();
            _autoMock.Mock<IImageDownloader>().Invocations.Clear();
        }

        private void VerifyDownloadCount([NotNull] Func<Times> times)
        {
            _autoMock.Mock<IImageDownloader>().Verify(x => x.DownloadImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), times);
        }

        private void VerifyImagesSearch([NotNull] Func<Times> times, [CanBeNull] string searchText = null)
        {
            _autoMock.Mock<IImageSearcher>()
                .Verify(
                    x => x.SearchImagesAsync(
                        It.Is<string>(s => searchText == null || s == searchText),
                        It.IsAny<CancellationToken>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<string>()),
                    times);
        }

        private void VerifyMessage([NotNull] Func<Times> times)
        {
            _autoMock.Mock<IMessageHub>().Verify(x => x.Publish(It.IsAny<Message>()), times);
        }

        private void WithImageSearchError([CanBeNull] string searchText = null)
        {
            WithImageSearchResult(null, searchText);
        }

        private void WithImageSearchResult([CanBeNull] IReadOnlyCollection<ImageInfo> imageInfos, [CanBeNull] string searchText = null)
        {
            _autoMock.Mock<IImageSearcher>()
                .Setup(
                    x => x.SearchImagesAsync(
                        It.Is<string>(s => searchText == null || s == searchText),
                        It.IsAny<CancellationToken>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<string>()))
                .ReturnsAsync(imageInfos);
        }

        private void WithInitialEmptyImage()
        {
            var image = _autoMock.Create<ImageInfoWithBitmap>();
            image.ImageInfo = _autoMock.Create<ImageInfo>();
            WithInitialImage(customImage: image);
        }

        private void WithInitialImage(
            int searchIndex = 0,
            bool isAlternate = false,
            [CanBeNull] int?[] notAvailableIndexes = null,
            [CanBeNull] ImageInfoWithBitmap customImage = null)
        {
            _autoMock.Mock<IWordImageInfoRepository>()
                .Setup(x => x.TryGetById(_key))
                .Returns(new WordImageInfo(_key, customImage ?? _imageInfoWithBitmap, notAvailableIndexes ?? new int?[2]));
            _autoMock.Mock<IWordImageSearchIndexRepository>().Setup(x => x.TryGetById(_key)).Returns(new WordImageSearchIndex(_key, searchIndex, isAlternate));
        }

        private void WithNoImageFound([CanBeNull] string searchText = null)
        {
            WithImageSearchResult(new ImageInfo[0], searchText);
        }

        private void WithSuccessfulImageSearch([CanBeNull] string searchText = null)
        {
            WithImageSearchResult(
                new[]
                {
                    _autoMock.Create<ImageInfo>()
                },
                searchText);
        }

        [Test]
        [NotNull]
        public async Task IsLoadingIsTrueDuringProcessing()
        {
            // Arrange
            var semaphore = new SemaphoreSlim(0, 1);
            var runningTask = Task.Run(async () => await semaphore.WaitAsync().ConfigureAwait(false));
            _autoMock.Mock<ICancellationTokenSourceProvider>()
                .Setup(x => x.ExecuteAsyncOperation(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<bool>()))
                .Returns(runningTask);

            // Act
            _sut = await CreateViewModelAsync(false, false).ConfigureAwait(false);

            // Assert
            Assert.That(_sut.IsLoading, Is.True);
            semaphore.Release();
            await runningTask.ConfigureAwait(false);
        }

        [Test]
        [NotNull]
        public async Task PerformsReload()
        {
            // Arrange
            WithInitialEmptyImage();
            WithSuccessfulImageSearch();
            _sut = await CreateViewModelAsync().ConfigureAwait(false);

            // Act
            await _sut.ReloadImageAsync().ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(Times.Once, DefaultSearchText);
            VerifyImagesSearch(Times.Never, FallbackSearchText);
            VerifyDownloadCount(Times.Once);
            Assert.That(_sut.SearchIndex, Is.Zero);
            Assert.That(_sut.IsAlternate, Is.False);
            Assert.That(_sut.ThumbnailBytes, Is.Not.Null);
            Assert.That(_sut.IsReloadVisible, Is.False);
        }

        [Test]
        [NotNull]
        public async Task ReloadIsNotVisibleDuringProcessing()
        {
            // Arrange
            var semaphore = new SemaphoreSlim(0, 1);
            var runningTask = Task.Run(async () => await semaphore.WaitAsync().ConfigureAwait(false));
            _autoMock.Mock<ICancellationTokenSourceProvider>()
                .Setup(x => x.ExecuteAsyncOperation(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<bool>()))
                .Returns(runningTask);

            // Act
            _sut = await CreateViewModelAsync(false, false).ConfigureAwait(false);

            // Assert
            Assert.That(_sut.IsReloadVisible, Is.False);
            semaphore.Release();
            await runningTask.ConfigureAwait(false);
        }

        [Test]
        public void When_Canceled_Throws()
        {
            // Arrange
            _autoMock.Mock<ICancellationTokenSourceProvider>()
                .Setup(x => x.ExecuteAsyncOperation(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<bool>()))
                .ThrowsAsync(new OperationCanceledException());

            // Assert
            Assert.That(
                async () =>
                {
                    // Act
                    _sut = await CreateViewModelAsync().ConfigureAwait(false);
                },
                Throws.Exception.TypeOf<TaskCanceledException>());
        }

        [Test]
        [NotNull]
        public async Task When_DownloadsEmptyImage_ShouldTrySearchWithDifferentText()
        {
            // Arrange
            _autoMock.Mock<IImageDownloader>()
                .SetupSequence(x => x.DownloadImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null)
                .ReturnsAsync(new byte[1]);
            WithSuccessfulImageSearch();

            // Act
            _sut = await CreateViewModelAsync(false).ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(Times.Once, DefaultSearchText);
            VerifyImagesSearch(Times.Once, FallbackSearchText);
            VerifyDownloadCount(() => Times.Exactly(2));
            Assert.That(_sut.SearchIndex, Is.Zero);
            Assert.That(_sut.IsAlternate, Is.True);
            Assert.That(_sut.ThumbnailBytes, Is.Not.Null);
        }

        [Test]
        [NotNull]
        public async Task When_ErrorOccursDuringSearch_StopsSearch()
        {
            // Arrange
            WithImageSearchError();

            // Act
            _sut = await CreateViewModelAsync(false).ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(Times.Once, DefaultSearchText);
            VerifyImagesSearch(Times.Never, FallbackSearchText);
            VerifyDownloadCount(Times.Never);
            Assert.That(_sut.SearchIndex, Is.Zero);
            Assert.That(_sut.IsAlternate, Is.False);
            Assert.That(_sut.ThumbnailBytes, Is.Null);
        }

        [Test]
        [NotNull]
        public async Task When_GoingBackwards_AlternatesSearchTextForEveryRequest()
        {
            // Arrange
            WithInitialImage(2);
            WithSuccessfulImageSearch();
            _sut = await CreateViewModelAsync().ConfigureAwait(false);

            for (var i = 1; i >= 0; i--)
            {
                // Act
                Reset();
                await _sut.SetPreviousImageAsync().ConfigureAwait(false);

                // Assert
                VerifyImagesSearch(Times.Once, FallbackSearchText);
                Assert.That(_sut.SearchIndex, Is.EqualTo(i));
                Assert.That(_sut.IsAlternate, Is.True);
                Assert.That(_sut.ThumbnailBytes, Is.Not.Null);

                // Act
                Reset();
                await _sut.SetPreviousImageAsync().ConfigureAwait(false);

                // Assert
                VerifyImagesSearch(Times.Once, DefaultSearchText);
                Assert.That(_sut.SearchIndex, Is.EqualTo(i));
                Assert.That(_sut.IsAlternate, Is.False);
                Assert.That(_sut.ThumbnailBytes, Is.Not.Null);
            }
        }

        [Test]
        [NotNull]
        public async Task When_GoingForwards_AlternatesSearchTextForEveryRequest()
        {
            // Arrange
            WithInitialImage(0, true);
            WithSuccessfulImageSearch();
            _sut = await CreateViewModelAsync().ConfigureAwait(false);

            for (var i = 1; i <= 2; i++)
            {
                // Act
                Reset();
                await _sut.SetNextImageAsync().ConfigureAwait(false);

                // Assert
                VerifyImagesSearch(Times.Once, DefaultSearchText);
                Assert.That(_sut.SearchIndex, Is.EqualTo(i));
                Assert.That(_sut.IsAlternate, Is.False);
                Assert.That(_sut.ThumbnailBytes, Is.Not.Null);

                // Act
                Reset();
                await _sut.SetNextImageAsync().ConfigureAwait(false);

                // Assert
                VerifyImagesSearch(Times.Once, FallbackSearchText);
                Assert.That(_sut.SearchIndex, Is.EqualTo(i));
                Assert.That(_sut.IsAlternate, Is.True);
                Assert.That(_sut.ThumbnailBytes, Is.Not.Null);
            }
        }

        [Test]
        [NotNull]
        public async Task When_ImageBytesAreNull_Then_ShouldRepeatSearchInitially()
        {
            // Arrange
            WithInitialEmptyImage();
            WithSuccessfulImageSearch();

            // Act
            _sut = await CreateViewModelAsync(false).ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(Times.Once, DefaultSearchText);
            VerifyImagesSearch(Times.Never, FallbackSearchText);
            VerifyDownloadCount(Times.Once);
            Assert.That(_sut.SearchIndex, Is.Zero);
            Assert.That(_sut.IsAlternate, Is.False);
            Assert.That(_sut.ThumbnailBytes, Is.Not.Null);
        }

        [Test]
        [NotNull]
        public async Task When_ImageIsEmpty_ReloadIsVisible()
        {
            // Arrange
            WithInitialEmptyImage();

            // Act
            _sut = await CreateViewModelAsync().ConfigureAwait(false);

            // Assert
            Assert.That(_sut.ThumbnailBytes, Is.Null);
            Assert.That(_sut.IsReloadVisible, Is.True);
        }

        [Test]
        [NotNull]
        public async Task When_IndexIsZero_And_IsAlternate_PreviousSearchShouldUseDefaultSearchText()
        {
            // Arrange
            WithInitialImage(0, true);
            WithSuccessfulImageSearch();
            _sut = await CreateViewModelAsync().ConfigureAwait(false);

            // Act
            await _sut.SetPreviousImageAsync().ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(Times.Once, DefaultSearchText);
            VerifyImagesSearch(Times.Never, FallbackSearchText);
            VerifyDownloadCount(Times.Once);
            Assert.That(_sut.SearchIndex, Is.Zero);
            Assert.That(_sut.IsAlternate, Is.False);
            Assert.That(_sut.ThumbnailBytes, Is.Not.Null);
        }

        [Test]
        [NotNull]
        public async Task When_IndexIsZero_AndNot_IsAlternate_PreviousButton_Should_DoNothing()
        {
            // Arrange
            WithInitialImage();
            WithSuccessfulImageSearch();
            _sut = await CreateViewModelAsync().ConfigureAwait(false);

            // Act
            await _sut.SetPreviousImageAsync().ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(Times.Never);
            VerifyDownloadCount(Times.Never);
            Assert.That(_sut.SearchIndex, Is.Zero);
            Assert.That(_sut.IsAlternate, Is.False);
            Assert.That(_sut.ThumbnailBytes, Is.Not.Null);
        }

        [Test]
        [NotNull]
        public async Task When_InitialImageIsEmpty_PerformsSearchWithDefaultSearchText()
        {
            // Arrange
            WithSuccessfulImageSearch();

            // Act
            _sut = await CreateViewModelAsync(false).ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(Times.Once, DefaultSearchText);
            VerifyImagesSearch(Times.Never, FallbackSearchText);
            VerifyDownloadCount(Times.Once);
            Assert.That(_sut.SearchIndex, Is.Zero);
            Assert.That(_sut.IsAlternate, Is.False);
            Assert.That(_sut.ThumbnailBytes, Is.Not.Null);
        }

        [Test]
        [NotNull]
        public async Task When_IsInitializedWithImage_NoSearchAndDownloadOccurs()
        {
            // Arrange
            WithInitialImage();
            WithSuccessfulImageSearch();

            // Act
            _sut = await CreateViewModelAsync().ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(Times.Never);
            VerifyDownloadCount(Times.Never);
            Assert.That(_sut.SearchIndex, Is.Zero);
            Assert.That(_sut.IsAlternate, Is.False);
            Assert.That(_sut.ThumbnailBytes, Is.Not.Null);
        }

        [Test]
        [NotNull]
        public async Task When_MoreThanOneImageIsReturned_Throws()
        {
            // Arrange
            WithInitialImage();
            WithImageSearchResult(new ImageInfo[2]);
            _sut = await CreateViewModelAsync().ConfigureAwait(false);

            // Act
            // Assert
            Assert.That(() => _sut.SetNextImageAsync(), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        [NotNull]
        public async Task When_NoImagesAvailableForBothSearchTexts_PerformsTwoAttemptsWithDifferentSearchTexts()
        {
            // Arrange
            WithNoImageFound();

            // Act
            _sut = await CreateViewModelAsync(false).ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(Times.Once, DefaultSearchText);
            VerifyImagesSearch(Times.Once, FallbackSearchText);
            VerifyDownloadCount(Times.Never);
            VerifyMessage(Times.Once);
            Assert.That(_sut.SearchIndex, Is.Zero);
            Assert.That(_sut.IsAlternate, Is.True);
            Assert.That(_sut.ThumbnailBytes, Is.Null);
        }

        [Test]
        [NotNull]
        public async Task When_OnlyDefaultSearchTextReturnsResults_IndexIncreasesEveryTime()
        {
            // Arrange
            WithSuccessfulImageSearch(DefaultSearchText);
            WithNoImageFound(FallbackSearchText);

            _sut = await CreateViewModelAsync().ConfigureAwait(false); // this should search using only DefaultSearchText

            // Act
            await _sut.SetNextImageAsync().ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(Times.Once, DefaultSearchText);
            VerifyImagesSearch(Times.Once, FallbackSearchText);
            VerifyDownloadCount(Times.Once);

            Reset();

            // Act
            await _sut.SetNextImageAsync().ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(Times.Once, DefaultSearchText);
            VerifyImagesSearch(Times.Never, FallbackSearchText);
            VerifyDownloadCount(Times.Once);
            Assert.That(_sut.SearchIndex, Is.EqualTo(2));
            Assert.That(_sut.IsAlternate, Is.False);
            Assert.That(_sut.ThumbnailBytes, Is.Not.Null);
        }

        [Test]
        [NotNull]
        public async Task When_OnlyFallbackSearchTextReturnsResults_IndexIncreasesEveryTime()
        {
            // Arrange
            WithSuccessfulImageSearch(FallbackSearchText);
            WithNoImageFound(DefaultSearchText);

            _sut = await CreateViewModelAsync().ConfigureAwait(false); // this should search using DefaultSearchText and then FallbackSearchText

            // Act
            await _sut.SetNextImageAsync().ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(Times.Never, DefaultSearchText);
            VerifyImagesSearch(Times.Once, FallbackSearchText);
            VerifyDownloadCount(Times.Once);

            // Act
            Reset();
            await _sut.SetNextImageAsync().ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(Times.Never, DefaultSearchText);
            VerifyImagesSearch(Times.Once, FallbackSearchText);
            VerifyDownloadCount(Times.Once);
            Assert.That(_sut.SearchIndex, Is.EqualTo(2));
            Assert.That(_sut.IsAlternate, Is.True);
            Assert.That(_sut.ThumbnailBytes, Is.Not.Null);
        }
    }
}