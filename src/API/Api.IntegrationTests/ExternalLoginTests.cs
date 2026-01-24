using System.Security.Claims;
using Api.ApiResponses;
using Api.Dtos;
using Api.Identity;
using API.Controllers;
using AutoMapper;
using Core.Entities.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Moq;

namespace Api.IntegrationTests;

/// <summary>
/// Integration tests for external login endpoints.
/// Tests controller behavior with mocked dependencies.
/// </summary>
[TestFixture]
public class ExternalLoginTests
{
    private Mock<UserManager<AppUser>> _userManagerMock = null!;
    private Mock<SignInManager<AppUser>> _signInManagerMock = null!;
    private Mock<IAuthenticationServices> _authServicesMock = null!;
    private Mock<IMapper> _mapperMock = null!;
    private Mock<ILoggerFactory> _loggerFactoryMock = null!;
    private Mock<IWebHostEnvironment> _environmentMock = null!;
    private AccountController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        // Create UserManager mock
        var userStoreMock = new Mock<IUserStore<AppUser>>();
        _userManagerMock = new Mock<UserManager<AppUser>>(
            userStoreMock.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        // Create SignInManager mock
        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var userClaimsPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
        _signInManagerMock = new Mock<SignInManager<AppUser>>(
            _userManagerMock.Object,
            contextAccessorMock.Object,
            userClaimsPrincipalFactoryMock.Object,
            null!, null!, null!, null!);

        // Create other mocks
        _authServicesMock = new Mock<IAuthenticationServices>();
        _mapperMock = new Mock<IMapper>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _environmentMock = new Mock<IWebHostEnvironment>();

        // Setup token settings
        var tokenSettings = new TokenSettings
        {
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7,
            CookieName = "refreshToken"
        };
        _authServicesMock.Setup(x => x.TokenSettings).Returns(tokenSettings);

        var lockoutSettings = new LockoutSettings();
        _authServicesMock.Setup(x => x.LockoutSettings).Returns(lockoutSettings);

        // Setup logger
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger<AccountController>>());

        // Setup environment
        _environmentMock.Setup(x => x.EnvironmentName).Returns("Development");

        // Create controller
        _controller = new AccountController(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _authServicesMock.Object,
            _mapperMock.Object,
            _loggerFactoryMock.Object,
            _environmentMock.Object);

