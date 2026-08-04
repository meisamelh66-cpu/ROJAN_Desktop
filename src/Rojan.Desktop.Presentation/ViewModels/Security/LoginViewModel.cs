using System.Windows.Input;
using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.ViewModels.Security;

/// <summary>
/// Owner App Login Experience. Drives the Login screen: collects email/
/// password, calls <see cref="IAuthenticationService.SignInWithCredentialsAsync"/>,
/// and maps every failure mode Task 3 names to a distinct, localized inline
/// message rather than a generic "something went wrong" - <see cref="ErrorMessage"/>
/// is set directly (never a dialog/MessageBox), since a login form's own
/// inline error is the conventional place a user looks first.
/// <see cref="SignedIn"/> is how the Shell (which owns the actual
/// <c>Window</c>) knows to close this screen and proceed - this ViewModel
/// has no <c>Window</c> reference of its own, matching this app's existing
/// "a ViewModel never holds a live Window" convention (e.g.
/// <c>MainWindow.xaml.cs</c>'s own doc comment).
/// </summary>
public sealed class LoginViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;

    private string _email = string.Empty;
    private string _password = string.Empty;
    private string? _errorMessage;
    private bool _isBusy;

    public LoginViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
        SignInCommand = new AsyncRelayCommand(_ => SignInAsync(), _ => CanSignIn());
    }

    public string Email
    {
        get => _email;
        set
        {
            if (SetProperty(ref _email, value))
            {
                ErrorMessage = null;
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                ErrorMessage = null;
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public ICommand SignInCommand { get; }

    /// <summary>Raised once sign-in actually succeeds - the Shell subscribes to know when to close the Login window and proceed to the Dashboard.</summary>
    public event EventHandler? SignedIn;

    private bool CanSignIn() => !IsBusy;

    private async Task SignInAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = Strings.Login_Error_MissingCredentials;
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await _authenticationService.SignInWithCredentialsAsync(Email.Trim(), Password).ConfigureAwait(true);
            SignedIn?.Invoke(this, EventArgs.Empty);
        }
        catch (ApiAuthenticationException)
        {
            ErrorMessage = Strings.Login_Error_InvalidCredentials;
        }
        catch (ApiConnectivityException)
        {
            ErrorMessage = Strings.Login_Error_Network;
        }
        catch (ApiTimeoutException)
        {
            ErrorMessage = Strings.Login_Error_Network;
        }
        catch (ApiException)
        {
            ErrorMessage = Strings.Login_Error_Generic;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
