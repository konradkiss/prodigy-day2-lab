// Every test class reseeds the one in-memory database, so the classes run one after another, not in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
