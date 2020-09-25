﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Threading.Tasks;
using Squidex.ClientLibrary;
using Squidex.ClientLibrary.Management;
using TestSuite.Fixtures;
using TestSuite.Model;
using Xunit;

#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable SA1507 // Code should not contain multiple blank lines in a row

namespace TestSuite.ApiTests
{
    public class ContentUpdateTests : IClassFixture<ContentFixture>
    {
        public ContentFixture _ { get; }

        public ContentUpdateTests(ContentFixture fixture)
        {
            _ = fixture;
        }

        [Fact]
        public async Task Should_return_published_item()
        {
            TestEntity content = null;
            try
            {
                // STEP 1: Create the item unpublished.
                content = await _.Contents.CreateAsync(new TestEntityData { Number = 1 });


                // STEP 2: Publish the item.
                await _.Contents.ChangeStatusAsync(content.Id, Status.Published);


                // STEP 3: Retrieve the item.
                await _.Contents.GetAsync(content.Id);
            }
            finally
            {
                if (content != null)
                {
                    await _.Contents.DeleteAsync(content.Id);
                }
            }
        }

        [Fact]
        public async Task Should_not_return_archived_item()
        {
            TestEntity content = null;
            try
            {
                // STEP 1: Create the item published.
                content = await _.Contents.CreateAsync(new TestEntityData { Number = 1 }, true);


                // STEP 2: Archive the item.
                await _.Contents.ChangeStatusAsync(content.Id, Status.Archived);


                // STEP 3. Get a 404 for the item because it is not published anymore.
                await Assert.ThrowsAsync<SquidexException>(() => _.Contents.GetAsync(content.Id));
            }
            finally
            {
                if (content != null)
                {
                    await _.Contents.DeleteAsync(content.Id);
                }
            }
        }

        [Fact]
        public async Task Should_not_return_unpublished_item()
        {
            TestEntity content = null;
            try
            {
                // STEP 1: Create the item unpublished.
                content = await _.Contents.CreateAsync(new TestEntityData { Number = 1 });


                // STEP 2: Change the status to publiushed and then to draft.
                await _.Contents.ChangeStatusAsync(content.Id, Status.Published);
                await _.Contents.ChangeStatusAsync(content.Id, Status.Draft);


                // STEP 3. Get a 404 for the item because it is not published anymore.
                await Assert.ThrowsAsync<SquidexException>(() => _.Contents.GetAsync(content.Id));
            }
            finally
            {
                if (content != null)
                {
                    await _.Contents.DeleteAsync(content.Id);
                }
            }
        }

        [Fact]
        public async Task Should_create_strange_text()
        {
            const string text = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.88 Safari/537.36";

            TestEntity content = null;
            try
            {
                // STEP 1: Create a content item with a text that caused a bug before.
                content = await _.Contents.CreateAsync(new TestEntityData { String = text }, true);


                // STEP 2: Get the item and ensure that the text is the same.
                var updated = await _.Contents.GetAsync(content.Id);

                Assert.Equal(text, updated.Data.String);
            }
            finally
            {
                if (content != null)
                {
                    await _.Contents.DeleteAsync(content.Id);
                }
            }
        }

        [Fact]
        public async Task Should_create_non_published_item()
        {
            TestEntity content = null;
            try
            {
                // STEP 1: Create the item unpublished.
                content = await _.Contents.CreateAsync(new TestEntityData { Number = 1 });


                // STEP 2. Get a 404 for the item because it is not published.
                await Assert.ThrowsAsync<SquidexException>(() => _.Contents.GetAsync(content.Id));
            }
            finally
            {
                if (content != null)
                {
                    await _.Contents.DeleteAsync(content.Id);
                }
            }
        }

        [Fact]
        public async Task Should_create_published_item()
        {
            TestEntity content = null;
            try
            {
                // STEP 1: Create the item published.
                content = await _.Contents.CreateAsync(new TestEntityData { Number = 1 }, true);


                // STEP 2: Get the item.
                await _.Contents.GetAsync(content.Id);
            }
            finally
            {
                if (content != null)
                {
                    await _.Contents.DeleteAsync(content.Id);
                }
            }
        }

