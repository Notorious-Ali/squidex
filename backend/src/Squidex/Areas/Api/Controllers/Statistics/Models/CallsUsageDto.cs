﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Domain.Apps.Entities.Billing;
using Squidex.Infrastructure.UsageTracking;

namespace Squidex.Areas.Api.Controllers.Statistics.Models;

public sealed class CallsUsageDto
{
    /// <summary>
    /// The total number of API calls.
    /// </summary>
    public long TotalCalls { get; set; }

    /// <summary>
    /// The total number of bytes transferred.
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// The total number of API calls this month.
    /// </summary>
    public long MonthCalls { get; set; }

    /// <summary>
    /// The total number of bytes transferred this month.
    /// </summary>
    public long MonthBytes { get; set; }

    /// <summary>
    /// The amount of calls that will block the app.
    /// </summary>
    public long BlockingApiCalls { get; set; }

    /// <summary>
    /// The included API traffic.
    /// </summary>
    public long AllowedBytes { get; set; }

    /// <summary>
    /// The included API calls.
    /// </summary>
    public long AllowedCalls { get; set; }

    /// <summary>
    /// The average duration in milliseconds.
    /// </summary>
    public double AverageElapsedMs { get; set; }

    /// <summary>
    /// The statistics by date and group.
    /// </summary>
    public Dictionary<string, CallsUsagePerDateDto[]> Details { get; set; }

    public static CallsUsageDto FromDomain(Plan plan, ApiStatsSummary summary, Dictionary<string, List<ApiStats>> details)
    {
        return new CallsUsageDto
        {
            AverageElapsedMs = summary.AverageElapsedMs,
            BlockingApiCalls = plan.BlockingApiCalls,
            AllowedBytes = plan.MaxApiBytes,
            AllowedCalls = plan.MaxApiCalls,
            TotalBytes = summary.TotalBytes,
            TotalCalls = summary.TotalCalls,
            MonthBytes = summary.MonthBytes,
            MonthCalls = summary.MonthCalls,
            Details = details.ToDictionary(x => x.Key, x => x.Value.Select(CallsUsagePerDateDto.FromDomain).ToArray()),
        };
    }
}
