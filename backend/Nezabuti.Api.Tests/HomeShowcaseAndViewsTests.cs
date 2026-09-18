using Nezabuti.Api.DTOs;
using Nezabuti.Api.Models;
using Nezabuti.Api.Services;

namespace Nezabuti.Api.Tests;

public class HomeShowcaseMapperTests
{
    private static PhotoRefDto MapPhoto(PhotoRef photo) => new()
    {
        PhotoId = photo.PhotoId,
        ThumbUrl = $"/uploads/{photo.ThumbPath}",
        PreviewUrl = $"/uploads/{photo.PreviewPath}",
        FullUrl = $"/uploads/{photo.FullPath}"
    };

    private static PhotoRef Photo(string id) => new()
    {
        PhotoId = id,
        ThumbPath = $"settings/home/{id}-thumb.webp",
        PreviewPath = $"settings/home/{id}-preview.webp",
        FullPath = $"settings/home/{id}-full.webp"
    };

    [Fact]
    public void ToPublic_ExcludesDisabledSlides_AndSortsByOrder()
    {
        var showcase = new HomeShowcaseSettings
        {
            HowItWorksEnabled = true,
            HowItWorksSlides =
            [
                new() { Id = "b", Title = "Другий", Enabled = true, SortOrder = 2, Image = Photo("2") },
                new() { Id = "a", Title = "Перший", Enabled = true, SortOrder = 1, Image = Photo("1") },
                new() { Id = "c", Title = "Вимкнений", Enabled = false, SortOrder = 0, Image = Photo("3") }
            ]
        };

        var (enabled, slides, _) = HomeShowcaseMapper.ToPublic(showcase, MapPhoto);

        Assert.True(enabled);
        Assert.Equal(["Перший", "Другий"], slides.Select(s => s.Title).ToArray());
        Assert.DoesNotContain(slides, s => s.Title == "Вимкнений");
        Assert.All(slides, s => Assert.StartsWith("/uploads/settings/home/", s.DesktopImageUrl));
        Assert.Equal(0, slides[0].SortOrder);
        Assert.Equal(1, slides[1].SortOrder);
    }

    [Fact]
    public void ToPublic_FallsBackMobileUrlToDesktop_WhenMobileMissing()
    {
        var showcase = new HomeShowcaseSettings
        {
            HowItWorksEnabled = true,
            HowItWorksSlides =
            [
                new() { Title = "Лише десктоп", Enabled = true, SortOrder = 0, Image = Photo("desk") }
            ]
        };

        var (_, slides, _) = HomeShowcaseMapper.ToPublic(showcase, MapPhoto);
        Assert.Equal("/uploads/settings/home/desk-preview.webp", slides[0].DesktopImageUrl);
        Assert.Equal(slides[0].DesktopImageUrl, slides[0].MobileImageUrl);
        Assert.Null(slides[0].MobileImage);
        Assert.Equal("desk", slides[0].DesktopImage!.PhotoId);
    }

    [Fact]
    public void ToPublic_UsesDistinctMobileImage_WhenPresent()
    {
        var showcase = new HomeShowcaseSettings
        {
            HowItWorksEnabled = true,
            HowItWorksSlides =
            [
                new()
                {
                    Title = "Обидва",
                    Enabled = true,
                    SortOrder = 3,
                    DesktopImage = Photo("d"),
                    MobileImage = Photo("m")
                }
            ]
        };

        var (_, slides, _) = HomeShowcaseMapper.ToPublic(showcase, MapPhoto);
        Assert.Single(slides);
        Assert.Equal(0, slides[0].SortOrder);
        Assert.Equal("/uploads/settings/home/d-preview.webp", slides[0].DesktopImageUrl);
        Assert.Equal("/uploads/settings/home/m-preview.webp", slides[0].MobileImageUrl);
        Assert.Equal("m", slides[0].MobileImage!.PhotoId);
    }

