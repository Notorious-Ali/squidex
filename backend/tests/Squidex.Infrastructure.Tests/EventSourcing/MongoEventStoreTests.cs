﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable MA0048 // File name must match type name

using Squidex.Infrastructure.MongoDb;

namespace Squidex.Infrastructure.EventSourcing;

public abstract class MongoEventStoreTests(MongoEventStoreFixture fixture) : EventStoreTests<MongoEventStore>, IAsyncLifetime
{
    private ProfilerCollection profiler;

    public MongoEventStoreFixture _ { get; } = fixture;

    public override MongoEventStore CreateStore()
    {
        return _.EventStore;
    }

    public Task InitializeAsync()
    {
        profiler = new ProfilerCollection(_.Database);

        return profiler.ClearAsync();
    }

    public async Task DisposeAsync()
    {
        var queries = await profiler.GetQueriesAsync("Events2");

        Assert.All(queries, query =>
        {
            Assert.InRange(query.DocsExamined, 0, query.NumDocuments);
            Assert.InRange(query.KeysExamined, query.NumDocuments, (Math.Max(1, query.NumDocuments) * 2) + 1);
        });
    }
}

[Trait("Category", "Dependencies")]
public sealed class MongoEventStoreTests_Direct(MongoEventStoreFixture_Direct fixture) : MongoEventStoreTests(fixture), IClassFixture<MongoEventStoreFixture_Direct>
{
}

[Trait("Category", "Dependencies")]
public sealed class MongoEventStoreTests_Replica(MongoEventStoreFixture_Replica fixture) : MongoEventStoreTests(fixture), IClassFixture<MongoEventStoreFixture_Replica>
{
}
