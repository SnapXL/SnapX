using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;


namespace SnapX.Core.Models;

[Table("ApplicationConfig")]
public record SavedConfiguration
{
    [Key]
    public int Id { get; set; }

    public string ConfigSection { get; set; }

    public string SettingKey { get; set; }

    public string SettingValue { get; set; }

    public string DataType { get; set; }

    public DateTime UpdatedAt { get; set; }
}
