using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NetAdapterSwitcher;

internal sealed class PowerShellNetworkService
{
    public async Task<List<DefaultRouteInfo>> GetDefaultRoutesAsync()
    {
        const string script = """
            $result = Get-NetRoute -AddressFamily IPv4 -DestinationPrefix '0.0.0.0/0' -ErrorAction SilentlyContinue |
                ForEach-Object {
                    [pscustomobject]@{
                        InterfaceIndex = [int]$_.InterfaceIndex
                        NextHop = [string]$_.NextHop
                        RouteMetric = [int]$_.RouteMetric
                    }
                }
            ConvertTo-Json -InputObject @($result) -Compress
            """;
        string json = await RunAsync(script);
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<DefaultRouteInfo>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }

    public async Task<List<NetworkAdapterInfo>> GetAdaptersAsync()
    {
        const string script = """
            $ErrorActionPreference = 'Stop'
            $routes = @(Get-NetRoute -AddressFamily IPv4 -DestinationPrefix '0.0.0.0/0' -ErrorAction SilentlyContinue)
            $addresses = @(Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue)
            $adapters = @{}
            Get-NetAdapter -IncludeHidden -ErrorAction SilentlyContinue |
                Where-Object {
                    $_.HardwareInterface -eq $true -and
                    ([int]$_.NdisPhysicalMedium -eq 14 -or [int]$_.NdisPhysicalMedium -eq 9) -and
                    $_.Status -ne 'Not Present'
                } |
                ForEach-Object { $adapters[[int]$_.InterfaceIndex] = $_ }

            $result = Get-NetIPInterface -AddressFamily IPv4 -ErrorAction Stop |
                Where-Object { $adapters.ContainsKey([int]$_.InterfaceIndex) } |
                ForEach-Object {
                    $ipif = $_
                    $interfaceIndex = [int]$ipif.InterfaceIndex
                    $adapter = $adapters[$interfaceIndex]
                    $defaultRoutes = @($routes | Where-Object InterfaceIndex -eq $ipif.InterfaceIndex)
                    $bestRoute = $defaultRoutes | Sort-Object RouteMetric | Select-Object -First 1
                    $ips = @($addresses |
                        Where-Object InterfaceIndex -eq $ipif.InterfaceIndex |
                        Sort-Object SkipAsSource, IPAddress |
                        ForEach-Object { "$($_.IPAddress)/$($_.PrefixLength)" }) -join ', '
                    $gateways = @($defaultRoutes |
                        Sort-Object RouteMetric |
                        ForEach-Object NextHop |
                        Where-Object { $_ -and $_ -ne '0.0.0.0' } |
                        Select-Object -Unique) -join ', '

                    [pscustomobject]@{
                        InterfaceIndex = $interfaceIndex
                        Name = [string]$ipif.InterfaceAlias
                        NetworkType = [string]$(if ([int]$adapter.NdisPhysicalMedium -eq 9) { 'WLAN' } else { 'Ethernet' })
                        Description = [string]$(if ($adapter) { $adapter.InterfaceDescription } else { '' })
                        Status = [string]$(if ($adapter) { $adapter.Status } else { $ipif.ConnectionState })
                        IsConnected = [bool](
                            ($null -ne $adapter -and $adapter.Status.ToString() -eq 'Up') -or
                            $ipif.ConnectionState.ToString() -eq 'Connected'
                        )
                        MacAddress = [string]$(if ($adapter) { $adapter.MacAddress } else { '' })
                        LinkSpeed = [string]$(if ($adapter) { $adapter.LinkSpeed } else { '' })
                        IpAddress = [string]$ips
                        Gateway = [string]$gateways
                        InterfaceMetric = [int]$ipif.InterfaceMetric
                        RouteMetric = $(if ($null -ne $bestRoute) { [int]$bestRoute.RouteMetric } else { $null })
                        AutomaticMetric = [bool]($ipif.AutomaticMetric -eq 'Enabled')
                        HasDefaultRoute = [bool]($defaultRoutes.Count -gt 0)
                    }
                }

            ConvertTo-Json -InputObject @($result) -Depth 4 -Compress
            """;

        string json = await RunAsync(script);
        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "null")
            return [];

