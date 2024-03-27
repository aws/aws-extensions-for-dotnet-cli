var builder = WebApplication.CreateSlimBuilder(args);

var app = builder.Build();

app.MapGet("/", async context =>
{
    await context.Response.WriteAsync("Welcome to running ASP.NET on AWS Lambda");
});

app.Run();