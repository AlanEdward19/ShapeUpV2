using ShapeUp.Configurations;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProjectServices(builder.Configuration);

var app = builder.Build();
app.UseProjectPipeline();

app.Run();