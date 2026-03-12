# GnomeStack.Secrets.OperatingSystem

Provides KeyTar like support for libsecret, Windows Credential Manager,
and macOS Keychain for .NET.

The main class `OsSecretVault` does not yet support listing credential as the Darwin ("MacOS")
implementation does not yet implement it.  It does support setting, getting, and deleting
credentials.

Similar to node-keytar, the linux implementation uses libsecret. OsSecretVault and
the LibSecret class requires libsecret and the gnome-keyring schema to be installed.
No promises on supporting other schemas at this time.

- Debian/Ubuntu: `sudo apt-get install libsecret-1-dev`
- Red Hat-based: `sudo yum install libsecret-devel`
- Arch Linux: `sudo pacman -S libsecret`

The macOS implementation uses the Keychain API and windows uses the Credential Manager API.
The windows implementation uses Enterprise persistence by default.  If you want to use
local persistence, you'll need to use the `WinCredManager` class directly.

## Usage

```csharp
using GnomeStack.Standard;

// basically service, account, password
OsSecretVault.SetSecret("myapp", "myuser", "mypassword"); // you can also pass in a byte[] instead of a string
var password = OsSecretVault.GetSecret("myapp", "myuser");

// Various options for getting the password
// such as byte and char arrays. Array values can be cleared from memory.
// Strings can not be cleared from memory as they are immutable in .NET.  
// SecureString is provided to support powershell despite Microsoft
// wanting to deprecate SecureString because of its lack of support
// for encryption on operating systems outside of Windows.
var pwBytes = OsSecretVault.GetSecretAsBytes("myapp", "myuser");
var pwChars = OsSecretVault.GetSecretAsChars("myapp", "myuser");
var secureString = OsSecretVault.GetSecretAsSecureString("myapp", "myuser");
OsSecretVault.DeleteSecret("myapp", "myuser");
```

You may call the os specific implementations directly or use their
implementation of `IOsSecretVault` which is a provider for
`OsSecretVault`.

MIT License