        // Setup HttpContext
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Setup Url helper
        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(x => x.Action(It.IsAny<UrlActionContext>()))
            .Returns("https://localhost/api/account/external-login-callback?returnUrl=/");
        _controller.Url = urlHelperMock.Object;
    }

    #region ExternalLogin Endpoint Tests

    [Test]
    public void ExternalLogin_WithMissingProvider_ReturnsBadRequest()
    {
        // Arrange & Act
        var result = _controller.ExternalLogin(null!, "/");

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequest = (BadRequestObjectResult)result;
        var response = badRequest.Value as ApiResponse;
        Assert.That(response?.Message, Does.Contain("Provider is required"));
    }

    [Test]
    public void ExternalLogin_WithEmptyProvider_ReturnsBadRequest()
    {
        // Arrange & Act
        var result = _controller.ExternalLogin("", "/");

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequest = (BadRequestObjectResult)result;
        var response = badRequest.Value as ApiResponse;
        Assert.That(response?.Message, Does.Contain("Provider is required"));
    }

    [Test]
    public void ExternalLogin_WithInvalidProvider_ReturnsBadRequest()
    {
        // Arrange & Act
        var result = _controller.ExternalLogin("Facebook", "/");

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequest = (BadRequestObjectResult)result;
        var response = badRequest.Value as ApiResponse;
        Assert.That(response?.Message, Does.Contain("Invalid provider"));
    }

    [Test]
    public void ExternalLogin_WithValidProvider_ReturnsChallengeResult()
    {
        // Arrange
        _signInManagerMock.Setup(x => x.ConfigureExternalAuthenticationProperties(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new AuthenticationProperties());

        // Act
        var result = _controller.ExternalLogin("Google", "/");

        // Assert
        Assert.That(result, Is.InstanceOf<ChallengeResult>());
        var challenge = (ChallengeResult)result;
        Assert.That(challenge.AuthenticationSchemes, Does.Contain("Google"));
    }

    [Test]
    public void ExternalLogin_WithCaseInsensitiveProvider_ReturnsChallengeResult()
    {
        // Arrange
        _signInManagerMock.Setup(x => x.ConfigureExternalAuthenticationProperties(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new AuthenticationProperties());

        // Act
        var result = _controller.ExternalLogin("google", "/");

        // Assert
        Assert.That(result, Is.InstanceOf<ChallengeResult>());
    }

    [Test]
    public void ExternalLogin_WithDefaultReturnUrl_ReturnsChallengeResult()
    {
        // Arrange
        _signInManagerMock.Setup(x => x.ConfigureExternalAuthenticationProperties(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new AuthenticationProperties());

        // Act
        var result = _controller.ExternalLogin("Google");

        // Assert
        Assert.That(result, Is.InstanceOf<ChallengeResult>());
    }

    #endregion

    #region GetExternalLogins Endpoint Tests

    [Test]
    public async Task GetExternalLogins_WithNoUser_ReturnsUnauthorized()
    {
        // Arrange
        SetupUnauthenticatedUser();
        _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((AppUser)null!);

        // Act
        var result = await _controller.GetExternalLogins();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetExternalLogins_WithAuthenticatedUser_ReturnsLoginsList()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", "TestUser");
        SetupAuthenticatedUser(user.Email!, user);

        var logins = new List<UserLoginInfo>
        {
            new("Google", "google-key-123", "Google")
        };
        _userManagerMock.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(logins);

        // Act
        var result = await _controller.GetExternalLogins();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result!;
        var returnedLogins = okResult.Value as IEnumerable<ExternalLoginInfoDto>;
        Assert.That(returnedLogins, Is.Not.Null);
        Assert.That(returnedLogins!.Count(), Is.EqualTo(1));
        Assert.That(returnedLogins!.First().LoginProvider, Is.EqualTo("Google"));
    }

    [Test]
    public async Task GetExternalLogins_WithNoLinkedAccounts_ReturnsEmptyList()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", "TestUser");
        SetupAuthenticatedUser(user.Email!, user);

        _userManagerMock.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(new List<UserLoginInfo>());

        // Act
        var result = await _controller.GetExternalLogins();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result!;
        var returnedLogins = okResult.Value as IEnumerable<ExternalLoginInfoDto>;
        Assert.That(returnedLogins, Is.Not.Null);
        Assert.That(returnedLogins!.Count(), Is.EqualTo(0));
    }

    #endregion

    #region LinkExternalLogin Endpoint Tests

    [Test]
    public async Task LinkExternalLogin_WithMissingProvider_ReturnsBadRequest()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", "TestUser");
        SetupAuthenticatedUser(user.Email!);
        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email!))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.LinkExternalLogin(null!, "/");

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task LinkExternalLogin_WithInvalidProvider_ReturnsBadRequest()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", "TestUser");
        SetupAuthenticatedUser(user.Email!);
        _userManagerMock.Setup(x => x.FindByEmailAsync(user.Email!))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.LinkExternalLogin("Facebook", "/");

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequest = (BadRequestObjectResult)result;
        var response = badRequest.Value as ApiResponse;
        Assert.That(response?.Message, Does.Contain("Invalid provider"));
    }

    [Test]
    public async Task LinkExternalLogin_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        SetupUnauthenticatedUser();
        _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((AppUser)null!);

        // Act
        var result = await _controller.LinkExternalLogin("Google", "/");

        // Assert
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task LinkExternalLogin_WithAlreadyLinkedProvider_ReturnsBadRequest()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", "TestUser");
        SetupAuthenticatedUser(user.Email!, user);

        var logins = new List<UserLoginInfo>
        {
            new("Google", "google-key-123", "Google")
        };
        _userManagerMock.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(logins);

        // Act
        var result = await _controller.LinkExternalLogin("Google", "/");

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequest = (BadRequestObjectResult)result;
        var response = badRequest.Value as ApiResponse;
        Assert.That(response?.Message, Does.Contain("already linked"));
    }

    [Test]
    public async Task LinkExternalLogin_WithValidProviderNotLinked_ReturnsChallengeResult()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", "TestUser");
        SetupAuthenticatedUser(user.Email!, user);

        _userManagerMock.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(new List<UserLoginInfo>());

        _signInManagerMock.Setup(x => x.ConfigureExternalAuthenticationProperties(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new AuthenticationProperties());

        // Act
        var result = await _controller.LinkExternalLogin("Google", "/");

        // Assert
        Assert.That(result, Is.InstanceOf<ChallengeResult>());
    }

    #endregion

    #region UnlinkExternalLogin Endpoint Tests

    [Test]
    public async Task UnlinkExternalLogin_WithMissingProvider_ReturnsBadRequest()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", "TestUser");
        SetupAuthenticatedUser(user.Email!, user);

        // Act
        var result = await _controller.UnlinkExternalLogin(null!);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UnlinkExternalLogin_WithEmptyProvider_ReturnsBadRequest()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", "TestUser");
        SetupAuthenticatedUser(user.Email!, user);

        // Act
        var result = await _controller.UnlinkExternalLogin("");

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UnlinkExternalLogin_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        SetupUnauthenticatedUser();
        _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((AppUser)null!);

        // Act
        var result = await _controller.UnlinkExternalLogin("Google");

        // Assert
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task UnlinkExternalLogin_WithNotLinkedProvider_ReturnsNotFound()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", "TestUser");
        SetupAuthenticatedUser(user.Email!, user);

        _userManagerMock.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(new List<UserLoginInfo>());

        // Act
        var result = await _controller.UnlinkExternalLogin("Google");

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task UnlinkExternalLogin_LastLoginWithoutPassword_ReturnsBadRequest()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", "TestUser");
        SetupAuthenticatedUser(user.Email!, user);

        var logins = new List<UserLoginInfo>
        {
            new("Google", "google-key-123", "Google")
        };
        _userManagerMock.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(logins);

        _userManagerMock.Setup(x => x.HasPasswordAsync(user))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UnlinkExternalLogin("Google");

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequest = (BadRequestObjectResult)result;
        var response = badRequest.Value as ApiResponse;
        Assert.That(response?.Message, Does.Contain("Cannot unlink the last external login"));
    }

    [Test]
    public async Task UnlinkExternalLogin_WithPasswordAndLinkedProvider_ReturnsOk()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", "TestUser");
        SetupAuthenticatedUser(user.Email!, user);

        var logins = new List<UserLoginInfo>
        {
            new("Google", "google-key-123", "Google")
        };
        _userManagerMock.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(logins);

        _userManagerMock.Setup(x => x.HasPasswordAsync(user))
            .ReturnsAsync(true);

        _userManagerMock.Setup(x => x.RemoveLoginAsync(user, "Google", "google-key-123"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.UnlinkExternalLogin("Google");

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task UnlinkExternalLogin_WithMultipleLogins_ReturnsOk()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", "TestUser");
        SetupAuthenticatedUser(user.Email!, user);

        var logins = new List<UserLoginInfo>
        {
            new("Google", "google-key-123", "Google"),
            new("Facebook", "facebook-key-456", "Facebook")
        };
        _userManagerMock.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(logins);

        _userManagerMock.Setup(x => x.HasPasswordAsync(user))
            .ReturnsAsync(false);

        _userManagerMock.Setup(x => x.RemoveLoginAsync(user, "Google", "google-key-123"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.UnlinkExternalLogin("Google");

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }

    #endregion

    #region ExternalLoginCallback Tests

    [Test]
    public async Task ExternalLoginCallback_WithRemoteError_RedirectsWithError()
    {
        // Act
        var result = await _controller.ExternalLoginCallback("/", "access_denied");

        // Assert
        Assert.That(result, Is.InstanceOf<RedirectResult>());
        var redirect = (RedirectResult)result;
        Assert.That(redirect.Url, Does.Contain("error=external_login_failed"));
        Assert.That(redirect.Url, Does.Contain("access_denied"));
    }

    [Test]
    public async Task ExternalLoginCallback_WithNoExternalLoginInfo_RedirectsWithError()
    {
        // Arrange
        _signInManagerMock.Setup(x => x.GetExternalLoginInfoAsync(It.IsAny<string>()))
            .ReturnsAsync((ExternalLoginInfo)null!);

        // Act
        var result = await _controller.ExternalLoginCallback("/");

        // Assert
        Assert.That(result, Is.InstanceOf<RedirectResult>());
        var redirect = (RedirectResult)result;
        Assert.That(redirect.Url, Does.Contain("error=external_login_failed"));
    }

    #endregion

    #region LinkExternalLoginCallback Tests

    [Test]
    public async Task LinkExternalLoginCallback_WithRemoteError_RedirectsWithError()
    {
        // Act
        var result = await _controller.LinkExternalLoginCallback("/", "access_denied");

        // Assert
        Assert.That(result, Is.InstanceOf<RedirectResult>());
        var redirect = (RedirectResult)result;
        Assert.That(redirect.Url, Does.Contain("error=link_failed"));
        Assert.That(redirect.Url, Does.Contain("access_denied"));
    }

    [Test]
    public async Task LinkExternalLoginCallback_WithNoExternalLoginInfo_RedirectsWithError()
    {
        // Arrange
        _signInManagerMock.Setup(x => x.GetExternalLoginInfoAsync(It.IsAny<string>()))
            .ReturnsAsync((ExternalLoginInfo)null!);

        // Act
        var result = await _controller.LinkExternalLoginCallback("/");

        // Assert
        Assert.That(result, Is.InstanceOf<RedirectResult>());
        var redirect = (RedirectResult)result;
        Assert.That(redirect.Url, Does.Contain("error=link_failed"));
    }

    #endregion

    #region Helper Methods

    private static AppUser CreateTestUser(string email, string displayName)
    {
        return new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            UserName = email,
            DisplayName = displayName,
            EmailConfirmed = true
        };
    }

    private void SetupAuthenticatedUser(string email, AppUser? user = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, email)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext.HttpContext.User = principal;

        // Setup Users DbSet to return an async-capable queryable
        if (user != null)
        {
            var users = new List<AppUser> { user }.AsAsyncQueryable();
            _userManagerMock.Setup(x => x.Users).Returns(users);
        }
        else
        {
            var users = new List<AppUser>().AsAsyncQueryable();
            _userManagerMock.Setup(x => x.Users).Returns(users);
        }
    }

    private void SetupUnauthenticatedUser()
    {
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal();
        // Setup empty Users DbSet
        var users = new List<AppUser>().AsAsyncQueryable();
        _userManagerMock.Setup(x => x.Users).Returns(users);
    }

    #endregion
}