    [Fact]
    public void ToPublic_ReturnsSixEnabledSlidesInPackageOrder_WithShortCaptionAndDemo()
    {
        var showcase = new HomeShowcaseSettings
        {
            HowItWorksEnabled = true,
            HowItWorksSlides =
            [
                new() { Title = "QR-код, що веде до пам’яті", ShortCaption = "Скануйте QR-код — і відкрийте сторінку пам’яті.", Enabled = true, SortOrder = 1, Image = Photo("1"), AltText = "QR-код Nezabuti на меморіальній табличці біля місця пам’яті." },
                new() { Title = "Сторінка пам’яті одним скануванням", ShortCaption = "Відразу відкривається персональна сторінка з основною інформацією.", Enabled = true, SortOrder = 2, Image = Photo("2") },
                new() { Title = "Життєва історія в хронології", Enabled = true, SortOrder = 3, Image = Photo("3") },
                new() { Title = "Світлини, що зберігають спогади", Enabled = true, SortOrder = 4, Image = Photo("4") },
                new() { Title = "Теплі слова, що залишаються поруч", Enabled = true, SortOrder = 5, Image = Photo("5") },
                new() { Title = "Nezabuti — жива історія пам’яті", Enabled = true, SortOrder = 6, Image = Photo("6") }
            ],
            DemoEnabled = true,
            DemoTitle = "Подивіться, як виглядає готова сторінка",
            DemoDescription = "Ознайомтеся з прикладом оформлення меморіальної сторінки.",
            DemoUrl = "https://nezabuti.com.ua/m/YSQG27AFFT",
            DemoPreviewImage = Photo("2")
        };

        var (enabled, slides, demo) = HomeShowcaseMapper.ToPublic(showcase, MapPhoto);

        Assert.True(enabled);
        Assert.Equal(6, slides.Count);
        Assert.Equal("QR-код, що веде до пам’яті", slides[0].Title);
        Assert.Equal("Nezabuti — жива історія пам’яті", slides[5].Title);
        Assert.Equal("QR-код Nezabuti на меморіальній табличці біля місця пам’яті.", slides[0].AltText);
        Assert.NotNull(slides[0].DesktopImageUrl);
        Assert.Equal(0, slides[0].SortOrder);
        Assert.Equal(5, slides[5].SortOrder);
        Assert.NotNull(demo);
        Assert.EndsWith("/m/YSQG27AFFT", demo!.Url);
        Assert.Equal("2", demo.PreviewImage!.PhotoId);
    }

    [Fact]
    public void ToPublic_DisabledBlock_ReturnsNoSlides()
    {
        var showcase = new HomeShowcaseSettings
        {
            HowItWorksEnabled = false,
            HowItWorksSlides =
            [
                new() { Title = "Є", Enabled = true, Image = Photo("1") }
            ]
        };

        var (enabled, slides, demo) = HomeShowcaseMapper.ToPublic(showcase, MapPhoto);

        Assert.False(enabled);
        Assert.Empty(slides);
        Assert.Null(demo);
    }

    [Fact]
    public void ToPublic_DisabledDemo_IsNull()
    {
        var showcase = new HomeShowcaseSettings
        {
            DemoEnabled = false,
            DemoTitle = "Демо",
            DemoUrl = "/m/ABC"
        };

        var (_, _, demo) = HomeShowcaseMapper.ToPublic(showcase, MapPhoto);
        Assert.Null(demo);
    }

    [Fact]
    public void ToPublic_EnabledDemo_ReturnsUrlAndFallbackTitle()
    {
        var showcase = new HomeShowcaseSettings
        {
            DemoEnabled = true,
            DemoUrl = "/m/DEMO1234AB"
        };

        var (_, _, demo) = HomeShowcaseMapper.ToPublic(showcase, MapPhoto);

        Assert.NotNull(demo);
        Assert.Equal("/m/DEMO1234AB", demo!.Url);
        Assert.Equal(HomeShowcaseMapper.DefaultDemoTitle, demo.Title);
        Assert.Equal(HomeShowcaseMapper.DefaultDemoButtonLabel, demo.ButtonLabel);
    }

