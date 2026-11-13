using Android.Content;
using AndroidX.Core.Content;


namespace XrEngine.OpenXr
{
    public class AndroidShareService : IShareService
    {
        public async Task ShareFileAsync(string filePath, string mimeType, string? title = null)
        {
            await Task.Run(() =>
            {
                var intent = new Intent(Intent.ActionSend);
                intent.SetType(mimeType);

                var file = new Java.IO.File(filePath);
                var uri = FileProvider.GetUriForFile(Application.Context, $"{Application.Context.PackageName}.fileprovider", file);
                intent.PutExtra(Intent.ExtraStream, uri);

                if (!string.IsNullOrEmpty(title))
                    intent.PutExtra(Intent.ExtraSubject, title);

                var chooser = Intent.CreateChooser(intent, "Share");
                chooser.AddFlags(ActivityFlags.NewTask);

                Application.Context.StartActivity(chooser);
            });
        }
    }
}
