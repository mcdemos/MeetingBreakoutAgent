using Microsoft.AspNetCore.Mvc;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Builder;
using System.Threading.Tasks;

namespace MeetingBreakout.WebApp.Controllers
{
    [Route("api/messages")]
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly IAgentHttpAdapter _adapter;
        private readonly IAgent _agent;

        public BotController(IAgentHttpAdapter adapter, IAgent agent)
        {
            _adapter = adapter;
            _agent = agent;
        }

        [HttpPost]
        public async Task PostAsync()
        {
            await _adapter.ProcessAsync(Request, Response, _agent);
        }
    }
}