    [Theory]
    [InlineData("/m/ABC123XY", true)]
    [InlineData("https://nezabuti.com.ua/m/ABC", true)]
    [InlineData("http://localhost:8088/m/ABC", true)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("//evil.example", false)]
    [InlineData("", false)]
    public void IsAllowedDemoUrl(string url, bool expected)
    {
        Assert.Equal(expected, HomeShowcaseMapper.IsAllowedDemoUrl(url));
    }

    [Fact]
    public void ValidateForSave_EnabledDemoWithoutUrl_Throws()
    {
        var showcase = HomeShowcaseMapper.Normalize(new HomeShowcaseSettings
        {
            DemoEnabled = true,
            DemoUrl = "not-a-url"
        });

        var ex = Assert.Throws<InvalidOperationException>(() => HomeShowcaseMapper.ValidateForSave(showcase));
        Assert.Contains("посилання", ex.Message);
    }

    [Fact]
    public void FromAdminRequest_ReordersAndAssignsIds()
    {
        var dto = new HomeShowcaseAdminDto
        {
            HowItWorksEnabled = true,
            HowItWorksSlides =
            [
                new() { Title = "B", SortOrder = 5, Enabled = true },
                new() { Title = "A", SortOrder = 1, Enabled = true }
            ]
        };

        var saved = HomeShowcaseMapper.FromAdminRequest(dto);

        Assert.Equal(["A", "B"], saved.HowItWorksSlides.Select(s => s.Title).ToArray());
        Assert.Equal([0, 1], saved.HowItWorksSlides.Select(s => s.SortOrder).ToArray());
        Assert.All(saved.HowItWorksSlides, s => Assert.False(string.IsNullOrWhiteSpace(s.Id)));
    }

    [Fact]
    public void MissingShowcase_DoesNotThrow()
    {
        var (enabled, slides, demo) = HomeShowcaseMapper.ToPublic(null, MapPhoto);
        Assert.False(enabled);
        Assert.Empty(slides);
        Assert.Null(demo);
    }

    [Fact]
    public void PublicSiteSettingsDto_DoesNotExposeInternalPaths()
    {
        var names = typeof(PublicHowItWorksSlideDto).GetProperties().Select(p => p.Name).ToHashSet();
        Assert.DoesNotContain("ImagePath", names);
        Assert.DoesNotContain("ThumbPath", names);
        Assert.Contains("Image", names);
    }
}

public class MemorialListViewTests
{
    [Fact]
    public void Dtos_ExposeViewCount_NotOnPublicMemorial()
    {
        Assert.Contains("ViewCount", typeof(MemorialListItemDto).GetProperties().Select(p => p.Name));
        Assert.Contains("ViewCount", typeof(MemorialAdminDto).GetProperties().Select(p => p.Name));
        Assert.Contains("LastViewedAt", typeof(MemorialAdminDto).GetProperties().Select(p => p.Name));
        Assert.DoesNotContain("ViewCount", typeof(PublicMemorialDto).GetProperties().Select(p => p.Name));
        Assert.DoesNotContain("LastViewedAt", typeof(PublicMemorialDto).GetProperties().Select(p => p.Name));
    }

    [Fact]
    public async Task ListAsync_AttachesViewCount_IncludingDemo()
    {
        var repo = new FakeMemorialRepository();
        var client = BillingTestFixtures.CreateMemorial(isDemo: false);
        var demo = BillingTestFixtures.CreateMemorial(isDemo: true);
        client.UpdatedAt = DateTime.UtcNow.AddMinutes(-1);
        demo.UpdatedAt = DateTime.UtcNow;
        repo.Seed(client);
        repo.Seed(demo);

        var stats = new StubStatisticsService();
        stats.ByMemorialId[client.Id] = new MemorialStatisticsDto { PublicId = client.PublicId, TotalViews = 10 };
        stats.ByMemorialId[demo.Id] = new MemorialStatisticsDto { PublicId = demo.PublicId, TotalViews = 42 };

        var sut = BillingTestFixtures.CreateMemorialService(repo, stats: stats);
        var result = await sut.ListAsync(null, null, null, null, null, null, 1, 20);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(42, result.Items.Single(i => i.IsDemo).ViewCount);
        Assert.Equal(10, result.Items.Single(i => !i.IsDemo).ViewCount);
    }

