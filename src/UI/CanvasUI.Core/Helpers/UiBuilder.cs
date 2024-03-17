using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CanvasUI
{
    public struct UiBuilder
    {
        public static UiBuilder<T> From<T>(T element) where  T : UiElement
        {
            return new UiBuilder<T>(element);
        }
    }

    public interface IUiBuilder<out T>
    {
        public T Element { get; }
    }

    public interface IUiChildBuilder<out TChild, out TParent> : IUiBuilder<TChild> 
           where TChild : UiElement
           where TParent : UiContainer
    {
        public IUiBuilder<TParent> Parent { get; }
    }

    public struct UiBuilder<T> : IUiBuilder<T> where T : UiElement
    {
        public UiBuilder(T element)
        {
            Element = element;  
        }


        readonly T IUiBuilder<T>.Element => Element;

        public T Element;
    }

    public struct UiChildBuilder<TChild, TParent> : IUiChildBuilder<TChild, TParent>
           where TChild : UiElement
           where TParent : UiContainer
    {
        public UiChildBuilder(IUiBuilder<TParent> parent, TChild element)
        {
            Element = element;
            Parent = parent;
        }

        readonly TChild IUiBuilder<TChild>.Element => Element;

        readonly IUiBuilder<TParent> IUiChildBuilder<TChild, TParent>.Parent => Parent;

        public IUiBuilder<TParent> Parent;

        public TChild Element;
    } 

    public static class UiBuilderExtensions
    {
        public static void BuildStyle(this UiElement element, Action<UiStyleBuilder> build)
        {
            build(new UiStyleBuilder(element.Style));
        }

        public static IUiChildBuilder<TChild, TCont> BeginChild<TCont, TChild>(this IUiBuilder<TCont> builder) 
            where TChild : UiElement, new()
            where TCont : UiContainer
        {
            return new UiChildBuilder<TChild, TCont>(builder, new TChild());
        }

        public static IUiBuilder<UiContainer> EndChild<T>(this IUiBuilder<T> builder) where T : UiContainer
        {
            var childBuild = builder as IUiChildBuilder<T, UiContainer>;

            if (childBuild == null)
                throw new InvalidOperationException("Invalid call, missing BeginChild");

            childBuild.Parent.Element.AddChild(builder.Element);

            return childBuild.Parent;
        }


        public static IUiBuilder<TCont> Child<TCont, TChild>(this IUiBuilder<TCont> builder, Action<IUiBuilder<TChild>>? build = null) where TChild : UiElement, new() where TCont: UiContainer
        {
            var child = new TChild();

            if (build != null)
            {
                var childBuilder = UiBuilder.From(child);
                build(childBuilder);
            }

            builder.Element.AddChild(child);

            return builder;
        }

        public static IUiBuilder<T> Set<T>(this IUiBuilder<T> builder, Action<T> build) where T : UiElement
        {
            build(builder.Element);
            return builder;
        }

        public static IUiBuilder<T> Name<T>(this IUiBuilder<T> builder, string value) where T : UiElement
        {
            builder.Element.Name = value;
            return builder;
        }

        public static IUiBuilder<T> Style<T>(this IUiBuilder<T> builder, Action<UiStyleBuilder>? build) where T : UiElement
        {
            if (build != null)
            {
                var sb = new UiStyleBuilder(builder.Element.Style);
                build(sb);
            }
            return builder;
        }


        public static IUiBuilder<T> AsColumn<T>(this IUiBuilder<T> builder) where T : UiContainer
        {
            builder.Element.Style.Layout = UiLayoutType.Flex;
            builder.Element.Style.FlexDirection = UIOrientation.Vertical;
            return builder;
        }

        public static IUiBuilder<T> AsRow<T>(this IUiBuilder<T> builder) where T : UiContainer
        {
            builder.Element.Style.Layout = UiLayoutType.Flex;
            builder.Element.Style.FlexDirection = UIOrientation.Horizontal;
            return builder;
        }

        public static IUiBuilder<UiContainer> BeginColumn<T>(this IUiBuilder<T> builder, Action<UiStyleBuilder>? style = null) where T : UiContainer
        {
            return builder
                   .BeginChild<T, UiContainer>()
                   .Style(style)
                   .AsColumn();
        }

        public static IUiBuilder<UiContainer> BeginRow<T>(this IUiBuilder<T> builder, Action<UiStyleBuilder>? style = null) where T : UiContainer
        {
            return builder
                   .BeginChild<T, UiContainer>()
                   .Style(style)
                   .AsRow();
        }
    }
}
