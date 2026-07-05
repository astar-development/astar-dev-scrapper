using AStar.Dev.Wallpaper.Scrapper.Support;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Support;

public sealed class GivenTheImageSaver
{
    private readonly MockFileSystem fileSystem = new();
    private readonly IImageSaver sut;

    public GivenTheImageSaver() => sut = new ImageSaver(fileSystem);

    [Fact]
    public async Task when_the_image_is_empty_then_no_file_is_written()
    {
        fileSystem.Directory.CreateDirectory("/save/dir");

        await sut.SaveAsync([], "/save/dir/image.jpg");

        fileSystem.File.Exists("/save/dir/image.jpg").ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_image_is_not_empty_then_the_file_is_written_with_the_supplied_bytes()
    {
        fileSystem.Directory.CreateDirectory("/save/dir");
        byte[] image = [1, 2, 3,];

        await sut.SaveAsync(image, "/save/dir/image.jpg");

        fileSystem.File.ReadAllBytes("/save/dir/image.jpg").ShouldBe(image);
    }

    [Fact]
    public async Task when_the_path_contains_a_colon_after_the_second_character_then_it_is_replaced_with_an_underscore()
    {
        fileSystem.Directory.CreateDirectory("/tmp");
        byte[] image = [1,];

        await sut.SaveAsync(image, "/tmp/save:name.jpg");

        fileSystem.File.Exists("/tmp/save_name.jpg").ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_path_contains_a_quote_then_it_is_replaced_with_a_single_quote()
    {
        fileSystem.Directory.CreateDirectory("/save/dir");
        byte[] image = [1,];

        await sut.SaveAsync(image, "/save/dir/file\"name.jpg");

        fileSystem.File.Exists("/save/dir/file'name.jpg").ShouldBeTrue();
    }
}
