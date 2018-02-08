using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Autofac.Extras.Moq;
using Easy.MessageHub;
using JetBrains.Annotations;
using Moq;
using NUnit.Framework;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.ImageSearch;
using Remembrance.Contracts.ImageSearch.Data;
using Remembrance.ViewModel.Card;
using Remembrance.ViewModel.Translation;
using Scar.Common.Async;
using Scar.Common.Events;
using Scar.Common.Messages;

namespace Remembrance.Test
{
    [TestFixture]
    internal sealed class WordImageViewerViewModelTest
    {
        private AutoMock _autoMock;
        private const string WordText = "foo";
        private const string ParentText = "bar";
        private WordKey _key;
        private static readonly string SearchText = string.Format(WordImageViewerViewModel.SearchTemplate, WordText, ParentText);

        [SetUp]
        public void SetUp()
        {
            _autoMock = AutoMock.GetLoose();
            _autoMock.Mock<ICancellationTokenSourceProvider>()
                .Setup(x => x.ExecuteAsyncOperation(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<bool>()))
                .Returns((Func<CancellationToken, Task> f, bool b) => f(CancellationToken.None));
            _key = _autoMock.Create<WordKey>();
            _autoMock.Mock<IWordPropertiesReveivable>().SetupGet(x => x.Text).Returns(WordText); _autoMock.Mock<IImageDownloader>()
                .Setup(x => x.DownloadImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new byte[]
                    {
                        1
                    });
            _autoMock.Mock<IImageDownloader>()
                .Setup(x => x.LoadImage(It.IsAny<byte[]>()))
                .Returns(new BitmapImage());
        }

        [TearDown]
        public void TearDown()
        {
            _autoMock.Dispose();
        }

        [Test]
        public void PerformsTwoAttemptsWithDifferentTexts_When_NoImagesAvailableForBoth()
        {
            //Arrange
            _autoMock.Mock<IImageSearcher>().Setup(x => x.SearchImagesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(new ImageInfo[0]);

            //Act
            var sut = CreateViewModel();

            //Assert
            _autoMock.Mock<IImageSearcher>().Verify(x => x.SearchImagesAsync(SearchText, It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Once);
            _autoMock.Mock<IImageSearcher>().Verify(x => x.SearchImagesAsync(WordText, It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Once);
            _autoMock.Mock<IImageDownloader>().Verify(x => x.DownloadImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _autoMock.Mock<IMessageHub>().Verify(x => x.Publish(It.IsAny<Message>()), Times.Once);
            Assert.IsFalse(sut.IsLoading);
            Assert.AreEqual(0, sut.SearchIndex);
            Assert.IsNull(sut.Image);
        }

        [Test]
        public void StopsSearch_When_ErrorOccursDuringSearch()
        {
            //Arrange
            _autoMock.Mock<IImageSearcher>().Setup(x => x.SearchImagesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync((ImageInfo[])null);

            //Act
            var sut = CreateViewModel();

            //Assert
            _autoMock.Mock<IImageSearcher>().Verify(x => x.SearchImagesAsync(SearchText, It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Once);
            _autoMock.Mock<IImageSearcher>().Verify(x => x.SearchImagesAsync(WordText, It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            _autoMock.Mock<IImageDownloader>().Verify(x => x.DownloadImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _autoMock.Mock<IMessageHub>().Verify(x => x.Publish(It.IsAny<Message>()), Times.Never);
            Assert.IsFalse(sut.IsLoading);
            Assert.AreEqual(0, sut.SearchIndex);
            Assert.IsNull(sut.Image);
        }

        [Test]
        public void TriesSearchWithDifferentText_When_ErrorOccursDuringDownload()
        {
        }

        [Test]
        public void AlternatesSearchText_For_EveryNewRequest()
        {
        }

        [Test]
        public void IndexIncreasesEveryTime_When_OnlySearchTextReturnsResults()
        {
            //Arrange
            _autoMock.Mock<IImageSearcher>().Setup(x => x.SearchImagesAsync(SearchText, It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(new []{_autoMock.Create<ImageInfo>()});
            _autoMock.Mock<IImageSearcher>().Setup(x => x.SearchImagesAsync(WordText, It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(new ImageInfo[0]);
            var sut = CreateViewModel();

            //Act
            sut.SetNextImageCommand.Execute(null);
            sut.SetNextImageCommand.Execute(null);

            //Assert
            _autoMock.Mock<IImageSearcher>().Verify(x => x.SearchImagesAsync(SearchText, It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Exactly(3));
            _autoMock.Mock<IImageSearcher>().Verify(x => x.SearchImagesAsync(WordText, It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Once);
            _autoMock.Mock<IImageDownloader>().Verify(x => x.DownloadImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            _autoMock.Mock<IMessageHub>().Verify(x => x.Publish(It.IsAny<Message>()), Times.Never);
            Assert.IsFalse(sut.IsLoading);
            Assert.AreEqual(2, sut.SearchIndex);
            Assert.IsNotNull(sut.Image);
        }

        [Test]
        public void IndexIncreasesEveryTime_When_OnlyWordTextReturnsResults()
        {
            //Arrange
            _autoMock.Mock<IImageSearcher>().Setup(x => x.SearchImagesAsync(SearchText, It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(new ImageInfo[0]);
            _autoMock.Mock<IImageSearcher>().Setup(x => x.SearchImagesAsync(WordText, It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(new[] { _autoMock.Create<ImageInfo>() });
            var sut = CreateViewModel();

            //Act
            sut.SetNextImageCommand.Execute(null);
            sut.SetNextImageCommand.Execute(null);

            //Assert
            _autoMock.Mock<IImageSearcher>().Verify(x => x.SearchImagesAsync(SearchText, It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Once);
            _autoMock.Mock<IImageSearcher>().Verify(x => x.SearchImagesAsync(WordText, It.IsAny<CancellationToken>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Exactly(3));
            _autoMock.Mock<IImageDownloader>().Verify(x => x.DownloadImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            _autoMock.Mock<IMessageHub>().Verify(x => x.Publish(It.IsAny<Message>()), Times.Never);
            Assert.IsFalse(sut.IsLoading);
            Assert.AreEqual(2, sut.SearchIndex);
            Assert.IsNotNull(sut.Image);
        }

        [NotNull]
        private WordImageViewerViewModel CreateViewModel()
        {
            var sut = _autoMock.Create<WordImageViewerViewModel>();
            _autoMock.Mock<IWordPropertiesReveivable>().Raise(m => m.ParentTextSet += null, new EventArgs<string>(ParentText));
            _autoMock.Mock<IWordPropertiesReveivable>().Raise(m => m.WordKeySet += null, new EventArgs<WordKey>(_key));
            return sut;
        }
    }
}