    [Fact]
    public async Task ListAsync_SortsByViewCount_AcrossPages()
    {
        var repo = new FakeMemorialRepository();
        var low = BillingTestFixtures.CreateMemorial();
        var mid = BillingTestFixtures.CreateMemorial();
        var high = BillingTestFixtures.CreateMemorial();
        low.FullName = "Low";
        mid.FullName = "Mid";
        high.FullName = "High";
        repo.Seed(low);
        repo.Seed(mid);
        repo.Seed(high);
        repo.ViewCounts[low.Id] = 1;
        repo.ViewCounts[mid.Id] = 5;
        repo.ViewCounts[high.Id] = 9;

        var stats = new StubStatisticsService();
        stats.ByMemorialId[low.Id] = new MemorialStatisticsDto { PublicId = low.PublicId, TotalViews = 1 };
        stats.ByMemorialId[mid.Id] = new MemorialStatisticsDto { PublicId = mid.PublicId, TotalViews = 5 };
        stats.ByMemorialId[high.Id] = new MemorialStatisticsDto { PublicId = high.PublicId, TotalViews = 9 };

        var sut = BillingTestFixtures.CreateMemorialService(repo, stats: stats);

        var descPage1 = await sut.ListAsync(null, null, null, null, "viewCount", "desc", 1, 2);
        Assert.Equal(["High", "Mid"], descPage1.Items.Select(i => i.FullName).ToArray());
        Assert.Equal(3, descPage1.Total);

        var descPage2 = await sut.ListAsync(null, null, null, null, "viewCount", "desc", 2, 2);
        Assert.Equal(["Low"], descPage2.Items.Select(i => i.FullName).ToArray());

        var asc = await sut.ListAsync(null, null, null, null, "viewCount", "asc", 1, 20);
        Assert.Equal(["Low", "Mid", "High"], asc.Items.Select(i => i.FullName).ToArray());
    }

    [Fact]
    public async Task GetAdminAsync_IncludesViewCountAndLastViewedAt()
    {
        var repo = new FakeMemorialRepository();
        var memorial = BillingTestFixtures.CreateMemorial();
        repo.Seed(memorial);
        var last = new DateTime(2026, 9, 17, 17, 43, 0, DateTimeKind.Utc);
        var stats = new StubStatisticsService();
        stats.ByMemorialId[memorial.Id] = new MemorialStatisticsDto
        {
            PublicId = memorial.PublicId,
            TotalViews = 128,
            LastViewedAt = last
        };

        var sut = BillingTestFixtures.CreateMemorialService(repo, stats: stats);
        var dto = await sut.GetAdminAsync(memorial.Id);

        Assert.NotNull(dto);
        Assert.Equal(128, dto!.ViewCount);
        Assert.Equal(last, dto.LastViewedAt);
    }
}

public class HomeShowcaseSettingsServiceTests
{
    private sealed class StubSecrets : ISecretEncryptionService
    {
        public string Encrypt(string plaintext) => plaintext;
        public string? Decrypt(string? ciphertext) => ciphertext;
        public string Mask(string? plaintextOrCipher, int visibleTail = 4) => "***";
    }

    private static PhotoRefDto Img(string id) => new()
    {
        PhotoId = id,
        ThumbUrl = $"/uploads/settings/home/{id}-thumb.webp",
        PreviewUrl = $"/uploads/settings/home/{id}-preview.webp",
        FullUrl = $"/uploads/settings/home/{id}-full.webp"
    };

    private static SiteSettingsService CreateSut(StubSiteSettingsRepository repo) =>
        new(repo, new StubSecrets(), new FakeTelegramAdminNotifyService(), new StubPhotoService());

