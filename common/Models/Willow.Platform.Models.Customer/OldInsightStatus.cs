namespace Willow.Platform.Models
{
    public enum OldInsightStatus
    {
        Open = 0,
        Acknowledged = 10,
        InProgress = 20,
        Closed = 30,
    }

    public enum InsightStatus
    {
        Open = 0,
        Ignored = 10,
        InProgress = 20,
        Resolved = 30,
        New = 40,
        Deleted = 50
    }
}