// Several tests redirect Console.Error to assert on what signalme reports, and the animation tests are
// timing based. Both are process-wide concerns, so the suite runs one test at a time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