    [Fact]
    public async Task UpdateAsync_CreatesUpdatesReordersAndTogglesSlides()
    {
        var repo = new StubSiteSettingsRepository();
        var sut = CreateSut(repo);

        var created = await sut.UpdateAsync(new UpdateSiteSettingsRequest
        {
            HomeShowcase = new HomeShowcaseAdminDto
            {
                HowItWorksEnabled = true,
                HowItWorksSlides =
                [
                    new() { Title = "Другий", Description = "Б", SortOrder = 2, Enabled = true, Image = Img("2") },
                    new() { Title = "Перший", Description = "А", SortOrder = 1, Enabled = true, Image = Img("1") }
                ]
            }
        });

        Assert.Equal(["Перший", "Другий"], created.HomeShowcase.HowItWorksSlides.Select(s => s.Title).ToArray());
        Assert.Equal([0, 1], created.HomeShowcase.HowItWorksSlides.Select(s => s.SortOrder).ToArray());

        var disabled = await sut.UpdateAsync(new UpdateSiteSettingsRequest
        {
            HomeShowcase = new HomeShowcaseAdminDto
            {
                HowItWorksEnabled = true,
                HowItWorksSlides =
                [
                    new() { Id = created.HomeShowcase.HowItWorksSlides[0].Id, Title = "Перший", SortOrder = 0, Enabled = false, Image = Img("1") },
                    new() { Id = created.HomeShowcase.HowItWorksSlides[1].Id, Title = "Другий", SortOrder = 1, Enabled = true, Image = Img("2") }
                ]
            }
        });

        Assert.False(disabled.HomeShowcase.HowItWorksSlides[0].Enabled);
        Assert.True(disabled.HomeShowcase.HowItWorksSlides[1].Enabled);

        var pub = await sut.GetPublicAsync();
        Assert.Equal(["Другий"], pub.HowItWorksSlides.Select(s => s.Title).ToArray());
    }

    [Fact]
    public async Task UpdateAsync_SavesDemoCta()
    {
        var repo = new StubSiteSettingsRepository();
        var sut = CreateSut(repo);

        var saved = await sut.UpdateAsync(new UpdateSiteSettingsRequest
        {
            HomeShowcase = new HomeShowcaseAdminDto
            {
                DemoEnabled = true,
                DemoTitle = "Подивіться, як виглядає готова сторінка",
                DemoDescription = "Приклад",
                DemoUrl = "/m/DEMO1234AB"
            }
        });

        Assert.True(saved.HomeShowcase.DemoEnabled);
        Assert.Equal("/m/DEMO1234AB", saved.HomeShowcase.DemoUrl);

        var pub = await sut.GetPublicAsync();
        Assert.NotNull(pub.Demo);
        Assert.Equal("/m/DEMO1234AB", pub.Demo!.Url);
        Assert.Equal("Подивіться, як виглядає готова сторінка", pub.Demo.Title);
    }
}

public class HomeShowcaseSeederTests
{
    private static readonly string PackageJson = File.ReadAllText(FindSeedJson());