        [Fact]
        public async Task Should_create_item_with_custom_id()
        {
            var id = Guid.NewGuid().ToString();

            TestEntity content = null;
            try
            {
                // STEP 1: Create a new item with a custom id.
                content = await _.Contents.CreateAsync(new TestEntityData { Number = 1 }, id, true);

                Assert.Equal(id, content.Id);
            }
            finally
            {
                if (content != null)
                {
                    await _.Contents.DeleteAsync(content.Id);
                }
            }
        }

        [Fact]
        public async Task Should_not_create_item_with_custom_id_twice()
        {
            var id = Guid.NewGuid().ToString();

            TestEntity content = null;
            try
            {
                // STEP 1: Create a new item with a custom id.
                content = await _.Contents.CreateAsync(new TestEntityData { Number = 1 }, id, true);

                Assert.Equal(id, content.Id);


                // STEP 2: Create a new item with a custom id.
                var ex = await Assert.ThrowsAsync<SquidexException>(() => _.Contents.CreateAsync(new TestEntityData { Number = 1 }, id, true));

                Assert.Contains("\"statusCode\":409", ex.Message);
            }
            finally
            {
                if (content != null)
                {
                    await _.Contents.DeleteAsync(content.Id);
                }
            }
        }

        [Fact]
        public async Task Should_create_item_with_custom_id_and_upsert()
        {
            var id = Guid.NewGuid().ToString();

            TestEntity content = null;
            try
            {
                // STEP 1: Upsert a new item with a custom id.
                content = await _.Contents.UpsertAsync(id, new TestEntityData { Number = 1 }, true);

                Assert.Equal(id, content.Id);


                content = await _.Contents.UpsertAsync(id, new TestEntityData { Number = 2 }, true);

                Assert.Equal(2, content.Version);
            }
            finally
            {
                if (content != null)
                {
                    await _.Contents.DeleteAsync(content.Id);
                }
            }
        }

        [Fact]
        public async Task Should_upsert_item()
        {
            var id = Guid.NewGuid().ToString();

            TestEntity content = null;
            try
            {
                // STEP 1: Upsert a new item with a custom id.
                content = await _.Contents.UpsertAsync(id, new TestEntityData { Number = 1 }, true);


                // STEP 2: Upsert the item with a custom id and ensure that is has been updated.
                await _.Contents.UpsertAsync(id, new TestEntityData { Number = 2 });

                var updated = await _.Contents.GetAsync(content.Id);

                Assert.Equal(2, updated.Data.Number);
            }
            finally
            {
                if (content != null)
                {
                    await _.Contents.DeleteAsync(content.Id);
                }
            }
        }

        [Fact]
        public async Task Should_update_item()
        {
            TestEntity content = null;
            try
            {
                // STEP 1: Create a new item.
                content = await _.Contents.CreateAsync(new TestEntityData { Number = 2 }, true);


                // STEP 2: Update the item and ensure that the data has changed.
                await _.Contents.UpdateAsync(content.Id, new TestEntityData { Number = 2 });

                var updated = await _.Contents.GetAsync(content.Id);

                Assert.Equal(2, updated.Data.Number);
            }
            finally
            {
                if (content != null)
                {
                    await _.Contents.DeleteAsync(content.Id);
                }
            }
        }

        [Fact]
        public async Task Should_patch_item()
        {
            TestEntity content = null;
            try
            {
                // STEP 1: Create a new item.
                content = await _.Contents.CreateAsync(new TestEntityData { Number = 1 }, true);


                // STEP 2: Update the item and ensure that the data has changed.
                await _.Contents.PatchAsync(content.Id, new TestEntityData { Number = 2 });

                var updated = await _.Contents.GetAsync(content.Id);

                Assert.Equal(2, updated.Data.Number);
            }
            finally
            {
                if (content != null)
                {
                    await _.Contents.DeleteAsync(content.Id);
                }
            }
        }

        [Fact]
        public async Task Should_delete_item()
        {
            // STEP 1: Create a new item.
            var content = await _.Contents.CreateAsync(new TestEntityData { Number = 2 }, true);


            // STEP 2: Delete the item.
            await _.Contents.DeleteAsync(content.Id);


            // STEP 3: Retrieve all items and ensure that the deleted item does not exist.
            var updated = await _.Contents.GetAsync();

            Assert.DoesNotContain(updated.Items, x => x.Id == content.Id);
        }
    }
}
