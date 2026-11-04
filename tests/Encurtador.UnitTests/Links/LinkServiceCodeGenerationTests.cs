using Encurtador.Application;
using Encurtador.Application.Links;

namespace Encurtador.UnitTests.Links;

public sealed class LinkServiceCodeGenerationTests
{
    [Fact]
    public void Generated_codes_have_exact_length_and_use_only_unambiguous_alphabet()
    {
        var generator = new ShortCodeGenerator();

        for (var index = 0; index < 100; index++)
        {
            var code = generator.Generate();
            Assert.Equal(7, code.Length);
            Assert.All(code, character => Assert.Contains(character, ShortCodeGenerator.Alphabet));
        }

        Assert.DoesNotContain('0', ShortCodeGenerator.Alphabet);
        Assert.DoesNotContain('1', ShortCodeGenerator.Alphabet);
        Assert.DoesNotContain('I', ShortCodeGenerator.Alphabet);
        Assert.DoesNotContain('L', ShortCodeGenerator.Alphabet);
        Assert.DoesNotContain('O', ShortCodeGenerator.Alphabet);
        Assert.DoesNotContain('U', ShortCodeGenerator.Alphabet);
    }

    [Fact]
    public async Task Create_retries_generated_code_after_database_collision()
    {
        var store = new FakeLinkStore();
        store.ConflictOnNextCreate("ABC2345");
        var generator = new SequenceCodeGenerator("ABC2345", "BCD2345");
        var service = new LinkService(store, generator, TimeProvider.System);

        var created = await service.CreateAsync(new LinkCreationInput("https://example.com", null, null));

        Assert.Equal("BCD2345", created.Code);
        Assert.Equal(2, generator.Calls);
        Assert.Single(store.Links);
    }

    [Fact]
    public async Task Create_retries_availability_collisions_for_generated_codes()
    {
        var store = new FakeLinkStore();
        store.AddExisting(new ShortLink { Id = Guid.NewGuid(), Code = "ABC2345", DestinationUrl = "https://existing.example" });
        var generator = new SequenceCodeGenerator("ABC2345", "BCD2345");
        var service = new LinkService(store, generator, TimeProvider.System);

        var created = await service.CreateAsync(new LinkCreationInput("https://example.com", null, null));

        Assert.Equal("BCD2345", created.Code);
        Assert.Equal(2, generator.Calls);
        Assert.Equal(2, store.Links.Count);
    }

    [Fact]
    public async Task Create_fails_explicitly_after_bounded_generated_code_collisions()
    {
        var store = new FakeLinkStore();
        for (var index = 0; index < 8; index++)
        {
            store.ConflictOnNextCreate($"Code{index}");
        }

        var generator = new SequenceCodeGenerator(
            "Code0", "Code1", "Code2", "Code3", "Code4", "Code5", "Code6", "Code7");
        var service = new LinkService(store, generator, TimeProvider.System);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(new LinkCreationInput("https://example.com", null, null)));

        Assert.Equal(8, generator.Calls);
        Assert.Contains("código único", exception.Message);
        Assert.Empty(store.Links);
    }

    [Fact]
    public async Task Create_initializes_access_count_to_zero()
    {
        var store = new FakeLinkStore();
        var service = new LinkService(store, new SequenceCodeGenerator("ABC2345"), TimeProvider.System);

        await service.CreateAsync(new LinkCreationInput("https://example.com", null, null));

        Assert.Equal(0, Assert.Single(store.Links).AccessCount);
    }
}
