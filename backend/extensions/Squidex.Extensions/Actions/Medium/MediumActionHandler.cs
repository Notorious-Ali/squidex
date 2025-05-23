﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;
using Squidex.Domain.Apps.Core.HandleRules;
using Squidex.Domain.Apps.Core.Rules.EnrichedEvents;
using Squidex.Infrastructure.Http;
using Squidex.Infrastructure.Json;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Extensions.Actions.Medium;

public sealed class MediumActionHandler(RuleEventFormatter formatter, IHttpClientFactory httpClientFactory, IJsonSerializer serializer) : RuleActionHandler<MediumAction, MediumJob>(formatter)
{
    private const string Description = "Post to medium";

    private sealed class UserResponse
    {
        public UserResponseData Data { get; set; }
    }

    private sealed class UserResponseData
    {
        public string Id { get; set; }
    }

    protected override async Task<(string Description, MediumJob Data)> CreateJobAsync(EnrichedEvent @event, MediumAction action)
    {
        var ruleJob = new MediumJob { AccessToken = action.AccessToken, PublicationId = action.PublicationId };

        var requestBody = new
        {
            title = await FormatAsync(action.Title, @event),
            contentFormat = action.IsHtml ? "html" : "markdown",
            content = await FormatAsync(action.Content, @event),
            canonicalUrl = await FormatAsync(action.CanonicalUrl, @event),
            tags = await ParseTagsAsync(@event, action),
        };

        ruleJob.RequestBody = ToJson(requestBody);

        return (Description, ruleJob);
    }

    private async Task<string[]?> ParseTagsAsync(EnrichedEvent @event, MediumAction action)
    {
        if (string.IsNullOrWhiteSpace(action.Tags))
        {
            return null;
        }

        try
        {
            var jsonTags = await FormatAsync(action.Tags, @event);

            return serializer.Deserialize<string[]>(jsonTags!);
        }
        catch
        {
            return action.Tags.Split(',');
        }
    }

    protected override async Task<Result> ExecuteJobAsync(MediumJob job,
        CancellationToken ct = default)
    {
        var httpClient = httpClientFactory.CreateClient("MediumAction");

        string path;

        if (!string.IsNullOrWhiteSpace(job.PublicationId))
        {
            path = $"/v1/publications/{job.PublicationId}/posts";
        }
        else
        {
            HttpResponseMessage? response = null;

            var meRequest = BuildGetRequest(job, "/v1/me");
            try
            {
                response = await httpClient.SendAsync(meRequest, ct);

                var responseString = await response.Content.ReadAsStringAsync(ct);
                var responseJson = serializer.Deserialize<UserResponse>(responseString);

                var id = responseJson.Data?.Id;

                path = $"/v1/users/{id}/posts";
            }
            catch (Exception ex)
            {
                var requestDump = DumpFormatter.BuildDump(meRequest, response, ex.ToString());

                return Result.Failed(ex, requestDump);
            }
        }

        return await httpClient.OneWayRequestAsync(BuildPostRequest(job, path), job.RequestBody, ct);
    }

    private static HttpRequestMessage BuildPostRequest(MediumJob job, string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(job.RequestBody, Encoding.UTF8, "application/json"),
        };

        request.Headers.Add("Authorization", $"Bearer {job.AccessToken}");

        return request;
    }

    private static HttpRequestMessage BuildGetRequest(MediumJob job, string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);

        request.Headers.Add("Authorization", $"Bearer {job.AccessToken}");

        return request;
    }
}

public sealed class MediumJob
{
    public string RequestBody { get; set; }

    public string? PublicationId { get; set; }

    public string AccessToken { get; set; }
}
