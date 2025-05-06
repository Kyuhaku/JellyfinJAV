namespace JellyfinJav.Providers.R18Provider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Jellyfin.Data.Enums;
    using JellyfinJav.Api;
    using MediaBrowser.Controller.Entities;
    using MediaBrowser.Controller.Entities.Movies;
    using MediaBrowser.Controller.Library;
    using MediaBrowser.Controller.Providers;
    using MediaBrowser.Model.Entities;
    using MediaBrowser.Model.Providers;
    using Microsoft.Extensions.Logging;

    /// <summary>The provider for R18 videos.</summary>
    public class R18Provider : IRemoteMetadataProvider<Movie, MovieInfo>, IHasOrder
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private readonly ILibraryManager libraryManager;
        private readonly ILogger<R18Provider> logger;

#pragma warning disable SA1614 // Element parameter documentation should have text
        /// <summary>
        /// Initializes a new instance of the <see cref="R18Provider"/> class.
        /// </summary>
        /// <param name="libraryManager"></param>
        /// <param name="logger"></param>
        public R18Provider(ILibraryManager libraryManager, ILogger<R18Provider> logger)
#pragma warning restore SA1614 // Element parameter documentation should have text
        {
            this.libraryManager = libraryManager;
            this.logger = logger;
        }

        /// <inheritdoc />
        public string Name => "R18";

        /// <inheritdoc />
        public int Order => 99;

        /// <inheritdoc />
        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancelToken)
        {
            var originalTitle = Utility.GetVideoOriginalTitle(info, this.libraryManager);
            info.Name = originalTitle;

            this.logger.LogInformation("[JellyfinJav] R18 - originalTitle: " + originalTitle);

            Api.Video? video;
            if (info.ProviderIds.ContainsKey("R18"))
            {
                video = await R18Client.LoadVideo(info.ProviderIds["R18"]).ConfigureAwait(false);
                this.logger.LogInformation("[JellyfinJav] R18 - Scanning Video: " + video);
            }
            else
            {
                var code = Utility.ExtractCodeFromFilename(originalTitle);
                if (code is null)
                {
                    this.logger.LogInformation("[JellyfinJav] R18 - Code is NULL " + code + "|" + originalTitle);
                    return new MetadataResult<Movie>();
                }

                video = await R18Client.SearchFirst(code).ConfigureAwait(false);
                this.logger.LogInformation("[JellyfinJav] R18 - Searching r18.dev: " + video);
            }

            if (!video.HasValue)
            {
                this.logger.LogInformation("[JellyfinJav] R18 - Oh Noes!" + video);
                return new MetadataResult<Movie>();
            }

            this.logger.LogInformation("[JellyfinJav] R18 - Found metadata: " + video);

            return new MetadataResult<Movie>
            {
                Item = new Movie
                {
                    OriginalTitle = info.Name,
                    Name = Utility.CreateVideoDisplayName(video.Value),
                    PremiereDate = video.Value.ReleaseDate,
                    ProviderIds = new Dictionary<string, string> { { "R18", video.Value.Id } },
                    Studios = new[] { video.Value.Studio },
                    Genres = video.Value.Genres.ToArray(),
                },
                People = AddActressesToPeople(video.Value),
                HasMetadata = true,
            };
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo info, CancellationToken cancelToken)
        {
            var javCode = Utility.ExtractCodeFromFilename(info.Name);
            if (string.IsNullOrEmpty(javCode))
            {
                return Array.Empty<RemoteSearchResult>();
            }

            this.logger.LogInformation("[JellyfinJav] R18 - Getting Code: " + javCode);

            var searchResults = await R18Client.Search(javCode);

            if (searchResults == null || !searchResults.Any())
            {
                this.logger.LogInformation("[JellyfinJav] R18 - No results found for code: " + javCode);
                return Array.Empty<RemoteSearchResult>(); // Return an empty collection, not null
            }

            return searchResults.Select(video => new RemoteSearchResult
            {
                Name = video.Code,
                ProviderIds = new Dictionary<string, string>
        {
            { "R18", video.Id },
        },
                ImageUrl = video.Cover?.ToString(),
            }).ToList();  // Convert to a List to avoid potential issues
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancelToken)
        {
            return await HttpClient.GetAsync(url, cancelToken).ConfigureAwait(false);
        }

        private static string NormalizeActressName(string name)
        {
            if (Plugin.Instance?.Configuration.ActressNameOrder == ActressNameOrder.LastFirst)
            {
                return string.Join(" ", name.Split(' ').Reverse());
            }

            return name;
        }

        private List<PersonInfo> AddActressesToPeople(Api.Video video)
        {
            var people = new List<PersonInfo>();

            if (video.Actresses == null || !video.Actresses.Any())
            {
                this.logger.LogInformation("[JellyfinJav] R18 - No actresses found in video metadata.");
                return people;
            }

            foreach (var actress in video.Actresses)
            {
                var person = new PersonInfo
                {
                    Name = NormalizeActressName(actress),
                    Type = PersonKind.Actor
                };
                AddPerson(person, people);
            }

            return people;
        }

        private void AddPerson(PersonInfo p, List<PersonInfo> people)
        {
            if (people == null)
            {
                people = new List<PersonInfo>();
            }

            PeopleHelper.AddPerson(people, p);
        }
    }
}