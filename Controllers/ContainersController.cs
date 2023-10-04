using BunnyNetChallenge.ContainerStateCache;
using BunnyNetChallenge.Models;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Threading.Channels;
using ContainerState = BunnyNetChallenge.Models.ContainerState;

namespace BunnyNetChallenge.Controllers
{
    [ApiController]
    [Route("containers")]
    public class ContainersController : ControllerBase
    {
        private readonly Channel<CreateContainerRequest> _createContainersChannel;
        private readonly Channel<StopContainerRequest> _stopContainersChannel;
        private readonly IContainersStateCache _containersStateCache;
        private readonly IDockerClient _dockerClient;

        public ContainersController(
            Channel<CreateContainerRequest> createContainersChannel,
            Channel<StopContainerRequest> stopContainersChannel,
            IContainersStateCache containersStateCache,
            IDockerClient dockerClient)
        {
            _createContainersChannel = createContainersChannel;
            _stopContainersChannel = stopContainersChannel;
            _containersStateCache = containersStateCache;
            _dockerClient = dockerClient;
        }

        [HttpPost("/start")]
        [SwaggerOperation(Summary ="Create and start container by image name.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Start(CreateContainerRequest request)
        {
            var container = _containersStateCache.Get(request.ContainerName);
            if (container != null)
            {
                return BadRequest("Container with this name already exists.");
            }

            _createContainersChannel.Writer.TryWrite(request);

            return Ok();
        }

        [HttpPost("/stop")]
        [SwaggerOperation(Summary = "Stop container by container name.")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Stop(StopContainerRequest request)
        {
            var container = _containersStateCache.Get(request.ContainerName);
            if (container == null)
            {
                return NotFound();
            }

            _stopContainersChannel.Writer.TryWrite(request);

            return NoContent();
        }

        [HttpGet]
        [Route("/list")]
        [SwaggerOperation(Summary = "Get a list of containers statuses.")]
        public async Task<IEnumerable<ContainerStateModel>> GetList([Range(0, 100)]int pageSize = 10, [Range(0,100)] int page = 1)
        {
            //no pagination on Docker API side, so retrieve all
            var containers = await _dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters()
                {
                    All = true,
                });

            foreach (var container in containers)
            {
                UpdateCachedContainer(container);
            }

            return _containersStateCache.GetPaginatedList(pageSize, page);
        }

        private void UpdateCachedContainer(ContainerListResponse container)
        {
            var containerState = container.State;
            if (!string.IsNullOrWhiteSpace(containerState)
                && Enum.TryParse(typeof(ContainerState), containerState, ignoreCase: true, out var state))
            {
                var name = container.Names.First().Replace("/", string.Empty);
                var cachedContainer = _containersStateCache.Get(name);
                cachedContainer ??= new ContainerStateModel
                {
                    ContainerId = container.ID,
                    ContainerName = name,
                };
                cachedContainer.State = (ContainerState)state;

                _containersStateCache.AddOrUpdate(cachedContainer);
            }
        }
    }
}