    [Fact]
    public async Task EnsureSeeded_ImportsDesktopAndMobile_ThenSkipsWhenSettingsExist()
    {
        var seedRoot = CreateTempSeed(PackageJson);
        var repo = new StubSiteSettingsRepository();
        var photos = new CountingHomePhotoService();
        var sut = new HomeShowcaseSeeder(repo, photos, new TestHostEnvironment(), Microsoft.Extensions.Logging.Abstractions.NullLogger<HomeShowcaseSeeder>.Instance, seedRoot);

        await sut.EnsureSeededAsync();

        var showcase = repo.Settings.HomeShowcase;
        Assert.NotNull(showcase);
        Assert.True(showcase!.HowItWorksEnabled);
        Assert.Equal(6, showcase.HowItWorksSlides.Count);
        Assert.Equal(
            [
                "QR-код, що веде до пам’яті",
                "Сторінка пам’яті одним скануванням",
                "Життєва історія в хронології",
                "Світлини, що зберігають спогади",
                "Теплі слова, що залишаються поруч",
                "Nezabuti — жива історія пам’яті"
            ],
            showcase.HowItWorksSlides.Select(s => s.Title).ToArray());
        Assert.All(showcase.HowItWorksSlides, s =>
        {
            Assert.True(s.Enabled);
            Assert.False(string.IsNullOrWhiteSpace(s.DesktopImage?.PhotoId));
            Assert.False(string.IsNullOrWhiteSpace(s.MobileImage?.PhotoId));
            Assert.NotEqual(s.DesktopImage!.PhotoId, s.MobileImage!.PhotoId);
            Assert.StartsWith("settings/home/", s.DesktopImage.PreviewPath);
        });
        Assert.True(showcase.DemoEnabled);
        Assert.Equal("https://nezabuti.com.ua/m/YSQG27AFFT", showcase.DemoUrl);
        Assert.Equal(showcase.HowItWorksSlides[1].DesktopImage!.PhotoId, showcase.DemoPreviewImage!.PhotoId);
        Assert.Equal(12, photos.HomeUploads);

        var pub = HomeShowcaseMapper.ToPublic(showcase, p => new PhotoRefDto
        {
            PhotoId = p.PhotoId,
            PreviewUrl = $"/uploads/{p.PreviewPath}",
            ThumbUrl = $"/uploads/{p.ThumbPath}",
            FullUrl = $"/uploads/{p.FullPath}"
        });
        Assert.Equal(6, pub.Slides.Count);
        Assert.NotEqual(pub.Slides[0].DesktopImageUrl, pub.Slides[0].MobileImageUrl);
        Assert.Equal(HomeShowcaseSeeder.MobileRevision, showcase.SeedMobileRevision);

        await sut.EnsureSeededAsync();
        Assert.Equal(12, photos.HomeUploads);
    }

    [Fact]
    public async Task EnsureSeeded_RefreshesSeedMobileImages_WithoutChangingDesktop()
    {
        var seedRoot = CreateTempSeed(PackageJson);
        var repo = new StubSiteSettingsRepository
        {
            Settings = new SiteSettings
            {
                HomeShowcase = new HomeShowcaseSettings
                {
                    HowItWorksEnabled = true,
                    SeedMobileRevision = "old-mobile",
                    HowItWorksSlides =
                    [
                        new()
                        {
                            Id = "seed-how-01",
                            Title = "QR-код, що веде до пам’яті",
                            Description = "На пам’ятнику або меморіальній табличці розміщується QR-код.",
                            Enabled = true,
                            SortOrder = 0,
                            DesktopImage = new PhotoRef { PhotoId = "desk-01", PreviewPath = "settings/home/desk-01-preview.webp", ThumbPath = "settings/home/desk-01-thumb.webp", FullPath = "settings/home/desk-01-full.webp" },
                            MobileImage = new PhotoRef { PhotoId = "old-m-01", PreviewPath = "settings/home/old-m-01-preview.webp", ThumbPath = "settings/home/old-m-01-thumb.webp", FullPath = "settings/home/old-m-01-full.webp" }
                        },
                        new()
                        {
                            Id = "seed-how-02",
                            Title = "Сторінка пам’яті одним скануванням",
                            Description = "На смартфоні відкривається меморіальна сторінка.",
                            Enabled = true,
                            SortOrder = 1,
                            DesktopImage = new PhotoRef { PhotoId = "desk-02", PreviewPath = "settings/home/desk-02-preview.webp", ThumbPath = "settings/home/desk-02-thumb.webp", FullPath = "settings/home/desk-02-full.webp" },
                            MobileImage = new PhotoRef { PhotoId = "old-m-02", PreviewPath = "settings/home/old-m-02-preview.webp", ThumbPath = "settings/home/old-m-02-thumb.webp", FullPath = "settings/home/old-m-02-full.webp" }
                        }
                    ]
                }
            }
        };
        var photos = new CountingHomePhotoService();
        var sut = new HomeShowcaseSeeder(repo, photos, new TestHostEnvironment(), Microsoft.Extensions.Logging.Abstractions.NullLogger<HomeShowcaseSeeder>.Instance, seedRoot);

        await sut.EnsureSeededAsync();

        var slides = repo.Settings.HomeShowcase!.HowItWorksSlides;
        Assert.Equal(2, photos.HomeUploads);
        Assert.Equal(["old-m-01", "old-m-02"], photos.DeletedPhotoIds);
        Assert.Equal(["desk-01", "desk-02"], slides.Select(s => s.DesktopImage!.PhotoId).ToArray());
        Assert.Equal(["QR-код, що веде до пам’яті", "Сторінка пам’яті одним скануванням"], slides.Select(s => s.Title).ToArray());
        Assert.All(slides, s =>
        {
            Assert.False(string.IsNullOrWhiteSpace(s.MobileImage?.PhotoId));
            Assert.NotEqual(s.DesktopImage!.PhotoId, s.MobileImage!.PhotoId);
            Assert.DoesNotContain(s.MobileImage.PhotoId, new[] { "old-m-01", "old-m-02" });
        });
        Assert.Equal(HomeShowcaseSeeder.MobileRevision, repo.Settings.HomeShowcase.SeedMobileRevision);

        await sut.EnsureSeededAsync();
        Assert.Equal(2, photos.HomeUploads);
    }

