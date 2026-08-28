using MongoDB.Bson.Serialization.Attributes;

namespace Nezabuti.Api.Models;

public class PhotoRef
{
    [BsonElement("photoId")]
    public string PhotoId { get; set; } = string.Empty;

    [BsonElement("thumbPath")]
    public string ThumbPath { get; set; } = string.Empty;

    [BsonElement("previewPath")]
    public string PreviewPath { get; set; } = string.Empty;

    [BsonElement("fullPath")]
    public string FullPath { get; set; } = string.Empty;

    [BsonElement("width")]
    public int? Width { get; set; }

    [BsonElement("height")]
    public int? Height { get; set; }
}
