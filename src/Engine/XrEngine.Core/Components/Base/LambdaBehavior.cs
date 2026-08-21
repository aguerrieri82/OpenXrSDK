namespace XrEngine
{
    public class LambdaBehavior<T> : Behavior<T> where T : EngineObject
    {
        readonly Action<T, RenderContext> _update;

        public LambdaBehavior(Action<T, RenderContext> update)
        {
            _update = update;
        }
        protected override void Update(RenderContext ctx)
        {
            _update(_host, ctx);
        }

    }
}
