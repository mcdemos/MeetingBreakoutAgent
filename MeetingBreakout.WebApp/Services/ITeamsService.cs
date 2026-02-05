using System.Threading;
using System.Threading.Tasks;

using Microsoft.Agents.Builder;
using Microsoft.Agents.Extensions.Teams.Models;

namespace MeetingBreakout.WebApp.Services;

public interface ITeamsService {
  Task<TeamsMeetingParticipant> GetMeetingParticipantAsync(ITurnContext turnContext, string? meetingId = null, string? participantId = null, string? tenantId = null, CancellationToken cancellationToken = default);
  Task<TeamsChannelAccount> GetMemberAsync(ITurnContext turnContext, string userId, CancellationToken cancellationToken = default);
  Task<TeamsPagedMembersResult> GetPagedMembersAsync(ITurnContext turnContext, int? pageSize = null, string? continuationToken = null, CancellationToken cancellationToken = default);
}
