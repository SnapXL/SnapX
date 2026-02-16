using Avalonia.Controls;
using SnapX.Core;
using SnapX.Core.Upload.OAuth;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Avalonia.Views.Settings.Views;

public partial class OAuth2ListenerView : UserControl
{
    public IOAuth2Loopback? OAuth { get; private set; }
    public OAuth2Info? OAuth2Info { get; private set; }
    public OAuthUserInfo? UserInfo { get; private set; }

    private OAuthListener? _listener;
    private CancellationTokenSource? _cts;

    public event EventHandler<(OAuth2Info Info, OAuthUserInfo User)>? AuthenticationCompleted;
    public event EventHandler? AuthenticationCancelled;

    public OAuth2ListenerView()
    {
        InitializeComponent();
        CancelButton.Click += (s, e) => Cancel();
    }

    public async Task StartListeningAsync(IOAuth2Loopback oauth)
    {
        OAuth = oauth;
        OAuth2Info = null;
        UserInfo = null;
        _cts = new CancellationTokenSource();

        try
        {
            using (_listener = new OAuthListener(oauth))
            {
                var result = await _listener.ConnectAsync();

                if (result && !_cts.IsCancellationRequested)
                {
                    OAuth2Info = _listener.OAuth.AuthInfo;
                    UserInfo = await Task.Run(oauth.GetUserInfo);

                    AuthenticationCompleted?.Invoke(this, (OAuth2Info, UserInfo));
                }
            }
        }
        catch (Exception ex)
        {
            DebugHelper.WriteException(ex);
            ex.ShowError();
        }
    }

    public void Cancel()
    {
        _cts?.Cancel();
        _listener?.Dispose();
        AuthenticationCancelled?.Invoke(this, EventArgs.Empty);
    }
}
