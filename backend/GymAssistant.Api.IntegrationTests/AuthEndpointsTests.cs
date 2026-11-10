using System.Net;
using System.Net.Http.Json;
using GymAssistant.Api.Authentication;
using GymAssistant.Api.Data;
using GymAssistant.Api.Endpoints;
using GymAssistant.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class AuthEndpointsTests : IClassFixture<GymAssistantWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly GymAssistantWebApplicationFactory _factory;
    public AuthEndpointsTests(GymAssistantWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    [Fact]
    public async Task Register_ShouldCreateUser_WhenRequestIsValid()
    {
        //Arrange
        using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = Guid.NewGuid().ToString()+"@gmail.com";
        var pass = "Qweqwe123!@#";
        var req = new RegisterRequest(email,pass);
        //Act
        Assert.False(await db.Users.AnyAsync(x => x.Email == email));
        var res = await _client.PostAsJsonAsync("/api/auth/register",req);

        //Assert   
        Assert.Equal(System.Net.HttpStatusCode.OK,res.StatusCode);
        db.ChangeTracker.Clear();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email);

        Assert.NotNull(user);
        Assert.Equal(email,user.Email);
        Assert.Equal(email,user.UserName);
        Assert.True(await userManager.CheckPasswordAsync(user, pass));
    }


    [Fact]
    public async Task Register_ShouldRejectDuplicateEmail()
    {

        //Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user1 = await UserHelper.CreateUserAsync(userManager);
        Assert.NotNull(user1);
        Assert.NotNull(user1.Email);
        var pass = "Qweqwe123!@#";
        var req = new RegisterRequest(user1.Email,pass);
        //Act
        var res = await _client.PostAsJsonAsync("/api/auth/register",req);
        //Assert
        Assert.Equal(HttpStatusCode.BadRequest,res.StatusCode);
        var usersCount = await db.Users.CountAsync(x => x.Email == user1.Email);
        Assert.Equal(1, usersCount);
    }


[Fact]
public async Task Register_ShouldRejectWeakPassword()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var email = $"{Guid.NewGuid()}@example.com";
    var request = new RegisterRequest(email, "123");

    // Act
    var response = await _client.PostAsJsonAsync(
        "/api/auth/register", request);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    Assert.False(await db.Users.AnyAsync(x => x.Email == email));
}


[Theory]
[InlineData("", "Password123!")]
[InlineData("test@example.com", "")]
[InlineData("", "")]
[InlineData(" ", "Password123!")]
public async Task Login_WithEmptyFields_ShouldReturnBadRequest(
    string email, string password)
{
    // Arrange
    var request = new LoginRequest(email, password);

    // Act
    var response = await _client.PostAsJsonAsync("/api/auth/login", request);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}

[Fact]
public async Task Login_WhenUserDoesNotExist_ShouldReturnUnauthorized()
{
    // Arrange
    var email = $"{Guid.NewGuid()}@example.com";
    var request = new LoginRequest(email, "Password123!");

    // Act
    var response = await _client.PostAsJsonAsync("/api/auth/login", request);

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.False(response.Headers.Contains("Set-Cookie"));
}

[Fact]
public async Task Login_WithInvalidPassword_ShouldReturnUnauthorized()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    var user = await UserHelper.CreateUserAsync(userManager);
    var request = new LoginRequest(user.Email!, "WrongPassword123!");

    // Act
    var response = await _client.PostAsJsonAsync("/api/auth/login", request);

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    Assert.False(response.Headers.Contains("Set-Cookie"));

    var tokenCount = await db.RefreshTokens
        .CountAsync(x => x.UserId == user.Id);

    Assert.Equal(0, tokenCount);
}

[Fact]
public async Task Login_WithValidCredentials_ShouldReturnAccessTokenAndExpiration()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    var password = "Password123!";
    var user = await UserHelper.CreateUserAsync(userManager, password);
    var request = new LoginRequest(user.Email!, password);

    // Act
    var response = await _client.PostAsJsonAsync("/api/auth/login", request);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

    Assert.NotNull(loginResponse);
    Assert.False(string.IsNullOrWhiteSpace(loginResponse.AccessToken));
    Assert.True(loginResponse.ExpiresAtUtc > DateTime.UtcNow);
}