    [Fact]
    public async Task EnsureSeeded_SkippedInProduction()
    {
        var seedRoot = CreateTempSeed(PackageJson);
        var repo = new StubSiteSettingsRepository();
        var photos = new CountingHomePhotoService();
        var sut = new HomeShowcaseSeeder(repo, photos, new TestHostEnvironment("Production"), Microsoft.Extensions.Logging.Abstractions.NullLogger<HomeShowcaseSeeder>.Instance, seedRoot);

        await sut.EnsureSeededAsync();

        Assert.Empty(repo.Settings.HomeShowcase?.HowItWorksSlides ?? []);
        Assert.Equal(0, photos.HomeUploads);
    }

    [Fact]
    public async Task EnsureSeeded_DoesNotOverwriteExistingSlides()
    {
        var seedRoot = CreateTempSeed(PackageJson);
        var repo = new StubSiteSettingsRepository
        {
            Settings = new SiteSettings
            {
                HomeShowcase = new HomeShowcaseSettings
                {
                    HowItWorksSlides = [new() { Title = "Існуючий", Enabled = true }]
                }
            }
        };
        var photos = new CountingHomePhotoService();
        var sut = new HomeShowcaseSeeder(repo, photos, new TestHostEnvironment(), Microsoft.Extensions.Logging.Abstractions.NullLogger<HomeShowcaseSeeder>.Instance, seedRoot);

        await sut.EnsureSeededAsync();

        Assert.Equal(["Існуючий"], repo.Settings.HomeShowcase!.HowItWorksSlides.Select(s => s.Title).ToArray());
        Assert.Equal(0, photos.HomeUploads);
    }

