namespace Throughput.Sim;

public sealed class Item
{
    public required string Id { get; init; }
    public required string ProductId { get; init; }
    public string Type { get; set; } = "";
    public long CreatedAt { get; init; }
    public ItemActivity Activity { get; set; } = ItemActivity.Waiting;
    public ItemLocation Location { get; set; }
    public long ActivityStartTime { get; set; }
    public long[] TimeInActivity { get; } = new long[5];
    public long ShippedAt { get; set; } = -1;
    public List<ItemTraceEntry> Trace { get; } = [];

    public void SetActivity(ItemActivity activity, ItemLocation location, long now)
    {
        if (Activity == activity && Location == location)
        {
            return;
        }

        TimeInActivity[(int)Activity] += now - ActivityStartTime;
        Activity = activity;
        Location = location;
        ActivityStartTime = now;
        Trace.Add(new ItemTraceEntry(now, activity.ToString(), location.Id));
    }
}
