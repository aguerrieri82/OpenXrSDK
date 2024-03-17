using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CanvasUI
{
    public delegate void PropertyChangedHandler<T>(T? obj, IProperty property, object? value, object? oldValue);

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
                PropertyChanged?.Invoke(Value, result, result.Get(), null);
            };

            return result;
        }

        public event PropertyChangedHandler<T>? PropertyChanged;

        public T Value;
    }
}
