namespace XrEngine
{
    public class ServiceManager
    {
        readonly HashSet<IActiveService> _activeServices = [];

        ServiceManager()
        {

        }

        public void Register(IActiveService service)
        {
            _activeServices.Add(service);
        }

        public void Shutdown()
        {
            foreach (var service in _activeServices)
                service.Dispose();

            _activeServices.Clear();
        }

        public static readonly ServiceManager Instance = new();
    }
}
