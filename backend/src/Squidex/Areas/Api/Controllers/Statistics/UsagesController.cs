﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using Squidex.Areas.Api.Controllers.Statistics.Models;
using Squidex.Domain.Apps.Entities.Apps;
using Squidex.Domain.Apps.Entities.Assets;
using Squidex.Domain.Apps.Entities.Billing;
using Squidex.Hosting;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Commands;
using Squidex.Infrastructure.UsageTracking;
using Squidex.Shared;
using Squidex.Web;

namespace Squidex.Areas.Api.Controllers.Statistics;

/// <summary>
/// Retrieves usage information for apps and teams.
/// </summary>
[ApiExplorerSettings(GroupName = nameof(Statistics))]
public sealed class UsagesController(
    ICommandBus commandBus,
    IDataProtectionProvider dataProtection,
    IApiUsageTracker usageTracker,
    IAppLogStore usageLog,
    IUsageGate usageGate,
    IAssetUsageTracker assetUsageTracker,
    IUrlGenerator urlGenerator)
    : ApiController(commandBus)
{
    private readonly IDataProtector dataProtector = dataProtection.CreateProtector("LogToken");

    /// <summary>
    /// Get api calls as log file.
    /// </summary>
    /// <param name="app">The name of the app.</param>
    /// <response code="200">Usage tracking results returned.</response>
    /// <response code="404">App not found.</response>
    [HttpGet]
    [Route("apps/{app}/usages/log/")]
    [ProducesResponseType(typeof(LogDownloadDto), StatusCodes.Status200OK)]
    [ApiPermissionOrAnonymous(PermissionIds.AppUsage)]
    [ApiCosts(0)]
    public IActionResult GetLog(string app)
    {
        var token = dataProtector.Protect(App.Id.ToString());

        // Generate an URL with a encrypted token to provide a normal GET link without authorization infos.
        var url = urlGenerator.BuildUrl($"/api/apps/log/{token}/");

        var response = new LogDownloadDto { DownloadUrl = url };

        return Ok(response);
    }

    [HttpGet]
    [Route("apps/log/{token}/")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult GetLogFile(string token)
    {
        // Decrypt the token that has previously been generated.
        var appId = DomainId.Create(dataProtector.Unprotect(token));

        var fileDate = DateTime.UtcNow;
        var fileName = $"Usage-{fileDate:yyy-MM-dd}.csv";

        return new FileCallbackResult("text/csv", (body, range, ct) =>
        {
            var timeTo = Instant.FromDateTimeUtc(fileDate);
            var timeFrom = timeTo.Minus(Duration.FromDays(30));

            return usageLog.ReadLogAsync(appId, timeFrom, timeTo, body, ct);
        })
        {
            FileDownloadName = fileName,
        };
    }

    /// <summary>
    /// Get api calls in date range for app.
    /// </summary>
    /// <param name="app">The name of the app.</param>
    /// <param name="fromDate">The from date.</param>
    /// <param name="toDate">The to date.</param>
    /// <response code="200">API call returned.</response>
    /// <response code="404">App not found.</response>
    [HttpGet]
    [Route("apps/{app}/usages/calls/{fromDate}/{toDate}/")]
    [ProducesResponseType(typeof(CallsUsageDto), StatusCodes.Status200OK)]
    [ApiPermissionOrAnonymous(PermissionIds.AppUsage)]
    [ApiCosts(0)]
    public async Task<IActionResult> GetUsages(string app, DateOnly fromDate, DateOnly toDate)
    {
        var (summary, details) = await usageTracker.QueryAsync(AppId.ToString(), fromDate, toDate, HttpContext.RequestAborted);

        // Use the current app plan to show the limits to the user.
        var (plan, _, _) = await usageGate.GetPlanForAppAsync(App, false, HttpContext.RequestAborted);

        var response = CallsUsageDto.FromDomain(plan, summary, details);

        return Ok(response);
    }

    /// <summary>
    /// Get api calls in date range for team.
    /// </summary>
    /// <param name="team">The name of the team.</param>
    /// <param name="fromDate">The from date.</param>
    /// <param name="toDate">The to date.</param>
    /// <response code="200">API call returned.</response>
    /// <response code="404">Team not found.</response>
    [HttpGet]
    [Route("teams/{team}/usages/calls/{fromDate}/{toDate}/")]
    [ProducesResponseType(typeof(CallsUsageDto), StatusCodes.Status200OK)]
    [ApiPermissionOrAnonymous(PermissionIds.TeamUsage)]
    [ApiCosts(0)]
    public async Task<IActionResult> GetUsagesForTeam(string team, DateOnly fromDate, DateOnly toDate)
    {
        var (summary, details) = await usageTracker.QueryAsync(TeamId.ToString(), fromDate, toDate, HttpContext.RequestAborted);

        // Use the current team plan to show the limits to the user.
        var (plan, _) = await usageGate.GetPlanForTeamAsync(Team, HttpContext.RequestAborted);

        var response = CallsUsageDto.FromDomain(plan, summary, details);

        return Ok(response);
    }

    /// <summary>
    /// Get total asset size for app.
    /// </summary>
    /// <param name="app">The name of the app.</param>
    /// <response code="200">Storage usage returned.</response>
    /// <response code="404">App not found.</response>
    [HttpGet]
    [Route("apps/{app}/usages/storage/today/")]
    [ProducesResponseType(typeof(CurrentStorageDto), StatusCodes.Status200OK)]
    [ApiPermissionOrAnonymous(PermissionIds.AppUsage)]
    [ApiCosts(0)]
    public async Task<IActionResult> GetCurrentStorageSize(string app)
    {
        var (_, size) = await assetUsageTracker.GetTotalByAppAsync(AppId, HttpContext.RequestAborted);

        // Use the current app plan to show the limits to the user.
        var (plan, _, _) = await usageGate.GetPlanForAppAsync(App, false, HttpContext.RequestAborted);

        var response = new CurrentStorageDto { Size = size, MaxAllowed = plan.MaxAssetSize };

        return Ok(response);
    }

    /// <summary>
    /// Get total asset size for team.
    /// </summary>
    /// <param name="team">The ID of the team.</param>
    /// <response code="200">Storage usage returned.</response>
    /// <response code="404">Team not found.</response>
    [HttpGet]
    [Route("teams/{team}/usages/storage/today/")]
    [ProducesResponseType(typeof(CurrentStorageDto), StatusCodes.Status200OK)]
    [ApiPermissionOrAnonymous(PermissionIds.TeamUsage)]
    [ApiCosts(0)]
    public async Task<IActionResult> GetTeamCurrentStorageSizeForTeam(string team)
    {
        var (_, size) = await assetUsageTracker.GetTotalByTeamAsync(TeamId, HttpContext.RequestAborted);

        // Use the current team plan to show the limits to the user.
        var (plan, _) = await usageGate.GetPlanForTeamAsync(Team, HttpContext.RequestAborted);

        var response = new CurrentStorageDto { Size = size, MaxAllowed = plan.MaxAssetSize };

        return Ok(response);
    }

    /// <summary>
    /// Get asset usage by date for app.
    /// </summary>
    /// <param name="app">The name of the app.</param>
    /// <param name="fromDate">The from date.</param>
    /// <param name="toDate">The to date.</param>
    /// <response code="200">Storage usage returned.</response>
    /// <response code="404">App not found.</response>
    [HttpGet]
    [Route("apps/{app}/usages/storage/{fromDate}/{toDate}/")]
    [ProducesResponseType(typeof(StorageUsagePerDateDto[]), StatusCodes.Status200OK)]
    [ApiPermissionOrAnonymous(PermissionIds.AppUsage)]
    [ApiCosts(0)]
    public async Task<IActionResult> GetStorageSizes(string app, DateOnly fromDate, DateOnly toDate)
    {
        var usages = await assetUsageTracker.QueryByAppAsync(AppId, fromDate, toDate, HttpContext.RequestAborted);

        var models = usages.Select(StorageUsagePerDateDto.FromDomain).ToArray();

        return Ok(models);
    }

    /// <summary>
    /// Get asset usage by date for team.
    /// </summary>
    /// <param name="team">The ID of the team.</param>
    /// <param name="fromDate">The from date.</param>
    /// <param name="toDate">The to date.</param>
    /// <response code="200">Storage usage returned.</response>
    /// <response code="404">Team not found.</response>
    [HttpGet]
    [Route("teams/{team}/usages/storage/{fromDate}/{toDate}/")]
    [ProducesResponseType(typeof(StorageUsagePerDateDto[]), StatusCodes.Status200OK)]
    [ApiPermissionOrAnonymous(PermissionIds.TeamUsage)]
    [ApiCosts(0)]
    public async Task<IActionResult> GetStorageSizesForTeam(string team, DateOnly fromDate, DateOnly toDate)
    {
        var usages = await assetUsageTracker.QueryByTeamAsync(TeamId, fromDate, toDate, HttpContext.RequestAborted);

        var models = usages.Select(StorageUsagePerDateDto.FromDomain).ToArray();

        return Ok(models);
    }
}
