using System.Linq.Expressions;

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
        public Binder(T value)
        {
            Value = value;
        }

        public IProperty<TVal> Prop<TVal>(Expression<Func<T, TVal>> exp)
        {
            Func<T, TVal> getter = exp.Compile();

            if (exp is not LambdaExpression lambda)
                throw new Exception();

            Expression body = exp.Body;
            ParameterExpression param = Expression.Parameter(typeof(TVal), "v");
            BinaryExpression assign = Expression.Assign(body, param);
            Expression<Action<T, TVal>> setExp = Expression.Lambda<Action<T, TVal>>(assign, lambda.Parameters[0], param);
            Action<T, TVal> setter = setExp.Compile();
            string name = body.ToString();
            name = name.Substring(name.IndexOf('.') + 1);

            SimpleProperty<TVal> result = new SimpleProperty<TVal>(() => getter(Value), v => setter(Value, v), name!);

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
