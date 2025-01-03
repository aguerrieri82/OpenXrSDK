﻿using System.Linq.Expressions;
using System.Reflection;

namespace OpenXr.Framework
{

    public interface IXrActionBuilder
    {
        public IList<string> Profiles { get; }

        public IList<IXrInput> Inputs { get; }

        public IList<XrHaptic> Haptics { get; }

        public object Result { get; }
    }


    public class XrActionsBuilder<TProfile> : IXrActionBuilder where TProfile : IXrBasicInteractionProfile, new()
    {
        readonly List<IXrInput> _inputs = [];
        readonly TProfile _result;
        readonly XrApp _app;
        readonly IList<XrHaptic> _haptics = [];
        readonly IList<string> _profiles = [];

        protected class PropOrField
        {
            public string? Path;

            public Type? Type;

            public object? Value;

            public Action<object>? SetValue;

            public bool IsInput;

            public bool IsHaptic;

            public string? Name;
        }


        public XrActionsBuilder(XrApp app)
        {
            foreach (var path in typeof(TProfile).GetCustomAttributes<XrPathAttribute>())
                _profiles.Add(path.Value);

            if (_profiles.Count == 0)
                throw new NotSupportedException($"XrPathAttribute missing on type '{typeof(TProfile)}'");

            _result = new TProfile();
            _app = app;
        }

        public XrActionsBuilder<TProfile> AddAction(Expression<Func<TProfile, IXrAction?>> selector)
        {
            ProcessExpression(selector, out var path, out var name);
            return this;
        }

        protected object ProcessExpression<T>(Expression<Func<TProfile, T>> selector, out string path, out string name)
        {
            path = "";
            name = "";

            object Visit(ref string curPath, ref string curName, Expression exp)
            {
                if (exp is MemberExpression me)
                {
                    var curObj = Visit(ref curPath, ref curName, me.Expression!);

                    var member = GetMember(curObj, me.Member);

                    curName += member.Name;
                    curPath += member.Path;

                    if (member.Value == null)
                    {
                        if (member.IsInput)
                            member.Value = CreateInput(member.Type!, curPath, curName);
                        else if (member.IsHaptic)
                            member.Value = CreateHaptic(member.Type!, curPath, curName);
                        else
                            member.Value = Activator.CreateInstance(member.Type!)!;

                        member.SetValue!(member.Value);
                    }

                    return member.Value;
                }

                if (exp is UnaryExpression ue && ue.NodeType == ExpressionType.Convert)
                    return Visit(ref curPath, ref curName, ue.Operand);

                if (exp is ParameterExpression)
                    return _result!;

                throw new NotSupportedException();
            }

            return Visit(ref path, ref name, selector.Body);
        }

        protected IXrInput CreateInput(Type type, string path, string name)
        {
            if (type.IsAbstract && !(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(XrInput<>)))
                throw new NotSupportedException();

            IXrInput result;

            if (type.IsGenericType)
            {
                var arg = type.GetGenericArguments()[0];
                var resultType = typeof(XrInput<>).MakeGenericType(arg);
                var factory = resultType.GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
                result = (IXrInput)factory!.Invoke(null, [_app, path, name])!;
            }
            else
                result = (IXrInput)Activator.CreateInstance(type, _app, path, name)!;

            _inputs.Add(result);

            return result;
        }


        protected XrHaptic CreateHaptic(Type type, string path, string name)
        {
            if (type.IsAbstract)
                throw new NotSupportedException();

            var result = (XrHaptic)Activator.CreateInstance(type, _app, path, name)!;

            _haptics.Add(result);

            return result;
        }

        protected PropOrField GetMember(object obj, MemberInfo member)
        {
            var result = new PropOrField();
            result.Name = member.Name;

            if (member is FieldInfo fi)
            {
                result.Value = fi.GetValue(obj);
                result.Type = fi.FieldType;
                result.SetValue = value => fi.SetValue(obj, value);
            }

            else if (member is PropertyInfo pi)
            {
                result.Value = pi.GetValue(obj);
                result.Type = pi.PropertyType;
                result.SetValue = value => pi.SetValue(obj, value);
            }

            result.Path = member.GetCustomAttribute<XrPathAttribute>()?.Value ?? "";

            result.IsInput = typeof(IXrInput).IsAssignableFrom(result.Type);
            result.IsHaptic = typeof(XrHaptic).IsAssignableFrom(result.Type);

            return result;
        }

        public void AddAll()
        {
            AddAll(a => a!);
        }

        public XrActionsBuilder<TProfile> AddAll(Expression<Func<TProfile, object?>> selector)
        {
            var rootObj = ProcessExpression(selector, out var rootPath, out var rootName);

            void Visit(object curObj, string curPath, string curName)
            {
                void ProcessMember(PropOrField member)
                {
                    if (member.IsInput)
                    {
                        if (member.Value == null)
                        {
                            var value = CreateInput(member.Type!, curPath + member.Path, curName + member.Name);
                            member.SetValue!(value);
                        }
                    }
                    else if (member.IsHaptic)
                    {
                        if (member.Value == null)
                        {
                            var value = CreateHaptic(member.Type!, curPath + member.Path, curName + member.Name);
                            member.SetValue!(value);
                        }
                    }
                    else
                    {
                        if (member.Value == null)
                        {
                            member.Value = Activator.CreateInstance(member.Type!)!;
                            member.SetValue!(member.Value);
                        }

                        Visit(member.Value, curPath + member.Path, curName + member.Name);
                    }
                }

                foreach (var prop in curObj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    ProcessMember(GetMember(curObj, prop));

                foreach (var field in curObj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                    ProcessMember(GetMember(curObj, field));
            }

            Visit(rootObj, rootPath, rootName);

            return this;
        }

        object IXrActionBuilder.Result => _result!;

        public TProfile Result => _result;

        public IList<string> Profiles => _profiles;

        public IList<IXrInput> Inputs => _inputs;

        public IList<XrHaptic> Haptics => _haptics;
    }
}
