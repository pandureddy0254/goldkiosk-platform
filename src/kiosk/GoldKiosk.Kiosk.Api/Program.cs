using GoldKiosk.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddDefaultOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapDefaultOpenApi();

app.Run();
