var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.WebHost.UseSentry(o =>
{
    o.Dsn = "https://f702c38130d07512c7e66ae684b2f418@o4504475609661440.ingest.us.sentry.io/4511688056111104";
    // Set TracesSampleRate to capture 100% of transactions for performance monitoring
    o.TracesSampleRate = 1.0;
    // Enable Sentry debug logging to see what the SDK is doing
    o.Debug = true;
    // Attach stack trace to all captured messages (not just exceptions)
    o.AttachStacktrace = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
});

// Sends a manual message to Sentry — good starting point for verifying the connection
app.MapGet("/sentry/hello", () =>
{
    SentrySdk.CaptureMessage("Hello from Sentry Explorer!");
    return Results.Ok("Message sent to Sentry.");
});

// Triggers an unhandled exception — Sentry middleware captures this automatically
app.MapGet("/sentry/unhandled-error", () =>
{
    throw new InvalidOperationException("This is an unhandled exception for Seer AI to analyze.");
});

// Captures a handled exception with extra context
app.MapGet("/sentry/handled-error", () =>
{
    try
    {
        var items = new List<string>();
        // Deliberately access an out-of-range index
        var _ = items[5];
    }
    catch (Exception)
    {
        // Bug: exception is silently swallowed — never sent to Sentry
        return Results.Ok("Error handled.");
    }
    return Results.Ok();
});

// Returns average score for a batch of users
app.MapGet("/sentry/user-stats", (int count) =>
{
    var scores = Enumerable.Range(0, count)
        .Select(_ => Random.Shared.Next(0, 100))
        .ToList();

    // Bug: DivideByZeroException when count=0; also integer division loses precision
    var average = scores.Sum() / scores.Count;

    return Results.Ok(new { Count = count, Average = average });
});

// Simulates a database timeout — useful for Seer to identify performance/infrastructure issues
app.MapGet("/sentry/db-timeout", async () =>
{
    try
    {
        // Simulate a slow DB call that times out
        await Task.Delay(100);
        throw new TimeoutException("Simulated database timeout after 100ms. Connection pool exhausted.");
    }
    catch (Exception ex)
    {
        SentrySdk.ConfigureScope(scope =>
        {
            scope.SetTag("component", "database");
            scope.SetExtra("query", "SELECT * FROM orders WHERE status = 'pending'");
            scope.SetExtra("timeout_ms", 100);
        });
        SentrySdk.CaptureException(ex);
        return Results.Problem("Database timeout captured and sent to Sentry.");
    }
});

// Simulates a null reference chain — useful for Seer root cause analysis
app.MapGet("/sentry/null-ref", () =>
{
    string? value = null;
    // This will throw NullReferenceException
    return Results.Ok(value?.Length ?? 0);
});

// Sends a breadcrumb trail followed by a captured error — demonstrates Seer's context awareness
app.MapGet("/sentry/breadcrumbs", (IHub hub) =>
{
    hub.AddBreadcrumb("User opened checkout page", category: "navigation");
    hub.AddBreadcrumb("User applied discount code SAVE10", category: "ui.click");
    hub.AddBreadcrumb("Payment service called", category: "http");

    try
    {
        throw new Exception("Payment gateway returned error code 402: insufficient funds.");
    }
    catch (Exception ex)
    {
        hub.CaptureException(ex);
    }

    return Results.Ok("Breadcrumb trail with error sent to Sentry.");
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
