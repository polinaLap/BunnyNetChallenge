using BunnyNetChallenge.ContainerStateCache;
using BunnyNetChallenge.RequestProcessors;
using BunnyNetChallenge.Settings;
using Docker.DotNet;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    x => x.EnableAnnotations());

//add Docker API client
var dockerSettings = builder.Configuration.GetSection("DockerClientSettings").Get<DockerClientSettings>();
builder.Services.AddSingleton<IDockerClient>(new DockerClientConfiguration(new Uri(dockerSettings.BaseUrl)).CreateClient());

//add channels and background services for requests processing
var createContainersChannel = Channel.CreateUnbounded<CreateContainerRequest>();
builder.Services.AddSingleton(createContainersChannel);
var stopContainersChannel = Channel.CreateUnbounded<StopContainerRequest>();
builder.Services.AddSingleton(stopContainersChannel);
builder.Services.AddHostedService<CreateContainerRequestProcessor>();
builder.Services.AddHostedService<StopContainerRequestProcessor>();

//add shared dictionary
builder.Services.AddSingleton<IContainersStateCache, ContainersStateCache>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

