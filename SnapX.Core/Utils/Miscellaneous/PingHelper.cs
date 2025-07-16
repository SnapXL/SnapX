// SPDX-License-Identifier: GPL-3.0-or-later


using System.Net;
using System.Net.NetworkInformation;

namespace SnapX.Core.Utils.Miscellaneous;
public static class PingHelper
{
    public static PingResult PingHost(string host, int timeout = 1000, int pingCount = 4, int waitTime = 100)
    {
        var pingResult = new PingResult();
        var address = GetIpFromHost(host);
        var buffer = new byte[32];
        var pingOptions = new PingOptions(128, true);

        using var ping = new Ping();
        for (int i = 0; i < pingCount; i++)
        {
            try
            {
                var pingReply = ping.Send(address, timeout, buffer, pingOptions);
                if (pingReply == null) continue;

                pingResult.PingReplyList.Add(pingReply);
            }
            catch (Exception e)
            {
                DebugHelper.WriteException(e);
            }

            if (waitTime > 0 && i + 1 < pingCount)
            {
                Thread.Sleep(waitTime);
            }
        }

        return pingResult;
    }

    private static IPAddress GetIpFromHost(string host)
    {
        if (!IPAddress.TryParse(host, out IPAddress address))
        {
            try
            {
                address = Dns.GetHostEntry(host).AddressList[0];
            }
            catch (Exception e)
            {
                DebugHelper.WriteException(e);
            }
        }

        return address;
    }
}

