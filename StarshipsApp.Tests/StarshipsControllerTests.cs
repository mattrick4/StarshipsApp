using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarshipsApp.Controllers;
using StarshipsApp.Data;
using StarshipsApp.Models;
using System.Runtime.CompilerServices;

namespace StarshipsApp.Tests
{
    public class StarshipsControllerTests
    {
        // ---------- Helpers ----------
        // Create new DbContextOptions for an in-memory database with the given name
        private static DbContextOptions<AppDbContext> CreateOptions(string dbName) =>
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .EnableSensitiveDataLogging()
                .Options;

        // Create new AppDbContext with in-memory database
        private static AppDbContext CreateContext(string dbName) => new(CreateOptions(dbName));

        // Seed sample starships into the context
        private static async Task SeedAsync(AppDbContext context)
        {
            context.Starships.AddRange(
                new Starship { 
                                Id = 1, 
                                Name = "X-Wing", 
                                Model = "T-65B", 
                                Manufacturer = "Incom Corporation", 
                                StarshipClass = "Starfighter", 
                                Crew = "1", 
                                Passengers = "0", 
                                Url = null },
                new Starship { 
                                Id = 2, 
                                Name = "Millennium Falcon", 
                                Model = "YT-1300", 
                                Manufacturer = "Corellian Engineering Corporation", 
                                StarshipClass = "Light Freighter", 
                                Crew = "2", 
                                Passengers = "6", 
                                Url = null }
            );

            await context.SaveChangesAsync();
        }

        private static StarshipsController CreateController(AppDbContext context) => new(context);

        // Context that throws DbUpdateConcurrencyException on SaveChangesAsync (used to test concurrency paths)
        private sealed class ThrowingSaveChangesAppDbContext : AppDbContext
        {
            public ThrowingSaveChangesAppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
                => throw new DbUpdateConcurrencyException("Simulated concurrency exception");
        }

        // Reusable harness: creates context (unique per test), optional seeding, and controller.
        private static async Task WithControllerAsync(
            Func<AppDbContext, StarshipsController, Task> testBody,
            bool seed = false,
            [CallerMemberName] string? dbName = null)
        {
            using var mockContext = CreateContext(dbName!);
            if (seed)
            {
                await SeedAsync(mockContext);
            }

            var mockController = CreateController(mockContext);
            await testBody(mockContext, mockController);
        }

        private static Task WithSeededControllerAsync(
            Func<AppDbContext, StarshipsController, Task> testBody,
            [CallerMemberName] string? dbName = null)
            => WithControllerAsync(testBody, seed: true, dbName);

        private static Task WithEmptyControllerAsync(
            Func<AppDbContext, StarshipsController, Task> testBody,
            [CallerMemberName] string? dbName = null)
            => WithControllerAsync(testBody, seed: false, dbName);

        // Variant for the concurrency tests that need a throwing context sharing the same DB
        private static async Task WithThrowingControllerAsync(
            Func<ThrowingSaveChangesAppDbContext, StarshipsController, Task> testBody,
            Func<AppDbContext, Task>? seedAction = null,
            [CallerMemberName] string? dbName = null)
        {
            if (seedAction != null)
            {
                using var seedContext = CreateContext(dbName!);
                await seedAction(seedContext);
            }

            using var throwingContext = new ThrowingSaveChangesAppDbContext(CreateOptions(dbName!));
            var mockController = CreateController(throwingContext);
            await testBody(throwingContext, mockController);
        }

        // ---------- READ: Index ----------
        [Fact]
        public async Task Index_Returns_View_With_All_Starships() =>
            await WithSeededControllerAsync(async (context, controller) =>
            {
                var result = await controller.Index();

                var view = Assert.IsType<ViewResult>(result);
                var model = Assert.IsAssignableFrom<IEnumerable<Starship>>(view.Model);
                Assert.Equal(2, model.Count());
            });

