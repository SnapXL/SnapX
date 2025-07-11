using System.Text;
using System.Text.Json.Serialization;

namespace SnapX.Core.Hotkey;

public record HotkeyInfo()
{
    public Keys Hotkey { get; set; }

    [JsonIgnore]
    public ushort ID { get; set; }

    [JsonIgnore]
    public HotkeyStatus Status { get; set; } = HotkeyStatus.NotConfigured;

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
            var modifiers = Modifiers.None;

            if (Alt) modifiers |= Modifiers.Alt;
            if (Control) modifiers |= Modifiers.Control;
            if (Shift) modifiers |= Modifiers.Shift;
            if (Win) modifiers |= Modifiers.Win;

            return modifiers;
        }
    }

    public bool IsOnlyModifiers => KeyCode == Keys.ControlKey || KeyCode == Keys.ShiftKey || KeyCode == Keys.Menu || (KeyCode == Keys.None && Win);

    public bool IsValidHotkey => KeyCode != Keys.None && !IsOnlyModifiers;

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
        var text = "";

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
        else if (KeyCode is >= Keys.D0 and <= Keys.D9)
        {
            text += (KeyCode - Keys.D0).ToString();
        }
        else if (KeyCode is >= Keys.NumPad0 and <= Keys.NumPad9)
        {
            text += "Numpad " + (KeyCode - Keys.NumPad0);
        }
        else
        {
            text += ToStringWithSpaces(KeyCode);
        }

        return text;
    }

    private string ToStringWithSpaces(Keys key)
    {
        var name = key.ToString();

        var result = new StringBuilder();

        for (var i = 0; i < name.Length; i++)
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

