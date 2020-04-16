using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.Moq;
using Easy.MessageHub;
using Moq;
using NUnit.Framework;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.SharedBetweenMachines;
using Remembrance.Contracts.ImageSearch;
using Remembrance.Contracts.ImageSearch.Data;
using Remembrance.ViewModel;
using Scar.Common.Async;
using Scar.Common.Messages;

namespace Remembrance.Test
{
    [TestFixture]
    [Parallelizable]
    [Apartment(ApartmentState.STA)]
    sealed class WordImageViewerViewModelTests
    {
        const string FallbackSearchText = "foo";

        const string ParentText = "bar";

        static readonly string DefaultSearchText = string.Format(CultureInfo.InvariantCulture, WordImageViewerViewModel.DefaultSearchTextTemplate, FallbackSearchText, ParentText);

        public void TearDown()
        {
            // TODO: Additional test
            // Assert.That(_sut.IsLoading, Is.False, "IsLoading should be false after each kind of processing");
        }

        [Test]
        public async Task IsLoadingIsTrueDuringProcessing()
        {
            // Arrange
            using var autoMock = CreateAutoMock();
            using var semaphore = new SemaphoreSlim(0, 1);
            var runningTask = Task.Run(async () => await semaphore.WaitAsync().ConfigureAwait(false));
            autoMock.Mock<ICancellationTokenSourceProvider>().Setup(x => x.ExecuteAsyncOperation(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<bool>())).Returns(runningTask);

            // Act
            var sut = await CreateViewModelAsync(autoMock, false, false).ConfigureAwait(false);

            // Assert
            Assert.That(sut.IsLoading, Is.True);
            semaphore.Release();
            await runningTask.ConfigureAwait(false);
        }

        [Test]
        public async Task PerformsReload()
        {
            // Arrange
            using var autoMock = CreateAutoMock();
            WithInitialEmptyImage(autoMock);
            WithSuccessfulImageSearch(autoMock);
            var sut = await CreateViewModelAsync(autoMock).ConfigureAwait(false);

            // Act
            await sut.ReloadImageAsync().ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(autoMock, Times.Once, DefaultSearchText);
            VerifyImagesSearch(autoMock, Times.Never, FallbackSearchText);
            VerifyDownloadCount(autoMock, Times.Once);
            Assert.That(sut.SearchIndex, Is.Zero);
            Assert.That(sut.IsAlternate, Is.False);
            Assert.That(sut.ThumbnailBytes, Is.Not.Null);
            Assert.That(sut.IsReloadVisible, Is.False);
        }

        [Test]
        public async Task ReloadIsNotVisibleDuringProcessing()
        {
            // Arrange
            using var autoMock = CreateAutoMock();
            using var semaphore = new SemaphoreSlim(0, 1);
            var runningTask = Task.Run(async () => await semaphore.WaitAsync().ConfigureAwait(false));
            autoMock.Mock<ICancellationTokenSourceProvider>().Setup(x => x.ExecuteAsyncOperation(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<bool>())).Returns(runningTask);

            // Act
            var sut = await CreateViewModelAsync(autoMock, false, false).ConfigureAwait(false);

            // Assert
            Assert.That(sut.IsReloadVisible, Is.False);
            semaphore.Release();
            await runningTask.ConfigureAwait(false);
        }

        [Test]
        public void When_Canceled_Throws()
        {
            // Arrange
            using var autoMock = CreateAutoMock();
            autoMock.Mock<ICancellationTokenSourceProvider>().Setup(x => x.ExecuteAsyncOperation(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<bool>())).Throws(new OperationCanceledException());

            // Assert
            Assert.That(
                async () =>
                {
                    // Act
                    await CreateViewModelAsync(autoMock).ConfigureAwait(false);
                },
                Throws.Exception.TypeOf<OperationCanceledException>());
        }

