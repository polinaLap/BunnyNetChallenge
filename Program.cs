using BunnyNetChallenge;
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

// Start worker threads to process requests
//Task.Run(() => ProcessRequests(createContainersChannel.));
//Task.Run(() => ProcessRequests(stopContainersChannel));

app.Run();

