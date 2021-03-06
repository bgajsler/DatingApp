using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;

namespace DatingApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController] // comment this out to turn off Model State validation and Request Message routing
    public class AuthController : ControllerBase    // ControllerBase class doesn't support views, but Controller class does
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;

        public AuthController(IAuthRepository repo, IConfiguration config)
        {
            this._repo = repo;
            this._config = config;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        // public async Task<IActionResult> Register([FromBody]UserForRegisterDto userForRegisterDto) if [ApiController] is disabled
        public async Task<IActionResult> Register(UserForRegisterDto userForRegisterDto)
        {
            // validate request

            // if APIController is turned off
            // if(!ModelState.IsValid)
            //     return BadRequest(ModelState);

            userForRegisterDto.Username = userForRegisterDto.Username.ToLower();

            if(await _repo.UserExists(userForRegisterDto.Username ))
                return BadRequest("Username already exists!");

            var userToCreate = new User
            {
                Username = userForRegisterDto.Username 
            };

            var createdUse = await _repo.Register(userToCreate, userForRegisterDto.Password );

            // CreateAtRoute()
            return StatusCode(201);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserForRegisterDto userForLoginDto)
        {
            var userFromRepo = await _repo.Login(userForLoginDto.Username, userForLoginDto.Password);
            if(userFromRepo == null)
                return Unauthorized();            
 
            var  claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userFromRepo.Id.ToString()),
                new Claim(ClaimTypes.Name, userFromRepo.Username)      
            };            

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Token").Value));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new { token = tokenHandler.WriteToken(token) });
        }
    }
}