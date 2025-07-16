// SPDX-License-Identifier: GPL-3.0-or-later


using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.Upload.BaseServices;

public abstract class UploaderService<T> : IUploaderService
{
    public abstract T EnumValue { get; }

    // Unique identifier
    public string ServiceIdentifier => EnumValue.ToString();

    public string ServiceName => ((Enum)(object)EnumValue).GetLocalizedDescription();



    public abstract bool CheckConfig(UploadersConfig config);

    public override string ToString()
    {
        return ServiceName;
    }
}
