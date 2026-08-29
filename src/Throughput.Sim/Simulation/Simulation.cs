namespace Throughput.Sim;

public sealed class Simulation
{
    private readonly Scenario _scenario;
    private readonly EventQueue _events = new();
    private readonly List<SimCommand> _commands;
    private readonly List<SimCommand> _futureCommands = [];
    private readonly Dictionary<string, ProcessDefinition> _processById = [];
    private readonly Dictionary<string, long> _queueWeighted = [];
    private readonly Dictionary<string, int> _queueCurrentLength = [];
    private ulong _sequence;
    private long _taskCounter;
    private long _itemCounter;
    private long _now;
    private long _lastSampleTime;
    private long _wipWeighted;
    private long _peakWip;
    private long _wip;
    private long _eventsProcessed;
    private int _futureCommandIndex;

    public Dictionary<string, Machine> Machines { get; } = [];
    public Dictionary<string, Worker> Workers { get; } = [];
    public Dictionary<string, Item> Items { get; } = [];
    public Dictionary<string, Queue> Queues { get; } = [];
    public List<WorkerTask> PendingTasks { get; } = [];
    public long ShiftStartMs { get; }
    public long ShiftEndMs { get; }
    public string SourceQueueId { get; private set; } = "SOURCE_BUFFER";
    public long ShippedCount { get; private set; }
    public List<long> ShippedLeadTimesMs { get; } = [];

    public Simulation(Scenario scenario, IReadOnlyList<SimCommand>? commands = null)
    {
        _scenario = scenario;
        ShiftStartMs = scenario.ShiftStartMs;
        ShiftEndMs = scenario.ShiftEndMs;
        _commands = new List<SimCommand>();
        _commands.AddRange(scenario.InitialCommands);
        if (commands is not null)
        {
            _commands.AddRange(commands);
        }
    }

    public RunResult Run()
    {
        Initialize();

        while (_events.TryPop(out var ev))
        {
            if (ev.Time >= ShiftDurationMs)
            {
                break;
            }

            _now = ev.Time;
            SampleAccumulators();
            _eventsProcessed++;
            ProcessEvent(ev);
        }

        _now = ShiftDurationMs;
        SampleAccumulators();
        FinalizeState();
        Validate();
        var summary = BuildSummary();
        var hash = ComputeStateHash();
        return new RunResult
        {
            Scenario = _scenario,
            Commands = _commands,
            Summary = summary,
            FinalStateHash = hash,
            EventsProcessed = _eventsProcessed
        };
    }

    public long ShiftDurationMs => ShiftEndMs - ShiftStartMs;

    private void Initialize()
    {
        foreach (var p in _scenario.Processes)
        {
            _processById[p.Id] = p;
        }

        foreach (var def in _scenario.Machines)
        {
            Machines[def.Id] = new Machine
            {
                Id = def.Id,
                ProcessId = def.ProcessId,
                CycleTimeMs = def.CycleTimeMs,
                Position = def.Position,
                InputQueueId = def.InputQueueId,
                FeedsQueueId = def.FeedsQueueId
            };
        }

        foreach (var def in _scenario.Machines)
        {
            var q = new Queue { Id = def.InputQueueId, FeedsMachineId = def.Id };
            Queues[q.Id] = q;
            _queueWeighted[q.Id] = 0;
            _queueCurrentLength[q.Id] = 0;
        }

        Queues[SourceQueueId] = new Queue { Id = SourceQueueId, FeedsMachineId = _scenario.Machines[0].Id };
        _queueWeighted[SourceQueueId] = 0;
        _queueCurrentLength[SourceQueueId] = 0;

        Queues[_scenario.DockId] = new Queue { Id = _scenario.DockId, FeedsMachineId = _scenario.DockId };

        foreach (var def in _scenario.Workers)
        {
            Workers[def.Id] = new Worker
            {
                Id = def.Id,
                SpeedMmPerSec = def.SpeedMmPerSec,
                Position = def.StartingPosition,
                Priority = def.DefaultPriority.ToList()
            };
        }

        foreach (var cmd in _commands)
        {
            if (cmd.Time == 0)
            {
                ApplyCommand(cmd);
            }
            else
            {
                _futureCommands.Add(cmd);
            }
        }

        _futureCommands.Sort((a, b) =>
        {
            int c = a.Time.CompareTo(b.Time);
            if (c != 0)
            {
                return c;
            }

            c = ((int)a.Type).CompareTo((int)b.Type);
            if (c != 0)
            {
                return c;
            }

            c = string.CompareOrdinal(a.MachineId ?? "", b.MachineId ?? "");
            if (c != 0)
            {
                return c;
            }

            return string.CompareOrdinal(a.WorkerId ?? "", b.WorkerId ?? "");
        });

        foreach (var cmd in _futureCommands)
        {
            Schedule(EventType.Command, "CMD", cmd.Time);
        }

        _lastSampleTime = 0;
        RefillSource();
    }

