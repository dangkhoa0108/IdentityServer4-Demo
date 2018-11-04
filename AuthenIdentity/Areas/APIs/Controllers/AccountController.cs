using System;
using System.Net.Http;
using System.Threading.Tasks;
using AuthenIdentity.Models;
using IdentityModel.Client;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace AuthenIdentity.Areas.APIs.Controllers
{
    [Area("APIs")]
    [Route("apis/[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody]RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = new ApplicationUser { UserName = model.UserName, FirstName = model.FirstName, LastName = model.LastName, Email = model.Email};

            var result = await _userManager.CreateAsync(user, model.Password);

            string role = "Basic User";

            if (result.Succeeded)
            {
                if (await _roleManager.FindByNameAsync(role) == null)
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
                await _userManager.AddToRoleAsync(user, role);
                await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("userName", user.UserName));
                await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("firstName", user.FirstName));
                await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("lastName", user.LastName));
                await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("email", user.Email));
                await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("role", role));

                return Ok(new ProfileViewModel(user));
            }

            return BadRequest(result.Errors);


        }

        [HttpPost]
        public async Task<IActionResult> Test(string username, string password)
        {
            var disco = await DiscoveryClient.GetAsync("https://localhost:44321/");
            if(disco.IsError) throw new Exception(disco.Error);
            var client= new TokenClient(disco.TokenEndpoint, "ro.angular","secret");
            var token= await client.RequestResourceOwnerPasswordAsync(username, password, "api1");
            var a = new HttpClient();
            a.SetBearerToken(token.AccessToken);
            return new ObjectResult(a.GetStringAsync("https://localhost:44321/api/values/Get").Result);
        }

        //[HttpPost]
        //public async Task<object> Login(string userName, string password)
        //{
        //    var result = await _signInManager.PasswordSignInAsync(userName, password, false, false);
        //    if (result.Succeeded)
        //    {
        //        var appUser = _userManager.Users.SingleOrDefault(x => x.UserName == userName);
        //        if (appUser != null) return await GenerateJwtToken(appUser.Email, appUser);
        //    }
        //    throw new ApplicationException("INVALID_LOGIN_ATTEMPT");
        //}
        //private async Task<object> GenerateJwtToken(string email, IdentityUser user)
        //{
        //    var claims = new List<Claim>
        //    {
        //        new Claim(JwtRegisteredClaimNames.Sub, email),
        //        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        //        new Claim(ClaimTypes.NameIdentifier, user.Id)
        //    };

        //    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtKey"]));
        //    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        //    var expires = DateTime.Now.AddDays(Convert.ToDouble(_configuration["JwtExpireDays"]));

        //    var token = new JwtSecurityToken(
        //        _configuration["JwtIssuer"],
        //        _configuration["JwtIssuer"],
        //        claims,
        //        expires: expires,
        //        signingCredentials: creds
        //    );

        //    return new JwtSecurityTokenHandler().WriteToken(token);
        //}
    }
}