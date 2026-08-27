using System.Linq.Expressions;
using System.Reflection;

namespace UI.Binding
{
    public delegate void PropertyChangedHandler<T>(T? obj, IProperty property, object? value, object? oldValue);

    public static class Binder
    {
        public static Binder<T> Create<T>(T value)
        {
            return new Binder<T>(value);
        }

    }

    public class Binder<T>
    {
        readonly Action<Action>? _dispatcher;

        public Binder(T value, Action<Action>? dispatcher = null)
        {
            Value = value;
            _dispatcher = dispatcher;
        }

        public IProperty<TVal> Prop<TVal>(Expression<Func<T, TVal>> exp)
        {

            var getter = exp.Compile();

            var body = exp.Body;
            var param = Expression.Parameter(typeof(TVal), "v");
            var assign = Expression.Assign(body, param);
            var setExp = Expression.Lambda<Action<T, TVal>>(assign, exp.Parameters[0], param);
            var setter = setExp.Compile();

            var attributes = (body as MemberExpression)?.Member.GetCustomAttributes();

            var name = body.ToString();
            name = name.Substring(name.IndexOf('.') + 1);

            var result = new SimpleProperty<TVal>(() => getter(Value), v =>
            {
                if (_dispatcher != null)
                    _dispatcher(() => setter(Value, v));
                else
                    setter(Value, v);

            }, name, attributes);

            result.Changed += (s, e) =>
            {
                PropertyChanged?.Invoke(Value, result, result.Value, null);
            };

            return result;
        }

        public event PropertyChangedHandler<T>? PropertyChanged;

        public T Value;
    }
}
