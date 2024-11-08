using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FIG.Assessment.Models.Database;
[Table("User")]
public class UserDb
{
    [Key]
    public int UserId { get; set; }
    public string UserName { get; set; }
    public string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
}