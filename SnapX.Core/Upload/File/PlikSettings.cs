
// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Utils;

namespace SnapX.Core.Upload.File;

public class PlikSettings
{
    public string URL { get; set; } = "";
    [JsonEncrypt]
    [YamlEncrypt]
    public string APIKey { get; set; } = "";
    public bool IsSecured { get; set; } = false;
    public string Login { get; set; } = "";
    [JsonEncrypt]
    [YamlEncrypt]
    public string Password { get; set; } = "";
    public bool Removable { get; set; } = false;
    public bool OneShot { get; set; } = false;
    public int TTLUnit { get; set; } = 2;
    public decimal TTL { get; set; } = 30;
    public bool HasComment { get; set; } = false;
    public string Comment { get; set; } = "";
}

