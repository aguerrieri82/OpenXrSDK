using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XrEngine.UI.Components;

namespace XrEngine.UI
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

    public struct StyleBuilder
    {
        public StyleBuilder(UiStyle style)
        {
            Style = style;
        }

        public readonly StyleBuilder BackgroundColor(Color color)
        {
            Style.BackgroundColor = color;
            return this;
        }

        public readonly StyleBuilder Padding(float value, Unit unit = Unit.Dp)
        {
            Style.Padding = UnitRectValue.All(value, unit);
            return this;
        }

        public readonly StyleBuilder Padding(float vert, float hor, Unit unit = Unit.Dp)
        {
            Style.Padding = UnitRectValue.Axis(vert, hor, unit);
            return this;
        }

        public readonly StyleBuilder Margin(float value, Unit unit = Unit.Dp)
        {
            Style.Margin = UnitRectValue.All(value, unit);
            return this;
        }

        public readonly StyleBuilder Margin(float vert, float hor, Unit unit = Unit.Dp)
        {
            Style.Margin = UnitRectValue.Axis(vert, hor, unit);
            return this;
        }

        public readonly StyleBuilder Border(float width, Color color, BorderStyle style = BorderStyle.Solid)
        {
            Style.Border = BorderRectValue.All(width, color, style);
            return this;
        }


        public readonly StyleBuilder AlignContent(UiAlignment value)
        {
            Style.AlignContent = value;
            return this;
        }

        public readonly StyleBuilder AlignItems(UiAlignment value)
        {
            Style.AlignItems = value;
            return this;
        }

        public readonly StyleBuilder FlexVertical()
        {
            Style.Layout = UiLayoutType.Flex;
            Style.FlexDirection = UIOrientation.Vertical;
            return this;
        }

        public readonly StyleBuilder FlexHorizontal()
        {
            Style.Layout = UiLayoutType.Flex;
            Style.FlexDirection = UIOrientation.Horizontal;
            return this;
        }

        public readonly StyleBuilder RowGap(float value, Unit unit = Unit.Dp)
        {
            Style.RowGap = UnitValue.Get(value, unit);
            return this;
        }

        public readonly StyleBuilder ColGap(float value, Unit unit = Unit.Dp)
        {
            Style.ColGap = UnitValue.Get(value, unit);
            return this;
        }

        public readonly StyleBuilder FlexShrink(float value)
        {
            Style.FlexShrink = value;
            return this;
        }

        public readonly StyleBuilder FlexGrow(float value)
        {
            Style.FlexGrow = value;
            return this;
        }

        public readonly StyleBuilder FlexBasis(float value)
        {
            Style.FlexBasis = value;
            return this;
        }

        public readonly StyleBuilder TextAlign(UiAlignment value)
        {
            Style.TextAlign = value;
            return this;
        }

        public readonly StyleBuilder TextAlignCenter()
        {
            Style.TextAlign = UiAlignment.Center;
            return this;
        }

        public readonly StyleBuilder TextAlignEnd()
        {
            Style.TextAlign = UiAlignment.End;
            return this;
        }

        public readonly StyleBuilder AlignSelf(UiAlignment value)
        {
            Style.AlignSelf = value;
            return this;
        }

        public readonly StyleBuilder Height(float value, Unit unit = Unit.Dp)
        {
            Style.Height = UnitValue.Get(value, unit);
            return this;
        }

        public readonly StyleBuilder Width(float value, Unit unit = Unit.Dp)
        {
            Style.Width = UnitValue.Get(value, unit);
            return this;
        }

        public readonly StyleBuilder FontSize(float value, Unit unit = Unit.Dp)
        {
            Style.FontSize = UnitValue.Get(value, unit);
            return this;
        }


        public UiStyle Style;
    }

    public static class UiBuilderExtensions
    {
        public static void BuildStyle(this UiElement element, Action<StyleBuilder> build)
        {
            build(new StyleBuilder(element.Style));
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


        public static IUiBuilder<UiTextBlock> Text(this IUiBuilder<UiTextBlock> builder, string value)
        {
            builder.Element.Text = value;
            return builder;
        }

        public static IUiBuilder<T> Content<T>(this IUiBuilder<T> builder, object? value) where T : UiContentView
        {
            builder.Element.Content = value;
            return builder;
        }

        public static IUiBuilder<T> Style<T>(this IUiBuilder<T> builder, Action<StyleBuilder>? build) where T : UiElement
        {
            if (build != null)
            {
                var sb = new StyleBuilder(builder.Element.Style);
                build(sb);
            }
            return builder;
        }

        public static IUiBuilder<T> AddButton<T>(this IUiBuilder<T> builder, Action<IUiBuilder<UiButton>>? build = null) where T : UiContainer
        {
            return builder.Child(build);
        }

        public static IUiBuilder<T> AddText<T>(this IUiBuilder<T> builder, Action<IUiBuilder<UiTextBlock>>? build = null) where T : UiContainer
        {
            return builder.Child(build);
        }

        public static IUiBuilder<T> AddText<T>(this IUiBuilder<T> builder, string text, Action<StyleBuilder>? style = null) where T : UiContainer
        {
            return builder.Child<T, UiTextBlock>(b => b.Text(text).Style(style));
        }

        public static IUiBuilder<T> AddCheckBox<T>(this IUiBuilder<T> builder, Action<IUiBuilder<UiCheckBox>>? build = null) where T : UiContainer
        {
            return builder.Child(build);
        }

        public static IUiBuilder<T> AddCheckBox<T>(this IUiBuilder<T> builder, object? content) where T : UiContainer
        {
            return builder.Child<T, UiCheckBox>(b => b.Element.Content = content);
        }

        public static IUiBuilder<T> AddIcon<T>(this IUiBuilder<T> builder, Action<IUiBuilder<UiIcon>>? build = null) where T : UiContainer
        {
            return builder.Child(build);
        }

        public static IUiBuilder<T> AddIcon<T>(this IUiBuilder<T> builder, IconName icon, float size = 24) where T : UiContainer
        {
            return builder.Child<T, UiIcon>(b =>
            {
                b.Element.Icon = icon;
                b.Element.Style.FontSize = UnitValue.Get(size);
            });
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

        public static IUiBuilder<UiContainer> BeginColumn<T>(this IUiBuilder<T> builder, Action<StyleBuilder>? style = null) where T : UiContainer
        {
            return builder
                   .BeginChild<T, UiContainer>()
                   .Style(style)
                   .AsColumn();
        }

        public static IUiBuilder<UiContainer> BeginRow<T>(this IUiBuilder<T> builder, Action<StyleBuilder>? style = null) where T : UiContainer
        {
            return builder
                   .BeginChild<T, UiContainer>()
                   .Style(style)
                   .AsRow();
        }
    }
}
