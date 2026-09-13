using Android.Media;
using Microsoft.Maui.ApplicationModel;

namespace Swift.Services;

/// <summary>
/// Plays Platforms/Android/Resources/raw/alert.mp3 when the speed limit is
/// crossed. This project targets net-android only, so it's safe to call
/// Android.Media APIs directly here without any #if ANDROID guard or a
/// Platforms-folder partial-class split — every file in this project
/// already compiles under the Android target.
/// </summary>
public class AlertSoundService
{
    private MediaPlayer? _player;

    public void PlayAlert()
    {
        try
        {
            _player?.Release();
            _player = null;

            var activity = Platform.CurrentActivity;
            if (activity is null) return;

            var resId = activity.Resources?.GetIdentifier("alert", "raw", activity.PackageName) ?? 0;
            if (resId == 0) return;

            _player = MediaPlayer.Create(activity, resId);
            _player?.Start();
        }
        catch
        {
            // A missing or momentarily busy audio device should never take
            // down speed tracking — the visual alert still fires either way.
        }
    }
}