        return JsonSerializer.Deserialize<List<NetworkAdapterInfo>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }

    public Task SetPreferredAsync(int interfaceIndex)
    {
        string script = $$"""
            $ErrorActionPreference = 'Stop'
            $index = {{interfaceIndex}}
            Set-NetIPInterface -InterfaceIndex $index -AddressFamily IPv4 -AutomaticMetric Disabled -InterfaceMetric 5 -ErrorAction Stop
            Get-NetRoute -InterfaceIndex $index -AddressFamily IPv4 -DestinationPrefix '0.0.0.0/0' -ErrorAction Stop |
                Set-NetRoute -RouteMetric 0 -ErrorAction Stop
            $otherIndexes = @(Get-NetRoute -AddressFamily IPv4 -DestinationPrefix '0.0.0.0/0' -ErrorAction SilentlyContinue |
                Where-Object InterfaceIndex -ne $index |
                Select-Object -ExpandProperty InterfaceIndex -Unique)
            $metric = 50
            foreach ($otherIndex in $otherIndexes) {
                Set-NetIPInterface -InterfaceIndex $otherIndex -AddressFamily IPv4 -AutomaticMetric Disabled -InterfaceMetric $metric -ErrorAction Stop
                $metric += 10
            }
            Clear-DnsClientCache -ErrorAction SilentlyContinue
            """;

        return RunAsync(script);
    }

    public Task RestoreRouteMetricsAsync(IEnumerable<DefaultRouteInfo> routes)
    {
        string commands = string.Join(Environment.NewLine, routes.Select(route =>
        {
            string nextHop = route.NextHop.Replace("'", "''");
            return $"Get-NetRoute -InterfaceIndex {route.InterfaceIndex} -AddressFamily IPv4 -DestinationPrefix '0.0.0.0/0' -NextHop '{nextHop}' -ErrorAction SilentlyContinue | Set-NetRoute -RouteMetric {route.RouteMetric} -ErrorAction Stop";
        }));

        return string.IsNullOrWhiteSpace(commands) ? Task.CompletedTask : RunAsync(commands);
    }

    public Task RestoreAutomaticMetricAsync(int? interfaceIndex = null)
    {
        string target = interfaceIndex.HasValue
            ? $"Get-NetIPInterface -InterfaceIndex {interfaceIndex.Value} -AddressFamily IPv4"
            : "Get-NetIPInterface -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -ne 'Loopback Pseudo-Interface 1' }";

        string script = $$"""
            $ErrorActionPreference = 'Stop'
            {{target}} | Set-NetIPInterface -AutomaticMetric Enabled -ErrorAction Stop
            """;

        return RunAsync(script);
    }

    private static async Task<string> RunAsync(string script)
    {
        const string encodingPreamble = """
            $ProgressPreference = 'SilentlyContinue'
            $WarningPreference = 'SilentlyContinue'
            $InformationPreference = 'SilentlyContinue'
            [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
            $OutputEncoding = [Console]::OutputEncoding
            """;
        string encoded = Convert.ToBase64String(
            Encoding.Unicode.GetBytes(encodingPreamble + Environment.NewLine + script));
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -OutputFormat Text -EncodedCommand {encoded}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        string output = await outputTask;
        string error = await errorTask;
        if (process.ExitCode != 0)
            throw new InvalidOperationException(CleanPowerShellError(error));

        return output.Trim();
    }

    private static string CleanPowerShellError(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            return "PowerShell 命令执行失败。";

        if (error.Contains("<Objs", StringComparison.OrdinalIgnoreCase))
        {
            string[] messages = Regex.Matches(error, """<S S="Error">(?<text>.*?)</S>""",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase)
                .Select(match => WebUtility.HtmlDecode(match.Groups["text"].Value)
                    .Replace("_x000D__x000A_", Environment.NewLine)
                    .Replace("_x000D_", "\r")
                    .Replace("_x000A_", "\n"))
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToArray();
            if (messages.Length > 0)
                return string.Join(Environment.NewLine, messages);

            return "Windows PowerShell 初始化网络模块失败，请稍后重试。";
        }

        string[] usefulLines = error.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !line.StartsWith("#< CLIXML", StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToArray();
        return usefulLines.Length > 0
            ? string.Join(Environment.NewLine, usefulLines)
            : "PowerShell 命令执行失败。";
    }
}
