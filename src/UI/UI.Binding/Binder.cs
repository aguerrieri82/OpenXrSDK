﻿using System.Linq.Expressions;

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
            var getter = exp.Compile();

            if (exp is not LambdaExpression lambda)
                throw new Exception();

            var body = exp.Body;
            var param = Expression.Parameter(typeof(TVal), "v");
            var assign = Expression.Assign(body, param);
            var setExp = Expression.Lambda<Action<T, TVal>>(assign, lambda.Parameters[0], param);
            var setter = setExp.Compile();
            var name = body.ToString();
            name = name.Substring(name.IndexOf('.') + 1);

            var result = new SimpleProperty<TVal>(() => getter(Value), v => setter(Value, v), name!);

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
