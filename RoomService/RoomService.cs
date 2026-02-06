using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure;
using Azure.Data.Tables;
using Azure.Identity;

namespace RoomService;

public class RoomEntity: ITableEntity {
  public required string PartitionKey { get; set; } // Option "1", "2", "3"
  public required string RowKey { get; set; } // Meeting ID (Conversation ID)
  public DateTimeOffset? Timestamp { get; set; }
  public ETag ETag { get; set; }

  public string Status { get; set; } = "Free"; // "Free", "Assigned"
  public string? MeetingLink { get; set; }
  public string? AssignedParticipantId { get; set; }
  public string? AssignedParticipantName { get; set; }
  public bool IsOrganizerPresent { get; set; }
  public bool IsParticipantPresent { get; set; }
}

public class RoomService: IRoomService {
private readonly TableClient? _tableClient; // Nullable if config missing
private readonly ILogger<RoomService> _logger;

public RoomService(TableClient tableClient, ILogger<RoomService> logger) {
  _tableClient = tableClient;
  _logger = logger;
}

public RoomService(IConfiguration configuration, ILogger<RoomService> logger) {
  _logger = logger;
  // Assuming connection string for simplicity, or use Managed Identity uri
  var storageAccountName = configuration["Storage:AccountName"];
  var tableName = configuration["Storage:TableName"] ?? "BreakoutRooms";
  var accountUrl = $"https://{storageAccountName}.table.core.windows.net";

  if (!string.IsNullOrEmpty(storageAccountName)) {
    var credential = new DefaultAzureCredential();
    _tableClient = new TableClient(new Uri(accountUrl), tableName, credential);
  } else {
    // Fallback for local dev if connection string provided
    var connectionString = configuration["Storage:ConnectionString"];
    if (!string.IsNullOrEmpty(connectionString)) {
      _tableClient = new TableClient(connectionString, tableName);
    }
    if (_tableClient == null) {
      _logger.LogWarning("TableClient not initialized. Missing Storage configuration.");
    }
  }
}

  public async Task EnsureTableExistsAsync() {
    if (_tableClient != null) {
      await _tableClient.CreateIfNotExistsAsync();
    }
  }

  public async Task<RoomEntity?> GetFreeRoomAsync(string option) {
    if (_tableClient == null) {
      return null;
    }

    var query = _tableClient.QueryAsync<RoomEntity>(filter: $"PartitionKey eq '{option}' and Status eq 'Free'");

    await foreach (var room in query) {
      return room;
    }
    return null;
  }

  public async Task AssignRoomAsync(RoomEntity room, string participantId, string participantName) {
    if (_tableClient == null) {
      return;
    }

    room.Status = "Assigned";
    room.AssignedParticipantId = participantId;
    room.AssignedParticipantName = participantName;
    room.IsParticipantPresent = false;
    room.IsOrganizerPresent = false;
    // Reset join status on new assignment? 
    // Requirement: "status should be changed in the moment where the assignment is made"

    await _tableClient.UpdateEntityAsync(room, room.ETag, TableUpdateMode.Replace);
    _logger.LogInformation("Assigned room {RoomName} to {ParticipantName} ({ParticipantId})", room.RowKey, participantName, participantId);
  }

  public async Task<RoomEntity?> GetRoomByMeetingIdAsync(string meetingId) {
    if (_tableClient == null) {
      return null;
    }

    var query = _tableClient.QueryAsync<RoomEntity>(filter: $"RowKey eq '{meetingId}'");
    await foreach (var room in query) {
      return room;
    }
    return null;
  }

  public async Task UpdatePresenceAsync(string meetingId, string userId, bool isJoining) {
    if (_tableClient == null) {
      return;
    }

    var room = await GetRoomByMeetingIdAsync(meetingId);
    if (room == null) {
      return;
    }

    bool changed = false;

    if (userId == room.AssignedParticipantId) {
      if (room.IsParticipantPresent != isJoining) {
        room.IsParticipantPresent = isJoining;
        changed = true;
      }
    } else {
      // Assume any other user is an organizer checking in
      if (room.IsOrganizerPresent != isJoining) {
        room.IsOrganizerPresent = isJoining;
        changed = true;
      }
    }

    if (changed) {
      // Logic: "when both ... leave the room, its status should become free again"
      // If this update causes both to be absent
      if (room.Status == "Assigned" && !room.IsOrganizerPresent && !room.IsParticipantPresent) {
        // Only free if we just processed a LEAVE
        if (!isJoining) {
          room.Status = "Free";
          room.AssignedParticipantId = null;
          room.AssignedParticipantName = null;
        }
      }

      await _tableClient.UpdateEntityAsync(room, room.ETag, TableUpdateMode.Replace);
    }
  }

  public async Task InitializeRoomsAsync() {
    if (_tableClient == null) return;
    try {
        await EnsureTableExistsAsync();

        string[] options = ["1", "2", "3"];
        foreach (var option in options) {
          for (int i = 1; i <= 10; i++) {
            var room = new RoomEntity {
              PartitionKey = option,
              RowKey = $"Room_{option}_{i}",
              Status = "Free",
              MeetingLink = $"https://teams.microsoft.com/l/meetup-join/mock_link_{option}_{i}",
              IsOrganizerPresent = false,
              IsParticipantPresent = false,
              AssignedParticipantId = null,
              AssignedParticipantName = null
            };
            await _tableClient.UpsertEntityAsync(room, TableUpdateMode.Replace);
          }
        }
        _logger.LogInformation("Rooms initialized successfully.");
    } catch (Exception ex) {
        _logger.LogError(ex, "Error initializing rooms.");
        throw;
    }
  }
}
