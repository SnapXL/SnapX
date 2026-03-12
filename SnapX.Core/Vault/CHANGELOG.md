# CHANGE LOG

## 0.1.6

- Lib GnomeStack.Core reverted Env back to older API.

## 0.1.5

- Move source to github.com/gnomestack/dotnet-std from github.com/gnomestack/dotnet
- Change library name from Os.Secrets to Gnomestack.Secrets.OperatingSystem.
- Change namespaces to well known namespaces within Gnomestack.
- Move OsSecretsVault to Standard namespace
- Move implements to GnomeStack.Secrets.{Os}

## 0.1.2 initial creation

- Initial creation with basic functionality for Linux, Windows, and MacOS.
- Implement GetSecret, SetSecret, and DeleteSecret.
- Attempt to implement basic compatibility with node-keytar.
