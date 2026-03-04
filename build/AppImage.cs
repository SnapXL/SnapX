namespace DefaultNamespace;

public class AppImage(IBuildLogger Logger, ICommandRunner CommandRunner, FS FileSystem, BuildConfig config)
{
    public async Task ProcessAppImage()
    {
        Logger.Information("For creating AppImages, this script expects https://github.com/probonopd/go-appimage/tree/master/src/mkappimage in $PATH and named mkappimage without .AppImage extension");

        var TargetInstallAssembly = config.TargetInstallAssembly ?? "snapx-ui";

        if (!config.ShouldSkip("tarball"))
        {
            config.SkippedStepsRaw = config.SkippedStepsRaw.Append("archive").ToArray();
            config.SetSkippedSteps(config.SkippedStepsRaw);

            config.Tarballdir = config.Appdir;
            config.DestDir = config.Appdir;

            config.Prefix = "usr";
            config.BinDir = Path.Combine(config.Appdir, config.Prefix, "bin");
            config.LibDir = Path.Combine(config.Appdir, config.Prefix, "lib");

            var tarballCreator = new Tarball(Logger, CommandRunner, FileSystem, config);
            await tarballCreator.ProcessTarball();
        }

        if (Directory.Exists(config.Metainfodir))
        {
            var files = Directory.EnumerateFiles(config.Metainfodir);
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                if (fileName.Contains("metainfo"))
                {
                    var newPath = Path.Combine(Path.GetDirectoryName(file)!, fileName.Replace("metainfo", "appdata"));
                    File.Move(file, newPath, true);
                }
            }
        }

        // Find the highest resolution PNG for the root
        var pngFiles = Directory.EnumerateFiles(config.Icondir, "*.png", SearchOption.AllDirectories);
        string? largestPng = null;
        long maxSize = -1;

        foreach (var file in pngFiles)
        {
            var fileInfo = new FileInfo(file);
            if (fileInfo.Length > maxSize)
            {
                largestPng = file;
                maxSize = fileInfo.Length;
            }
        }

        if (largestPng != null)
        {
            File.Copy(largestPng, Path.Combine(config.Appdir, Path.GetFileName(largestPng)), true);
        }

        if (Directory.Exists(config.Applicationsdir))
        {
            foreach (var file in Directory.EnumerateFiles(config.Applicationsdir, "*.desktop"))
            {
                File.Copy(file, Path.Combine(config.Appdir, Path.GetFileName(file)), true);
            }
        }
        // We inscribe the sacred launch scroll into AppRun — a bash incantation of great power.
        // This script is the entrypoint, the oracle, the keeper of $APPDIR and guardian of $LD_LIBRARY_PATH.
        // It does what all wise elders do: prints debug info, chases symbolic links, and launches binaries like it's 1999.
        // And yes, it even negotiates with zenity when things go sideways.
        // May the chmod +x bless it, and may your AppImage rise without segfault.
        var appRunPath = Path.Combine(config.Appdir, "AppRun");
        var script = $"""
                      #!/usr/bin/env sh
                      HERE="$(dirname "$(readlink -f "$0")")"
                      APP_LIBS="$HERE/usr/lib"

                      LOADER=$(ls "$APP_LIBS"/ld-linux*.so* 2>/dev/null | head -n 1)

                      if [ -n "$LOADER" ] && [ -f "$LOADER" ]; then
                          export LD_LIBRARY_PATH="$APP_LIBS:$LD_LIBRARY_PATH"
                          exec "$LOADER" --library-path "$APP_LIBS" "$APP_LIBS/{TargetInstallAssembly}" "$@"
                      else
                          export LD_LIBRARY_PATH="$APP_LIBS:$LD_LIBRARY_PATH"
                          exec "$APP_LIBS/{TargetInstallAssembly}" "$@"
                      fi
                      """;

                              await File.WriteAllTextAsync(appRunPath, script);
                              await CommandRunner.RunAsync("chmod", $"+x {appRunPath}");

                              var arch = (await CommandRunner.CaptureAsync("arch", "")).Trim();
                              await CommandRunner.RunAsync(
                                  "env",
                                  $"VERSION={config.SnapXVersion} APPIMAGELAUNCHER_DISABLE=1 mkappimage -s --comp zstd --ll -u \"gh-releases-zsync|SnapXL|SnapX|latest|SnapX-*{arch}.AppImage.zsync\" {config.Appdir}"
                              );
                          }

                      }
