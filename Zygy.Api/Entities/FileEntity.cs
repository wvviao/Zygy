using LinqToDB.Mapping;

namespace Zygy.Api.Entities;

[Table("t_files")]
public class FileEntity
{
    [PrimaryKey]
    [Column("id")]
    public Guid? Id { get; set; }

    [Column("filename")]
    public string Filename { get; set; } = null!;

    [Column("mime_type")]
    public string MimeType { get; set; } = null!;

    [Column("etag")]
    public string Etag { get; set; } = null!;

    [Column("created_date")]
    public DateTimeOffset CreatedDate { get; set; }
}
