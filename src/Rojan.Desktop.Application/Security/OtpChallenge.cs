namespace Rojan.Desktop.Application.Security;

/// <summary>
/// Owner App Mobile Login: the outcome of <see cref="IAuthenticationService.RequestOtpAsync"/> -
/// how long the issued code is valid for, and how long the caller must wait
/// before requesting another one. Deliberately an Application-layer type,
/// not a Domain one (mirrors why <see cref="IAuthenticationService.SignInWithCredentialsAsync"/>
/// returns plain <see cref="System.Threading.Tasks.Task"/> rather than a
/// Domain type - see that method's own doc comment): <c>MobileOtpLoginViewModel</c>
/// (Presentation) is the caller, and <c>Rojan.Desktop.ArchitectureTests.DependencyDirectionTests.Presentation_ShouldNotDependOnDomainInfrastructureOrShell</c>
/// forbids that layer from referencing anything under <c>Rojan.Desktop.Domain</c>.
/// </summary>
public sealed record OtpChallenge(string PhoneNumber, TimeSpan ExpiresIn, TimeSpan CanResendAfter);
