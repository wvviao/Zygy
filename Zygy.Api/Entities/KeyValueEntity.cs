using LinqToDB.Mapping;

namespace Zygy.Api.Entities;

[Table("t_kv")]
public class KeyValueEntity
{
    [PrimaryKey]
    [Column("id")]
    public Guid? Id { get; set; }

    [Column("group")]
    public string Group { get; set; } = null!;

    [Column("key")]
    public string Key { get; set; } = null!;

    [Column("value")]
    public string Value { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_by")]
    public string CreatedBy { get; set; } = null!;

    [Column("created_date")]
    public DateTimeOffset CreatedDate { get; set; }

    [Column("updated_by")]
    public string? UpdatedBy { get; set; }

    [Column("updated_date")]
    public DateTimeOffset? UpdatedDate { get; set; }

    [Column("enabled")]
    public bool Enabled { get; set; }
}