    private void ProcessEvent(SimEvent ev)
    {
        switch (ev.Type)
        {
            case EventType.WorkerArrivePickup:
                OnWorkerArrivePickup(Workers[ev.Entity]);
                break;
            case EventType.WorkerPickupComplete:
                OnWorkerPickupComplete(Workers[ev.Entity]);
                break;
            case EventType.WorkerArriveDrop:
                OnWorkerArriveDrop(Workers[ev.Entity]);
                break;
            case EventType.WorkerDropComplete:
                OnWorkerDropComplete(Workers[ev.Entity]);
                break;
            case EventType.MachineCycleComplete:
                OnMachineCycleComplete(ev.Entity);
                break;
            case EventType.Command:
                ApplyCommand(_futureCommands[_futureCommandIndex++]);
                Dispatch();
                break;
        }
    }

    private void OnWorkerArrivePickup(Worker worker)
    {
        var task = worker.CurrentTask!;
        worker.Position = task.PickupPosition;

        if (task.Kind == TaskKind.LoadMachine)
        {
            worker.SetState(WorkerState.Loading, _now);
            Schedule(EventType.WorkerPickupComplete, worker.Id, _now + _scenario.Ruleset.LoadTimeMs);
        }
        else
        {
            bool fromMachine = task.PickupEntityId != _scenario.SourceId;
            long handleMs = fromMachine ? _scenario.Ruleset.UnloadTimeMs : _scenario.Ruleset.LoadTimeMs;
            worker.SetState(fromMachine ? WorkerState.Unloading : WorkerState.Loading, _now);
            Schedule(EventType.WorkerPickupComplete, worker.Id, _now + handleMs);
        }
    }

    private void OnWorkerPickupComplete(Worker worker)
    {
        var task = worker.CurrentTask!;

        if (task.Kind == TaskKind.LoadMachine)
        {
            var machine = Machines[task.LoadMachineId!];
            var q = Queues[machine.InputQueueId];
            string itemId = q.Items[0];
            q.Items.RemoveAt(0);
            TrackQueueChange(machine.InputQueueId, -1);

            var item = Items[itemId];
            machine.CurrentItemId = itemId;
            item.SetActivity(ItemActivity.Processing, ItemLocation.Machine(machine.Id), _now);
            machine.SetState(MachineState.Processing, _now);
            Schedule(EventType.MachineCycleComplete, machine.Id, _now + machine.CycleTimeMs);
            if (machine.Id == _scenario.Machines[0].Id)
            {
                RefillSource();
            }

            CompleteWorkerTask(worker);
        }
        else
        {
            string itemId;
            if (task.PickupEntityId == _scenario.SourceId)
            {
                var sq = Queues[SourceQueueId];
                itemId = sq.Items[0];
                sq.Items.RemoveAt(0);
                TrackQueueChange(SourceQueueId, -1);
            }
            else
            {
                var machine = Machines[task.PickupEntityId];
                itemId = machine.OutputItemId!;
                machine.OutputItemId = null;
                machine.SetState(MachineState.WaitingForInput, _now);
                TryCreateLoadTask(machine);
            }

            task.CarriedItemId = itemId;
            var item = Items[itemId];
            item.SetActivity(ItemActivity.Transporting, ItemLocation.Worker(worker.Id), _now);
            worker.SetState(WorkerState.Moving, _now);

            long distMm = (long)worker.Position.ManhattanDistance(task.DropPosition) * _scenario.Ruleset.TileMm;
            worker.TotalDistanceMm += distMm;
            long travel = GridPosition.TravelTimeMs(worker.Position, task.DropPosition, worker.SpeedMmPerSec, _scenario.Ruleset.TileMm);
            Schedule(EventType.WorkerArriveDrop, worker.Id, _now + travel);
        }
    }

    private void OnWorkerArriveDrop(Worker worker)
    {
        var task = worker.CurrentTask!;
        worker.Position = task.DropPosition;
        worker.SetState(WorkerState.Loading, _now);
        Schedule(EventType.WorkerDropComplete, worker.Id, _now + _scenario.Ruleset.LoadTimeMs);
    }

