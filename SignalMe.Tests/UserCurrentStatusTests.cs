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
    [InlineData(UserStatus.DoNotDisturb)]
    public void A_status_survives_a_round_trip(UserStatus status) {
        using TemporaryStatusStore statuses = new();

        statuses.Store.Set(status);

        Assert.Equal(status, statuses.Store.Get());
    }

    /// <summary>
    ///     Away is what the device shows while the session is locked, never a status to come back to. A V1
    ///     install may have written it; V2 reads it as "nothing to restore".
    /// </summary>
    [Fact]
    public void A_file_left_by_v1_saying_away_reads_as_no_status() {
        using TemporaryStatusStore statuses = new();
        Directory.CreateDirectory(statuses.Directory);
        File.WriteAllText(statuses.FilePath, "away");

        Assert.Null(statuses.Store.Get());
    }

    [Fact]
    public void Away_cannot_be_remembered() {
        using TemporaryStatusStore statuses = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => statuses.Store.Set(UserStatus.Away));

        // Rejected before anything touched the disk.
        Assert.False(Directory.Exists(statuses.Directory));
    }

    [Fact]
    public void Away_does_not_overwrite_the_remembered_status() {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Busy);

        Assert.Throws<ArgumentOutOfRangeException>(() => statuses.Store.Set(UserStatus.Away));

        Assert.Equal(UserStatus.Busy, statuses.Store.Get());
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

    /// <summary>
    ///     The status is written aside and moved into place, so an interruption mid-write cannot leave a
    ///     half-written file where the status used to be.
    /// </summary>
    [Fact]
    public void A_status_is_written_atomically() {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Busy);

        statuses.Store.Set(UserStatus.DoNotDisturb);

        Assert.Equal(UserStatus.DoNotDisturb, statuses.Store.Get());
        // No scratch file is left behind next to it.
        Assert.Equal([statuses.FilePath], Directory.GetFiles(statuses.Directory));
    }

    [Fact]
    public void A_leftover_temporary_file_is_ignored() {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Busy);
        File.WriteAllText(statuses.FilePath + ".tmp", "do-not-disturb");

        // A crash between the write and the move must not change what signalme remembers.
        Assert.Equal(UserStatus.Busy, statuses.Store.Get());
    }

    [Fact]
    public void The_directory_is_created_on_demand() {
        using TemporaryStatusStore statuses = new();
        Assert.False(Directory.Exists(statuses.Directory));

        statuses.Store.Set(UserStatus.Busy);

        Assert.True(File.Exists(statuses.FilePath));
    }

}
