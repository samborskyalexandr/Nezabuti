namespace Nezabuti.Api.Models;

public static class PlanCodes
{
    public const string Memory = "Memory";
    public const string Story = "Story";
    public const string Legacy = "Legacy";
    public const string Custom = "Custom";

    public static readonly HashSet<string> Standard =
    [
        Memory, Story, Legacy, Custom
    ];
}
