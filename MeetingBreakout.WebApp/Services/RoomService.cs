using System;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Azure.Identity;
using System.Linq;

namespace MeetingBreakout.WebApp.Services
{
    public class RoomEntity : ITableEntity
    {
        public string PartitionKey { get; set; } // Option "1", "2", "3"
        public string RowKey { get; set; } // Meeting ID (Conversation ID)
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string Status { get; set; } = "Free"; // "Free", "Assigned"
        public string MeetingLink { get; set; }
        public string AssignedParticipantId { get; set; }
        public string AssignedParticipantName { get; set; }
        public bool IsOrganizerPresent { get; set; }
        public bool IsParticipantPresent { get; set; }
    }

    public class RoomService
    {
        private readonly TableClient _tableClient;

        public RoomService(IConfiguration configuration)
        {
            // Assuming connection string for simplicity, or use Managed Identity uri
            var storageAccountName = configuration["Storage:AccountName"];
            var tableName = configuration["Storage:TableName"] ?? "BreakoutRooms";
            var accountUrl = $"https://{storageAccountName}.table.core.windows.net";

            if (!string.IsNullOrEmpty(storageAccountName))
            {
                var credential = new DefaultAzureCredential();
                _tableClient = new TableClient(new Uri(accountUrl), tableName, credential);
            }
            else
            {
                // Fallback for local dev if connection string provided
                var connectionString = configuration["Storage:ConnectionString"];
                if (!string.IsNullOrEmpty(connectionString))
                {
                    _tableClient = new TableClient(connectionString, tableName);
                }
            }
        }

        public async Task EnsureTableExistsAsync()
        {
            if (_tableClient != null)
            {
                await _tableClient.CreateIfNotExistsAsync();
            }
        }

        public async Task<RoomEntity?> GetFreeRoomAsync(string option)
        {
            if (_tableClient == null) return null;

            // Query for Free rooms in the partition (Option)
            var query = _tableClient.QueryAsync<RoomEntity>(filter: $"PartitionKey eq '{option}' and Status eq 'Free'");
            
            await foreach (var room in query)
            {
                return room; // Return the first free room
            }
            return null;
        }

        public async Task AssignRoomAsync(RoomEntity room, string participantId, string participantName)
        {
            room.Status = "Assigned";
            room.AssignedParticipantId = participantId;
            room.AssignedParticipantName = participantName;
            room.IsParticipantPresent = false;
            room.IsOrganizerPresent = false;
             // Reset join status on new assignment? 
             // Requirement: "status should be changed in the moment where the assignment is made"
            
            await _tableClient.UpdateEntityAsync(room, room.ETag, TableUpdateMode.Replace);
        }

        public async Task<RoomEntity?> GetRoomByMeetingIdAsync(string meetingId)
        {
             // Optimization: Query by RowKey
             var query = _tableClient.QueryAsync<RoomEntity>(filter: $"RowKey eq '{meetingId}'");
             await foreach (var room in query)
             {
                 return room;
             }
             return null;
        }

        public async Task UpdatePresenceAsync(string meetingId, string userId, bool isJoining)
        {
            var room = await GetRoomByMeetingIdAsync(meetingId);
            if (room == null) return;

            bool changed = false;

            if (userId == room.AssignedParticipantId)
            {
                if (room.IsParticipantPresent != isJoining)
                {
                    room.IsParticipantPresent = isJoining;
                    changed = true;
                }
            }
            else
            {
                // Assume any other user is an organizer checking in
                if (room.IsOrganizerPresent != isJoining)
                {
                    room.IsOrganizerPresent = isJoining;
                    changed = true;
                }
            }

            if (changed)
            {
                // Logic: "when both ... leave the room, its status should become free again"
                // If this update causes both to be absent
                if (room.Status == "Assigned" && !room.IsOrganizerPresent && !room.IsParticipantPresent)
                {
                    // Only free if we just processed a LEAVE
                    if (!isJoining) 
                    {
                         room.Status = "Free";
                         room.AssignedParticipantId = null;
                         room.AssignedParticipantName = null;
                    }
                }

                await _tableClient.UpdateEntityAsync(room, room.ETag, TableUpdateMode.Replace);
            }
        }
    }
}
