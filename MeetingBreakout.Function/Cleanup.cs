using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RoomService;
using System;
using System.Threading.Tasks;

namespace MeetingBreakout.Function
{
    public class Cleanup
    {
        private readonly ILogger<Cleanup> _logger;
        private readonly IRoomService _roomService;

        public Cleanup(ILogger<Cleanup> logger, IRoomService roomService)
        {
            _logger = logger;
            _roomService = roomService;
        }

        [Function("CleanupHttp")]
        public async Task<IActionResult> RunHttp([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            await _roomService.InitializeRoomsAsync();
            return new OkObjectResult("Rooms initialized/cleaned up.");
        }

        [Function("CleanupTimer")]
        public async Task RunTimer([TimerTrigger("0 0 1 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            await _roomService.InitializeRoomsAsync();
        }
    }
}
