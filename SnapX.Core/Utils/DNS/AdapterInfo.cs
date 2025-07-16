// SPDX-License-Identifier: GPL-3.0-or-later
using System.Net.NetworkInformation;


namespace SnapX.Core.Utils.DNS;

public class AdapterInfo : IDisposable
{
    private NetworkInterface adapter;

    public AdapterInfo(NetworkInterface adapter)
    {
        this.adapter = adapter;
    }

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

    public bool IsEnabled() => adapter.OperationalStatus == OperationalStatus.Up;

    public string? GetCaption() => adapter.ToString();

    public string GetDescription() => adapter.Description;

    public string[] GetDNS() => adapter.GetIPProperties().UnicastAddresses.ToList().Select(x => x.Address.ToString()).ToArray();
    // TODO: SetDNS needs to be implemented on a per platform basis
    public uint SetDNS(string primary, string secondary) => throw new NotImplementedException("SetDNS is not implemented.");
    public uint SetDNSAutomatic()
    {
        return SetDNS(null, null);
    }

    public void Dispose() => Console.WriteLine($"Disposed adapter {adapter.Description}");

    public override string ToString() => GetDescription();
}
