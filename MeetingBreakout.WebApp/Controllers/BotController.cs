using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Builder;

namespace MeetingBreakout.WebApp.Controllers {
  [Route("api/messages")]
  [ApiController]
  public class BotController(IAgentHttpAdapter adapter, IAgent agent): ControllerBase {
    private readonly IAgentHttpAdapter _adapter = adapter;
    private readonly IAgent _agent = agent;

    [HttpPost]
    public async Task PostAsync() {
      await _adapter.ProcessAsync(Request, Response, _agent);
    }
  }
}
