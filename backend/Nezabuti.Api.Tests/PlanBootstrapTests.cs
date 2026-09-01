using Nezabuti.Api.Models;

namespace Nezabuti.Api.Tests;

public class PlanBootstrapTests
{
    [Fact]
    public void PlanCodes_StandardIncludesFourPlans()
    {
        Assert.Equal(4, PlanCodes.Standard.Count);
        Assert.Contains(PlanCodes.Memory, PlanCodes.Standard);
        Assert.Contains(PlanCodes.Story, PlanCodes.Standard);
        Assert.Contains(PlanCodes.Legacy, PlanCodes.Standard);
        Assert.Contains(PlanCodes.Custom, PlanCodes.Standard);
    }

    [Fact]
    public void PlanCodes_ValuesAreStable()
    {
        Assert.Equal("Memory", PlanCodes.Memory);
        Assert.Equal("Story", PlanCodes.Story);
        Assert.Equal("Legacy", PlanCodes.Legacy);
        Assert.Equal("Custom", PlanCodes.Custom);
    }
}