        [Fact]
        public async Task Index_When_Empty_Returns_Empty_Model() =>
            await WithEmptyControllerAsync(async (_, mockController) =>
            {
                var result = await mockController.Index();
                var view = Assert.IsType<ViewResult>(result);
                var model = Assert.IsAssignableFrom<IEnumerable<Starship>>(view.Model);
                Assert.Empty(model);
            });

        [Fact]
        public async Task Index_Does_Not_Track_Entities()
        {
            var dbName = nameof(Index_Does_Not_Track_Entities);

            // Seed using one context (which we dispose), then query with a fresh one to ensure tracker starts empty
            using (var seedContext = CreateContext(dbName))
            {
                await SeedAsync(seedContext);
            }

            using var queryContext = CreateContext(dbName);
            var mockController = CreateController(queryContext);
            var result = await mockController.Index();

            Assert.IsType<ViewResult>(result);
            Assert.Empty(queryContext.ChangeTracker.Entries<Starship>());
        }

        // ---------- READ: Details ----------
        [Fact]
        public async Task Details_NullId_Returns_NotFound()
        {
            var dbName = nameof(Details_NullId_Returns_NotFound);
            using var mockContext = CreateContext(dbName);
            await SeedAsync(mockContext);
            var mockController = CreateController(mockContext);
            var result = await mockController.Details(null);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_InvalidId_Returns_NotFound()
        {
            var dbName = nameof(Details_InvalidId_Returns_NotFound);
            using var mockContext = CreateContext(dbName);
            await SeedAsync(mockContext);
            var mockController = CreateController(mockContext);
            var result = await mockController.Details(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ValidId_Returns_View_With_Model()
        {
            var dbName = nameof(Details_ValidId_Returns_View_With_Model);
            using var mockContext = CreateContext(dbName);
            await SeedAsync(mockContext);
            var mockController = CreateController(mockContext);
            var result = await mockController.Details(1);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Starship>(view.Model);
            Assert.Equal(1, model.Id);
            Assert.Equal("X-Wing", model.Name);
        }

        [Fact]
        public async Task Details_Does_Not_Track_Entity()
        {
            var dbName = nameof(Details_Does_Not_Track_Entity);

            using (var seedContext = CreateContext(dbName))
            {
                await SeedAsync(seedContext);
            }

            using var queryContext = CreateContext(dbName);
            var mockController = CreateController(queryContext);
            var result = await mockController.Details(1);

            Assert.IsType<ViewResult>(result);
            Assert.Empty(queryContext.ChangeTracker.Entries<Starship>());
        }

        // ---------- CREATE ----------
        [Fact]
        public async Task Create_Get_Returns_View() =>
            await WithEmptyControllerAsync(async (_, mockController) =>
            {
                var result = mockController.Create();
                Assert.IsType<ViewResult>(result);
            });

        [Fact]
        public async Task Create_Post_InvalidModel_Returns_View_With_Model()
        {
            var dbName = nameof(Create_Post_InvalidModel_Returns_View_With_Model);
            using var mockContext = CreateContext(dbName);
            var mockController = CreateController(mockContext);
            mockController.ModelState.AddModelError("Name", "Required");
            var input = new Starship();
            var result = await mockController.Create(input);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(input, view.Model);
            Assert.Empty(mockContext.Starships);
        }

        [Fact]
        public async Task Create_Post_ValidModel_Persists_And_Redirects()
        {
            var dbName = nameof(Create_Post_ValidModel_Persists_And_Redirects);
            using var mockContext = CreateContext(dbName);
            var mockController = CreateController(mockContext);

            var input = new Starship
            {
                Id = 10,
                Name = "A-Wing",
                Model = "RZ-1",
                Manufacturer = "Alliance Underground Engineering",
                StarshipClass = "Interceptor",
                Crew = "1",
                Passengers = "0"
            };

            var result = await mockController.Create(input);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(StarshipsController.Index), redirect.ActionName);
            Assert.Single(mockContext.Starships);
            Assert.NotNull(await mockContext.Starships.FindAsync(10));
        }

        // ---------- UPDATE: Edit ----------
        [Fact]
        public async Task Edit_Get_InvalidId_Returns_NotFound() =>
            await WithSeededControllerAsync(async (_, mockController) =>
            {
                var result = await mockController.Edit(999);
                Assert.IsType<NotFoundResult>(result);
            });

        [Fact]
        public async Task Edit_Get_NullId_Returns_NotFound() =>
            await WithSeededControllerAsync(async (_, mockController) =>
            {
                var result = await mockController.Edit(null);
                Assert.IsType<NotFoundResult>(result);
            });

        [Fact]
        public async Task Edit_Get_ValidId_Returns_View_With_Model()
        {
            var dbName = nameof(Edit_Get_ValidId_Returns_View_With_Model);
            using var mockContext = CreateContext(dbName);
            await SeedAsync(mockContext);
            var mockController = CreateController(mockContext);

            var result = await mockController.Edit(2);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Starship>(view.Model);
            Assert.Equal(2, model.Id);
            Assert.Equal("Millennium Falcon", model.Name);
        }

        [Fact]
        public async Task Edit_Post_IdMismatch_Returns_BadRequest() =>
            await WithSeededControllerAsync(async (_, mockController) =>
            {
                var input = new Starship { Id = 123, Name = "Test" };
                var result = await mockController.Edit(999, input);
                Assert.IsType<BadRequestResult>(result);
            });

        [Fact]
        public async Task Edit_Post_InvalidModel_Returns_View_With_Model() =>
            await WithSeededControllerAsync(async (_, mockController) =>
            {
                mockController.ModelState.AddModelError("Name", "Required");
                var input = new Starship { Id = 1, Name = "" };

                var result = await mockController.Edit(1, input);

                var view = Assert.IsType<ViewResult>(result);
                Assert.Same(input, view.Model);
            });

        [Fact]
        public async Task Edit_Post_InvalidModel_Does_Not_Persist_Changes() =>
            await WithSeededControllerAsync(async (mockContext, mockController) =>
            {
                // Arrange: make model invalid and attempt to change existing entity
                mockController.ModelState.AddModelError("Name", "Required");
                var input = new Starship
                {
                    Id = 1,
                    Name = "", // invalid
                    Model = "T-65C",
                    Manufacturer = "Incom Corporation",
                    StarshipClass = "Starfighter",
                    Crew = "1",
                    Passengers = "0"
                };

                // Act
                var result = await mockController.Edit(1, input);
                Assert.IsType<ViewResult>(result);

                // Assert DB unchanged
                var reloaded = await mockContext.Starships.AsNoTracking().FirstAsync(s => s.Id == 1);
                Assert.Equal("X-Wing", reloaded.Name);
                Assert.Equal("T-65B", reloaded.Model);
            });

        [Fact]
        public async Task Edit_Post_ValidModel_Persists_And_Redirects() =>
            await WithSeededControllerAsync(async (mockContext, mockController) =>
            {
                var updated = new Starship
                {
                    Id = 1,
                    Name = "X-Wing Updated",
                    Model = "T-65C",
                    Manufacturer = "Incom Corporation",
                    StarshipClass = "Starfighter",
                    Crew = "1",
                    Passengers = "0"
                };

                var result = await mockController.Edit(1, updated);

                var redirect = Assert.IsType<RedirectToActionResult>(result);
                Assert.Equal(nameof(StarshipsController.Index), redirect.ActionName);

                var reloaded = await mockContext.Starships.AsNoTracking().FirstAsync(s => s.Id == 1);
                Assert.Equal("X-Wing Updated", reloaded.Name);
                Assert.Equal("T-65C", reloaded.Model);
            });

        [Fact]
        public async Task Edit_Post_When_Missing_Returns_NotFound() =>
            await WithEmptyControllerAsync(async (_, mockController) =>
            {
                var input = new Starship
                {
                    Id = 42,
                    Name = "Ghost",
                    Model = "VCX-100",
                    Manufacturer = "Corellian Engineering Corporation",
                    StarshipClass = "Light Freighter",
                    Crew = "2",
                    Passengers = "6"
                };

                var result = await mockController.Edit(42, input);
                Assert.IsType<NotFoundResult>(result);
            });

        [Fact]
        public async Task Edit_Post_Concurrency_When_Missing_Returns_NotFound() =>
            await WithThrowingControllerAsync(
                async (_, mockController) =>
                {
                    var input = new Starship { Id = 42, Name = "Ghost", Model = "VCX-100", Manufacturer = "Corellian Engineering Corporation", StarshipClass = "Light Freighter", Crew = "2", Passengers = "6" };
                    var result = await mockController.Edit(42, input);
                    Assert.IsType<NotFoundResult>(result);
                },
                seedAction: async seedContext => await seedContext.Database.EnsureCreatedAsync()
            );

        [Fact]
        public async Task Edit_Post_Concurrency_When_Exists_Rethrows() =>
            await WithThrowingControllerAsync(
                async (_, mockController) =>
                {
                    var input = new Starship { Id = 77, Name = "Slave I Updated", Model = "Firespray-31", Manufacturer = "Kuat Systems Engineering", StarshipClass = "Patrol and Attack Craft", Crew = "1", Passengers = "6" };
                    await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => mockController.Edit(77, input));
                },
                seedAction: async seedContext =>
                {
                    seedContext.Starships.Add(new Starship { Id = 77, Name = "Slave I", Model = "Firespray-31", Manufacturer = "Kuat Systems Engineering", StarshipClass = "Patrol and Attack Craft", Crew = "1", Passengers = "6" });
                    await seedContext.SaveChangesAsync();
                }
            );

        // ---------- DELETE ----------
        [Fact]
        public async Task Delete_Get_NullId_Returns_NotFound()
        {
            var dbName = nameof(Delete_Get_NullId_Returns_NotFound);
            using var mockContext = CreateContext(dbName);
            await SeedAsync(mockContext);
            var mockController = CreateController(mockContext);
            var result = await mockController.Delete(null);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Get_InvalidId_Returns_NotFound()
        {
            var dbName = nameof(Delete_Get_InvalidId_Returns_NotFound);
            using var mockContext = CreateContext(dbName);
            await SeedAsync(mockContext);
            var mockController = CreateController(mockContext);
            var result = await mockController.Delete(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Get_ValidId_Returns_View_With_Model()
        {
            var dbName = nameof(Delete_Get_ValidId_Returns_View_With_Model);
            using var mockContext = CreateContext(dbName);
            await SeedAsync(mockContext);
            var mockController = CreateController(mockContext);
            var result = await mockController.Delete(2);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Starship>(view.Model);
            Assert.Equal(2, model.Id);
        }

        [Fact]
        public async Task DeleteConfirmed_Removes_And_Redirects() {
            var dbName = nameof(DeleteConfirmed_Removes_And_Redirects);
            using var mockContext = CreateContext(dbName);
            await SeedAsync(mockContext);
            var mockController = CreateController(mockContext);
            var result = await mockController.DeleteConfirmed(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(StarshipsController.Index), redirect.ActionName);
            Assert.Null(await mockContext.Starships.FindAsync(1));
            Assert.Single(mockContext.Starships); // Only id 2 remains
        }

        [Fact]
        public async Task DeleteConfirmed_NonExisting_Still_Redirects() {
            var dbName = nameof(DeleteConfirmed_NonExisting_Still_Redirects);
            using var mockContext = CreateContext(dbName);
            await SeedAsync(mockContext);
            var mockController = CreateController(mockContext);
            var result = await mockController.DeleteConfirmed(999);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(StarshipsController.Index), redirect.ActionName);
            Assert.Equal(2, mockContext.Starships.Count());
        }
    }
}