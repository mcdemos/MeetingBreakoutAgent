using System;
using System.Threading.Tasks;

namespace MeetingBreakout.WebApp.Services;

public interface ISharePointService {
  Task AddBreakoutSelectionAsync(string name, string alias, string option, DateTimeOffset timestamp);
}
