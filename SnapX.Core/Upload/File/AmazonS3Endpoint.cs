// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Upload.File;

public class AmazonS3Endpoint
{
    public string Name { get; set; }
    public string Endpoint { get; set; }
    public string Region { get; set; }

    public AmazonS3Endpoint(string name, string endpoint)
    {
        Name = name;
        Endpoint = endpoint;
    }

    public AmazonS3Endpoint(string name, string endpoint, string region)
    {
        Name = name;
        Endpoint = endpoint;
        Region = region;
    }

    public override string ToString()
    {
        return Name;
    }
}
