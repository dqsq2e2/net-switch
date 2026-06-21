namespace NetAdapterSwitcher;

internal sealed class NetworkAdapterInfo
{
    public int InterfaceIndex { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string NetworkType { get; set; } = "";
    public string Status { get; set; } = "";
    public bool IsConnected { get; set; }
    public string MacAddress { get; set; } = "";
    public string LinkSpeed { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public string Gateway { get; set; } = "";
    public int InterfaceMetric { get; set; }
    public int? RouteMetric { get; set; }
    public bool AutomaticMetric { get; set; }
    public bool HasDefaultRoute { get; set; }

    public int EffectiveMetric => InterfaceMetric + (RouteMetric ?? 0);
    public string PriorityText => HasDefaultRoute ? EffectiveMetric.ToString() : "—";
    public string MetricModeText => AutomaticMetric ? "自动" : "手动";
    public string StatusText => IsConnected ? "已连接" : "未连接";
}
