using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SPEAgentWithRetrieval.Core.Models;
using SPEAgentWithRetrieval.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register configuration
builder.Services.Configure<AzureAIFoundryOptions>(
    builder.Configuration.GetSection(AzureAIFoundryOptions.SectionName));
builder.Services.Configure<Microsoft365Options>(
    builder.Configuration.GetSection(Microsoft365Options.SectionName));
builder.Services.Configure<ChatSettingsOptions>(
    builder.Configuration.GetSection(ChatSettingsOptions.SectionName));

// Register services
builder.Services.AddScoped<IRetrievalService, CopilotRetrievalService>();
builder.Services.AddScoped<IFoundryService, FoundryService>();
builder.Services.AddScoped<IChatService, ChatService>();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
