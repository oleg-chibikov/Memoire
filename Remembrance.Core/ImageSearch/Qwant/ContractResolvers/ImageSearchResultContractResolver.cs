using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Remembrance.Contracts.ImageSearch.Data;
using Remembrance.Contracts.ImageSearch.Data.Qwant;
using Remembrance.Core.ImageSearch.Qwant.JsonConverters;

namespace Remembrance.Core.ImageSearch.Qwant.ContractResolvers
{
    internal sealed class ImageSearchResultContractResolver : CustomContractResolver
    {
        protected override IReadOnlyDictionary<Type, JsonConverter>? PropertyConverters { get; } = new Dictionary<Type, JsonConverter>
        {
            { typeof(string), new UrlConverter() }
        };

        protected override IReadOnlyDictionary<string, string> PropertyMappings { get; } = new Dictionary<string, string>
        {
            { nameof(ImageInfo.Name), "title" },
            { nameof(ImageInfo.Url), "media" },
            { nameof(ImageInfo.ThumbnailUrl), "thumbnail" },
            { nameof(QwantResponse.Data), "data" },
            { nameof(QwantData.Result), "result" },
            { nameof(QwantResult.Items), "items" }
        };
    }
}