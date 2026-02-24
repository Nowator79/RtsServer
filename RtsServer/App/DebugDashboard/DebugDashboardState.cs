namespace RtsServer.App.DebugDashboard;

/// <summary>
/// Снимок состояния сервера для отладочного дашборда.
/// </summary>
public class DebugDashboardState
{
    public DateTime TimestampUtc { get; set; }
    public List<ClientInfo> Clients { get; set; } = new();
    public List<GameInfo> Games { get; set; } = new();
    public Dictionary<string, QueueInfo> MatchmakingQueues { get; set; } = new();
}

public class ClientInfo
{
    public string Id { get; set; } = "";
    public string? UserName { get; set; }
    public int? UserId { get; set; }
    public string Status { get; set; } = "";
}

public class GameInfo
{
    public int Id { get; set; }
    public string? MapCode { get; set; }
    public int PlayersCount { get; set; }
    public List<string> PlayerNames { get; set; } = new();
    public int UnitsCount { get; set; }
    public int ConstructionsCount { get; set; }
}

public class QueueInfo
{
    public string Type { get; set; } = "";
    public int Count { get; set; }
    public List<string> UserNames { get; set; } = new();
}

/// <summary>
/// Детали матча для страницы с картой и юнитами.
/// </summary>
public class GameDetailDto
{
    public int Id { get; set; }
    public string? MapCode { get; set; }
    public int MapWidth { get; set; }
    public int MapLength { get; set; }
    public List<string> PlayerNames { get; set; } = new();
    public List<UnitOnMapDto> Units { get; set; } = new();
    public List<ConstructionOnMapDto> Constructions { get; set; } = new();
}

public class UnitOnMapDto
{
    public int Id { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public int OwnerId { get; set; }
    public string Code { get; set; } = "";
}

public class ConstructionOnMapDto
{
    public int Id { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public int OwnerId { get; set; }
    public string Code { get; set; } = "";
}
