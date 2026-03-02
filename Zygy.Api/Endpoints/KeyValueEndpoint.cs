using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LinqToDB;
using LinqToDB.Async;
using Zygy.Api.Entities;
using Zygy.Api.Models.Requests;
using Zygy.Api.Models.Responses;
using Zygy.Api.Repositories;
using Zygy.Api.Services;
using Zygy.Api.Utilities;

namespace Zygy.Api.Endpoints;

[RegisterSingleton<IEndpoint, KeyValueEndpoint>(Duplicate = DuplicateStrategy.Append)]
public class KeyValueEndpoint(IEncryptionService es) : IEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var builder = app.MapGroup("/kv").RequireAuthorization(ReadSysPolicy).WithTags("键值");
        builder.MapGet("", Search);
        builder.MapPost("/update", Update).RequireAuthorization(WriteSysPolicy);
        builder.MapPost("/delete", DeleteKeyValue).RequireAuthorization(WriteSysPolicy);
        builder.MapPost("", CreateKeyValue).RequireAuthorization(WriteSysPolicy);
        builder.MapGet("/exists", Exists);
    }

    [EndpointSummary("查询")]
    public async ValueTask<Ok<PageResponse<KeyValueEntity>>> Search(
        [FromServices] AppDbContext db,
        [FromQuery(Name = "search_text")] string? searchText,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery(Name = "per_page"), Range(1, 100)]
        int perPage = 10,
        CancellationToken ct = default)
    {
        IQueryable<KeyValueEntity> q = db.KeyValue;
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            q = q.Where(kv => kv.Group.Contains(searchText) || kv.Key.Contains(searchText));
        }

        var data = await q.OrderBy(kv => kv.Group)
            .ThenBy(kv => kv.Key)
            .Pagination(page, perPage)
            .ToListAsync(ct);
        var total = await q.CountAsync(ct);

        data = data.Select(kv =>
        {
            kv.Value = es.DecryptFromBase64String(kv.Value);
            kv.CreatedBy = es.DecryptFromBase64String(kv.CreatedBy);
            kv.UpdatedBy = es.DecryptFromBase64String(kv.UpdatedBy);
            return kv;
        }).ToList();

        return PageResponse.Success(data, total);
    }

    [EndpointSummary("新建")]
    public async ValueTask<Ok<ApiResponse>> CreateKeyValue(
        [FromBody] CreateKeyValueRequest request,
        [FromServices] AppDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var userId = user.UserId;
        var now = DateTimeOffset.UtcNow;
        var value = es.EncryptToBase64String(request.Value);
        var createdBy = es.EncryptToBase64String(userId);
        var lastModifiedBy = createdBy;

        await db.KeyValue.InsertAsync(() => new KeyValueEntity
        {
            Id = Guid.CreateVersion7(),
            Group = request.Group,
            Key = request.Key,
            Value = value,
            Description = request.Description,
            CreatedBy = createdBy,
            CreatedDate = now,
            UpdatedBy = lastModifiedBy,
            UpdatedDate = now,
            Enabled = request.Enabled
        }, ct);

        return ApiResponse.Success();
    }

    [EndpointSummary("检查是否存在")]
    public static async ValueTask<Ok<ApiResponse<bool>>> Exists(
        [FromQuery(Name = "group"), Required(AllowEmptyStrings = false)]
        string group,
        [FromQuery(Name = "key"), Required(AllowEmptyStrings = false)]
        string key,
        [FromServices] AppDbContext db,
        CancellationToken ct = default) =>
        ApiResponse.Success(await db.KeyValue.Where(e => e.Group == group && e.Key == key).AnyAsync(ct));

    [EndpointSummary("更新")]
    public async ValueTask<Results<
        Ok<ApiResponse>,
        BadRequest<ApiResponse>
    >> Update(
        [FromBody] UpdateKeyValueRequest request,
        [FromServices] AppDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        if (request.Value == null && request.Description == null && request.Enabled == null ||
            request.Group != null && string.IsNullOrWhiteSpace(request.Group) ||
            request.Key != null && string.IsNullOrWhiteSpace(request.Key))
        {
            return ApiResponse.InvalidParam();
        }

        var updatedBy = es.EncryptToBase64String(user.UserId);
        var utcNow = DateTimeOffset.UtcNow;

        var updater = db.KeyValue.Where(e => e.Id == request.Id).AsUpdatable();

        if (!string.IsNullOrWhiteSpace(request.Group))
        {
            updater = updater.Set(e => e.Group, request.Group);
        }

        if (!string.IsNullOrWhiteSpace(request.Key))
        {
            updater = updater.Set(e => e.Key, request.Key);
        }

        if (request.Value != null)
        {
            var value = es.EncryptToBase64String(request.Value);
            updater = updater.Set(e => e.Value, value);
        }

        if (request.Description != null)
        {
            updater = updater.Set(e => e.Description, request.Description);
        }

        if (request.Enabled != null)
        {
            updater = updater.Set(e => e.Enabled, request.Enabled);
        }

        await updater.Set(e => e.UpdatedDate, utcNow)
            .Set(e => e.UpdatedBy, updatedBy)
            .UpdateAsync(ct);

        return ApiResponse.Success();
    }

    [EndpointSummary("删除")]
    public static async ValueTask<Ok<ApiResponse>> DeleteKeyValue(
        [FromBody] DeleteKeyValueRequest request,
        [FromServices] AppDbContext db,
        CancellationToken ct)
    {
        await db.KeyValue
            .DeleteAsync(i => request.Ids.Contains(i.Id!.Value), ct);
        return ApiResponse.Success();
    }
}
