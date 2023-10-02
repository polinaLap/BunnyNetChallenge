using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Threading.Channels;

namespace BunnyNetChallenge.Controllers
{
    [ApiController]
    [Route("containers")]
    public class ContainersController : ControllerBase
    {
        private readonly Channel<CreateContainerRequest> _createContainersChannel;
        private readonly Channel<StopContainerRequest> _stopContainersChannel;
        private readonly IDockerClient _dockerClient;

        public ContainersController(IDockerClient dockerClient, Channel<CreateContainerRequest> createContainersChannel, Channel<StopContainerRequest> stopContainersChannel)
        {
            _dockerClient = dockerClient;
            _createContainersChannel = createContainersChannel;
            _stopContainersChannel = stopContainersChannel;
        }

        [HttpPost]
        [SwaggerOperation(Summary ="Create and start container by image name.")]
        public async Task<IActionResult> Post(CreateContainerRequest request, CancellationToken cancellationToken)
        {
            await _createContainersChannel.Writer.WriteAsync(request, cancellationToken);

            return Ok();
            ////pull image if not pulled yet
            //await _dockerClient.Images.CreateImageAsync(
            //    new ImagesCreateParameters
            //    {
            //        FromImage = request.ImageName,
            //        Tag = string.IsNullOrEmpty(request.Tag) ? "latest" : request.Tag,
            //    },
            //    null,
            //    new Progress<JSONMessage>(x => _logger.LogDebug(x.ProgressMessage)));

            ////create container using pulled image
            //var response = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters()
            //{
            //    Image = request.ImageName       
            //});

            ////start created container
            //await _dockerClient.Containers.StartContainerAsync(
            //    response.ID,
            //    new ContainerStartParameters()
            //    );
        }

        [HttpDelete]
        [SwaggerOperation(Summary = "Stop container by container ID.")]
        public async Task<IActionResult> Delete(StopContainerRequest request, CancellationToken cancellationToken)
        {
            await _stopContainersChannel.Writer.WriteAsync(request, cancellationToken);

            return NoContent();
         //   await _dockerClient.Containers.StopContainerAsync(containerId, new ContainerStopParameters(), CancellationToken.None);
        }

        [HttpGet(Name = "GetContainersInfo")]
        [SwaggerOperation(Summary = "Get a list of containers statuses.")]
        public async Task<IEnumerable<ContainerStatusInfo>> GetList([Range(0, 100)]int limit = 10, [Range(0,100)] int page = 1)
        {
            var containers = await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters()
            {
                All = true
            });

            var resultList = containers.Select(x => new ContainerStatusInfo
            {
                ID = x.ID,
                ImageName = x.Image,
                State = x.State,
                Created = x.Created
            })
                .OrderBy(x => x.Created).ToList();

            var startIndex = (page - 1) * limit;
            if (startIndex >= resultList.Count)
                return new List<ContainerStatusInfo>();

            var endIndex = Math.Min(startIndex + limit, resultList.Count);
            return resultList.GetRange(startIndex, endIndex - startIndex);
        }
    }
}
public class CreateContainerRequest
{
    [Required]
    public string ImageName { get; set; }

    public string Tag { get; set; }
}
public class StopContainerRequest
{
    [Required]
    public string ContainerID { get; set; }
}

public class ContainerStatusInfo
{
    public string ID { get; set; }
    public string ImageName { get; set; }
    public string State { get; set; }
    public DateTime Created { get; set; }
}