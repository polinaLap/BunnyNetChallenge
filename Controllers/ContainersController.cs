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
        public IActionResult Post(CreateContainerRequest request)
        {
            Console.WriteLine("Thread {0} is in controller", Thread.CurrentThread.ManagedThreadId);
            _createContainersChannel.Writer.TryWrite(request);

            return Ok();
        }

        [HttpDelete]
        [SwaggerOperation(Summary = "Stop container by container ID.")]
        public IActionResult Delete(StopContainerRequest request)
        {
            _stopContainersChannel.Writer.TryWrite(request);

            return NoContent();
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
