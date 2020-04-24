using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Easy.MessageHub;
using Newtonsoft.Json;
using Remembrance.Contracts.Classification;
using Remembrance.Contracts.Classification.Data;
using Remembrance.Contracts.Classification.Data.UClassify;
using Remembrance.Core.Classification.ContractResolvers;
using Remembrance.Resources;
using Scar.Common.Messages;

namespace Remembrance.Core.Classification
{
    sealed class UClassifyTopicsClient : IDisposable, IClassificationClient
    {
        const string UserName = "uclassify";
        const string UriPart = UserName + "/Topics/classify";
        const string Token = "UDZpCiVwonVZ";
        static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { ContractResolver = new UClassifyTopicsResponseContractResolver() };

        readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.uclassify.com/v1/"),
            DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Token", Token) }
        };

        readonly IMessageHub _messageHub;

        public UClassifyTopicsClient(IMessageHub messageHub)
        {
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
        }

        public async Task<IEnumerable<ClassificationCategory>> GetCategoriesAsync(string text, CancellationToken cancellationToken)
        {
            _ = text ?? throw new ArgumentNullException(nameof(text));
            try
            {
                using var content = CreateHttpContent(new UClassifyTopicsRequest(text));
                var response = await _httpClient.PostAsync(UriPart, content, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"{response.StatusCode}: {response.ReasonPhrase}");
                }

                var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var convertedResponse = JsonConvert.DeserializeObject<IReadOnlyCollection<UClassifyTopicsResponseItem>>(result, SerializerSettings);
                if (convertedResponse == null)
                {
                    return Array.Empty<ClassificationCategory>();
                }

                return convertedResponse.SelectMany(x => x.Items.Select(y => new ClassificationCategory { ClassName = y.ClassName, Match = y.Match }).OrderByDescending(y => y.Match));
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is InvalidOperationException || ex is JsonException)
            {
                _messageHub.Publish(Errors.CannotCategorize.ToError(ex));
            }

            return Array.Empty<ClassificationCategory>();
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        static HttpContent CreateHttpContent(object content)
        {
            var ms = new MemoryStream();
            SerializeJsonIntoStream(content, ms);
            ms.Seek(0, SeekOrigin.Begin);
            var httpContent = new StreamContent(ms);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return httpContent;
        }

        static void SerializeJsonIntoStream(object value, Stream stream)
        {
            using var sw = new StreamWriter(stream, new UTF8Encoding(false), 1024, true);
            using var jtw = new JsonTextWriter(sw) { Formatting = Formatting.None };
            var js = new JsonSerializer();
            js.Serialize(jtw, value);
            jtw.Flush();
        }
    }
}
