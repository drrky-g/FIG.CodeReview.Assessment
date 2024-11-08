using System.ComponentModel.DataAnnotations;

namespace FIG.Assessment.Models.Requests;

public class LoginRequest
{
    [Required]
    public string UserName { get; set; }
    [Required]
    public string Password { get; set; }
    public string ReturnUrl { get; set; }
}