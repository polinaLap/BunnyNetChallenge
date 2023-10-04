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
        private readonly IContainersStateCache _containersStateCache;

        public ContainersController(
            Channel<CreateContainerRequest> createContainersChannel, 
            Channel<StopContainerRequest> stopContainersChannel, 
            IContainersStateCache containersStateCache)
        {
            _createContainersChannel = createContainersChannel;
            _stopContainersChannel = stopContainersChannel;
            _containersStateCache = containersStateCache;
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

        [HttpGet]
        [SwaggerOperation(Summary = "Get a containers status by id.")]
        public ContainerStateModel Get(string id)
        {
            return _containersStateCache.Get(id);
        }

        [HttpGet]
        [Route("/list")]
        [SwaggerOperation(Summary = "Get a list of containers statuses.")]
        public IEnumerable<ContainerStateModel> GetList([Range(0, 100)]int pageSize = 10, [Range(0,100)] int page = 1)
        {
            return _containersStateCache.GetPaginatedList(pageSize, page);
        }
    }
}
