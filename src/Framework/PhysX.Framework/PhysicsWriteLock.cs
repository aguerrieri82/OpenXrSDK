namespace PhysX.Framework
{
    public unsafe struct PhysicsWriteLock : IDisposable
    {
        readonly PxScene* _scene;
        bool _isDisposed;

        public PhysicsWriteLock(PxScene* scene)
        {
            _scene = scene;
            _scene->LockWriteMut(null, 0);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _scene->UnlockWriteMut();
            _isDisposed = true;
        }
    }
}
