using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace SnapX.Core.Utils.Converters;

public sealed class PreferredIdentityTypeInspector(ITypeInspector Inner) : TypeInspectorSkeleton
{
    public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container)
    {
        var properties = Inner.GetProperties(type, container).ToList();

        if (properties.Count == 0)
            return properties;

        var preferred = properties.FirstOrDefault(p =>
            string.Equals(p.Name, "Name", StringComparison.OrdinalIgnoreCase));

        if (preferred == null)
        {
            preferred = properties.FirstOrDefault(p =>
                string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase));
        }

        if (preferred != null)
        {
            properties.Remove(preferred);
            properties.Insert(0, preferred);
        }

        return properties;
    }

    public override string GetEnumName(Type enumType, string name)
        => Inner.GetEnumName(enumType, name);

    public override string GetEnumValue(object enumValue)
        => Inner.GetEnumValue(enumValue);
}
