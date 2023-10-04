using BunnyNetChallenge;
using BunnyNetChallenge.ObsoleteContainersMonitoring;
using BunnyNetChallenge.RequestProcessors;
using Docker.DotNet;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    x => x.EnableAnnotations());

//add Docker API client
var dockerSettings = builder.Configuration.GetSection("DockerClientSettings").Get<DockerClientSettings>();
builder.Services.AddSingleton<IDockerClient>(new DockerClientConfiguration(new Uri(dockerSettings.BaseUrl)).CreateClient());

//add channels for requests processing
var createContainersChannel = Channel.CreateUnbounded<CreateContainerRequest>();
builder.Services.AddSingleton(createContainersChannel);
var stopContainersChannel = Channel.CreateUnbounded<StopContainerRequest>();
builder.Services.AddSingleton(stopContainersChannel);
builder.Services.AddTransient<IRequestProcessor<CreateContainerRequest>,  CreateContainerRequestProcessor>();
builder.Services.AddTransient<IRequestProcessor<StopContainerRequest>, StopContainerRequestProcessor>();

builder.Services.AddSingleton<IContainersStateCache, ContainersStateCache>();

//Obsolete - I've done it before Zvone's response :)
//add docker events processing
//builder.Services.AddSingleton<IContainersMonitoringService, ContainersMonitoringService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

var cancellationTokenSource = new CancellationTokenSource();
var cancellationToken = cancellationTokenSource.Token;

//var dockerEventProcessor = app.Services.GetRequiredService<IContainersMonitoringService>();
//var eventsMonitoringTask = Task.Run(() => dockerEventProcessor.StartMonitoringAsync(cancellationToken));

// Start worker threads to process requests
var createContainersProcessor = app.Services.GetRequiredService<IRequestProcessor<CreateContainerRequest>>();
var createTask = Task.Run(()=>createContainersProcessor.StartProcessingAsync(cancellationToken));
var stopContainersProcessor = app.Services.GetRequiredService<IRequestProcessor<StopContainerRequest>>();
var stopTask = Task.Run(() => stopContainersProcessor.StartProcessingAsync(cancellationToken));

// Shutdown gracefully on application stop
app.Lifetime.ApplicationStopped.Register(() => cancellationTokenSource.Cancel());

app.Run();

