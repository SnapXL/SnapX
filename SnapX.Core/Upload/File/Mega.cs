// SPDX-License-Identifier: GPL-3.0-or-later


using CG.Web.MegaApiClient;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;

namespace SnapX.Core.Upload.File;
public class MegaFileUploaderService : FileUploaderService
{
    public override FileDestination EnumValue { get; } = FileDestination.Mega;

    public override bool CheckConfig(UploadersConfig config)
    {
        return config.MegaAuthInfos != null && config.MegaAuthInfos.Email != null && config.MegaAuthInfos.Hash != null &&
            config.MegaAuthInfos.PasswordAesKey != null;
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new Mega(config.MegaAuthInfos?.GetMegaApiClientAuthInfos(), config.MegaParentNodeId);
    }
}

public sealed class Mega : FileUploader, IWebClient
{
    // Pack all chunks in a single upload fragment
    // (by default, MegaApiClient splits files in 1MB fragments and do multiple uploads)
    // It allows to have a consistent upload progression in SnapX
    private const int UploadChunksPackSize = -1;

    private readonly MegaApiClient megaClient;
    private readonly MegaApiClient.AuthInfos authInfos;
    private readonly string parentNodeId;

    public Mega() : this(null, null)
    {
    }

    public Mega(MegaApiClient.AuthInfos authInfos) : this(authInfos, null)
    {
    }

    public Mega(MegaApiClient.AuthInfos authInfos, string parentNodeId)
    {
        AllowReportProgress = false;
        var options = new Options(chunksPackSize: UploadChunksPackSize);
        megaClient = new MegaApiClient(options, this);
        this.authInfos = authInfos;
        this.parentNodeId = parentNodeId;
    }

    public bool TryLogin()
    {
        try
        {
            Login();
            return true;
        }
        catch (ApiException)
        {
            return false;
        }
    }

    private void Login()
    {
        if (authInfos == null)
        {
            megaClient.LoginAnonymous();
        }
        else
        {
            megaClient.Login(authInfos);
        }
    }

    internal IEnumerable<DisplayNode> GetDisplayNodes()
    {
        var nodes = megaClient.GetNodes()
            .Where(n => n.Type == NodeType.Directory || n.Type == NodeType.Root)
            .ToArray();

        var displayNodes = nodes.Select(node => new DisplayNode(node, nodes))
            .OrderBy(node => node.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        displayNodes.Insert(0, DisplayNode.EmptyNode);
        return displayNodes;
    }


    public INode GetParentNode() =>
        authInfos == null || parentNodeId == null
            ? megaClient.GetNodes().SingleOrDefault(n => n.Type == NodeType.Root)
            : megaClient.GetNodes().SingleOrDefault(n => n.Id == parentNodeId);

    public override UploadResult Upload(Stream stream, string? fileName)
    {
        Login();

        var createdNode = megaClient.Upload(stream, fileName, GetParentNode());

        return new UploadResult
        {
            IsURLExpected = true,
            URL = megaClient.GetDownloadLink(createdNode).ToString()
        };
    }


    #region IWebClient

    public Stream GetRequestRaw(Uri url)
    {
        throw new NotImplementedException();
    }

    public string? PostRequestJson(Uri url, string? jsonData)
    {
        return SendRequest(HttpMethod.Post, url.ToString(), jsonData, RequestHelpers.ContentTypeJSON);
    }

    public string? PostRequestRaw(Uri url, Stream dataStream)
    {
        try
        {
            AllowReportProgress = true;
            return SendRequest(HttpMethod.Post, url.ToString(), dataStream, "application/octet-stream");
        }
        finally
        {
            AllowReportProgress = false;
        }
    }

    public Stream PostRequestRawAsStream(Uri url, Stream dataStream)
    {
        throw new NotImplementedException();
    }

    #endregion IWebClient

    internal class DisplayNode
    {
        public static readonly DisplayNode EmptyNode = new DisplayNode();

        private DisplayNode()
        {
            DisplayName = "[Select a folder]";
        }

        public DisplayNode(INode node, IEnumerable<INode> nodes)
        {
            Node = node;
            DisplayName = GenerateDisplayName(node, nodes);
        }

        public INode Node { get; private set; }

        public string DisplayName { get; private set; }

        private string GenerateDisplayName(INode node, IEnumerable<INode> nodes)
        {
            var nodesTree = new List<string>();
            var parent = node;

            while (parent != null)
            {
                nodesTree.Add(parent.Type == NodeType.Directory ? parent.Name : parent.Type.ToString());
                parent = nodes.FirstOrDefault(n => n.Id == parent.ParentId);
            }

            nodesTree.Reverse();
            return string.Join(@"\", nodesTree);
        }
    }
}

