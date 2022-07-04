using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _DataContext;
        private readonly ITokenService _ITokenService;
        public AccountController(DataContext dataContext, ITokenService tokenService)
        {
            _DataContext = dataContext;
            _ITokenService = tokenService;
        }
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if (await UsernameExists(registerDto.Username)) return BadRequest("Username is taken");

            using var hmac = new HMACSHA512();
            var user = new AppUser()
            {
                UserName = registerDto.Username,
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSalt = hmac.Key
            };
            _DataContext.AppUser.Add(user);
            await _DataContext.SaveChangesAsync();
            return new UserDto
            {
                Username = registerDto.Username,
                Token = _ITokenService.CreateToken(user)
            };
        }
        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _DataContext.AppUser.SingleOrDefaultAsync(x => x.UserName == loginDto.Username);
            if (user is null) return Unauthorized("Invalid username");

            var hmac = new HMACSHA512(user.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid username");
            }
            return new UserDto
            {
                Username = loginDto.Username,
                Token = _ITokenService.CreateToken(user)
            };
        }
        public async Task<bool> UsernameExists(string Username)
        {
            return await _DataContext.AppUser.AnyAsync(x => x.UserName == Username);
        }
    }
}