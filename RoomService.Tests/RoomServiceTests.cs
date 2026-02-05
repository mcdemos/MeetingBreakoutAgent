using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Azure;
using Azure.Data.Tables;

using Moq;

using RoomService;

namespace RoomService.Tests {
  [TestClass]
  public class RoomServiceTests {
    [TestMethod]
    public async Task GetFreeRoomAsync_ReturnsRoom_WhenRoomExists() {
      // Arrange
      var mockTableClient = new Mock<TableClient>();
      var expectedRoom = new RoomEntity {
        PartitionKey = "1",
        RowKey = "room1",
        Status = "Free",
        MeetingLink = "http://link",
        AssignedParticipantId = null,
        AssignedParticipantName = null
      };

      var page = Page<RoomEntity>.FromValues([expectedRoom], null, Mock.Of<Response>());
      var asyncPageable = AsyncPageable<RoomEntity>.FromPages([page]);

      mockTableClient
        .Setup(c => c.QueryAsync<RoomEntity>(It.Is<string>(f => f.Contains("Status eq 'Free'")), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
        .Returns(asyncPageable);

      var service = new RoomService(mockTableClient.Object);

      // Act
      var result = await service.GetFreeRoomAsync("1");

      // Assert
      Assert.IsNotNull(result);
      Assert.AreEqual("room1", result.RowKey);
    }
  }
}
