﻿using BlazorAuthenticationLearn.Server.Data;
using BlazorAuthenticationLearn.Shared.Models;
using BlazorAuthenticationLearn.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlazorAuthenticationLearn.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly PostgresqlDataContext _context;

    public AuthController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, PostgresqlDataContext context)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
    }

    [HttpPost("Login")]
    public async Task<ActionResult<CurrentUser>> Login(LoginRequest loginRequest)
    {
        var user = await _userManager.FindByNameAsync(loginRequest.UserName);
        if (user == null) return BadRequest("Could not find your account.");
        var singInResult = await _signInManager.CheckPasswordSignInAsync(user, loginRequest.Password, false);
        if (!singInResult.Succeeded) return BadRequest("Password is incorrect.");
        await _signInManager.SignInAsync(user, loginRequest.RememberMe);
        return Ok();
    }

    [HttpPost("Register")]
    public async Task<ActionResult<CurrentUser>> CreateUser(RegisterRequest registerRequest)
    {
        var checkEmail = await _context.Users
            .Where(x => x.Email == registerRequest.Email)
            .FirstOrDefaultAsync();
        
        if (checkEmail != null) return BadRequest("Email is already in use.");
        
        var user = new ApplicationUser
        {
            UserName = registerRequest.UserName,
            Email = registerRequest.Email
        };
        
        var result = await _userManager.CreateAsync(user, registerRequest.Password);
        if (!result.Succeeded) return BadRequest(result.Errors.FirstOrDefault()?.Description);
        
        return Ok(await Login(new LoginRequest
        {
            UserName = registerRequest.UserName,
            Password = registerRequest.Password
        }));
    }

    [HttpPost("Logout"), Authorize]
    public async Task<ActionResult<OkResult>> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok();
    }
    
    [HttpGet("CurrentUserInfo")]
    public async Task<ActionResult<CurrentUser>> CurrentUserInfo()
    {
        return Ok(new CurrentUser
        {
            IsAuthenticated = User.Identity.IsAuthenticated,
            UserName = User.Identity.Name,
            Claims = User.Claims
                .ToDictionary(c => c.Type, c => c.Value)
        });
    }
}
