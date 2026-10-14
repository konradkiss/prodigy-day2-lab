namespace Orders;

/* Runtime configuration. Secrets come from the environment, never from the code. */
public static class Config
{
    public static int Port => int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port) ? port : 3000;

    /* The packing team's token, read when it is needed. Without it the warehouse endpoints refuse every request. */
    public static string StaffToken() => Environment.GetEnvironmentVariable("STAFF_TOKEN") ?? "";
}
