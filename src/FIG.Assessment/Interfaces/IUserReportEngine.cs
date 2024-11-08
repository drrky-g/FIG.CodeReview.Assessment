using FIG.Assessment.Models.Database;

namespace FIG.Assessment.Interfaces;

public interface IUserReportService
{
    Task<IEnumerable<UserDb>> GetNewUsersAsync(DateTime startingFrom);
    Task<IEnumerable<UserDb>> GetDeactivatedUsersAsync(DateTime startingFrom);
}