using System.Text;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace SnapX.Core.Hotkey;

public class HotkeyInfo
{
    public Keys Hotkey { get; set; }

    [JsonIgnore, YamlIgnore]
    public ushort ID { get; set; }

    [JsonIgnore, YamlIgnore]
    public HotkeyStatus Status { get; set; }

    public Keys KeyCode => Hotkey & Keys.KeyCode;

    public Keys ModifiersKeys => Hotkey & Keys.Modifiers;

    public bool Control => Hotkey.HasFlag(Keys.Control);

    public bool Shift => Hotkey.HasFlag(Keys.Shift);

    public bool Alt => Hotkey.HasFlag(Keys.Alt);

    public bool Win { get; set; }

    public Modifiers ModifiersEnum
    {
        get
        {
            Modifiers modifiers = Modifiers.None;

            if (Alt) modifiers |= Modifiers.Alt;
            if (Control) modifiers |= Modifiers.Control;
            if (Shift) modifiers |= Modifiers.Shift;
            if (Win) modifiers |= Modifiers.Win;

            return modifiers;
        }
    }

    public bool IsOnlyModifiers => KeyCode == Keys.Control || KeyCode == Keys.ShiftKey || KeyCode == Keys.Menu || (KeyCode == Keys.None && Win);

    public bool IsValidHotkey => KeyCode != Keys.None && !IsOnlyModifiers;

    public HotkeyInfo()
    {
        Status = HotkeyStatus.NotConfigured;
    }

    public HotkeyInfo(Keys hotkey) : this()
    {
        Hotkey = hotkey;
    }

    public HotkeyInfo(Keys hotkey, ushort id) : this(hotkey)
    {
        ID = id;
    }

    public override string ToString()
    {
        string text = "";

        if (Control)
        {
            text += "Ctrl + ";
        }

        if (Shift)
        {
            text += "Shift + ";
        }

        if (Alt)
        {
            text += "Alt + ";
        }

        if (Win)
        {
            text += "Win + ";
        }

        if (IsOnlyModifiers)
        {
            text += "...";
        }
        else if (KeyCode == Keys.Back)
        {
            text += "Backspace";
        }
        else if (KeyCode == Keys.Return)
        {
            text += "Enter";
        }
        else if (KeyCode == Keys.Capital)
        {
            text += "Caps Lock";
        }
        else if (KeyCode == Keys.Next)
        {
            text += "Page Down";
        }
        else if (KeyCode == Keys.Scroll)
        {
            text += "Scroll Lock";
        }
        else if (KeyCode >= Keys.D0 && KeyCode <= Keys.D9)
        {
            text += (KeyCode - Keys.D0).ToString();
        }
        else if (KeyCode >= Keys.NumPad0 && KeyCode <= Keys.NumPad9)
        {
            text += "Numpad " + (KeyCode - Keys.NumPad0).ToString();
        }
        else
        {
            text += ToStringWithSpaces(KeyCode);
        }

        return text;
    }

    private string ToStringWithSpaces(Keys key)
    {
        string name = key.ToString();

        var result = new StringBuilder();

        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
            {
                result.Append(" " + name[i]);
            }
            else
            {
                result.Append(name[i]);
            }
        }

        return result.ToString();
    }
}

