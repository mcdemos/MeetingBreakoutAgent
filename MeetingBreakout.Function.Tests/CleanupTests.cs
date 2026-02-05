using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RoomService;

namespace MeetingBreakout.Function.Tests
{
    [TestClass]
    public class CleanupTests
    {
        private Mock<ILogger<Cleanup>> _loggerMock = null!;
        private Mock<IRoomService> _roomServiceMock = null!;
        private Cleanup _cleanupFunction = null!;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<Cleanup>>();
            _roomServiceMock = new Mock<IRoomService>();
            _cleanupFunction = new Cleanup(_loggerMock.Object, _roomServiceMock.Object);
        }

        [TestMethod]
        public async Task RunHttp_CallsInitializeRoomsAsync()
        {
            // Arrange
            var reqMock = new Mock<HttpRequest>();

            // Act
            var result = await _cleanupFunction.RunHttp(reqMock.Object);

            // Assert
            _roomServiceMock.Verify(rs => rs.InitializeRoomsAsync(), Times.Once);
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        }

        [TestMethod]
        public async Task RunTimer_CallsInitializeRoomsAsync()
        {
            // Arrange
            // TimerInfo is tricky to mock as it doesn't have a public constructor or interface in some versions
            // But we can just pass null if the code doesn't access properties, or use a workaround.
            // In the Cleanup.cs code provided: "TimerInfo myTimer" is unused inside the method body except in signature.
            TimerInfo timerInfo = null!; 

            // Act
            await _cleanupFunction.RunTimer(timerInfo);

            // Assert
            _roomServiceMock.Verify(rs => rs.InitializeRoomsAsync(), Times.Once);
        }
    }
}
