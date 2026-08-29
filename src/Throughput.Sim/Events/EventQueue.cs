namespace Throughput.Sim;

public sealed class EventQueue
{
    private readonly SortedSet<SimEvent> _events = new(new EventOrdering());

    private sealed class EventOrdering : IComparer<SimEvent>
    {
        public int Compare(SimEvent a, SimEvent b)
        {
            int c = a.Time.CompareTo(b.Time);
            if (c != 0) return c;
            return a.Sequence.CompareTo(b.Sequence);
        }
    }

    public void Push(SimEvent ev) => _events.Add(ev);

    public bool TryPop(out SimEvent ev)
    {
        if (_events.Count == 0)
        {
            ev = default;
            return false;
        }

        var min = _events.Min;
        _events.Remove(min);
        ev = min;
        return true;
    }

    public int Count => _events.Count;
}
