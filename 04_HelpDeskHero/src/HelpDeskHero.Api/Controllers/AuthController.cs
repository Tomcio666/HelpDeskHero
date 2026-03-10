using HelpDeskHero.Api.Domain;
using HelpDeskHero.Api.Infrastructure.Services;
using HelpDeskHero.Shared.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HelpDeskHero.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TokenService _tokenService;
    private readonly RefreshTokenService _refreshTokenService;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        TokenService tokenService,
        RefreshTokenService refreshTokenService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponseDto>> Login([FromBody] LoginRequestDto dto, CancellationToken ct)
    {
        var user = await _userManager.FindByNameAsync(dto.UserName);
        if (user is null || !user.IsActive)
        {
            return Unauthorized();
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return Unauthorized();
        }

        var (accessToken, accessExp) = await _tokenService.CreateAccessTokenAsync(user);
        var (refreshToken, refreshExp) = await _refreshTokenService.CreateAsync(
            user.Id,
            string.IsNullOrWhiteSpace(dto.DeviceName) ? "Browser" : dto.DeviceName,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new TokenResponseDto
        {
            AccessToken = accessToken,
            AccessTokenExpiresAtUtc = accessExp,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAtUtc = refreshExp,
            UserName = user.UserName ?? string.Empty,
            DisplayName = user.DisplayName,
            Roles = roles.ToArray()
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponseDto>> Refresh([FromBody] RefreshRequestDto dto, CancellationToken ct)
    {
        var refresh = await _refreshTokenService.GetActiveByRawTokenAsync(dto.RefreshToken, ct);
        if (refresh is null || refresh.User is null || !refresh.IsActive || !refresh.User.IsActive)
        {
            return Unauthorized();
        }

        await _refreshTokenService.RevokeAsync(refresh, ct);

        var user = refresh.User;
        var (accessToken, accessExp) = await _tokenService.CreateAccessTokenAsync(user);
        var (newRefreshToken, refreshExp) = await _refreshTokenService.CreateAsync(
            user.Id,
            string.IsNullOrWhiteSpace(dto.DeviceName) ? "Browser" : dto.DeviceName,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            ct);

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new TokenResponseDto
        {
            AccessToken = accessToken,
            AccessTokenExpiresAtUtc = accessExp,
            RefreshToken = newRefreshToken,
            RefreshTokenExpiresAtUtc = refreshExp,
            UserName = user.UserName ?? string.Empty,
            DisplayName = user.DisplayName,
            Roles = roles.ToArray()
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequestDto dto, CancellationToken ct)
    {
        var refresh = await _refreshTokenService.GetActiveByRawTokenAsync(dto.RefreshToken, ct);
        if (refresh is not null)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (refresh.UserId == currentUserId)
            {
                await _refreshTokenService.RevokeAsync(refresh, ct);
            }
        }

        return NoContent();
    }
}