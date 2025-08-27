namespace DefaultNamespace;

public class AppImage(IBuildLogger Logger, ICommandRunner CommandRunner, FS FileSystem, BuildConfig config)
{
    public async Task ProcessAppImage()
    {
        Logger.Information($"For creating AppImages, this script expects https://github.com/probonopd/go-appimage/tree/master/src/mkappimage in $PATH and named mkappimage without .AppImage extension");
        if (!config.ShouldSkip("tarball"))
        {
            config.SkippedStepsRaw = config.SkippedStepsRaw.Append("archive").ToArray();
            config.SetSkippedSteps(config.SkippedStepsRaw);
            config.Tarballdir = config.Appdir;
            config.DestDir = config.Appdir;
            config.Prefix = "usr";
            config.BinDir += Path.DirectorySeparatorChar;
            config.LibDir = config.BinDir;
            config.DisableWrapperScript = true;
            var tarballCreator = new Tarball(Logger, CommandRunner, FileSystem, config);
            await tarballCreator.ProcessTarball();
        }
        var files = Directory.EnumerateFiles(config.Metainfodir);

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);

            if (fileName.Contains("metainfo"))
            {
                var newFileName = fileName.Replace("metainfo", "appdata");
                var directory = Path.GetDirectoryName(file)!;
                var newPath = Path.Combine(directory, newFileName);

                File.Move(file, newPath, true);
            }
        }

        // Only the biggest PNG — the most pixel-dense, the thiccest of them all —
        // shall be offered to the AppDir as a symbol of our respect and devotion.
        // 🏆✨ Let the icon hunger games begin.
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
            var destPath = Path.Combine(config.Appdir, Path.GetFileName(largestPng));
            File.Copy(largestPng, destPath, overwrite: true);
        }
        var sourceFiles = Directory.EnumerateFiles(config.Applicationsdir, "*", SearchOption.TopDirectoryOnly);

        foreach (var file in sourceFiles)
        {
            var fileName = Path.GetFileName(file);
            var destPath = Path.Combine(config.Appdir, fileName);

            File.Copy(file, destPath, overwrite: true);
        }
        // We inscribe the sacred launch scroll into AppRun — a bash incantation of great power.
        // This script is the entrypoint, the oracle, the keeper of $APPDIR and guardian of $LD_LIBRARY_PATH.
        // It does what all wise elders do: prints debug info, chases symbolic links, and launches binaries like it's 1999.
        // And yes, it even negotiates with zenity when things go sideways.
        // May the chmod +x bless it, and may your AppImage rise without segfault.
        await File.WriteAllTextAsync(Path.Join(config.Appdir, "AppRun"),
            $$"""
            #!/bin/bash
            set -e

            if [ ! -z "$DEBUG" ] ; then
              env
              set -x
            fi

            THIS="$0"
            # http://stackoverflow.com/questions/3190818/
            args=("$@")
            NUMBER_OF_ARGS="$#"

            if [ -z "$APPDIR" ] ; then
              # Find the AppDir. It is the directory that contains AppRun.
              # This assumes that this script resides inside the AppDir or a subdirectory.
              # If this script is run inside an AppImage, then the AppImage runtime likely has already set $APPDIR
              path="$(dirname "$(readlink -f "${THIS}")")"
              while [[ "$path" != "" && ! -e "$path/$1" ]]; do
                path=${path%/*}
              done
              APPDIR="$path"
            fi

            export PATH="${APPDIR}:${APPDIR}/usr/sbin:${PATH}"
            export XDG_DATA_DIRS="./share/:/usr/share/gnome:/usr/local/share/:/usr/share/:${XDG_DATA_DIRS}"
            export LD_LIBRARY_PATH="${APPDIR}/usr/lib:${LD_LIBRARY_PATH}"
            export XDG_DATA_DIRS="${APPDIR}"/usr/share/:"${XDG_DATA_DIRS}":/usr/share/gnome/:/usr/local/share/:/usr/share/
            export GSETTINGS_SCHEMA_DIR="${APPDIR}/usr/share/glib-2.0/schemas:${GSETTINGS_SCHEMA_DIR}"

            BIN="$APPDIR/usr/bin/{{config.TargetInstallAssembly ?? "snapx-ui"}}"

            if [ -z "$APPIMAGE_EXIT_AFTER_INSTALL" ] ; then
              trap atexit EXIT
            fi

            isEulaAccepted=1

            atexit()
            {
              if [ $isEulaAccepted == 1 ] ; then
                if [ $NUMBER_OF_ARGS -eq 0 ] ; then
                  exec "$BIN"
                else
                  exec "$BIN" "${args[@]}"
                fi
              fi
            }

            error()
            {
              if [ -x /usr/bin/zenity ] ; then
                LD_LIBRARY_PATH="" zenity --error --text "${1}" 2>/dev/null
              elif [ -x /usr/bin/kdialog ] ; then
                LD_LIBRARY_PATH="" kdialog --msgbox "${1}" 2>/dev/null
              elif [ -x /usr/bin/Xdialog ] ; then
                LD_LIBRARY_PATH="" Xdialog --msgbox "${1}" 2>/dev/null
              else
                echo "${1}"
              fi
              exit 1
            }

            yesno()
            {
              TITLE=$1
              TEXT=$2
              if [ -x /usr/bin/zenity ] ; then
                LD_LIBRARY_PATH="" zenity --question --title="$TITLE" --text="$TEXT" 2>/dev/null || exit 0
              elif [ -x /usr/bin/kdialog ] ; then
                LD_LIBRARY_PATH="" kdialog --title "$TITLE" --yesno "$TEXT" || exit 0
              elif [ -x /usr/bin/Xdialog ] ; then
                LD_LIBRARY_PATH="" Xdialog --title "$TITLE" --clear --yesno "$TEXT" 10 80 || exit 0
              else
                echo "zenity, kdialog, Xdialog missing. Skipping ${THIS}."
                exit 0
              fi
            }

            check_dep()
            {
              DEP=$1
              if [ -z $(which "$DEP") ] ; then
                echo "$DEP is missing. Skipping ${THIS}."
                exit 0
              fi
            }

            if [ -z "$APPIMAGE" ] ; then
              APPIMAGE="$APPDIR/AppRun"
              # not running from within an AppImage; hence using the AppRun for Exec=
            fi
            """
            );
        await CommandRunner.RunAsync("chmod", $"+x {Path.Join(config.Appdir, "AppRun")}");
        var arch = await CommandRunner.CaptureAsync("arch", "");
        await CommandRunner.RunAsync(
            "env",
            $"VERSION={config.SnapXVersion} APPIMAGELAUNCHER_DISABLE=1 mkappimage --comp zstd --ll -u \"gh-releases-zsync|SnapXL|SnapX|latest|SnapX-*{arch}.AppImage.zsync\" {config.Appdir}"
        );

    }

}
