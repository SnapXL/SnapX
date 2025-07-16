// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.File;

public class PlikSettings
{
    public string URL { get; set; } = "";
    public string APIKey { get; set; } = "";
    public bool IsSecured { get; set; } = false;
    public string Login { get; set; } = "";
    public string Password { get; set; } = "";
    public bool Removable { get; set; } = false;
    public bool OneShot { get; set; } = false;
    public int TTLUnit { get; set; } = 2;
    public decimal TTL { get; set; } = 30;
    public bool HasComment { get; set; } = false;
    public string Comment { get; set; } = "";
}

