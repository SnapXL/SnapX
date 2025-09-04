import { dotnet } from './_framework/dotnet.js'

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .withModuleConfig({
        onDownloadResourceProgress: (loaded, total) => {
            console.log(`Progress: ${loaded}/${total} resources downloaded`);

            const progressElement = document.getElementById("progress");
            const progress = document.getElementById("progress-text");
            if (total > 0) {
                progressElement.value = (loaded / total) * 100;
                progress.textContent = `${((loaded / total) * 100).toFixed(1)}%`;
            }
        }
    })
    .create();

const config = dotnetRuntime.getConfig();

console.log("config", config);

await dotnetRuntime.runMainAndExit(config.mainAssemblyName, [window.location.search]);
