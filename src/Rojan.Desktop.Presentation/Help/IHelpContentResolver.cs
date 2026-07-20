using Rojan.Desktop.Application.Help;

namespace Rojan.Desktop.Presentation.Help;

/// <summary>Phase 26: Smart Context Help. The one place a <see cref="HelpTopicDto"/>'s <see cref="HelpTopicDto.KeyPrefix"/> is expanded into actual localized text - see <see cref="ResolvedHelpContent"/>'s own doc comment for why this has to live in Presentation.</summary>
public interface IHelpContentResolver
{
    public ResolvedHelpContent Resolve(HelpTopicDto topic);
}
