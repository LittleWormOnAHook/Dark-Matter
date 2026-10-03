using UnityEngine.UIElements;

namespace Project.EditorTools.Theme
{
    /// <summary>Small Genesis building blocks: header bar, footer, nav items, pills, toasts, buttons.</summary>
    public static class GWidgets
    {
        public static VisualElement HeaderBar(string title, string subtitle = null)
        {
            var bar = new VisualElement();
            bar.AddToClassList("g-header");
            var logo = new VisualElement();
            logo.AddToClassList("g-header__logo");
            bar.Add(logo);
            var t = GenesisTheme.HeaderLabel(title, 13f);
            t.AddToClassList("g-header__title");
            bar.Add(t);
            if (!string.IsNullOrEmpty(subtitle))
            {
                var s = new Label(subtitle.ToUpperInvariant());
                s.AddToClassList("g-header__sub");
                bar.Add(s);
            }
            return bar;
        }

        public static VisualElement Footer(out Label status, string statusText = "Ready")
        {
            var f = new VisualElement();
            f.AddToClassList("g-footer");
            var dot = new VisualElement();
            dot.AddToClassList("g-dot");
            f.Add(dot);
            status = new Label(statusText);
            f.Add(status);
            return f;
        }

        public static VisualElement NavItem(string text, bool on, System.Action onClick)
        {
            var item = new VisualElement();
            item.AddToClassList("g-nav__item");
            if (on) item.AddToClassList("g-nav__item--on");
            var pip = new VisualElement();
            pip.AddToClassList("g-nav__pip");
            item.Add(pip);
            item.Add(new Label(text));
            item.RegisterCallback<ClickEvent>(_ =>
            {
                item.parent?.Query(className: "g-nav__item").ForEach(x => x.RemoveFromClassList("g-nav__item--on"));
                item.AddToClassList("g-nav__item--on");
                onClick?.Invoke();
            });
            return item;
        }

        public enum PillKind { Ok, Warn, Bad }

        public static Label Pill(string text, PillKind kind)
        {
            var l = new Label(text.ToUpperInvariant());
            l.AddToClassList("g-pill");
            l.AddToClassList(kind == PillKind.Ok ? "g-pill--ok" : kind == PillKind.Warn ? "g-pill--warn" : "g-pill--bad");
            GenesisTheme.Header(l);
            return l;
        }

        public static Button ActionButton(string text, System.Action onClick, bool primary = false, bool danger = false)
        {
            var b = new Button(onClick) { text = text.ToUpperInvariant() };
            if (primary) b.AddToClassList("g-primary");
            if (danger) b.AddToClassList("g-danger");
            GenesisTheme.Header(b);
            return b;
        }

        /// <summary>Shows a toast in the bottom-right of <paramref name="root"/> for a few seconds.</summary>
        public static void Toast(VisualElement root, string message, long ms = 2600)
        {
            if (root == null) return;
            foreach (var old in root.Query(className: "g-toast").ToList()) old.RemoveFromHierarchy();
            var toast = new Label(message) { pickingMode = PickingMode.Ignore };
            toast.AddToClassList("g-toast");
            root.Add(toast);
            toast.schedule.Execute(() => toast.AddToClassList("g-toast--hide")).StartingIn(ms);
            toast.schedule.Execute(() => toast.RemoveFromHierarchy()).StartingIn(ms + 400);
        }
    }
}