[Fact]
public async Task Login_WithValidCredentials_ShouldCreateRefreshTokenInDatabase()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var refreshTokenService = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();

    var password = "Password123!";
    var user = await UserHelper.CreateUserAsync(userManager, password);
    var request = new LoginRequest(user.Email!, password);

    // Act
    var response = await _client.PostAsJsonAsync("/api/auth/login", request);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var tokens = await db.RefreshTokens
        .Where(x => x.UserId == user.Id)
        .ToListAsync();

    var storedToken = Assert.Single(tokens);

    Assert.NotNull(storedToken.TokenHash);
    Assert.NotEmpty(storedToken.TokenHash);
    Assert.True(storedToken.ExpiresAtUtc > storedToken.CreatedAtUtc);

    Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));

    var cookie = Assert.Single(
        cookies,
        x => x.StartsWith("refreshToken="));

    var rawToken = cookie.Split(';')[0]
        .Substring("refreshToken=".Length);

    var expectedHash = refreshTokenService.HashRefreshToken(rawToken);

    Assert.True(expectedHash.SequenceEqual(storedToken.TokenHash));
    Assert.Equal(
        refreshTokenService.HashRefreshToken(rawToken),
        storedToken.TokenHash);
}

[Fact]
public async Task Login_WithValidCredentials_ShouldSetSecureHttpOnlyCookie()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    var password = "Password123!";
    var user = await UserHelper.CreateUserAsync(userManager, password);
    var request = new LoginRequest(user.Email!, password);

    // Act
    var response = await _client.PostAsJsonAsync("/api/auth/login", request);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));

    var cookie = Assert.Single(
        cookies,
        x => x.StartsWith("refreshToken="));

    Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
}


[Theory]
[InlineData("")]
[InlineData(" ")]
[InlineData("   ")]
public async Task ForgotPassword_WithEmptyEmail_ShouldReturnBadRequest(
    string email)
{
    // Arrange
    var request = new ForgotPasswordRequest(email);

    // Act
    var response = await _client.PostAsJsonAsync(
        "/api/auth/forgot-password", request);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}

[Fact]
public async Task ForgotPassword_WhenUserDoesNotExist_ShouldReturnOk()
{
    // Arrange
    var email = $"{Guid.NewGuid()}@example.com";
    var request = new ForgotPasswordRequest(email);

    // Act
    var response = await _client.PostAsJsonAsync(
        "/api/auth/forgot-password", request);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}

[Fact]
public async Task ForgotPassword_ShouldReturnSameResponse_ForExistingAndNonExistingUser()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();

    var user = await UserHelper.CreateUserAsync(userManager);
    var unknownEmail = $"{Guid.NewGuid()}@example.com";

    // Act
    var existingResponse = await _client.PostAsJsonAsync(
        "/api/auth/forgot-password",
        new ForgotPasswordRequest(user.Email!));

    var unknownResponse = await _client.PostAsJsonAsync(
        "/api/auth/forgot-password",
        new ForgotPasswordRequest(unknownEmail));

    // Assert
    Assert.Equal(HttpStatusCode.OK, existingResponse.StatusCode);
    Assert.Equal(existingResponse.StatusCode, unknownResponse.StatusCode);

    var existingBody = await existingResponse.Content.ReadAsStringAsync();
    var unknownBody = await unknownResponse.Content.ReadAsStringAsync();

    Assert.Equal(existingBody, unknownBody);
}

[Fact]
public async Task ForgotPassword_WhenUserExists_ShouldSendResetEmail()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();

    var fakeEmailSender = scope.ServiceProvider
        .GetRequiredService<IEmailSender>() as FakeEmailSender;

    Assert.NotNull(fakeEmailSender);

    var user = await UserHelper.CreateUserAsync(userManager);

    // Act
    var response = await _client.PostAsJsonAsync(
        "/api/auth/forgot-password",
        new ForgotPasswordRequest(user.Email!));

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var sentEmail = Assert.Single(
        fakeEmailSender.SentEmails,
        x => x.Email == user.Email);

    Assert.Contains("/reset-password?", sentEmail.Link);

    var uri = new Uri(sentEmail.Link);
    var query = QueryHelpers.ParseQuery(uri.Query);

    var userId = query["userId"].ToString();
    var token = query["token"].ToString();
   
    Assert.Equal(user.Id.ToString(), userId);
    Assert.False(string.IsNullOrWhiteSpace(token));

    var tokenValid = await userManager.VerifyUserTokenAsync(
        user,
        userManager.Options.Tokens.PasswordResetTokenProvider,
        UserManager<ApplicationUser>.ResetPasswordTokenPurpose,
        token);

    Assert.True(tokenValid);
}

