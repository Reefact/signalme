// Several tests redirect Console.Error to assert on what signalme reports, which is process-wide state, so
// the suite runs one test at a time. The animations no longer wait for real, so this costs nothing.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
