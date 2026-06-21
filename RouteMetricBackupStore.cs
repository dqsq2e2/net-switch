using System.Text.Json;

namespace NetAdapterSwitcher;

internal sealed class RouteMetricBackupStore
{
    private readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NetAdapterSwitcher",
        "route-metric-backup.json");

    public async Task SaveMissingAsync(IEnumerable<DefaultRouteInfo> routes)
    {
        var saved = await LoadAsync();
        foreach (var route in routes)
        {
            if (!saved.Any(x => x.InterfaceIndex == route.InterfaceIndex &&
                                x.NextHop.Equals(route.NextHop, StringComparison.OrdinalIgnoreCase)))
                saved.Add(route);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        await File.WriteAllTextAsync(_path, JsonSerializer.Serialize(saved));
    }

    public async Task<List<DefaultRouteInfo>> GetAsync(int? interfaceIndex = null)
    {
        var saved = await LoadAsync();
        return interfaceIndex.HasValue
            ? saved.Where(x => x.InterfaceIndex == interfaceIndex.Value).ToList()
            : saved;
    }

    public async Task RemoveAsync(int? interfaceIndex = null)
    {
        if (!File.Exists(_path)) return;
        if (!interfaceIndex.HasValue)
        {
            File.Delete(_path);
            return;
        }

        var saved = await LoadAsync();
        saved.RemoveAll(x => x.InterfaceIndex == interfaceIndex.Value);
        if (saved.Count == 0)
            File.Delete(_path);
        else
            await File.WriteAllTextAsync(_path, JsonSerializer.Serialize(saved));
    }

    private async Task<List<DefaultRouteInfo>> LoadAsync()
    {
        if (!File.Exists(_path)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<DefaultRouteInfo>>(
                await File.ReadAllTextAsync(_path)) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
