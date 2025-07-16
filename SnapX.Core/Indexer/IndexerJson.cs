// SPDX-License-Identifier: GPL-3.0-or-later


using System.Text;
using System.Text.Json;

namespace SnapX.Core.Indexer;
public class IndexerJson : Indexer
{
    private Utf8JsonWriter jsonWriter;
    public IndexerJson(IndexerSettings indexerSettings) : base(indexerSettings)
    {
    }

    private string Index(string folderPath)
    {
        var folderInfo = new FolderInfo(folderPath);
        folderInfo.Update();

        var sbContent = new StringBuilder();

        // Use MemoryStream and Utf8JsonWriter for JSON writing
        using var memoryStream = new MemoryStream();
        using var jsonWriter = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true });

        jsonWriter.WriteStartObject();
        IndexFolder(folderInfo);
        jsonWriter.WriteEndObject();

        // Convert the memory stream to a string
        sbContent.Append(Encoding.UTF8.GetString(memoryStream.ToArray()));

        return sbContent.ToString();
    }

    protected override void IndexFolder(FolderInfo dir, int level = 0)
    {
        IndexFolderSimple(dir);
    }

    private void IndexFolderSimple(FolderInfo dir)
    {
        jsonWriter.WritePropertyName(dir.FolderName);
        jsonWriter.WriteStartArray();

        foreach (FolderInfo subdir in dir.Folders)
        {
            jsonWriter.WriteStartObject();
            IndexFolder(subdir);
            jsonWriter.WriteEndObject();
        }

        jsonWriter.WriteEndArray();
    }

    private void IndexFolderParseable(FolderInfo dir)
    {
        jsonWriter.WritePropertyName("Name");

        if (dir.Folders.Count > 0)
        {
            jsonWriter.WritePropertyName("Folders");
            jsonWriter.WriteStartArray();

            foreach (FolderInfo subdir in dir.Folders)
            {
                jsonWriter.WriteStartObject();
                IndexFolder(subdir);
                jsonWriter.WriteEndObject();
            }

            jsonWriter.WriteEndObject();
        }

        if (dir.Files.Count > 0)
        {
            jsonWriter.WritePropertyName("Files");
            jsonWriter.WriteStartArray();

            foreach (FileInfo fi in dir.Files)
            {
                jsonWriter.WriteStartObject();

                jsonWriter.WritePropertyName("Name");
                jsonWriter.WriteEndObject();
            }

        }
    }
}

