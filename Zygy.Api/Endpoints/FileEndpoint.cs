using Amazon.S3;
using Amazon.S3.Model;
using Zygy.Api.Models.Responses;
using Zygy.Api.Utilities;

namespace Zygy.Api.Endpoints;

[RegisterSingleton<IEndpoint, FileEndpoint>(Duplicate = DuplicateStrategy.Append)]
public class FileEndpoint(IAmazonS3 s3Client, IConfiguration config) : IEndpoint
{
    private readonly string _defaultBucket = config.GetRequiredValue("S3:DefaultBucket");

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var builder = app.MapGroup("/file").WithTags("文件");
        builder.MapGet("/{id}", Download);
        builder.MapPost("/upload", Upload).RequireAuthorization(WriteSysPolicy).DisableAntiforgery();
    }

    [EndpointSummary("上传")]
    public async ValueTask<Results<
        Ok<ApiResponse<string>>,
        BadRequest
    >> Upload([FromForm] IFormFile file, CancellationToken ct)
    {
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext))
        {
            return TypedResults.BadRequest();
        }

        await using var stream = file.OpenReadStream();
        var fileId = Guid.CreateVersion7().EncodeBase32() + ext.ToLowerInvariant();
        var req = new PutObjectRequest
        {
            BucketName = _defaultBucket,
            Key = fileId,
            InputStream = stream,
            AutoCloseStream = true
        };
        await s3Client.PutObjectAsync(req, ct);
        return ApiResponse.Success(fileId);
    }

    [EndpointSummary("下载")]
    public async ValueTask<Results<
        FileStreamHttpResult,
        NotFound
    >> Download([FromRoute] string id, CancellationToken ct)
    {
        try
        {
            var res = await s3Client.GetObjectAsync(_defaultBucket, id, ct);
            return TypedResults.Stream(res.ResponseStream, contentType: res.Headers.ContentType);
        }
        catch (NoSuchKeyException)
        {
            return TypedResults.NotFound();
        }
    }
}
