﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;

namespace Squidex.Infrastructure.Diagnostics;

public sealed class MongoHealthCheck(IMongoDatabase mongoDatabase) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var collectionNames = await mongoDatabase.ListCollectionNamesAsync(cancellationToken: cancellationToken);

        var result = await collectionNames.AnyAsync(cancellationToken);

        var status = result ?
            HealthStatus.Healthy :
            HealthStatus.Unhealthy;

        return new HealthCheckResult(status, "Application must query data from MongoDB");
    }
}