        [Test]
        public async Task When_DownloadsEmptyImage_ShouldTrySearchWithDifferentText()
        {
            // Arrange
            using var autoMock = CreateAutoMock();
            autoMock.Mock<IImageDownloader>().SetupSequence(x => x.DownloadImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(null).ReturnsAsync(new byte[1]);
            WithSuccessfulImageSearch(autoMock);

            // Act
            var sut = await CreateViewModelAsync(autoMock, false).ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(autoMock, Times.Once, DefaultSearchText);
            VerifyImagesSearch(autoMock, Times.Once, FallbackSearchText);
            VerifyDownloadCount(autoMock, () => Times.Exactly(2));
            Assert.That(sut.SearchIndex, Is.Zero);
            Assert.That(sut.IsAlternate, Is.True);
            Assert.That(sut.ThumbnailBytes, Is.Not.Null);
        }

        [Test]
        public async Task When_ErrorOccursDuringSearch_StopsSearch()
        {
            // Arrange
            using var autoMock = CreateAutoMock();
            WithImageSearchError(autoMock);

            // Act
            var sut = await CreateViewModelAsync(autoMock, false).ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(autoMock, Times.Once, DefaultSearchText);
            VerifyImagesSearch(autoMock, Times.Never, FallbackSearchText);
            VerifyDownloadCount(autoMock, Times.Never);
            Assert.That(sut.SearchIndex, Is.Zero);
            Assert.That(sut.IsAlternate, Is.False);
            Assert.That(sut.ThumbnailBytes, Is.Null);
        }

        [Test]
        public async Task When_GoingBackwards_AlternatesSearchTextForEveryRequest()
        {
            // Arrange
            using var autoMock = CreateAutoMock();
            WithInitialImage(autoMock, 2);
            WithSuccessfulImageSearch(autoMock);
            var sut = await CreateViewModelAsync(autoMock).ConfigureAwait(false);

            for (var i = 1; i >= 0; i--)
            {
                // Act
                Reset(autoMock);
                await sut.SetPreviousImageAsync().ConfigureAwait(false);

                // Assert
                VerifyImagesSearch(autoMock, Times.Once, FallbackSearchText);
                Assert.That(sut.SearchIndex, Is.EqualTo(i));
                Assert.That(sut.IsAlternate, Is.True);
                Assert.That(sut.ThumbnailBytes, Is.Not.Null);

                // Act
                Reset(autoMock);
                await sut.SetPreviousImageAsync().ConfigureAwait(false);

                // Assert
                VerifyImagesSearch(autoMock, Times.Once, DefaultSearchText);
                Assert.That(sut.SearchIndex, Is.EqualTo(i));
                Assert.That(sut.IsAlternate, Is.False);
                Assert.That(sut.ThumbnailBytes, Is.Not.Null);
            }
        }

        [Test]
        public async Task When_GoingForwards_AlternatesSearchTextForEveryRequest()
        {
            // Arrange
            using var autoMock = CreateAutoMock();
            WithInitialImage(autoMock, 0, true);
            WithSuccessfulImageSearch(autoMock);
            var sut = await CreateViewModelAsync(autoMock).ConfigureAwait(false);

            for (var i = 1; i <= 2; i++)
            {
                // Act
                Reset(autoMock);
                await sut.SetNextImageAsync().ConfigureAwait(false);

                // Assert
                VerifyImagesSearch(autoMock, Times.Once, DefaultSearchText);
                Assert.That(sut.SearchIndex, Is.EqualTo(i));
                Assert.That(sut.IsAlternate, Is.False);
                Assert.That(sut.ThumbnailBytes, Is.Not.Null);

                // Act
                Reset(autoMock);
                await sut.SetNextImageAsync().ConfigureAwait(false);

                // Assert
                VerifyImagesSearch(autoMock, Times.Once, FallbackSearchText);
                Assert.That(sut.SearchIndex, Is.EqualTo(i));
                Assert.That(sut.IsAlternate, Is.True);
                Assert.That(sut.ThumbnailBytes, Is.Not.Null);
            }
        }

        [Test]
        public async Task When_ImageBytesAreNull_Then_ShouldRepeatSearchInitially()
        {
            // Arrange
            using var autoMock = CreateAutoMock();
            WithInitialEmptyImage(autoMock);
            WithSuccessfulImageSearch(autoMock);

            // Act
            var sut = await CreateViewModelAsync(autoMock, false).ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(autoMock, Times.Once, DefaultSearchText);
            VerifyImagesSearch(autoMock, Times.Never, FallbackSearchText);
            VerifyDownloadCount(autoMock, Times.Once);
            Assert.That(sut.SearchIndex, Is.Zero);
            Assert.That(sut.IsAlternate, Is.False);
            Assert.That(sut.ThumbnailBytes, Is.Not.Null);
        }

        [Test]
        public async Task When_ImageIsEmpty_ReloadIsVisible()
        {
            // Arrange
            using var autoMock = CreateAutoMock();
            WithInitialEmptyImage(autoMock);

            // Act
            var sut = await CreateViewModelAsync(autoMock).ConfigureAwait(false);

            // Assert
            Assert.That(sut.ThumbnailBytes, Is.Null);
            Assert.That(sut.IsReloadVisible, Is.True);
        }

        [Test]
        public async Task When_IndexIsZero_And_IsAlternate_PreviousSearchShouldUseDefaultSearchText()
        {
            // Arrange
            using var autoMock = CreateAutoMock();
            WithInitialImage(autoMock, 0, true);
            WithSuccessfulImageSearch(autoMock);
            var sut = await CreateViewModelAsync(autoMock).ConfigureAwait(false);

            // Act
            await sut.SetPreviousImageAsync().ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(autoMock, Times.Once, DefaultSearchText);
            VerifyImagesSearch(autoMock, Times.Never, FallbackSearchText);
            VerifyDownloadCount(autoMock, Times.Once);
            Assert.That(sut.SearchIndex, Is.Zero);
            Assert.That(sut.IsAlternate, Is.False);
            Assert.That(sut.ThumbnailBytes, Is.Not.Null);
        }

        [Test]
        public async Task When_IndexIsZero_AndNot_IsAlternate_PreviousButton_Should_DoNothing()
        {
            // Arrange
            using var autoMock = CreateAutoMock();
            WithInitialImage(autoMock);
            WithSuccessfulImageSearch(autoMock);
            var sut = await CreateViewModelAsync(autoMock).ConfigureAwait(false);

            // Act
            await sut.SetPreviousImageAsync().ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(autoMock, Times.Never);
            VerifyDownloadCount(autoMock, Times.Never);
            Assert.That(sut.SearchIndex, Is.Zero);
            Assert.That(sut.IsAlternate, Is.False);
            Assert.That(sut.ThumbnailBytes, Is.Not.Null);
        }

        [Test]
        public async Task When_InitialImageIsEmpty_PerformsSearchWithDefaultSearchText()
        {
            // Arrange
            using var autoMock = CreateAutoMock();
            WithSuccessfulImageSearch(autoMock);

            // Act
            var sut = await CreateViewModelAsync(autoMock, false).ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(autoMock, Times.Once, DefaultSearchText);
            VerifyImagesSearch(autoMock, Times.Never, FallbackSearchText);
            VerifyDownloadCount(autoMock, Times.Once);
            Assert.That(sut.SearchIndex, Is.Zero);
            Assert.That(sut.IsAlternate, Is.False);
            Assert.That(sut.ThumbnailBytes, Is.Not.Null);
        }

        [Test]
        public async Task When_IsInitializedWithImage_NoSearchAndDownloadOccurs()
        {
            // Arrange
            using var autoMock = CreateAutoMock();
            WithInitialImage(autoMock);
            WithSuccessfulImageSearch(autoMock);

            // Act
            var sut = await CreateViewModelAsync(autoMock).ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(autoMock, Times.Never);
            VerifyDownloadCount(autoMock, Times.Never);
            Assert.That(sut.SearchIndex, Is.Zero);
            Assert.That(sut.IsAlternate, Is.False);
            Assert.That(sut.ThumbnailBytes, Is.Not.Null);
        }

        [Test]
        public async Task When_MoreThanOneImageIsReturned_Throws()
        {
            // Arrange
            using var autoMock = CreateAutoMock();
            WithInitialImage(autoMock);
            WithImageSearchResult(autoMock, new ImageInfo[2]);
            var sut = await CreateViewModelAsync(autoMock).ConfigureAwait(false);

            // Act
            // Assert
            Assert.That(() => sut.SetNextImageAsync(), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public async Task When_NoImagesAvailableForBothSearchTexts_PerformsTwoAttemptsWithDifferentSearchTexts()
        {
            // Arrange
            using var autoMock = CreateAutoMock();
            WithNoImageFound(autoMock);

            // Act
            var sut = await CreateViewModelAsync(autoMock, false).ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(autoMock, Times.Once, DefaultSearchText);
            VerifyImagesSearch(autoMock, Times.Once, FallbackSearchText);
            VerifyDownloadCount(autoMock, Times.Never);
            VerifyMessage(autoMock, Times.Once);
            Assert.That(sut.SearchIndex, Is.Zero);
            Assert.That(sut.IsAlternate, Is.True);
            Assert.That(sut.ThumbnailBytes, Is.Null);
        }

        [Test]
        public async Task When_OnlyDefaultSearchTextReturnsResults_IndexIncreasesEveryTime()
        {
            // Arrange
            using var autoMock = CreateAutoMock();
            WithSuccessfulImageSearch(autoMock, DefaultSearchText);
            WithNoImageFound(autoMock, FallbackSearchText);

            var sut = await CreateViewModelAsync(autoMock).ConfigureAwait(false); // this should search using only DefaultSearchText

            // Act
            await sut.SetNextImageAsync().ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(autoMock, Times.Once, DefaultSearchText);
            VerifyImagesSearch(autoMock, Times.Once, FallbackSearchText);
            VerifyDownloadCount(autoMock, Times.Once);

            Reset(autoMock);

            // Act
            await sut.SetNextImageAsync().ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(autoMock, Times.Once, DefaultSearchText);
            VerifyImagesSearch(autoMock, Times.Never, FallbackSearchText);
            VerifyDownloadCount(autoMock, Times.Once);
            Assert.That(sut.SearchIndex, Is.EqualTo(2));
            Assert.That(sut.IsAlternate, Is.False);
            Assert.That(sut.ThumbnailBytes, Is.Not.Null);
        }

        [Test]
        public async Task When_OnlyFallbackSearchTextReturnsResults_IndexIncreasesEveryTime()
        {
            // Arrange
            using var autoMock = CreateAutoMock();
            WithSuccessfulImageSearch(autoMock, FallbackSearchText);
            WithNoImageFound(autoMock, DefaultSearchText);

            var sut = await CreateViewModelAsync(autoMock).ConfigureAwait(false); // this should search using DefaultSearchText and then FallbackSearchText

            // Act
            await sut.SetNextImageAsync().ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(autoMock, Times.Never, DefaultSearchText);
            VerifyImagesSearch(autoMock, Times.Once, FallbackSearchText);
            VerifyDownloadCount(autoMock, Times.Once);

            // Act
            Reset(autoMock);
            await sut.SetNextImageAsync().ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(autoMock, Times.Never, DefaultSearchText);
            VerifyImagesSearch(autoMock, Times.Once, FallbackSearchText);
            VerifyDownloadCount(autoMock, Times.Once);
            Assert.That(sut.SearchIndex, Is.EqualTo(2));
            Assert.That(sut.IsAlternate, Is.True);
            Assert.That(sut.ThumbnailBytes, Is.Not.Null);
        }

        static AutoMock CreateAutoMock()
        {
            var autoMock = AutoMock.GetLoose();
            autoMock.Mock<ICancellationTokenSourceProvider>()
                .Setup(x => x.ExecuteAsyncOperation(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<bool>()))
                .Returns((Func<CancellationToken, Task> f, bool b) => f(CancellationToken.None));
            autoMock.Mock<IImageDownloader>().Setup(x => x.DownloadImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new byte[1]);
            return autoMock;
        }

        static ImageInfoWithBitmap CreateImageInfoWithBitmap(AutoMock autoMock)
        {
            var imageInfoWithBitmap = autoMock.Create<ImageInfoWithBitmap>();
            imageInfoWithBitmap.ThumbnailBitmap = new byte[1];
            imageInfoWithBitmap.ImageInfo = autoMock.Create<ImageInfo>();
            return imageInfoWithBitmap;
        }

        static WordKey CreateMockKey(AutoMock autoMock)
        {
            var key = autoMock.Create<WordKey>();
            var word = key.Word;
            word.Text = FallbackSearchText;
            var translationEntryKey = key.TranslationEntryKey;
            translationEntryKey.SourceLanguage = Constants.RuLanguage;
            translationEntryKey.TargetLanguage = Constants.EnLanguage;
            translationEntryKey.Text = word.Text;
            return key;
        }

        static async Task<WordImageViewerViewModel> CreateViewModelAsync(AutoMock autoMock, bool reset = true, bool wait = true)
        {
            var sut = autoMock.Create<WordImageViewerViewModel>(new TypedParameter(typeof(string), ParentText), new TypedParameter(typeof(WordKey), CreateMockKey(autoMock)));
            if (wait)
            {
                await sut.ConstructionTask.ConfigureAwait(false);
            }

            if (reset)
            {
                Reset(autoMock);
            }

            return sut;
        }

        static void Reset(AutoMock autoMock)
        {
            autoMock.Mock<IImageSearcher>().Reset();
            autoMock.Mock<IImageDownloader>().Reset();
        }

        static void VerifyDownloadCount(AutoMock autoMock, Func<Times> times)
        {
            autoMock.Mock<IImageDownloader>().Verify(x => x.DownloadImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), times);
        }

        static void VerifyImagesSearch(AutoMock autoMock, Func<Times> times, string? searchText = null)
        {
            autoMock.Mock<IImageSearcher>()
                .Verify(
                    x => x.SearchImagesAsync(It.Is<string>(s => (searchText == null) || (s == searchText)), It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()),
                    times);
        }

        static void VerifyMessage(AutoMock autoMock, Func<Times> times)
        {
            autoMock.Mock<IMessageHub>().Verify(x => x.Publish(It.IsAny<Message>()), times);
        }

        static void WithImageSearchError(AutoMock autoMock, string? searchText = null)
        {
            WithImageSearchResult(autoMock, null, searchText);
        }

        static void WithImageSearchResult(AutoMock autoMock, IReadOnlyCollection<ImageInfo>? imageInfos, string? searchText = null)
        {
            autoMock.Mock<IImageSearcher>()
                .Setup(x => x.SearchImagesAsync(It.Is<string>(s => (searchText == null) || (s == searchText)), It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(imageInfos);
        }

        static void WithInitialEmptyImage(AutoMock autoMock)
        {
            var image = autoMock.Create<ImageInfoWithBitmap>();
            image.ImageInfo = autoMock.Create<ImageInfo>();
            WithInitialImage(autoMock, customImage: image);
        }

        static void WithInitialImage(AutoMock autoMock, int searchIndex = 0, bool isAlternate = false, IReadOnlyCollection<int?>? notAvailableIndexes = null, ImageInfoWithBitmap? customImage = null)
        {
            var key = CreateMockKey(autoMock);
            autoMock.Mock<IWordImageInfoRepository>()
                .Setup(x => x.TryGetById(key))
                .Returns(new WordImageInfo(key, customImage ?? CreateImageInfoWithBitmap(autoMock), notAvailableIndexes ?? new int?[2]));
            autoMock.Mock<IWordImageSearchIndexRepository>().Setup(x => x.TryGetById(key)).Returns(new WordImageSearchIndex(key, searchIndex, isAlternate));
        }

        static void WithNoImageFound(AutoMock autoMock, string? searchText = null)
        {
            WithImageSearchResult(autoMock, Array.Empty<ImageInfo>(), searchText);
        }

        static void WithSuccessfulImageSearch(AutoMock autoMock, string? searchText = null)
        {
            WithImageSearchResult(
                autoMock,
                new[]
                {
                    autoMock.Create<ImageInfo>()
                },
                searchText);
        }
    }
}
