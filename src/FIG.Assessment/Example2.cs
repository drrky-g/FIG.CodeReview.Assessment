using System.Security.Claims;
using FIG.Assessment.Interfaces;
using FIG.Assessment.Models.Requests;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FIG.Assessment;

public class Example2 : Controller
{
    private readonly ILoginService _loginService;
    public Example2(ILoginService loginService)
        => _loginService = loginService;

    //I'm aware of an exploit penetration testers use where they will check the response time of requests like this
    //A quick response time indicates that the user they tried to log in as didn't exist in database
    //A longer one indicates that they got past the user check and got to the hash check
    //Using controls like rate limiting, some kind of token verification from the post, and login attempt limits
    //helps protect from brute-forcing an endpoint like this
    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromForm] LoginRequest model)
    {
        if(!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var user = await _loginService.GetUserByUsernameAsync(model.UserName);
        
        // first check user exists by the given username
        if (user == null)
            return Redirect("/Error?msg=invalid_username");
        
        // then check password is correct
        if(!_loginService.IsPasswordValid(model.Password, user))
            return Redirect("/Error?msg=invalid_password");


        // if we get this far, we have a real user. sign them in
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, $"{user.UserId}")
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(principal);

        return this.Redirect(model.ReturnUrl);
    }
}


