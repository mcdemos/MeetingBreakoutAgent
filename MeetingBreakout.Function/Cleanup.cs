using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using RoomService;

namespace MeetingBreakout.Function {
  public class Cleanup(ILogger<Cleanup> logger, IRoomService roomService) {
    private readonly ILogger<Cleanup> _logger = logger;
    private readonly IRoomService _roomService = roomService;

    [Function("CleanupHttp")]
    public async Task<IActionResult> RunHttp([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req) {
      _logger.LogInformation("C# HTTP trigger function processed a request.");
      await _roomService.InitializeRoomsAsync();
      return new OkObjectResult("Rooms initialized/cleaned up.");
    }

    [Function("CleanupTimer")]
    public async Task RunTimer([TimerTrigger("0 0 2 * * *")] TimerInfo myTimer) {
      _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
      await _roomService.InitializeRoomsAsync();
    }
  }
}