    private void OnWorkerDropComplete(Worker worker)
    {
        var task = worker.CurrentTask!;
        string itemId = task.CarriedItemId!;
        var item = Items[itemId];

        if (task.DropQueueId == _scenario.DockId)
        {
            item.SetActivity(ItemActivity.Shipped, ItemLocation.Dock(_scenario.DockId), _now);
            item.ShippedAt = _now;
            ShippedCount++;
            ShippedLeadTimesMs.Add(_now - item.CreatedAt);
            _wip--;
        }
        else
        {
            var q = Queues[task.DropQueueId!];
            q.Items.Add(itemId);
            TrackQueueChange(task.DropQueueId!, +1);
            item.SetActivity(ItemActivity.Queueing, ItemLocation.Queue(task.DropQueueId!), _now);
            TryCreateLoadTask(Machines[q.FeedsMachineId]);
        }

        CompleteWorkerTask(worker);
    }

    private void OnMachineCycleComplete(string machineId)
    {
        var machine = Machines[machineId];
        if (machine.CurrentItemId is null) throw new InvalidOperationException($"Machine {machineId} cycle complete at {_now} with null current item, state {machine.State}, output {machine.OutputItemId ?? "null"}");
        var item = Items[machine.CurrentItemId!];
        machine.CurrentItemId = null;
        machine.OutputItemId = item.Id;
        item.Type = _processById[machine.ProcessId].Outputs[0].ItemType;
        item.SetActivity(ItemActivity.Waiting, ItemLocation.Machine(machineId), _now);
        machine.SetState(MachineState.WaitingForWorker, _now);
        CreateTransportTask(machine);
    }

    private void CompleteWorkerTask(Worker worker)
    {
        var task = worker.CurrentTask!;
        task.CarriedItemId = null;
        worker.CurrentTask = null;
        worker.TasksCompleted++;
        worker.SetState(WorkerState.Idle, _now);
        Dispatch();
    }

    private void TryCreateLoadTask(Machine machine)
    {
        if (machine.CurrentItemId is not null || machine.OutputItemId is not null)
        {
            return;
        }

        if (machine.State is not (MachineState.WaitingForInput or MachineState.WaitingForJob))
        {
            return;
        }

        if (PendingTasks.Any(t => t.Kind == TaskKind.LoadMachine && t.LoadMachineId == machine.Id))
        {
            return;
        }

        var q = Queues[machine.InputQueueId];
        if (q.Items.Count == 0)
        {
            machine.SetState(MachineState.WaitingForInput, _now);
            return;
        }

        machine.SetState(MachineState.WaitingForJob, _now);
        CreateLoadTask(machine);
    }

    private void CreateLoadTask(Machine machine)
    {
        var task = new WorkerTask
        {
            TaskId = NextTaskId(),
            CreatedAt = _now,
            Kind = TaskKind.LoadMachine,
            PickupEntityId = machine.Id,
            PickupPosition = machine.Position,
            LoadMachineId = machine.Id
        };
        PendingTasks.Add(task);
        TryAssignNewTask(task);
    }

    private void CreateTransportTask(Machine machine)
    {
        string dropQueueId = machine.FeedsQueueId;
        var dropPos = dropQueueId == _scenario.DockId
            ? _scenario.DockPosition
            : Machines[Queues[dropQueueId].FeedsMachineId].Position;

        var task = new WorkerTask
        {
            TaskId = NextTaskId(),
            CreatedAt = _now,
            Kind = TaskKind.TransportItem,
            PickupEntityId = machine.Id,
            PickupPosition = machine.Position,
            DropQueueId = dropQueueId,
            DropPosition = dropPos
        };
        PendingTasks.Add(task);
        TryAssignNewTask(task);
    }

    private void RefillSource()
    {
        var first = _scenario.Machines[0];
        var firstInput = Queues[first.InputQueueId];
        if (firstInput.Items.Count > 0)
        {
            return;
        }

        var q = Queues[SourceQueueId];
        if (q.Items.Count > 0)
        {
            return;
        }

        var item = CreateItem();
        q.Items.Add(item.Id);
        TrackQueueChange(SourceQueueId, +1);

        var task = new WorkerTask
        {
            TaskId = NextTaskId(),
            CreatedAt = _now,
            Kind = TaskKind.TransportItem,
            PickupEntityId = _scenario.SourceId,
            PickupPosition = _scenario.SourcePosition,
            DropQueueId = first.InputQueueId,
            DropPosition = first.Position
        };
        PendingTasks.Add(task);
        TryAssignNewTask(task);
    }