[Fact]
public async Task ForgotPassword_WhenUserDoesNotExist_ShouldNotSendEmail()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();

    var fakeEmailSender = scope.ServiceProvider
        .GetRequiredService<IEmailSender>() as FakeEmailSender;

    Assert.NotNull(fakeEmailSender);

    var email = $"{Guid.NewGuid()}@example.com";

    // Act
    var response = await _client.PostAsJsonAsync(
        "/api/auth/forgot-password",
        new ForgotPasswordRequest(email));

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    Assert.DoesNotContain(
        fakeEmailSender.SentEmails,
        x => x.Email == email);
}


[Fact]
public async Task ResetPassword_ShouldChangePassword_WhenOK()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();

    var emailSender = (FakeEmailSender)scope.ServiceProvider
        .GetRequiredService<IEmailSender>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var user = await UserHelper.CreateUserAsync(userManager);

    var oldPassword = "Password123!";
    var newPassword = "MyStrongNewPassword!123";

    Assert.True(await userManager.CheckPasswordAsync(user, oldPassword));

    // Act - Generate reset link
    var forgotResponse = await _client.PostAsJsonAsync(
        "/api/auth/forgot-password",
        new ForgotPasswordRequest(user.Email!));

    Assert.Equal(HttpStatusCode.OK, forgotResponse.StatusCode);

    var emailFromFake = Assert.Single(
        emailSender.SentEmails,
        x => x.Email == user.Email);

    var uri = new Uri(emailFromFake.Link);
    var query = QueryHelpers.ParseQuery(uri.Query);

    var userId = query["userId"].ToString();
    var token = query["token"].ToString();

    Assert.Equal(user.Id.ToString(), userId);
    Assert.False(string.IsNullOrWhiteSpace(token));

    // Act - Reset password
    var resetRequest = new ResetPasswordRequest(
        userId, token, newPassword);

    var resetResponse = await _client.PostAsJsonAsync(
        "/api/auth/reset-password", resetRequest);

    // Assert
    Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

    // Reload user from database
    db.ChangeTracker.Clear();
    var updatedUser = await userManager.FindByIdAsync(user.Id.ToString());

    Assert.NotNull(updatedUser);

    Assert.False(
        await userManager.CheckPasswordAsync(updatedUser, oldPassword));

    Assert.True(
        await userManager.CheckPasswordAsync(updatedUser, newPassword));
}


[Fact]
public async Task ResetPassword_ShouldRevokeAllActiveRefreshTokens_WhenSuccessful()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();

    var db = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();

    var emailSender = (FakeEmailSender)scope.ServiceProvider
        .GetRequiredService<IEmailSender>();

    var password = "Password123!";
    var user = await UserHelper.CreateUserAsync(userManager, password);

    var loginRequest = new LoginRequest(user.Email!, password);

    // Create two active sessions
    var loginResponse1 = await _client.PostAsJsonAsync(
        "/api/auth/login", loginRequest);

    var loginResponse2 = await _client.PostAsJsonAsync(
        "/api/auth/login", loginRequest);

    Assert.Equal(HttpStatusCode.OK, loginResponse1.StatusCode);
    Assert.Equal(HttpStatusCode.OK, loginResponse2.StatusCode);

    var tokensBefore = await db.RefreshTokens
        .Where(x => x.UserId == user.Id)
        .AsNoTracking()
        .ToListAsync();

    Assert.Equal(2, tokensBefore.Count);
    Assert.All(tokensBefore, x => Assert.Null(x.RevokedAtUtc));

    // Generate password reset link
    var forgotResponse = await _client.PostAsJsonAsync(
        "/api/auth/forgot-password",
        new ForgotPasswordRequest(user.Email!));

    Assert.Equal(HttpStatusCode.OK, forgotResponse.StatusCode);

    var sentEmail = Assert.Single(
        emailSender.SentEmails,
        x => x.Email == user.Email);

    var uri = new Uri(sentEmail.Link);
    var query = QueryHelpers.ParseQuery(uri.Query);

    var userId = query["userId"].ToString();
    var token = query["token"].ToString();

    Assert.Equal(user.Id.ToString(), userId);
    Assert.False(string.IsNullOrWhiteSpace(token));

    // Act
    var resetResponse = await _client.PostAsJsonAsync(
        "/api/auth/reset-password",
        new ResetPasswordRequest(
            userId,
            token,
            "MyStrongNewPassword!123"));

    // Assert
    Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

    var tokensAfter = await db.RefreshTokens
        .Where(x => x.UserId == user.Id)
        .AsNoTracking()
        .ToListAsync();

    Assert.Equal(2, tokensAfter.Count);

    Assert.All(tokensAfter, x =>
    {
        Assert.NotNull(x.RevokedAtUtc);
        Assert.True(x.RevokedAtUtc <= DateTime.UtcNow);
    });
}


