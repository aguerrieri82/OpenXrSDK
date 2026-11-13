using System.Diagnostics.CodeAnalysis;
using XrEngine;
using XrEngine.OpenXr;


namespace XrSamples.Graffiti
{
    [RequiresUnreferencedCode("Bridge")]
    public class Bridge
    {
        private readonly GraffitiStore _store;

        public Bridge()
        {
            _store = Context.Require<GraffitiStore>();
        }

        public IList<GraffitiInfo> ListGraffiti()
        {
            return _store.List();
        }

        public string GetThumbnail(Guid id)
        {
            return _store.GetThumbnail(id);
        }

        public Task ClearCanvasAsync()
        {
            return _store.ClearAsync();
        }

        public Task NewGraffitiAsync()
        {
            return _store.NewAsync();
        }

        public Task<GraffitiInfo> SaveGraffitiAsync(bool asNew)
        {
            return _store.SaveAsync(asNew);
        }

        public Task LoadGraffitiAsync(Guid id, bool originalPose)
        {
            return _store.LoadAsync(id, originalPose);
        }

        public void DeleteGraffiti(Guid id)
        {
            _store.Delete(id);
        }

        public async Task ShareColorImageAsync(Guid graffitiId)
        {
            var colorPath = Path.Combine(XrPlatform.Current!.PersistentPath, "Graffiti", "Saves", graffitiId.ToString(), "color.png");
            await Context.Require<IShareService>().ShareFileAsync(colorPath, "image/png", $"Graffiti {graffitiId:D}");
        }
    }
}