    private Item CreateItem()
    {
        _itemCounter++;
        var item = new Item
        {
            Id = $"{_scenario.ProductId}-{_itemCounter:00000}",
            ProductId = _scenario.ProductId,
            Type = _scenario.SourceItemType,
            CreatedAt = _now,
            ActivityStartTime = _now
        };
        item.SetActivity(ItemActivity.Waiting, ItemLocation.Source(_scenario.SourceId), _now);
        Items[item.Id] = item;
        _wip++;
        if (_wip > _peakWip)
        {
            _peakWip = _wip;
        }

        return item;
    }

    private void TryAssignNewTask(WorkerTask task)
    {
        Worker? best = null;
        long bestDist = long.MaxValue;
        long bestIdle = long.MinValue;
        string? bestId = null;

        foreach (var worker in Workers.Values)
        {
            if (worker.State != WorkerState.Idle || worker.CurrentTask is not null)
            {
                continue;
            }

            long dist = (long)worker.Position.ManhattanDistance(task.PickupPosition) * _scenario.Ruleset.TileMm;
            long idle = _now - worker.IdleSince;
            bool better = best is null
                || dist < bestDist
                || (dist == bestDist && idle > bestIdle)
                || (dist == bestDist && idle == bestIdle && string.CompareOrdinal(worker.Id, bestId!) < 0);
            if (better)
            {
                best = worker;
                bestDist = dist;
                bestIdle = idle;
                bestId = worker.Id;
            }
        }

        if (best is not null)
        {
            Assign(best, task);
        }
    }

    private void Dispatch()
    {
        foreach (var workerId in Workers.Keys.OrderBy(x => x, StringComparer.Ordinal))
        {
            var worker = Workers[workerId];
            if (worker.State != WorkerState.Idle || worker.CurrentTask is not null)
            {
                continue;
            }

            var task = SelectBestTask(worker);
            if (task is not null)
            {
                Assign(worker, task);
            }
        }
    }

    private WorkerTask? SelectBestTask(Worker worker)
    {
        WorkerTask? best = null;
        foreach (var task in PendingTasks)
        {
            if (best is null || CompareTask(worker, task, best) < 0)
            {
                best = task;
            }
        }

        return best;
    }

    private int CompareTask(Worker worker, WorkerTask a, WorkerTask b)
    {
        int c = PriorityRank(worker, a).CompareTo(PriorityRank(worker, b));
        if (c != 0)
        {
            return c;
        }

        c = a.CreatedAt.CompareTo(b.CreatedAt);
        if (c != 0)
        {
            return c;
        }

        long da = (long)worker.Position.ManhattanDistance(a.PickupPosition) * _scenario.Ruleset.TileMm;
        long db = (long)worker.Position.ManhattanDistance(b.PickupPosition) * _scenario.Ruleset.TileMm;
        c = da.CompareTo(db);
        if (c != 0)
        {
            return c;
        }

        return a.TaskId.CompareTo(b.TaskId);
    }

    private int PriorityRank(Worker worker, WorkerTask task)
    {
        string id = task.Kind == TaskKind.LoadMachine ? task.LoadMachineId! : task.PickupEntityId;
        int idx = worker.Priority.IndexOf(id);
        return idx < 0 ? int.MaxValue : idx;
    }

    private void Assign(Worker worker, WorkerTask task)
    {
        PendingTasks.Remove(task);
        task.AssignedWorkerId = worker.Id;
        worker.CurrentTask = task;

        if (task.Kind == TaskKind.LoadMachine)
        {
            Machines[task.LoadMachineId!].SetState(MachineState.WaitingForWorker, _now);
        }

        worker.SetState(WorkerState.Moving, _now);
        long distMm = (long)worker.Position.ManhattanDistance(task.PickupPosition) * _scenario.Ruleset.TileMm;
        worker.TotalDistanceMm += distMm;
        long travel = GridPosition.TravelTimeMs(worker.Position, task.PickupPosition, worker.SpeedMmPerSec, _scenario.Ruleset.TileMm);
        Schedule(EventType.WorkerArrivePickup, worker.Id, _now + travel);
    }

    private void ApplyCommand(SimCommand cmd)
    {
        switch (cmd.Type)
        {
            case CommandType.MoveMachine:
                Machines[cmd.MachineId!].Position = cmd.Position!.Value;
                break;
            case CommandType.SetWorkerPriority:
                Workers[cmd.WorkerId!].Priority = cmd.Priorities!.ToList();
                break;
            case CommandType.Pause:
                break;
        }
    }

