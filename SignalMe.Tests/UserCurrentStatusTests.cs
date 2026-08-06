using SignalMe.Infrastructure;
using SignalMe.Services;
using SignalMe.Tests.Fakes;

namespace SignalMe.Tests;

public sealed class UserCurrentStatusTests {

    [Fact]
    public void No_file_means_no_status() {
        using TemporaryStatusStore statuses = new();

        Assert.Null(statuses.Store.Get());
    }

    [Theory]
    [InlineData(UserStatus.Available)]
    [InlineData(UserStatus.Busy)]
    [InlineData(UserStatus.Away)]
    [InlineData(UserStatus.DoNotDisturb)]
    public void A_status_survives_a_round_trip(UserStatus status) {
        using TemporaryStatusStore statuses = new();

        statuses.Store.Set(status);

        Assert.Equal(status, statuses.Store.Get());
    }

    [Fact]
    public void Setting_no_status_removes_the_file() {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Busy);

        statuses.Store.Set(null);

        Assert.False(File.Exists(statuses.FilePath));
        Assert.Null(statuses.Store.Get());
    }

    [Fact]
    public void Clearing_an_absent_status_is_not_an_error() {
        using TemporaryStatusStore statuses = new();

        statuses.Store.Set(null);

        Assert.Null(statuses.Store.Get());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("anything at all")]
    [InlineData("AVAILABLE")]
    public void A_file_that_cannot_be_understood_reads_as_no_status(string content) {
        using TemporaryStatusStore statuses = new();
        Directory.CreateDirectory(statuses.Directory);
        File.WriteAllText(statuses.FilePath, content);

        // It used to throw, which took down every mood command until the file was deleted.
        Assert.Null(statuses.Store.Get());
    }

    [Fact]
    public void Surrounding_whitespace_is_tolerated() {
        using TemporaryStatusStore statuses = new();
        Directory.CreateDirectory(statuses.Directory);
        File.WriteAllText(statuses.FilePath, "  busy \n");

        Assert.Equal(UserStatus.Busy, statuses.Store.Get());
    }

    [Fact]
    public void The_directory_is_created_on_demand() {
        using TemporaryStatusStore statuses = new();
        Assert.False(Directory.Exists(statuses.Directory));

        statuses.Store.Set(UserStatus.Away);

        Assert.True(File.Exists(statuses.FilePath));
    }

}
