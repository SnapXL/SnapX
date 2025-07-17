// SPDX-License-Identifier: GPL-3.0-or-later
using System.Net.NetworkInformation;


namespace SnapX.Core.Utils.DNS;

public class AdapterInfo(NetworkInterface Adapter) : IDisposable
{
    public static List<AdapterInfo> GetEnabledAdapters()
    {
        var adapters = new List<AdapterInfo>();
        var enabledInterfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
            .ToList();

        foreach (var ni in enabledInterfaces)
        {
            adapters.Add(new AdapterInfo(ni));
        }
        return adapters;
    }

    public bool IsEnabled() => Adapter.OperationalStatus == OperationalStatus.Up;

    public string? GetCaption() => Adapter.ToString();

    public string GetDescription() => Adapter.Description;

    public string[] GetDNS() => Adapter.GetIPProperties().UnicastAddresses.ToList().Select(x => x.Address.ToString()).ToArray();
    // TODO: SetDNS needs to be implemented on a per platform basis
    public uint SetDNS(string primary, string? secondary) => throw new NotImplementedException("SetDNS is not implemented.");
    public uint SetDNSAutomatic()
    {
        return SetDNS(null, null);
    }

    public void Dispose() => Console.WriteLine($"Disposed adapter {Adapter.Description}");

    public override string ToString() => GetDescription();
}