    private void SampleAccumulators()
    {
        if (_now == _lastSampleTime)
        {
            return;
        }

        long dt = _now - _lastSampleTime;
        _wipWeighted += _wip * dt;
        foreach (var kv in _queueCurrentLength)
        {
            _queueWeighted[kv.Key] += (long)kv.Value * dt;
        }

        _lastSampleTime = _now;
    }

    private void TrackQueueChange(string queueId, int delta)
    {
        _queueCurrentLength[queueId] += delta;
    }

    private void FinalizeState()
    {
        foreach (var m in Machines.Values)
        {
            m.TimeInState[(int)m.State] += _now - m.StateStartTime;
        }

        foreach (var w in Workers.Values)
        {
            w.TimeInState[(int)w.State] += _now - w.StateStartTime;
        }

        foreach (var it in Items.Values)
        {
            if (it.ShippedAt < 0)
            {
                it.TimeInActivity[(int)it.Activity] += _now - it.ActivityStartTime;
            }
        }
    }

    private void Validate()
    {
        foreach (var m in Machines.Values)
        {
            long total = m.TimeInState.Sum();
            if (total != ShiftDurationMs)
            {
                throw new InvalidOperationException($"Machine {m.Id} state time {total} != shift {ShiftDurationMs}");
            }

            if (m.State == MachineState.Processing && m.CurrentItemId is null)
            {
                throw new InvalidOperationException($"Machine {m.Id} is Processing without an item");
            }

            if (m.CurrentItemId is not null && m.State != MachineState.Processing)
            {
                throw new InvalidOperationException($"Machine {m.Id} holds current item but state is {m.State}");
            }

            if (m.OutputItemId is not null && m.State != MachineState.WaitingForWorker)
            {
                throw new InvalidOperationException($"Machine {m.Id} holds output item but state is {m.State}");
            }

            if (m.State == MachineState.WaitingForInput && Queues[m.InputQueueId].Items.Count > 0)
            {
                throw new InvalidOperationException($"Machine {m.Id} waiting for input but input queue non-empty");
            }
        }

        foreach (var w in Workers.Values)
        {
            long total = w.TimeInState.Sum();
            if (total != ShiftDurationMs)
            {
                throw new InvalidOperationException($"Worker {w.Id} state time {total} != shift {ShiftDurationMs}");
            }

            if (w.State == WorkerState.Idle && w.CurrentTask is not null)
            {
                throw new InvalidOperationException($"Worker {w.Id} idle but has task");
            }

            if (w.State != WorkerState.Idle && w.CurrentTask is null)
            {
                throw new InvalidOperationException($"Worker {w.Id} is {w.State} without a task");
            }
        }

        var located = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var q in Queues.Values)
        {
            foreach (var id in q.Items)
            {
                located[id] = located.GetValueOrDefault(id) + 1;
            }
        }

        foreach (var m in Machines.Values)
        {
            if (m.CurrentItemId is not null)
            {
                located[m.CurrentItemId] = located.GetValueOrDefault(m.CurrentItemId) + 1;
            }

            if (m.OutputItemId is not null)
            {
                located[m.OutputItemId] = located.GetValueOrDefault(m.OutputItemId) + 1;
            }
        }

        foreach (var w in Workers.Values)
        {
            if (w.CurrentTask?.CarriedItemId is not null)
            {
                located[w.CurrentTask.CarriedItemId] = located.GetValueOrDefault(w.CurrentTask.CarriedItemId) + 1;
            }
        }

        int active = 0;
        foreach (var it in Items.Values)
        {
            bool shipped = it.ShippedAt >= 0;
            if (shipped)
            {
                if (it.Activity != ItemActivity.Shipped)
                {
                    throw new InvalidOperationException($"Item {it.Id} shipped but activity is {it.Activity}");
                }
            }
            else
            {
                active++;
                int count = located.GetValueOrDefault(it.Id);
                if (count != 1)
                {
                    throw new InvalidOperationException($"Item {it.Id} is active but located {count} times");
                }
            }

            long lifespan = shipped ? it.ShippedAt - it.CreatedAt : _now - it.CreatedAt;
            long activityTotal = it.TimeInActivity.Sum();
            if (activityTotal != lifespan)
            {
                var traceDump = string.Join(" | ", it.Trace.Select(t => $"{t.Time}:{t.Activity}:{t.Location}"));
                throw new InvalidOperationException(
                    $"Item {it.Id} activity time {activityTotal} != lifespan {lifespan} (created {it.CreatedAt}, shipped {it.ShippedAt}). Trace: {traceDump}");
            }

            for (int i = 1; i < it.Trace.Count; i++)
            {
                if (it.Trace[i].Time < it.Trace[i - 1].Time)
                {
                    throw new InvalidOperationException($"Item {it.Id} trace timestamps went backwards");
                }
            }
        }

