using backend.Domain;

namespace Domain.Tests;

public class UploadedFileTests
{
    [Fact]
    public void UploadedFile_Holds_OriginalName_And_StoredPath()
    {
        var file = new UploadedFile
        {
            OriginalName = "MySolution.zip",
            StoredPath = "/data/uploads/abc/MySolution.zip",
        };

        Assert.Equal("MySolution.zip", file.OriginalName);
        Assert.Equal("/data/uploads/abc/MySolution.zip", file.StoredPath);
    }
}
