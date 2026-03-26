using ShapeUp.Configurations;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProjectServices(builder.Configuration);

var app = builder.Build();

await app.ApplyMigrationsAsync();

app.UseProjectPipeline();

app.Run();

namespace ShapeUp
{
    public partial class Program;
}
