using SnapX.Core.Hotkey;
using SnapX.Core.Job;
using SnapX.Core.Upload;
using SnapX.Core.Utils;

namespace SnapX.Core.CLI;

public class SnapXCLIManager(string?[] Arguments) : CLIManager(Arguments)
{
    public async Task UseCommandLineArgs() => UseCommandLineArgs(Commands);

    public async Task UseCommandLineArgs(List<CLICommand> commands)
    {
        if (commands is { Count: > 0 })
        {
            var taskSettings = FindCLITask(commands);

            foreach (var command in commands)
            {

                if (command.IsCommand)
                {
                    if (CheckCustomUploader(command) || CheckImageEffect(command) || await CheckCLIHotkey(command) || await CheckCLIWorkflow(command) ||
                        await CheckNativeMessagingInput(command))
                    {
                    }

                    continue;
                }

                if (URLHelpers.IsValidURL(command.Command))
                {
                    DebugHelper.WriteLine("URL: " + command.Command);
                    UploadManager.DownloadAndUploadFile(command.Command, taskSettings);
                }
                else
                {
                    UploadManager.UploadFile(command.Command, taskSettings);
                }
            }
        }
    }

    private TaskSettings? FindCLITask(List<CLICommand> commands)
    {
        if (SnapX.HotkeysConfig == null) return null;
        var command = commands.FirstOrDefault(x => x.CheckCommand("task") && !string.IsNullOrEmpty(x.Parameter));

        return command == null ? null : (from hotkeySetting in SnapX.HotkeysConfig.Hotkeys where command.Parameter == hotkeySetting.TaskSettings.ToString() select TaskSettings.GetSafeTaskSettings(hotkeySetting.TaskSettings)).FirstOrDefault();
    }

    private bool CheckCustomUploader(CLICommand command)
    {
        if (command.Command.Equals("CustomUploader", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(command.Parameter) && command.Parameter.EndsWith(".sxcu", StringComparison.OrdinalIgnoreCase))
            {
                TaskHelpers.ImportCustomUploader(command.Parameter);
            }

            return true;
        }

        return false;
    }

    private bool CheckImageEffect(CLICommand command)
    {
        if (command.Command.Equals("ImageEffect", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(command.Parameter) && command.Parameter.EndsWith(".sxie", StringComparison.OrdinalIgnoreCase))
            {
                TaskHelpers.ImportImageEffect(command.Parameter);
            }

            return true;
        }

        return false;
    }

    private async Task<bool> CheckCLIHotkey(CLICommand command)
    {
        foreach (HotkeyType job in Helpers.GetEnums<HotkeyType>())
        {
            if (command.CheckCommand(job.ToString()))
            {
                await TaskHelpers.ExecuteJob(job, command);

                return true;
            }
        }

        return false;
    }

    private async Task<bool> CheckCLIWorkflow(CLICommand command)
    {
        if (SnapX.HotkeysConfig != null && command.CheckCommand("workflow") && !string.IsNullOrEmpty(command.Parameter))
        {
            foreach (HotkeySettings hotkeySetting in SnapX.HotkeysConfig.Hotkeys)
            {
                if (hotkeySetting.TaskSettings.Job != HotkeyType.None)
                {
                    if (command.Parameter == hotkeySetting.TaskSettings.ToString())
                    {
                        await TaskHelpers.ExecuteJob(hotkeySetting.TaskSettings);

                        return true;
                    }
                }
            }
        }

        return false;
    }

    private async Task<bool> CheckNativeMessagingInput(CLICommand command)
    {
        if (command.Command.Equals("NativeMessagingInput", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(command.Parameter) && command.Parameter.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                await TaskHelpers.HandleNativeMessagingInput(command.Parameter);
            }

            return true;
        }

        return false;
    }
}
