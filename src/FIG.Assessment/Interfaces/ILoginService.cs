using FIG.Assessment.Models.Database;

namespace FIG.Assessment.Interfaces;

public interface ILoginService
{
    Task<UserDb?> GetUserByUsernameAsync(string username);
    bool IsPasswordValid(string password, UserDb user);
    
}