using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using Eidos.Models;
using Eidos.Pages.Account;

namespace Eidos.Tests.Unit.Pages;

public class LoginModelTests
{
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IConfiguration> _configurationMock;

    public LoginModelTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var contextAccessorMock = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            contextAccessorMock.Object,
            claimsFactoryMock.Object,
            null!, null!, null!, null!);

        _configurationMock = new Mock<IConfiguration>();
    }

    [Fact]
    public void IsGoogleConfigured_BothCredentialsSet_ReturnsTrue()
    {
        // Arrange
        _configurationMock.Setup(c => c["Authentication:Google:ClientId"]).Returns("test-client-id");
        _configurationMock.Setup(c => c["Authentication:Google:ClientSecret"]).Returns("test-client-secret");

        var model = new LoginModel(
            _signInManagerMock.Object,
            _userManagerMock.Object,
            _configurationMock.Object);

        // Act
        var result = model.IsGoogleConfigured;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsGoogleConfigured_ClientIdMissing_ReturnsFalse()
    {
        // Arrange
        _configurationMock.Setup(c => c["Authentication:Google:ClientId"]).Returns((string?)null);
        _configurationMock.Setup(c => c["Authentication:Google:ClientSecret"]).Returns("test-client-secret");

        var model = new LoginModel(
            _signInManagerMock.Object,
            _userManagerMock.Object,
            _configurationMock.Object);

        // Act
        var result = model.IsGoogleConfigured;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsGoogleConfigured_ClientSecretMissing_ReturnsFalse()
    {
        // Arrange
        _configurationMock.Setup(c => c["Authentication:Google:ClientId"]).Returns("test-client-id");
        _configurationMock.Setup(c => c["Authentication:Google:ClientSecret"]).Returns((string?)null);

        var model = new LoginModel(
            _signInManagerMock.Object,
            _userManagerMock.Object,
            _configurationMock.Object);

        // Act
        var result = model.IsGoogleConfigured;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsGoogleConfigured_EmptyCredentials_ReturnsFalse()
    {
        // Arrange
        _configurationMock.Setup(c => c["Authentication:Google:ClientId"]).Returns("");
        _configurationMock.Setup(c => c["Authentication:Google:ClientSecret"]).Returns("");

        var model = new LoginModel(
            _signInManagerMock.Object,
            _userManagerMock.Object,
            _configurationMock.Object);

        // Act
        var result = model.IsGoogleConfigured;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMicrosoftConfigured_BothCredentialsSet_ReturnsTrue()
    {
        // Arrange
        _configurationMock.Setup(c => c["Authentication:Microsoft:ClientId"]).Returns("test-client-id");
        _configurationMock.Setup(c => c["Authentication:Microsoft:ClientSecret"]).Returns("test-client-secret");

        var model = new LoginModel(
            _signInManagerMock.Object,
            _userManagerMock.Object,
            _configurationMock.Object);

        // Act
        var result = model.IsMicrosoftConfigured;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMicrosoftConfigured_ClientIdMissing_ReturnsFalse()
    {
        // Arrange
        _configurationMock.Setup(c => c["Authentication:Microsoft:ClientId"]).Returns((string?)null);
        _configurationMock.Setup(c => c["Authentication:Microsoft:ClientSecret"]).Returns("test-client-secret");

        var model = new LoginModel(
            _signInManagerMock.Object,
            _userManagerMock.Object,
            _configurationMock.Object);

        // Act
        var result = model.IsMicrosoftConfigured;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsGitHubConfigured_BothCredentialsSet_ReturnsTrue()
    {
        // Arrange
        _configurationMock.Setup(c => c["Authentication:GitHub:ClientId"]).Returns("test-client-id");
        _configurationMock.Setup(c => c["Authentication:GitHub:ClientSecret"]).Returns("test-client-secret");

        var model = new LoginModel(
            _signInManagerMock.Object,
            _userManagerMock.Object,
            _configurationMock.Object);

        // Act
        var result = model.IsGitHubConfigured;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsGitHubConfigured_ClientSecretMissing_ReturnsFalse()
    {
        // Arrange
        _configurationMock.Setup(c => c["Authentication:GitHub:ClientId"]).Returns("test-client-id");
        _configurationMock.Setup(c => c["Authentication:GitHub:ClientSecret"]).Returns((string?)null);

        var model = new LoginModel(
            _signInManagerMock.Object,
            _userManagerMock.Object,
            _configurationMock.Object);

        // Act
        var result = model.IsGitHubConfigured;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void MultipleProviders_OnlyConfiguredOnesReturnTrue()
    {
        // Arrange - Only GitHub configured
        _configurationMock.Setup(c => c["Authentication:Google:ClientId"]).Returns("");
        _configurationMock.Setup(c => c["Authentication:Google:ClientSecret"]).Returns("");
        _configurationMock.Setup(c => c["Authentication:Microsoft:ClientId"]).Returns((string?)null);
        _configurationMock.Setup(c => c["Authentication:Microsoft:ClientSecret"]).Returns((string?)null);
        _configurationMock.Setup(c => c["Authentication:GitHub:ClientId"]).Returns("github-id");
        _configurationMock.Setup(c => c["Authentication:GitHub:ClientSecret"]).Returns("github-secret");

        var model = new LoginModel(
            _signInManagerMock.Object,
            _userManagerMock.Object,
            _configurationMock.Object);

        // Act & Assert
        Assert.False(model.IsGoogleConfigured);
        Assert.False(model.IsMicrosoftConfigured);
        Assert.True(model.IsGitHubConfigured);
    }

    [Fact]
    public void IsRegisterMode_ModeIsRegister_ReturnsTrue()
    {
        // Arrange
        var model = new LoginModel(
            _signInManagerMock.Object,
            _userManagerMock.Object,
            _configurationMock.Object)
        {
            Mode = "register"
        };

        // Act
        var result = model.IsRegisterMode;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsRegisterMode_ModeIsNull_ReturnsFalse()
    {
        // Arrange
        var model = new LoginModel(
            _signInManagerMock.Object,
            _userManagerMock.Object,
            _configurationMock.Object)
        {
            Mode = null
        };

        // Act
        var result = model.IsRegisterMode;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ToggleUrl_InLoginMode_ReturnsRegisterUrl()
    {
        // Arrange
        var model = new LoginModel(
            _signInManagerMock.Object,
            _userManagerMock.Object,
            _configurationMock.Object)
        {
            Mode = null
        };

        // Act
        var result = model.ToggleUrl;

        // Assert
        Assert.Equal("/Account/Login?mode=register", result);
    }

    [Fact]
    public void ToggleUrl_InRegisterMode_ReturnsLoginUrl()
    {
        // Arrange
        var model = new LoginModel(
            _signInManagerMock.Object,
            _userManagerMock.Object,
            _configurationMock.Object)
        {
            Mode = "register"
        };

        // Act
        var result = model.ToggleUrl;

        // Assert
        Assert.Equal("/Account/Login", result);
    }
}
