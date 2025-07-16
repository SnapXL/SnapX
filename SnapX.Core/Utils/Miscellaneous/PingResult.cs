// SPDX-License-Identifier: GPL-3.0-or-later


using System.Net.NetworkInformation;
using System.Text;

namespace SnapX.Core.Utils.Miscellaneous;

public class PingResult
{
    public List<PingReply> PingReplyList { get; private set; }

    public int Min
    {
        get
        {
            return (int)PingReplyList.Where(x => x.Status == IPStatus.Success).Min(x => x.RoundtripTime);
        }
    }

    public int Max
    {
        get
        {
            return (int)PingReplyList.Where(x => x.Status == IPStatus.Success).Max(x => x.RoundtripTime);
        }
    }

    public int Average
    {
        get
        {
            return (int)PingReplyList.Where(x => x.Status == IPStatus.Success).Average(x => x.RoundtripTime);
        }
    }

    public PingResult()
    {
        PingReplyList = [];
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        foreach (PingReply pingReply in PingReplyList)
        {
            if (pingReply != null)
            {
                switch (pingReply.Status)
                {
                    case IPStatus.Success:
                        sb.AppendLine(string.Format("Reply from {0}: bytes={1} time={2}ms TTL={3}", pingReply.Address, pingReply.Buffer.Length, pingReply.RoundtripTime, pingReply.Options.Ttl));
                        break;
                    case IPStatus.TimedOut:
                        sb.AppendLine("Request timed out.");
                        break;
                    default:
                        sb.AppendLine(string.Format("Ping failed: {0}", pingReply.Status.ToString()));
                        break;
                }
            }
        }

        if (PingReplyList.Any(x => x.Status == IPStatus.Success))
        {
            sb.AppendLine(string.Format("Minimum = {0}ms, Maximum = {1}ms, Average = {2}ms", Min, Max, Average));
        }

        return sb.ToString().Trim();
    }
}

