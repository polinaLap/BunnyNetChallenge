using System.Threading.Channels;

namespace BunnyNetChallenge.RequestProcessors
{
    public class CreateContainerRequestProcessor : BaseRequestProcessor<CreateContainerRequest>
    {
        public CreateContainerRequestProcessor(Channel<CreateContainerRequest> requestChannel) : base(requestChannel)
        {
        }

        protected override Task ProcessRequestAsync(CreateContainerRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
