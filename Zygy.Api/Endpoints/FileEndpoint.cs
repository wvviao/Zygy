using System.ComponentModel.DataAnnotations;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Net.Http.Headers;
using SimpleBase;
using Zygy.Api.Entities;
using Zygy.Api.Models.Responses;
using Zygy.Api.Repositories;
using Zygy.Api.Utilities;

namespace Zygy.Api.Endpoints;

[RegisterSingleton<IEndpoint, FileEndpoint>(Duplicate = DuplicateStrategy.Append)]
public class FileEndpoint(IAmazonS3 s3Client, IConfiguration config) : IEndpoint
{
    private readonly string _defaultBucket = config.GetRequiredValue("S3:DefaultBucket");

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var builder = app.MapGroup("").WithTags("文件");
        builder.MapGet("/files/{id}", Download);
        builder.MapPost("/files/upload", Upload).RequireAuthorization(WriteSysPolicy).DisableAntiforgery();
        builder.MapGet("/files", Query).RequireAuthorization(ReadSysPolicy);
    }

    [EndpointSummary("查询")]
    public async ValueTask<Ok<PageResponse<QueryFileResponse>>> Query(
        [FromServices] AppDbContext db,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery(Name = "per_page"), Range(1, 100)]
        int perPage = 10,
        CancellationToken ct = default)
    {
        IQueryable<FileEntity> q = db.Files;
        var data = await q.OrderByDescending(e => e.Id)
            .Pagination(page, perPage)
            .ToListAsync(ct);
        var total = await q.CountAsync(ct);
        return PageResponse.Success(data.Select(e =>
        {
            var id = e.Id!.Value;
            var ext = Path.GetExtension(e.Filename);
            return new QueryFileResponse(
                Id: id,
                Path: id.EncodeBase32() + ext.ToLowerInvariant(),
                Filename: e.Filename);
        }).ToList(), total);
    }

    [EndpointSummary("上传")]
    public async ValueTask<Results<
        Ok<ApiResponse<string>>,
        BadRequest
    >> Upload([FromForm] IFormFile file, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext))
        {
            return TypedResults.BadRequest();
        }

        var id = Guid.CreateVersion7();
        await using var stream = file.OpenReadStream();
        var fileId = id.EncodeBase32() + ext.ToLowerInvariant();
        var req = new PutObjectRequest
        {
            BucketName = _defaultBucket,
            Key = fileId,
            InputStream = stream,
            AutoCloseStream = true
        };
        var res = await s3Client.PutObjectAsync(req, ct);
        if (res is not { HttpStatusCode: HttpStatusCode.OK })
        {
            return TypedResults.BadRequest();
        }

        await db.Files.InsertAsync(() => new FileEntity
        {
            Id = id,
            Filename = file.FileName,
            MimeType = file.ContentType,
            Etag = res.ETag,
            CreatedDate = DateTimeOffset.UtcNow
        }, ct);

        return ApiResponse.Success(fileId);
    }

    [EndpointSummary("下载")]
    public async ValueTask<Results<
        FileStreamHttpResult,
        NotFound
    >> Download([FromRoute] string id, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var idSpan = id.AsSpan();
        var dotIndex = idSpan.LastIndexOf('.');
        if (dotIndex == -1)
        {
            return TypedResults.NotFound();
        }

        Span<byte> buffer = stackalloc byte[16];
        if (!Base32.Crockford.TryDecode(idSpan[..dotIndex], buffer, out var bytesWritten) || bytesWritten != 16)
        {
            return TypedResults.NotFound();
        }

        var fileId = new Guid(buffer);
        var file = db.Files.FirstOrDefault(e => e.Id == fileId);
        if (file is null)
        {
            return TypedResults.NotFound();
        }

        GetObjectResponse? res;
        try
        {
            res = await s3Client.GetObjectAsync(_defaultBucket, id, ct);
        }
        catch (NoSuchKeyException)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Stream(
            res.ResponseStream,
            contentType: file.MimeType,
            lastModified: res.LastModified,
            entityTag: EntityTagHeaderValue.Parse(file.Etag),
            enableRangeProcessing: true);
    }
}
