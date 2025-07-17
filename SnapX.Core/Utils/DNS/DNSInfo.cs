// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Utils.DNS;

public class DNSInfo(string Name, string PrimaryDns, string SecondaryDns)
{
    public string Name { get; set; } = Name;
    public string PrimaryDNS { get; set; } = PrimaryDns;
    public string SecondaryDNS { get; set; } = SecondaryDns;

    public override string ToString()
    {
        return Name;
    }
}

