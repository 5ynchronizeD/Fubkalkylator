using Fubkalkylator.Core;
using Xunit;

namespace Fubkalkylator.Core.Tests;

public class OrderPlannerTests
{
    [Fact]
    public void Achievable_item_needs_at_least_one_log()
    {
        var plan = OrderPlanner.Plan(new[]
        {
            new OrderItem { ThicknessInches = 2, WidthInches = 6, Quantity = 3 },
        });
        var line = plan.Lines[0];
        Assert.True(line.Achievable);
        Assert.True(line.PiecesPerLog >= 1);
        Assert.True(line.LogsNeeded >= 1);
    }

    [Fact]
    public void More_pieces_never_needs_fewer_logs()
    {
        var few = OrderPlanner.Plan(new[] { new OrderItem { ThicknessInches = 2, WidthInches = 6, Quantity = 2 } });
        var many = OrderPlanner.Plan(new[] { new OrderItem { ThicknessInches = 2, WidthInches = 6, Quantity = 40 } });
        Assert.True(many.Lines[0].LogsNeeded >= few.Lines[0].LogsNeeded);
    }

    [Fact]
    public void Impossible_dimension_is_not_achievable()
    {
        var plan = OrderPlanner.Plan(new[]
        {
            new OrderItem { ThicknessInches = 2, WidthInches = 40, Quantity = 1 }, // bredare än max stock
        });
        Assert.False(plan.Lines[0].Achievable);
        Assert.Equal(0, plan.Lines[0].LogsNeeded);
    }

    [Fact]
    public void Total_logs_sums_the_lines()
    {
        var plan = OrderPlanner.Plan(new[]
        {
            new OrderItem { ThicknessInches = 2, WidthInches = 6, Quantity = 3 },
            new OrderItem { ThicknessInches = 1, WidthInches = 4, Quantity = 5 },
        });
        Assert.Equal(plan.Lines.Sum(l => l.LogsNeeded), plan.TotalLogs);
    }
}
