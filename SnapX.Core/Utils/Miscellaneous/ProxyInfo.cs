// SPDX-License-Identifier: GPL-3.0-or-later


using System.Net;
using System.Reflection;

namespace SnapX.Core.Utils.Miscellaneous;

public class ProxyInfo
{
    public ProxyMethod ProxyMethod { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }

    public ProxyInfo()
    {
        ProxyMethod = ProxyMethod.Manual;
    }

    public bool IsValidProxy()
    {
        if (ProxyMethod == ProxyMethod.Manual)
        {
            return !string.IsNullOrEmpty(Host) && Port > 0;
        }

        if (ProxyMethod == ProxyMethod.Automatic)
        {
            WebProxy systemProxy = GetDefaultWebProxy();

            if (systemProxy != null && systemProxy.Address != null && !string.IsNullOrEmpty(systemProxy.Address.Host) && systemProxy.Address.Port > 0)
            {
                Host = systemProxy.Address.Host;
                Port = systemProxy.Address.Port;
                return true;
            }
        }

        return false;
    }

    public IWebProxy GetWebProxy()
    {
        try
        {
            if (IsValidProxy())
            {
                NetworkCredential credentials = new NetworkCredential(Username, Password);
                string address = string.Format("{0}:{1}", Host, Port);
                return new WebProxy(address, true, null, credentials);
            }
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e, "GetWebProxy failed.");
        }

        return null;
    }

    private WebProxy GetDefaultWebProxy()
    {
        try
        {
            // Need better solution
            return (WebProxy)typeof(WebProxy).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
                null, [typeof(bool)], null).Invoke([true]);
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e, "Reflection failed.");
        }

        return null;
    }

    public override string ToString()
    {
        return string.Format("{0} - {1}:{2}", Username, Host, Port);
    }
}

