namespace Nezabuti.Api.Models.Blocks;

public static class BlockType
{
    public const string Text = "Text";
    public const string Timeline = "Timeline";
    public const string Gallery = "Gallery";
    public const string Image = "Image";
    public const string Quote = "Quote";
    public const string Service = "Service";
    public const string Awards = "Awards";
    public const string Memories = "Memories";

    public static readonly HashSet<string> All =
    [
        Text, Timeline, Gallery, Image, Quote, Service, Awards, Memories
    ];
}
