﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json;
using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchema.Generation.TypeMappers;
using NodaTime;
using NSwag.Generation;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using Squidex.Areas.Api.Controllers.Rules.Models;
using Squidex.Domain.Apps.Core.Assets;
using Squidex.Domain.Apps.Core.Contents;
using Squidex.Domain.Apps.Core.Schemas;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Json.Objects;
using Squidex.Infrastructure.Queries;
using Squidex.Infrastructure.Reflection;

namespace Squidex.Areas.Api.Config.OpenApi;

public static class OpenApiServices
{
    public static void AddSquidexOpenApiSettings(this IServiceCollection services)
    {
        services.AddSingletonAs<ErrorDtoProcessor>()
            .As<IOperationProcessor>();

        services.AddSingletonAs<ScopesProcessor>()
            .As<IOperationProcessor>();

        services.AddSingletonAs<TagByGroupNameProcessor>()
            .As<IOperationProcessor>();

        services.AddSingletonAs<CommonProcessor>()
            .As<IDocumentProcessor>();

        services.AddSingletonAs<TagXmlProcessor>()
            .As<IDocumentProcessor>();

        services.AddSingletonAs<SecurityProcessor>()
            .As<IDocumentProcessor>();

        services.AddSingletonAs<SchemaNameGenerator>()
            .As<ISchemaNameGenerator>();

        services.AddSingletonAs<AssetFileResolver>()
            .AsSelf();

        services.AddSingletonAs<JsonSchemaGenerator>()
            .AsSelf();

        services.AddSingletonAs<OpenApiSchemaGenerator>()
            .AsSelf();

        services.AddSingleton<JsonSchemaGeneratorSettings>(c =>
        {
            var settings = new SystemTextJsonSchemaGeneratorSettings();

            ConfigureSchemaSettings(settings, c.GetRequiredService<TypeRegistry>(), true);

            return settings;
        });

        services.AddSingleton(c =>
        {
            var settings = new OpenApiDocumentGeneratorSettings
            {
                SchemaSettings = new SystemTextJsonSchemaGeneratorSettings()
                {
                    SerializerOptions = c.GetRequiredService<JsonSerializerOptions>(),
                },
            };

            ConfigureSchemaSettings(settings.SchemaSettings, c.GetRequiredService<TypeRegistry>(), true);

            foreach (var processor in c.GetRequiredService<IEnumerable<IDocumentProcessor>>())
            {
                settings.DocumentProcessors.Add(processor);
            }

            return settings;
        });

        services.AddOpenApiDocument((settings, services) =>
        {
            ConfigureSchemaSettings(settings.SchemaSettings, services.GetRequiredService<TypeRegistry>(), false);
            settings.DocumentProcessors.Add(new AddAdditionalTypeProcessor<DynamicCreateRuleDto>());
            settings.DocumentProcessors.Add(new AddAdditionalTypeProcessor<DynamicRulesDto>());
            settings.DocumentProcessors.Add(new AddAdditionalTypeProcessor<DynamicUpdateRuleDto>());
        });
    }

    private static void ConfigureSchemaSettings(JsonSchemaGeneratorSettings settings, TypeRegistry typeRegistry, bool flatten)
    {
        settings.AllowReferencesWithProperties = true;
        settings.DefaultDictionaryValueReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull;
        settings.DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull;
        settings.FlattenInheritanceHierarchy = flatten;
        settings.ReflectionService = new ReflectionServices();
        settings.SchemaNameGenerator = new SchemaNameGenerator();
        settings.SchemaProcessors.Add(new DiscriminatorProcessor(typeRegistry));
        settings.SchemaProcessors.Add(new RequiredSchemaProcessor());
        settings.SchemaType = NJsonSchema.SchemaType.OpenApi3;

        settings.TypeMappers =
        [
            CreateAnyMap<FilterNode<JsonValue>>(),
            CreateAnyMap<JsonDocument>(),
            CreateAnyMap<JsonValue>(),
            CreateArrayMap<FieldNames>(JsonObjectType.String),
            CreateObjectMap<AssetMetadata>(),
            CreateObjectMap<JsonObject>(),
            CreateStringMap<DateOnly>(JsonFormatStrings.Date),
            CreateStringMap<DomainId>(),
            CreateStringMap<Instant>(JsonFormatStrings.DateTime),
            CreateStringMap<Language>(),
            CreateStringMap<LocalDate>(JsonFormatStrings.Date),
            CreateStringMap<LocalDateTime>(JsonFormatStrings.DateTime),
            CreateStringMap<NamedId<DomainId>>(),
            CreateStringMap<NamedId<Guid>>(),
            CreateStringMap<NamedId<string>>(),
            CreateStringMap<PropertyPath>(),
            CreateStringMap<RefToken>(),
            CreateStringMap<Status>(),
        ];
    }

    private static PrimitiveTypeMapper CreateObjectMap<T>()
    {
        return new PrimitiveTypeMapper(typeof(T), schema =>
        {
            schema.Type = JsonObjectType.Object;
            schema.AdditionalPropertiesSchema = new JsonSchema
            {
                Description = "Any",
            };
        });
    }

    private static PrimitiveTypeMapper CreateArrayMap<T>(JsonObjectType itemType)
    {
        return new PrimitiveTypeMapper(typeof(T), schema =>
        {
            schema.Type = JsonObjectType.Array;
            schema.Item = new JsonSchema
            {
                Type = itemType,
            };
        });
    }

    private static PrimitiveTypeMapper CreateStringMap<T>(string? format = null)
    {
        return new PrimitiveTypeMapper(typeof(T), schema =>
        {
            schema.Type = JsonObjectType.String;
            schema.Format = format;
        });
    }

    private static PrimitiveTypeMapper CreateAnyMap<T>()
    {
        return new PrimitiveTypeMapper(typeof(T), schema =>
        {
            schema.Type = JsonObjectType.None;
        });
    }

    public sealed class AddAdditionalTypeProcessor<T> : IDocumentProcessor where T : class
    {
        public void Process(DocumentProcessorContext context)
        {
            context.SchemaResolver.AppendSchema(context.SchemaGenerator.Generate(typeof(T), context.SchemaResolver), null);
        }
    }
}