        if (active != _wip)
        {
            throw new InvalidOperationException($"WIP {_wip} != active item count {active}");
        }

        if (ShippedCount != ShippedLeadTimesMs.Count)
        {
            throw new InvalidOperationException("Shipped count does not match lead-time records");
        }
    }

    private RunSummary BuildSummary()
    {
        long duration = ShiftDurationMs;
        var machines = Machines.Values
            .OrderBy(m => m.Id, StringComparer.Ordinal)
            .Select(m => new MachineStats(m.Id, (long[])m.TimeInState.Clone(), duration))
            .ToList();
        var workers = Workers.Values
            .OrderBy(w => w.Id, StringComparer.Ordinal)
            .Select(w => new WorkerStats(w.Id, (long[])w.TimeInState.Clone(), duration, w.TotalDistanceMm, w.TasksCompleted))
            .ToList();
        var queues = Queues.Values
            .Where(q => q.Id != _scenario.DockId)
            .OrderBy(q => q.Id, StringComparer.Ordinal)
            .Select(q => new QueueStats(q.Id, _queueWeighted.GetValueOrDefault(q.Id) / (double)duration, q.Items.Count))
            .ToList();

        var leads = ShippedLeadTimesMs.OrderBy(x => x).ToList();
        double throughput = ShippedCount > 0 ? ShippedCount / (duration / 3_600_000.0) : 0.0;
        double avgWip = _wipWeighted / (double)duration;
        long avgLead = leads.Count > 0 ? (long)leads.Average() : 0;
        long medianLead = leads.Count > 0 ? leads[leads.Count / 2] : 0;

        return new RunSummary(
            _scenario.Id,
            _scenario.Version,
            _scenario.Ruleset.Version,
            duration,
            ShippedCount,
            throughput,
            avgLead,
            medianLead,
            avgWip,
            _peakWip,
            machines,
            workers,
            queues);
    }

    public string ComputeStateHash()
    {
        var h = new StateHasher();
        h.Begin("sim");
        h.Field("time", _now);
        h.Field("shipped", ShippedCount);
        h.Field("wip_weighted", _wipWeighted);
        h.Field("wip_peak", _peakWip);

        foreach (var m in Machines.Values.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            h.Begin("machine");
            h.Field("id", m.Id);
            h.Field("pos", $"{m.Position.X},{m.Position.Y}");
            h.Field("state", (int)m.State);
            h.Field("times", string.Join(",", m.TimeInState));
            h.Field("current", m.CurrentItemId ?? "-");
            h.Field("output", m.OutputItemId ?? "-");
        }

        foreach (var w in Workers.Values.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            h.Begin("worker");
            h.Field("id", w.Id);
            h.Field("pos", $"{w.Position.X},{w.Position.Y}");
            h.Field("state", (int)w.State);
            h.Field("times", string.Join(",", w.TimeInState));
            h.Field("dist", w.TotalDistanceMm);
            h.Field("tasks", w.TasksCompleted);
            h.Field("pri", string.Join(",", w.Priority));
        }

        foreach (var q in Queues.Values.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            h.Begin("queue");
            h.Field("id", q.Id);
            h.Field("items", string.Join(",", q.Items));
            h.Field("weighted", _queueWeighted.GetValueOrDefault(q.Id));
        }

        foreach (var it in Items.Values.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            h.Begin("item");
            h.Field("id", it.Id);
            h.Field("type", it.Type);
            h.Field("activity", (int)it.Activity);
            h.Field("loc", $"{it.Location.Kind}:{it.Location.Id}");
            h.Field("created", it.CreatedAt);
            h.Field("shipped", it.ShippedAt);
            h.Field("times", string.Join(",", it.TimeInActivity));
            foreach (var tr in it.Trace)
            {
                h.Field("t", $"{tr.Time}:{tr.Activity}:{tr.Location}");
            }
        }

        return h.Finish();
    }

    private long NextTaskId() => _taskCounter++;

    private void Schedule(EventType type, string entity, long time)
    {
        _events.Push(new SimEvent(time, _sequence++, type, entity));
    }
}
