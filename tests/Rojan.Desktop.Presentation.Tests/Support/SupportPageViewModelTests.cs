using Rojan.Desktop.Application.Support;
using Rojan.Desktop.Presentation.ViewModels.Support;

namespace Rojan.Desktop.Presentation.Tests.Support;

public sealed class SupportPageViewModelTests
{
    private static (SupportPageViewModel Sut, StubSupportMessageService Messages, StubDevelopmentApplicationService Applications) CreateSut()
    {
        var messages = new StubSupportMessageService();
        var applications = new StubDevelopmentApplicationService();
        var sut = new SupportPageViewModel(new StubRojanBrandConfiguration(), messages, applications);
        return (sut, messages, applications);
    }

    [Fact]
    public void Constructor_ExposesBrandConfigurationValues()
    {
        var (sut, _, _) = CreateSut();

        Assert.Equal("rojanai.ir", sut.WebsiteUrl);
        Assert.Equal("09114050112", sut.PhoneNumber);
        Assert.Equal("support@rojanai.ir", sut.SupportEmail);
        Assert.Equal("api.rojanai.ir", sut.ApiBaseUrl);
    }

    [Fact]
    public void SubmitMessageCommand_CanExecute_OnlyWhenSubjectAndBodyProvided()
    {
        var (sut, _, _) = CreateSut();

        Assert.False(sut.SubmitMessageCommand.CanExecute(null));

        sut.MessageSubject = "Subject";
        Assert.False(sut.SubmitMessageCommand.CanExecute(null));

        sut.MessageBody = "Body";
        Assert.True(sut.SubmitMessageCommand.CanExecute(null));
    }

    [Fact]
    public void SubmitMessageCommand_Succeeds_ClearsFieldsAndSetsStatus()
    {
        var (sut, messages, _) = CreateSut();
        sut.MessageType = SupportMessageType.SuperAdmin;
        sut.MessageSubject = "Urgent issue";
        sut.MessageBody = "Please help";
        sut.MessageSenderEmail = "sara@example.com";

        sut.SubmitMessageCommand.Execute(null);

        Assert.Equal("Urgent issue", messages.LastSubmittedSubject);
        Assert.Equal(string.Empty, sut.MessageSubject);
        Assert.Equal(string.Empty, sut.MessageBody);
        Assert.NotNull(sut.MessageStatus);
        Assert.Null(sut.MessageError);
    }

    [Fact]
    public void SubmitMessageCommand_ServiceThrows_SetsErrorAndKeepsFields()
    {
        var (sut, messages, _) = CreateSut();
        messages.ThrowsOnSubmit = true;
        sut.MessageSubject = "Subject";
        sut.MessageBody = "Body";

        sut.SubmitMessageCommand.Execute(null);

        Assert.NotNull(sut.MessageError);
        Assert.Null(sut.MessageStatus);
        Assert.Equal("Subject", sut.MessageSubject);
    }

    [Fact]
    public void SubmitApplicationCommand_CanExecute_OnlyWhenRequiredFieldsProvided()
    {
        var (sut, _, _) = CreateSut();

        Assert.False(sut.SubmitApplicationCommand.CanExecute(null));

        sut.ApplicantFirstName = "Sara";
        sut.ApplicantLastName = "Ahmadi";
        Assert.False(sut.SubmitApplicationCommand.CanExecute(null));

        sut.CollaborationArea = "Backend";
        Assert.True(sut.SubmitApplicationCommand.CanExecute(null));
    }

    [Fact]
    public async Task SubmitApplicationCommand_Succeeds_ClearsFieldsAndSetsStatus()
    {
        var (sut, _, applications) = CreateSut();
        sut.ApplicantFirstName = "Sara";
        sut.ApplicantLastName = "Ahmadi";
        sut.CollaborationArea = "Backend";
        sut.ApplicantEmail = "sara@example.com";

        sut.SubmitApplicationCommand.Execute(null);

        Assert.Equal(string.Empty, sut.ApplicantFirstName);
        Assert.Equal(string.Empty, sut.CollaborationArea);
        Assert.NotNull(sut.ApplicationStatus);
        Assert.Null(sut.ApplicationError);
        Assert.Single(await applications.GetAllAsync());
    }

    [Fact]
    public void SubmitApplicationCommand_ServiceThrows_SetsErrorAndKeepsFields()
    {
        var (sut, _, applications) = CreateSut();
        applications.ThrowsOnSubmit = true;
        sut.ApplicantFirstName = "Sara";
        sut.ApplicantLastName = "Ahmadi";
        sut.CollaborationArea = "Backend";

        sut.SubmitApplicationCommand.Execute(null);

        Assert.NotNull(sut.ApplicationError);
        Assert.Null(sut.ApplicationStatus);
        Assert.Equal("Sara", sut.ApplicantFirstName);
    }
}
