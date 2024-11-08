using System.Security.Cryptography;
using System.Text;
using FIG.Assessment.Interfaces;
using FIG.Assessment.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace FIG.Assessment.Repositories;

public class UserRepository : ILoginService, IUserReportService
{
    private readonly UserContext _db;
    public UserRepository(UserContext db) =>
        _db = db;
    public async Task<UserDb?> GetUserByUsernameAsync(string username) => 
        await _db.Users.FirstOrDefaultAsync(user => user.UserName == username);
    
    public bool IsPasswordValid(string password, UserDb user)
    {
        var inputHash = MD5.HashData(Encoding.UTF8.GetBytes(password));
        return Encoding.UTF8.GetBytes(user.PasswordHash).SequenceEqual(inputHash);
    }

    public async Task<IEnumerable<UserDb>> GetNewUsersAsync(DateTime startingFrom) => 
        await _db.Users.Where(user => user.CreatedAt > startingFrom)
            .ToListAsync();

    public async Task<IEnumerable<UserDb>> GetDeactivatedUsersAsync(DateTime startingFrom) =>
        await _db.Users.Where(user => user.DeactivatedAt > startingFrom)
            .ToListAsync();
}