    [Fact]
    public async Task EnsureSeeded_MissingJson_DoesNotChangeSettings()
    {
        var seedRoot = Path.Combine(Path.GetTempPath(), "nezabuti-home-seed-missing-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(seedRoot);
        var repo = new StubSiteSettingsRepository();
        var photos = new CountingHomePhotoService();
        var sut = new HomeShowcaseSeeder(repo, photos, new TestHostEnvironment(), Microsoft.Extensions.Logging.Abstractions.NullLogger<HomeShowcaseSeeder>.Instance, seedRoot);

        await sut.EnsureSeededAsync();

        Assert.Empty(repo.Settings.HomeShowcase?.HowItWorksSlides ?? []);
        Assert.Equal(0, photos.HomeUploads);
    }

    [Fact]
    public async Task EnsureSeeded_MissingImages_DoesNotChangeSettings()
    {
        var seedRoot = Path.Combine(Path.GetTempPath(), "nezabuti-home-seed-no-images-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(seedRoot);
        File.WriteAllText(Path.Combine(seedRoot, "home-showcase.json"), PackageJson);
        var repo = new StubSiteSettingsRepository();
        var photos = new CountingHomePhotoService();
        var sut = new HomeShowcaseSeeder(repo, photos, new TestHostEnvironment(), Microsoft.Extensions.Logging.Abstractions.NullLogger<HomeShowcaseSeeder>.Instance, seedRoot);

        await sut.EnsureSeededAsync();

        Assert.Empty(repo.Settings.HomeShowcase?.HowItWorksSlides ?? []);
        Assert.Equal(0, photos.HomeUploads);
    }

    private static string CreateTempSeed(string json)
    {
        var root = Path.Combine(Path.GetTempPath(), "nezabuti-home-seed-" + Guid.NewGuid().ToString("N"));
        var desktop = Path.Combine(root, "desktop");
        var mobile = Path.Combine(root, "mobile");
        Directory.CreateDirectory(desktop);
        Directory.CreateDirectory(mobile);
        File.WriteAllText(Path.Combine(root, "home-showcase.json"), json);
        foreach (var name in new[]
                 {
                     "01_qr-code.png", "02_memorial-page.png", "03_life-timeline.png",
                     "04_gallery.png", "05_memories.png", "06_complete-story.png"
                 })
        {
            File.WriteAllBytes(Path.Combine(desktop, name), [0x89, 0x50, 0x4E, 0x47]);
        }

        foreach (var name in new[]
                 {
                     "01_qr-code-m.png", "02_memorial-page-m.png", "03_life-timeline-m.png",
                     "04_gallery-m.png", "05_memories-m.png", "06_complete-story-m.png"
                 })
        {
            File.WriteAllBytes(Path.Combine(mobile, name), [0x89, 0x50, 0x4E, 0x47]);
        }

        return root;
    }

    private static string FindSeedJson()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var fromRepo = Path.Combine(dir.FullName, "backend", "Nezabuti.Api", "Seed", "home-showcase", "home-showcase.json");
            if (File.Exists(fromRepo))
            {
                return fromRepo;
            }

            var fromApi = Path.Combine(dir.FullName, "Seed", "home-showcase", "home-showcase.json");
            if (File.Exists(fromApi))
            {
                return fromApi;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("Не знайдено home-showcase.json у Seed.");
    }
}

internal sealed class TestHostEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
{
    public TestHostEnvironment(string environmentName = "Development")
    {
        EnvironmentName = environmentName;
    }

    public string EnvironmentName { get; set; }
    public string ApplicationName { get; set; } = "tests";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
        new Microsoft.Extensions.FileProviders.NullFileProvider();
}

internal sealed class CountingHomePhotoService : IPhotoService
{
    public int HomeUploads { get; private set; }
    public List<string> DeletedPhotoIds { get; } = [];

    public Task DeletePhotoFilesAsync(string publicId, string photoId, CancellationToken ct = default) => Task.CompletedTask;
    public Task DeleteHomePhotoFilesAsync(string photoId, CancellationToken ct = default)
    {
        DeletedPhotoIds.Add(photoId);
        return Task.CompletedTask;
    }
    public Task DeleteMemorialDirectoryAsync(string publicId, CancellationToken ct = default) => Task.CompletedTask;
    public Task<PhotoRef> ProcessUploadAsync(string publicId, Stream uploadStream, string contentType, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<PhotoRef> ProcessHomeUploadAsync(Stream uploadStream, string contentType, CancellationToken ct = default)
    {
        HomeUploads++;
        var id = $"seed-{HomeUploads:00}";
        return Task.FromResult(new PhotoRef
        {
            PhotoId = id,
            ThumbPath = $"settings/home/{id}-thumb.webp",
            PreviewPath = $"settings/home/{id}-preview.webp",
            FullPath = $"settings/home/{id}-full.webp"
        });
    }

    public string GetAbsolutePath(string relativePath) => relativePath;
    public bool IsSafeRelativePath(string relativePath) => true;
}
