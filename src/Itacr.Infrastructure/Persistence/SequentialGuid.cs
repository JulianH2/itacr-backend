namespace Itacr.Infrastructure.Persistence;

/// <summary>
/// Keys that SQL Server sorts in creation order, so inserts append to the clustered index instead of splitting
/// pages. Same algorithm as EF Core's SequentialGuidValueGenerator, which generated the keys until ITACR-36.
/// </summary>
internal static class SequentialGuid
{
    private static long _counter = DateTime.UtcNow.Ticks;

    public static Guid Create()
    {
        var guidBytes = Guid.NewGuid().ToByteArray();
        var counterBytes = BitConverter.GetBytes(Interlocked.Increment(ref _counter));
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(counterBytes);
        }

        // SQL Server compares the last six bytes first, then bytes 8-9.
        guidBytes[8] = counterBytes[1];
        guidBytes[9] = counterBytes[0];
        guidBytes[10] = counterBytes[7];
        guidBytes[11] = counterBytes[6];
        guidBytes[12] = counterBytes[5];
        guidBytes[13] = counterBytes[4];
        guidBytes[14] = counterBytes[3];
        guidBytes[15] = counterBytes[2];

        return new Guid(guidBytes);
    }
}
