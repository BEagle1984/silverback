// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Silverback.Tools.Generators.Docs.Contributors;

internal static class DocsGenerator
{
    [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "Fixed endpoint of this repository")]
    private const string ContributorsUrl = "https://api.github.com/repos/BEagle1984/silverback/contributors?per_page=100";

    private const string UserAccountType = "User";

    private const int AvatarSize = 64;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public static async Task GenerateContributorsGridAsync(string outputFilePath)
    {
        IEnumerable<GitHubContributor> contributors = await GetContributorsAsync().ConfigureAwait(false);

        StringBuilder builder = new();
        builder.AppendLine("<ul class=\"sb-contributors\">");

        foreach (GitHubContributor contributor in contributors)
        {
            builder.AppendLine(GetListItem(contributor));
        }

        builder.AppendLine("</ul>");

        await File.WriteAllTextAsync(outputFilePath, builder.ToString()).ConfigureAwait(false);
    }

    private static async Task<IEnumerable<GitHubContributor>> GetContributorsAsync()
    {
        using HttpClient httpClient = new();

        // The GitHub API rejects the requests without a user agent
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Silverback.Docs", "1.0"));

        List<GitHubContributor>? contributors = await httpClient
            .GetFromJsonAsync<List<GitHubContributor>>(ContributorsUrl, JsonOptions)
            .ConfigureAwait(false);

        return contributors?.Where(contributor => contributor.Type == UserAccountType) ??
               throw new InvalidOperationException("The GitHub API returned no contributors.");
    }

    private static string GetListItem(GitHubContributor contributor)
    {
        string querySeparator = contributor.AvatarUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";

        // Twice the displayed size, to stay sharp on high density screens
        string avatarUrl = $"{contributor.AvatarUrl}{querySeparator}s={AvatarSize * 2}";

        return $"<li><a href=\"{WebUtility.HtmlEncode(contributor.HtmlUrl)}\">" +
               $"<img src=\"{WebUtility.HtmlEncode(avatarUrl)}\" alt=\"\" width=\"{AvatarSize}\" height=\"{AvatarSize}\" loading=\"lazy\">" +
               $"<span>{WebUtility.HtmlEncode(contributor.Login)}</span>" +
               "</a></li>";
    }
}
