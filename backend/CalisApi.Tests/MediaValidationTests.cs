using System.Buffers.Binary;
using System.Text;
using CalisApi.Common;
using CalisApi.Services.Storage;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalisApi.Tests;

[TestClass]
public class MediaValidationTests
{
    [TestMethod]
    public void Mp4_EnforcesDurationBoundaries_AndRejectsUnsupportedCodec()
    {
        MediaFileValidator.Validate(TestMp4.Movie(120), "video/mp4");
        Assert.ThrowsExactly<AppException>(() => MediaFileValidator.Validate(TestMp4.Movie(121), "video/mp4"));
        Assert.ThrowsExactly<AppException>(() => MediaFileValidator.Validate(TestMp4.Movie(0), "video/mp4"));
        Assert.ThrowsExactly<AppException>(() => MediaFileValidator.Validate(TestMp4.Movie(30, "hvc1"), "video/mp4"));
    }

    [TestMethod]
    public void Mp4_ExerciseVideos_AreLimitedToThirtySeconds()
    {
        MediaFileValidator.Validate(TestMp4.Movie(30), "video/mp4", maxSeconds: 30);
        Assert.ThrowsExactly<AppException>(() => MediaFileValidator.Validate(TestMp4.Movie(31), "video/mp4", maxSeconds: 30));
    }

    [TestMethod]
    public void Mp4_RejectsTruncation_AndClaimedSizesOutsideTheFile()
    {
        var valid = TestMp4.Movie(30);
        Assert.ThrowsExactly<AppException>(() => MediaFileValidator.Validate(valid[..^1], "video/mp4"));
        BinaryPrimitives.WriteUInt32BigEndian(valid.AsSpan(0, 4), uint.MaxValue);
        Assert.ThrowsExactly<AppException>(() => MediaFileValidator.Validate(valid, "video/mp4"));
        Assert.ThrowsExactly<AppException>(() => MediaFileValidator.Validate("<script>bad()</script>"u8.ToArray(), "image/png"));
    }

    [TestMethod]
    public async Task R2_SignsPrivateUrls_WithoutNetworkAccess_AndReportsMissingConfiguration()
    {
        using var storage = new R2ObjectStorage(Options.Create(new StorageSettings
        {
            AccountId = new string('a', 32), BucketName = "calisapp-test", AccessKeyId = "test-key",
            SecretAccessKey = "test-only-secret-not-a-real-credential",
        }));
        var put = await storage.UploadUrlAsync("staging/test", "image/png", DateTime.UtcNow.AddMinutes(15));
        var get = await storage.ReadUrlAsync("community/test", DateTime.UtcNow.AddMinutes(5));
        StringAssert.StartsWith(put, $"https://{new string('a', 32)}.r2.cloudflarestorage.com/");
        StringAssert.Contains(put, "X-Amz-Signature=");
        StringAssert.Contains(get, "response-cache-control=");
        using var empty = new R2ObjectStorage(Options.Create(new StorageSettings()));
        var exception = await Assert.ThrowsExactlyAsync<AppException>(() => empty.UploadUrlAsync("test", "image/png", DateTime.UtcNow.AddMinutes(1)));
        Assert.AreEqual(503, exception.StatusCode);
    }
}
