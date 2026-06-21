namespace NetAdapterSwitcher;

internal sealed class DefaultRouteInfo
{
    public int InterfaceIndex { get; set; }
    public string NextHop { get; set; } = "";
    public int RouteMetric { get; set; }
}
