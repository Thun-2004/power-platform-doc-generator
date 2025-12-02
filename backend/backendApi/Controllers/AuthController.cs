
using Microsoft.AspNetCore.Mvc; 
using backendApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
namespace backendApi.Controllers; 

//import model into controller 

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IConfiguration _config;

    public AuthController(UserManager<IdentityUser> userManager, IConfiguration config)
    {
        _userManager = userManager;
        _config      = config;
    } 

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        var user = new IdentityUser { UserName = model.Email, Email = model.Email };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { message = "User registered" });
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return Unauthorized("Invalid credentials");

        var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
        if (!passwordValid)
            return Unauthorized("Invalid credentials");

        var token = GenerateJwtToken(user);
        return Ok(new { token });
    }

    private string GenerateJwtToken(IdentityUser user)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new { userId = User.FindFirstValue(ClaimTypes.NameIdentifier) });
    }
    
    // [HttpPost("Register")]
    // public async Task<IActionResult> Register([FromBody] RegisterModel model)
    // {   
    //     User u1 = new User { Email = model.Email , Name = model.Username };
    //     users.Add(u1); 
    //     return Ok(u1); 
    // }

    // [HttpPost("Login")]
    // public async Task<IActionResult> Login([FromBody] LoginModel model)
    // {
        
    // }
}
