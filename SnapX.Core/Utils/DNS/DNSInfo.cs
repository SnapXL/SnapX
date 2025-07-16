// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Utils.DNS;

public class DNSInfo
{
    public string Name { get; set; }
    public string PrimaryDNS { get; set; }
    public string SecondaryDNS { get; set; }

    public DNSInfo(string name, string primaryDNS, string secondaryDNS)
    {
        Name = name;
        PrimaryDNS = primaryDNS;
        SecondaryDNS = secondaryDNS;
    }

    public override string ToString()
    {
        return Name;
    }
}