[Fact]
public async Task ResetPassword_ShouldNotRevokeRefreshTokens_WhenTokenIsInvalid()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();

    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();

    var password = "Password123!";
    var user = await UserHelper.CreateUserAsync(userManager, password);

    var loginResponse = await _client.PostAsJsonAsync(
        "/api/auth/login",
        new LoginRequest(user.Email!, password));

    Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

    var invalidToken = Guid.NewGuid().ToString();

    // Act
    var response = await _client.PostAsJsonAsync(
        "/api/auth/reset-password",
        new ResetPasswordRequest(
            user.Id.ToString(),
            invalidToken,
            "MyStrongNewPassword!123"));

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    var refreshToken = await db.RefreshTokens
        .AsNoTracking()
        .SingleAsync(x => x.UserId == user.Id);

    Assert.Null(refreshToken.RevokedAtUtc);

    db.ChangeTracker.Clear();

    var updatedUser = await userManager.FindByIdAsync(user.Id.ToString());

    Assert.NotNull(updatedUser);
    Assert.True(await userManager.CheckPasswordAsync(updatedUser, password));
}


[Fact]
public async Task ResetPassword_ShouldRejectAlreadyUsedToken()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();

    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var fakeEmailSender = (FakeEmailSender)scope.ServiceProvider
        .GetRequiredService<IEmailSender>();

    var user = await UserHelper.CreateUserAsync(userManager);

    var firstPassword = "MyNewStrongPassword123!";
    var secondPassword = "MyNewStrongPassword456@";

    // Generate reset token
    var forgotResponse = await _client.PostAsJsonAsync(
        "/api/auth/forgot-password",
        new ForgotPasswordRequest(user.Email!));

    Assert.Equal(HttpStatusCode.OK, forgotResponse.StatusCode);

    var sentEmail = Assert.Single(
        fakeEmailSender.SentEmails,
        x => x.Email == user.Email);

    var uri = new Uri(sentEmail.Link);
    var query = QueryHelpers.ParseQuery(uri.Query);

    var userId = query["userId"].ToString();
    var token = query["token"].ToString();

    Assert.Equal(user.Id.ToString(), userId);
    Assert.False(string.IsNullOrWhiteSpace(token));

    // Act - First password reset
    var firstResponse = await _client.PostAsJsonAsync(
        "/api/auth/reset-password",
        new ResetPasswordRequest(userId, token, firstPassword));

    // Act - Reuse the same token
    var secondResponse = await _client.PostAsJsonAsync(
        "/api/auth/reset-password",
        new ResetPasswordRequest(userId, token, secondPassword));

    // Assert
    Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
    Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);

    db.ChangeTracker.Clear();

    var updatedUser = await userManager.FindByIdAsync(user.Id.ToString());

    Assert.NotNull(updatedUser);

    Assert.True(
        await userManager.CheckPasswordAsync(updatedUser, firstPassword));

    Assert.False(
        await userManager.CheckPasswordAsync(updatedUser, secondPassword));

    Assert.False(
        await userManager.CheckPasswordAsync(updatedUser, "Password123!"));
}


}