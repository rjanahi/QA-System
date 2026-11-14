using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;
using DocumentSystemApi.Dtos;
using DocumentSystemApi.Services;

namespace DocumentSystemApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")] ///api/Authentication
    public class AuthenticationController : ControllerBase
    {
        private readonly SystemAuthenticationService _authService;

        public AuthenticationController(
            SystemAuthenticationService authService)
        {
            _authService = authService;
        }

        [AllowAnonymous]
        [HttpGet("csrf-token")]
        public IActionResult GetCsrfToken(
            [FromServices] IAntiforgery antiforgery)
        {
            var tokens = antiforgery.GetAndStoreTokens(
                HttpContext
            );

            Response.Headers.CacheControl = "no-store";

            return Ok(new
            {
                token = tokens.RequestToken
            });
        }


        [AllowAnonymous]
        [HttpPost("register")] //POST /api/Authentication/register
        public async Task<IActionResult> Register(
            [FromBody] RegisterDto dto)
        {
            try
            {
                var user = await _authService.RegisterAsync(dto);

                if (user == null)
                {
                    return Conflict(new
                    {
                        message = "Email is already registered."
                    });
                }

                return StatusCode(201, new
                {
                    message = "Registration successful.",
                    userId = user.Id,
                    fullName = user.FullName,
                    email = user.Email
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("login")] //POST /api/Authentication/login
        public async Task<IActionResult> Login(
            [FromBody] LoginDto dto)
        {
            var user = await _authService.LoginAsync(dto);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            // Create identity claims
            var claims = new List<Claim>
    {
        new Claim(
            ClaimTypes.NameIdentifier,
            user.Id.ToString()
        ),

        new Claim(
            ClaimTypes.Name,
            user.FullName
        ),

        new Claim(
            ClaimTypes.Email,
            user.Email
        )
    };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            // Create the authentication cookie
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false
                }
            );

            return Ok(new
            {
                message = "Login successful.",
                userId = user.Id,
                fullName = user.FullName,
                email = user.Email
            });
        }

        [Authorize]
        [HttpGet("me")] //GET /api/Authentication/me
        public IActionResult GetCurrentUser()
        {
            return Ok(new
            {
                userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                ),

                fullName = User.Identity?.Name,

                email = User.FindFirstValue(
                    ClaimTypes.Email
                )
            });
        }

        [Authorize]
        [HttpPost("logout")] //POST /api/Authentication/logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return Ok(new
            {
                message = "Logout successful."
            });
        }
    }
}