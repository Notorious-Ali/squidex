﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json.Serialization;
using Squidex.Infrastructure.Json.System;
using Squidex.Infrastructure.TestHelpers;

namespace Squidex.Infrastructure;

public class NamedIdTests
{
    internal sealed record Wrapper
    {
        [JsonConverter(typeof(StringConverter<NamedId<long>>))]
        public NamedId<long> Value { get; set; }
    }

    [Fact]
    public void Should_instantiate_token()
    {
        var id = Guid.NewGuid();

        var namedId = NamedId.Of(id, "my-name");

        Assert.Equal(id, namedId.Id);
        Assert.Equal("my-name", namedId.Name);
    }

    [Fact]
    public void Should_convert_named_id_to_string()
    {
        var id = Guid.NewGuid();

        var namedId = NamedId.Of(id, "my-name");

        Assert.Equal($"{id},my-name", namedId.ToString());
    }

    [Fact]
    public void Should_serialize_and_deserialize_null_guid_token()
    {
        NamedId<Guid>? value = null;

        var serialized = value.SerializeAndDeserializeJson();

        Assert.Equal(value, serialized);
    }

    [Fact]
    public void Should_serialize_and_deserialize_valid_guid_token()
    {
        var value = NamedId.Of(Guid.NewGuid(), "my-name");

        var serialized = value.SerializeAndDeserializeJson();

        Assert.Equal(value, serialized);
    }

    [Fact]
    public void Should_serialize_and_deserialize_null_long_token()
    {
        NamedId<long>? value = null;

        var serialized = value.SerializeAndDeserializeJson();

        Assert.Equal(value, serialized);
    }

    [Fact]
    public void Should_serialize_and_deserialize_valid_long_token()
    {
        var value = NamedId.Of(123L, "my-name");

        var serialized = value.SerializeAndDeserializeJson();

        Assert.Equal(value, serialized);
    }

    [Fact]
    public void Should_serialize_and_deserialize_null_string_token()
    {
        NamedId<string>? value = null;

        var serialized = value.SerializeAndDeserializeJson();

        Assert.Equal(value, serialized);
    }

    [Fact]
    public void Should_serialize_and_deserialize_valid_string_token()
    {
        var value = NamedId.Of(Guid.NewGuid().ToString(), "my-name");

        var serialized = value.SerializeAndDeserializeJson();

        Assert.Equal(value, serialized);
    }

    [Fact]
    public void Should_serialize_and_deserialize_null_id_token()
    {
        NamedId<DomainId>? value = null;

        var serialized = value.SerializeAndDeserializeJson();

        Assert.Equal(value, serialized);
    }

    [Fact]
    public void Should_serialize_and_deserialize_valid_id_token()
    {
        var value = NamedId.Of(DomainId.NewGuid().ToString(), "my-name");

        var serialized = value.SerializeAndDeserializeJson();

        Assert.Equal(value, serialized);
    }

    [Fact]
    public void Should_serialize_and_deserialize_old_object()
    {
        var value = new { id = 42L, name = "my-name" };

        var serialized = value.SerializeAndDeserializeJson<NamedId<long>, object>();

        Assert.Equal(NamedId.Of(42L, "my-name"), serialized);
    }

    [Fact]
    public void Should_deserialize_from_old_object_with_explicit_converter()
    {
        var value = new
        {
            value = new { id = 42, name = "my-name" },
        };

        var expected = new Wrapper
        {
            Value = NamedId.Of(42L, "my-name"),
        };

        var serialized = value.SerializeAndDeserializeJson<Wrapper, object>();

        Assert.Equal(expected, serialized);
    }

    [Fact]
    public void Should_throw_exception_if_string_id_is_not_valid_json()
    {
        Assert.ThrowsAny<Exception>(() => TestUtils.DeserializeJson<NamedId<string>>("123"));
    }

    [Fact]
    public void Should_throw_exception_if_long_id_is_not_valid_json()
    {
        Assert.ThrowsAny<Exception>(() => TestUtils.DeserializeJson<NamedId<long>>("invalid-long,name"));
    }

    [Fact]
    public void Should_throw_exception_if_guid_id_is_not_valid_json()
    {
        Assert.ThrowsAny<Exception>(() => TestUtils.DeserializeJson<NamedId<Guid>>("invalid-guid,name"));
    }
}
