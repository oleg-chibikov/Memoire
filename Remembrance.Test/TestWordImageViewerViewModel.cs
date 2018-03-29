using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Autofac;
using Autofac.Extras.Moq;
using Easy.MessageHub;
using JetBrains.Annotations;
using Moq;
using NUnit.Framework;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.ImageSearch;
using Remembrance.Contracts.ImageSearch.Data;
using Remembrance.ViewModel.Card;
using Scar.Common.Async;
using Scar.Common.Messages;

namespace Remembrance.Test
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    internal sealed class WordImageViewerViewModelTest
    {
        private const string FallbackSearchText = "foo";

        private const string ParentText = "bar";

        private static readonly string DefaultSearchText = string.Format(WordImageViewerViewModel.DefaultSearchTextTemplate, FallbackSearchText, ParentText);

        private AutoMock _autoMock;

        private ImageInfoWithBitmap _imageInfoWithBitmap;

        private WordKey _key;

        [SetUp]
        public void SetUp()
        {
            _autoMock = AutoMock.GetLoose();
            _autoMock.Mock<ICancellationTokenSourceProvider>()
                .Setup(x => x.ExecuteAsyncOperation(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<bool>()))
                .Returns((Func<CancellationToken, Task> f, bool b) => f(CancellationToken.None));
            _key = _autoMock.Create<WordKey>();
            var wordMock = Mock.Get(_key.Word);
            wordMock.SetupGet(x => x.Text).Returns(FallbackSearchText);
            _imageInfoWithBitmap = _autoMock.Create<ImageInfoWithBitmap>();
            _imageInfoWithBitmap.ThumbnailBitmap = new byte[1];
            _imageInfoWithBitmap.ImageInfo = _autoMock.Create<ImageInfo>();
            _autoMock.Mock<IImageDownloader>().Setup(x => x.DownloadImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new byte[1]);
            _autoMock.Mock<IImageDownloader>().Setup(x => x.LoadImage(It.IsAny<byte[]>())).Returns(new BitmapImage());
        }

        [TearDown]
        public void TearDown()
        {
            _autoMock.Dispose();
        }

        [Test]
        [NotNull]
        public async Task TriesSearchWithDifferentText_When_ErrorOccursDuringDownload()
        {
            await Task.CompletedTask.ConfigureAwait(false);

            // TODO:
        }

        [Test]
        [NotNull]
        public async Task VerifiesIsLoadingWasSetToTrueAndWasSetToFalseOnlyWhenEverythingWasCompleted()
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [Test]
        [NotNull]
        public async Task When_ErrorOccursDuringSearch_StopsSearch()
        {
            // Arrange
            WithImageSearchError();

            // Act
            var sut = await CreateViewModelAsync(false).ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(Times.Once, DefaultSearchText);
            VerifyImagesSearch(Times.Never, FallbackSearchText);
            VerifyDownloadCount(Times.Never);
            Assert.IsFalse(sut.IsReverse);
            Assert.AreEqual(0, sut.SearchIndex);
            Assert.That(sut.Image, Is.Null);
        }

        [Test]
        [NotNull]
        public async Task When_GoingBackwards_AlternatesSearchTextForEveryRequest()
        {
            // Arrange
            WithInitialImage(2);
            WithSuccessfulImageSearch();
            var sut = await CreateViewModelAsync().ConfigureAwait(false);

            for (var i = 1; i >= 0; i--)
            {
                // Act
                Reset();
                await sut.SetPreviousImageAsync().ConfigureAwait(false);

                // Assert
                VerifyImagesSearch(Times.Once, FallbackSearchText);
                Assert.IsTrue(sut.IsReverse);
                Assert.AreEqual(i, sut.SearchIndex);
                Assert.That(sut.Image, Is.Not.Null);

                // Act
                Reset();
                await sut.SetPreviousImageAsync().ConfigureAwait(false);

                // Assert
                VerifyImagesSearch(Times.Once, DefaultSearchText);
                Assert.IsFalse(sut.IsReverse);
                Assert.AreEqual(i, sut.SearchIndex);
                Assert.That(sut.Image, Is.Not.Null);
            }
        }

        [Test]
        [NotNull]
        public async Task When_GoingForwards_AlternatesSearchTextForEveryRequest()
        {
            // Arrange
            WithInitialImage(0, true);
            WithSuccessfulImageSearch();
            var sut = await CreateViewModelAsync().ConfigureAwait(false);

            for (var i = 1; i <= 2; i++)
            {
                // Act
                Reset();
                await sut.SetNextImageAsync().ConfigureAwait(false);

                // Assert
                VerifyImagesSearch(Times.Once, DefaultSearchText);
                Assert.IsFalse(sut.IsReverse);
                Assert.AreEqual(i, sut.SearchIndex);
                Assert.That(sut.Image, Is.Not.Null);

                // Act
                Reset();
                await sut.SetNextImageAsync().ConfigureAwait(false);

                // Assert
                VerifyImagesSearch(Times.Once, FallbackSearchText);
                Assert.IsTrue(sut.IsReverse);
                Assert.AreEqual(i, sut.SearchIndex);
                Assert.That(sut.Image, Is.Not.Null);
            }
        }

        [Test]
        [NotNull]
        public async Task When_IndexIsZero_And_IsReverse_PreviousSearchShouldUseDefaultSearchText()
        {
            // Arrange
            WithInitialImage(0, true);
            WithSuccessfulImageSearch();
            var sut = await CreateViewModelAsync().ConfigureAwait(false);

            // Act
            await sut.SetPreviousImageAsync().ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(Times.Once, DefaultSearchText);
            VerifyImagesSearch(Times.Never, FallbackSearchText);
            VerifyDownloadCount(Times.Once);
            Assert.IsFalse(sut.IsReverse);
            Assert.AreEqual(0, sut.SearchIndex);
            Assert.That(sut.Image, Is.Not.Null);
        }

        [Test]
        [NotNull]
        public async Task When_IndexIsZero_AndNot_IsReverse_PreviousButton_Should_DoNothing()
        {
            // Arrange
            WithInitialImage();
            WithSuccessfulImageSearch();
            var sut = await CreateViewModelAsync().ConfigureAwait(false);

            // Act
            await sut.SetPreviousImageAsync().ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(Times.Never);
            VerifyDownloadCount(Times.Never);
            Assert.IsFalse(sut.IsReverse);
            Assert.AreEqual(0, sut.SearchIndex);
            Assert.That(sut.Image, Is.Not.Null);
        }

        [Test]
        [NotNull]
        public async Task When_InitialImageIsEmpty_PerformsSearchWithDefaultSearchText()
        {
            // Arrange
            WithSuccessfulImageSearch();

            // Act
            var sut = await CreateViewModelAsync(false).ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(Times.Once, DefaultSearchText);
            VerifyImagesSearch(Times.Never, FallbackSearchText);
            VerifyDownloadCount(Times.Once);
            Assert.IsFalse(sut.IsReverse);
            Assert.AreEqual(0, sut.SearchIndex);
            Assert.That(sut.Image, Is.Not.Null);
        }

        [Test]
        [NotNull]
        public async Task When_IsInitialized_NoSearchAndDownloadOccurs()
        {
            // Arrange
            WithInitialImage();
            WithSuccessfulImageSearch();

            // Act
            var sut = await CreateViewModelAsync().ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(Times.Never);
            VerifyDownloadCount(Times.Never);
            Assert.IsFalse(sut.IsReverse);
            Assert.AreEqual(0, sut.SearchIndex);
            Assert.That(sut.Image, Is.Not.Null);
        }

        [Test]
        [NotNull]
        public async Task When_MoreThanOneImageIsReturned_Throws()
        {
            // Arrange
            WithInitialImage();
            WithImageSearchResult(new ImageInfo[2]);
            var sut = await CreateViewModelAsync().ConfigureAwait(false);

            // Act
            // Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => sut.SetNextImageAsync());
        }

        [Test]
        [NotNull]
        public async Task When_NoImagesAvailableForBothSearchTexts_PerformsTwoAttemptsWithDifferentSearchTexts()
        {
            // Arrange
            WithNoImageFound();

            // Act
            var sut = await CreateViewModelAsync(false).ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(Times.Once, DefaultSearchText);
            VerifyImagesSearch(Times.Once, FallbackSearchText);
            VerifyDownloadCount(Times.Never);
            VerifyMessage(Times.Once);
            Assert.IsTrue(sut.IsReverse);
            Assert.AreEqual(0, sut.SearchIndex);
            Assert.That(sut.Image, Is.Null);
        }

        [Test]
        [NotNull]
        public async Task When_OnlyDefaultSearchTextReturnsResults_IndexIncreasesEveryTime()
        {
            // Arrange
            WithSuccessfulImageSearch(DefaultSearchText);
            WithNoImageFound(FallbackSearchText);

            var sut = await CreateViewModelAsync().ConfigureAwait(false); // this should search using only DefaultSearchText

            // Act
            await sut.SetNextImageAsync().ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(Times.Once, DefaultSearchText);
            VerifyImagesSearch(Times.Once, FallbackSearchText);
            VerifyDownloadCount(Times.Once);

            Reset();

            // Act
            await sut.SetNextImageAsync().ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(Times.Once, DefaultSearchText);
            VerifyImagesSearch(Times.Never, FallbackSearchText);
            VerifyDownloadCount(Times.Once);
            Assert.IsFalse(sut.IsReverse);
            Assert.AreEqual(2, sut.SearchIndex);
            Assert.That(sut.Image, Is.Not.Null);
        }

        [Test]
        [NotNull]
        public async Task When_OnlyFallbackSearchTextReturnsResults_IndexIncreasesEveryTime()
        {
            // Arrange
            WithSuccessfulImageSearch(FallbackSearchText);
            WithNoImageFound(DefaultSearchText);

            var sut = await CreateViewModelAsync().ConfigureAwait(false); // this should search using DefaultSearchText and then FallbackSearchText

            // Act
            await sut.SetNextImageAsync().ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(Times.Never, DefaultSearchText);
            VerifyImagesSearch(Times.Once, FallbackSearchText);
            VerifyDownloadCount(Times.Once);

            // Act
            Reset();
            await sut.SetNextImageAsync().ConfigureAwait(false);

            // Assert
            VerifyImagesSearch(Times.Never, DefaultSearchText);
            VerifyImagesSearch(Times.Once, FallbackSearchText);
            VerifyDownloadCount(Times.Once);
            Assert.IsTrue(sut.IsReverse);
            Assert.AreEqual(2, sut.SearchIndex);
            Assert.That(sut.Image, Is.Not.Null);
        }

        [ItemNotNull]
        [NotNull]
        private async Task<WordImageViewerViewModel> CreateViewModelAsync(bool reset = true)
        {
            var sut = _autoMock.Create<WordImageViewerViewModel>(new TypedParameter(typeof(string), ParentText), new TypedParameter(typeof(WordKey), _key));
            await sut.ConstructionTask.ConfigureAwait(false);
            if (reset)
            {
                Reset();
            }

            return sut;
        }

        private void Reset()
        {
            _autoMock.Mock<IImageSearcher>().ResetCalls();
            _autoMock.Mock<IImageDownloader>().ResetCalls();
        }

        private void VerifyDownloadCount([NotNull] Func<Times> times)
        {
            _autoMock.Mock<IImageDownloader>().Verify(x => x.DownloadImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), times);
        }

        private void VerifyImagesSearch([NotNull] Func<Times> times, [CanBeNull] string searchText = null)
        {
            _autoMock.Mock<IImageSearcher>()
                .Verify(x => x.SearchImagesAsync(It.Is<string>(s => searchText == null || s == searchText), It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), times);
        }

        private void VerifyMessage([NotNull] Func<Times> times)
        {
            _autoMock.Mock<IMessageHub>().Verify(x => x.Publish(It.IsAny<Message>()), times);
        }

        private void WithImageSearchError([CanBeNull] string searchText = null)
        {
            WithImageSearchResult(null, searchText);
        }

        private void WithImageSearchResult([CanBeNull] ImageInfo[] imageInfos, [CanBeNull] string searchText = null)
        {
            _autoMock.Mock<IImageSearcher>()
                .Setup(x => x.SearchImagesAsync(It.Is<string>(s => searchText == null || s == searchText), It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(imageInfos);
        }

        private void WithInitialImage(int searchIndex = 0, bool isReverse = false, [CanBeNull] int?[] notAvailableIndexes = null)
        {
            _autoMock.Mock<IWordImagesInfoRepository>().Setup(x => x.TryGetById(_key)).Returns(new WordImageInfo(_key, searchIndex, _imageInfoWithBitmap, isReverse, notAvailableIndexes ?? new int?[2]));
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
    }
}