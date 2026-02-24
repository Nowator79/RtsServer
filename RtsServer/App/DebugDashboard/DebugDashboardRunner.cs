using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using RtsServer.App;
using RtsServer.App.Battle;
using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle.Units;
using RtsServer.App.NetWork.Tcp;

namespace RtsServer.App.DebugDashboard;

/// <summary>
/// Запускает HTTP-сервер с отладочной страницей и API состояния.
/// </summary>
public static class DebugDashboardRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static Task RunAsync(GameServer server, int port, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory,
            Args = Array.Empty<string>()
        });

        builder.WebHost.ConfigureKestrel(opts => opts.ListenLocalhost(port));
        var app = builder.Build();

        app.MapGet("/api/state", () =>
        {
            try
            {
                var state = BuildState(server);
                return Results.Json(state, JsonOptions);
            }
            catch (Exception ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: 500);
            }
        });

        app.MapGet("/api/game/{id}", (int id) =>
        {
            try
            {
                var detail = BuildGameDetail(server, id);
                return detail == null ? Results.NotFound() : Results.Json(detail, JsonOptions);
            }
            catch (Exception ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: 500);
            }
        });

        app.MapGet("/", () => Results.Content(GetHtml(), "text/html; charset=utf-8"));
        app.MapGet("/match/{id}", (int id) => Results.Content(GetMatchHtml(id), "text/html; charset=utf-8"));

        return app.RunAsync();
    }

    private static DebugDashboardState BuildState(GameServer server)
    {
        var state = new DebugDashboardState { TimestampUtc = DateTime.UtcNow };

        if (server.TcpServer?.Users != null)
        {
            foreach (var client in server.TcpServer.Users)
            {
                state.Clients.Add(new ClientInfo
                {
                    Id = client.Id,
                    UserName = client.User?.UserName,
                    UserId = client.User?.Id,
                    Status = client.User?.Status.GetStatus() ?? "—"
                });
            }
        }

        if (server.BattleManager?.Games != null)
        {
            foreach (var game in server.BattleManager.Games)
            {
                state.Games.Add(new GameInfo
                {
                    Id = game.Id,
                    MapCode = game.Map?.Code,
                    PlayersCount = game.Players.Count,
                    PlayerNames = game.Players.Select(p => p.UserAuth?.UserName ?? "?").ToList(),
                    UnitsCount = game.Units.Count,
                    ConstructionsCount = game.Constructions.Count
                });
            }
        }

        if (server.BattleManager?.MatchmakingQueues != null)
        {
            foreach (var (type, list) in server.BattleManager.MatchmakingQueues)
            {
                state.MatchmakingQueues[type] = new QueueInfo
                {
                    Type = type,
                    Count = list.Count,
                    UserNames = list.Select(u => u.UserName).ToList()
                };
            }
        }

        return state;
    }

    private static GameDetailDto? BuildGameDetail(GameServer server, int gameId)
    {
        var game = server.BattleManager?.Games?.FirstOrDefault(g => g.Id == gameId);
        if (game?.Map == null) return null;

        var dto = new GameDetailDto
        {
            Id = game.Id,
            MapCode = game.Map.Code,
            MapWidth = game.Map.Width,
            MapLength = game.Map.Length,
            PlayerNames = game.Players.Select(p => p.UserAuth?.UserName ?? "?").ToList()
        };

        foreach (var unit in game.Units.Where(u => !u.IsDestroyed))
        {
            dto.Units.Add(new UnitOnMapDto
            {
                Id = unit.Id,
                X = unit.Position.X,
                Y = unit.Position.Y,
                OwnerId = unit.OwnerId,
                Code = unit.Code ?? ""
            });
        }

        foreach (var c in game.Constructions.Where(c => !c.IsDestroyed))
        {
            dto.Constructions.Add(new ConstructionOnMapDto
            {
                Id = c.Id,
                X = c.Position.X,
                Y = c.Position.Y,
                OwnerId = c.OwnerId,
                Code = c.Code ?? ""
            });
        }

        return dto;
    }

    private static string GetHtml()
    {
        return """
<!DOCTYPE html>
<html lang="ru">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>RtsServer — отладка</title>
  <style>
    * { box-sizing: border-box; }
    body { font-family: system-ui, sans-serif; margin: 16px; background: #1a1a2e; color: #eee; }
    h1 { font-size: 1.25rem; margin-bottom: 8px; }
    h2 { font-size: 1rem; margin: 16px 0 8px; color: #a0a0c0; }
    section { background: #16213e; border-radius: 8px; padding: 12px; margin-bottom: 12px; }
    table { width: 100%; border-collapse: collapse; font-size: 14px; }
    th, td { text-align: left; padding: 6px 10px; border-bottom: 1px solid #2a2a4a; }
    th { color: #888; font-weight: 600; }
    .meta { color: #888; font-size: 12px; margin-bottom: 12px; }
    .error { color: #e88; }
  </style>
</head>
<body>
  <h1>RtsServer — отладка</h1>
  <p class="meta">Обновление каждые 2 сек. Время сервера: <span id="time">—</span></p>
  <section>
    <h2>Подключённые клиенты</h2>
    <div id="clients"></div>
  </section>
  <section>
    <h2>Игры</h2>
    <div id="games"></div>
  </section>
  <section>
    <h2>Очереди матчмейкинга</h2>
    <div id="queues"></div>
  </section>
  <script>
    const $ = id => document.getElementById(id);
    function render(state) {
      if (state.error) {
        $('time').textContent = '—';
        $('clients').innerHTML = '<p class="error">' + state.error + '</p>';
        $('games').innerHTML = '';
        $('queues').innerHTML = '';
        return;
      }
      const t = state.timestampUtc ? new Date(state.timestampUtc).toLocaleTimeString('ru') : '—';
      $('time').textContent = t;

      $('clients').innerHTML = state.clients?.length
        ? '<table><tr><th>Клиент</th><th>Пользователь</th><th>Id</th><th>Статус</th></tr>' +
          state.clients.map(c => '<tr><td>' + c.id + '</td><td>' + (c.userName ?? '—') + '</td><td>' + (c.userId ?? '—') + '</td><td>' + (c.status ?? '') + '</td></tr>').join('') + '</table>'
        : '<p>Нет подключений</p>';

      $('games').innerHTML = state.games?.length
        ? '<table><tr><th>Id</th><th>Карта</th><th>Игроки</th><th>Юниты</th><th>Постройки</th></tr>' +
          state.games.map(g => '<tr><td><a href="/match/' + g.id + '">' + g.id + '</a></td><td>' + (g.mapCode ?? '—') + '</td><td>' + (g.playerNames?.join(', ') ?? '—') + '</td><td>' + g.unitsCount + '</td><td>' + g.constructionsCount + '</td></tr>').join('') + '</table>'
        : '<p>Нет активных игр</p>';

      const queues = state.matchmakingQueues || {};
      const queueRows = Object.entries(queues).map(([type, q]) => '<tr><td>' + type + '</td><td>' + q.count + '</td><td>' + (q.userNames?.join(', ') || '—') + '</td></tr>').join('');
      $('queues').innerHTML = queueRows
        ? '<table><tr><th>Тип</th><th>В очереди</th><th>Имена</th></tr>' + queueRows + '</table>'
        : '<p>Очереди пусты</p>';
    }
    async function poll() {
      try {
        const r = await fetch('/api/state');
        const state = await r.json();
        render(state);
      } catch (e) {
        render({ error: e.message });
      }
    }
    poll();
    setInterval(poll, 2000);
  </script>
</body>
</html>
""";
    }

    private static string GetMatchHtml(int gameId)
    {
        return GetMatchHtmlTemplate().Replace("%%GAME_ID%%", gameId.ToString());
    }

    private static string GetMatchHtmlTemplate()
    {
        return """
<!DOCTYPE html>
<html lang="ru">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Матч %%GAME_ID%% — RtsServer</title>
  <style>
    * { box-sizing: border-box; }
    body { font-family: system-ui, sans-serif; margin: 16px; background: #1a1a2e; color: #eee; }
    h1 { font-size: 1.1rem; margin-bottom: 8px; }
    a { color: #6c9; }
    .meta { color: #888; font-size: 12px; margin-bottom: 12px; }
    .map-wrap { background: #0f0f1a; border-radius: 8px; padding: 12px; display: inline-block; }
    #map { display: block; border: 1px solid #333; background: #1a1a2e; }
    .error { color: #e88; }
    .legend { margin-top: 12px; font-size: 12px; display: flex; gap: 16px; flex-wrap: wrap; }
    .legend span { display: inline-flex; align-items: center; gap: 6px; }
    .legend .dot { width: 12px; height: 12px; border-radius: 50%; }
  </style>
</head>
<body>
  <h1>Матч %%GAME_ID%%</h1>
  <p class="meta">Обновление каждые 1.5 сек. <a href="/">← Назад к дашборду</a></p>
  <div class="map-wrap">
    <canvas id="map" width="480" height="480"></canvas>
    <div class="legend" id="legend"></div>
  </div>
  <p id="status" class="meta"></p>
  <script>
    const gameId = %%GAME_ID%%;
    const SIZE = 480;
    const COLORS = ['#4ade80', '#f87171', '#60a5fa', '#fbbf24', '#a78bfa'];
    const $ = id => document.getElementById(id);

    function draw(game) {
      const canvas = $('map');
      const ctx = canvas.getContext('2d');
      const W = game.mapWidth || 16;
      const H = game.mapLength || 16;
      const scale = Math.min(SIZE / W, SIZE / H);
      const ox = (SIZE - W * scale) / 2;
      const oy = (SIZE - H * scale) / 2;

      ctx.fillStyle = '#1a1a2e';
      ctx.fillRect(0, 0, SIZE, SIZE);
      ctx.strokeStyle = '#333';
      ctx.lineWidth = 1;
      ctx.strokeRect(ox, oy, W * scale, H * scale);

      ctx.fillStyle = '#0d0d14';
      ctx.fillRect(ox, oy, W * scale, H * scale);

      game.units.forEach(u => {
        const x = ox + (u.x + 0.5) * scale;
        const y = oy + (u.y + 0.5) * scale;
        const color = COLORS[u.ownerId % COLORS.length];
        ctx.fillStyle = color;
        ctx.beginPath();
        ctx.arc(x, y, Math.max(4, scale * 0.4), 0, Math.PI * 2);
        ctx.fill();
        ctx.strokeStyle = '#333';
        ctx.lineWidth = 1;
        ctx.stroke();
      });

      game.constructions.forEach(c => {
        const x = ox + (c.x + 0.5) * scale;
        const y = oy + (c.y + 0.5) * scale;
        const color = COLORS[c.ownerId % COLORS.length];
        ctx.fillStyle = color;
        const r = Math.max(3, scale * 0.3);
        ctx.fillRect(x - r, y - r, r * 2, r * 2);
        ctx.strokeStyle = '#333';
        ctx.strokeRect(x - r, y - r, r * 2, r * 2);
      });

      const names = game.playerNames || [];
      $('legend').innerHTML = names.map((name, i) =>
        '<span><span class="dot" style="background:' + COLORS[i % COLORS.length] + '"></span> ' + (name || 'Игрок ' + i) + '</span>'
      ).join('');
    }

    async function poll() {
      try {
        const r = await fetch('/api/game/' + gameId);
        if (r.status === 404) { $('status').textContent = 'Матч не найден'; return; }
        const game = await r.json();
        if (game.error) { $('status').textContent = game.error; return; }
        $('status').textContent = 'Карта: ' + (game.mapCode || '—') + ' | Юнитов: ' + (game.units?.length ?? 0) + ' | Построек: ' + (game.constructions?.length ?? 0);
        draw(game);
      } catch (e) {
        $('status').textContent = 'Ошибка: ' + e.message;
        $('status').className = 'error';
      }
    }
    poll();
    setInterval(poll, 1500);
  </script>
</body>
</html>
""";
    }
}
