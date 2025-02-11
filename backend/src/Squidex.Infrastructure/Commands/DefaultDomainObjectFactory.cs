﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.DependencyInjection;
using Squidex.Infrastructure.States;

#pragma warning disable RECS0108 // Warns about static fields in generic types

namespace Squidex.Infrastructure.Commands;

public sealed class DefaultDomainObjectFactory(IServiceProvider serviceProvider) : IDomainObjectFactory
{
    private static class DefaultFactory<T>
    {
        private static readonly ObjectFactory ObjectFactory =
            ActivatorUtilities.CreateFactory(typeof(T), [typeof(DomainId)]);

        public static T Create(IServiceProvider serviceProvider, DomainId id)
        {
            return (T)ObjectFactory(serviceProvider, [id]);
        }
    }

    private static class PersistenceFactory<T, TState>
    {
        private static readonly ObjectFactory ObjectFactory =
            ActivatorUtilities.CreateFactory(typeof(T), [typeof(DomainId), typeof(IPersistenceFactory<TState>)]);

        public static T Create(IServiceProvider serviceProvider, DomainId id, IPersistenceFactory<TState> persistenceFactory)
        {
            return (T)ObjectFactory(serviceProvider, [id, persistenceFactory]);
        }
    }

    public T Create<T>(DomainId id)
    {
        return DefaultFactory<T>.Create(serviceProvider, id);
    }

    public T Create<T, TState>(DomainId id, IPersistenceFactory<TState> factory)
    {
        return PersistenceFactory<T, TState>.Create(serviceProvider, id, factory);
    }
}
