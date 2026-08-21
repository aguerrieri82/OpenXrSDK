namespace XrEngine.Lighting
{
    public unsafe class LightContribution : IDisposable
    {
        VoxelLightContributionView _view;

        public LightContribution()
            : this(new())
        {
        }

        public LightContribution(VoxelLightContributionView view)
        {
            _view = view;
        }

        public void Dispose()
        {
            EngineNativeLib.FreeContributionView(ref _view);
            GC.SuppressFinalize(this);
        }

        public ref VoxelLightContributionView View => ref _view;

        public Span<VoxelLightCell> Cells => _view.Cells == null ? [] : new Span<VoxelLightCell>(_view.Cells, _view.CellCount);
    }
}