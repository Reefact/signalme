using SignalMe.Services;
using SignalMe.Tests.Fakes;

namespace SignalMe.Tests;

public sealed class UserCurrentStatusTests {

    [Fact]
    public void No_file_means_no_status() {
        using TemporaryStatusStore statuses = new();

        Check.That(statuses.Store.Get()).IsNull();
    }

    [Theory]
    [InlineData(UserStatus.Available)]
    [InlineData(UserStatus.Busy)]
    [InlineData(UserStatus.DoNotDisturb)]
    public void A_status_survives_a_round_trip(UserStatus status) {
        using TemporaryStatusStore statuses = new();

        statuses.Store.Set(status);

        Check.That(statuses.Store.Get()).IsEqualTo(status);
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

        Check.That(statuses.Store.Get()).IsNull();
    }

    [Fact]
    public void Away_cannot_be_remembered() {
        using TemporaryStatusStore statuses = new();

        Check.ThatCode(() => statuses.Store.Set(UserStatus.Away)).Throws<ArgumentOutOfRangeException>();

        // Rejected before anything touched the disk.
        Check.That(Directory.Exists(statuses.Directory)).IsFalse();
    }

    [Fact]
    public void Away_does_not_overwrite_the_remembered_status() {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Busy);

        Check.ThatCode(() => statuses.Store.Set(UserStatus.Away)).Throws<ArgumentOutOfRangeException>();

        Check.That(statuses.Store.Get()).IsEqualTo(UserStatus.Busy);
    }

    [Fact]
    public void Setting_no_status_removes_the_file() {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Busy);

        statuses.Store.Set(null);

        Check.That(File.Exists(statuses.FilePath)).IsFalse();
        Check.That(statuses.Store.Get()).IsNull();
    }

    [Fact]
    public void Clearing_an_absent_status_is_not_an_error() {
        using TemporaryStatusStore statuses = new();

        statuses.Store.Set(null);

        Check.That(statuses.Store.Get()).IsNull();
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
        Check.That(statuses.Store.Get()).IsNull();
    }

    [Fact]
    public void Surrounding_whitespace_is_tolerated() {
        using TemporaryStatusStore statuses = new();
        Directory.CreateDirectory(statuses.Directory);
        File.WriteAllText(statuses.FilePath, "  busy \n");

        Check.That(statuses.Store.Get()).IsEqualTo(UserStatus.Busy);
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

        Check.That(statuses.Store.Get()).IsEqualTo(UserStatus.DoNotDisturb);
        // No scratch file is left behind next to it.
        Check.That(Directory.GetFiles(statuses.Directory)).ContainsExactly([statuses.FilePath]);
    }

    [Fact]
    public void A_leftover_temporary_file_is_ignored() {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Busy);
        File.WriteAllText(statuses.FilePath + ".tmp", "do-not-disturb");

        // A crash between the write and the move must not change what signalme remembers.
        Check.That(statuses.Store.Get()).IsEqualTo(UserStatus.Busy);
    }

    [Fact]
    public void The_directory_is_created_on_demand() {
        using TemporaryStatusStore statuses = new();
        Check.That(Directory.Exists(statuses.Directory)).IsFalse();

        statuses.Store.Set(UserStatus.Busy);

        Check.That(File.Exists(statuses.FilePath)).IsTrue();
    }

}
