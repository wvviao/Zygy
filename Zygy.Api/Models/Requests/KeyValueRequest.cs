using System.ComponentModel.DataAnnotations;

namespace Zygy.Api.Models.Requests;

public record CreateKeyValueRequest(
    [Required(AllowEmptyStrings = false)]
    string Group,
    [Required(AllowEmptyStrings = false)]
    string Key,
    [Required(AllowEmptyStrings = false)]
    string Value)
{
    public string Description { get; set; } = "";
    public bool Enabled { get; set; } = true;
}

public record DeleteKeyValueRequest(
    [Required, MinLength(1)]
    List<Guid> Ids);

public record UpdateKeyValueRequest(Guid Id)
{
    public string? Group { get; set; }
    public string? Key { get; set; }
    public string? Value { get; set; }
    public string? Description { get; set; }
    public bool? Enabled { get; set; }
}
