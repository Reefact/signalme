// SystemConsoleTests redirect Console.Out and Console.In to drive the real console, which is process-wide
// state, so the suite runs one test at a time. The animations no longer wait for real, so this costs nothing.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
