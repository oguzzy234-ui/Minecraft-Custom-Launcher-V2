using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

class Game
{
    public string Name { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string Path { get; set; } = "";
    public string? Args { get; set; }
    public string? Description { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public string? Background { get; set; }
    public string? Version { get; set; }
    public string Key { get; set; } = "";
    public string? BaseIcon { get; set; }
}

class GameSetting { public string? Background { get; set; } public string? Color { get; set; } public string? PlayImage { get; set; } public string? Name { get; set; } public string? Icon { get; set; } public string? Description { get; set; } public string? Version { get; set; } public string? Path { get; set; } public long Seconds { get; set; } public DateTime? LastPlayed { get; set; } }
class AppSettings { public Dictionary<string, GameSetting> Games { get; set; } = new(); public string? AllPlayImage { get; set; } public List<Game> UserGames { get; set; } = new(); public bool MinimizeOnPlay { get; set; } public List<string> Order { get; set; } = new(); }

static class Program
{
    [STAThread]
    static void Main() => new Application().Run(new MainWindow());
}

class MainWindow : Window
{
    static SolidColorBrush B(string hex) => (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
    static Color C(string? hex, string fallback)
    {
        try { return (Color)ColorConverter.ConvertFromString(hex ?? fallback)!; }
        catch { return (Color)ColorConverter.ConvertFromString(fallback)!; }
    }
    static Color Shade(Color c, double f) =>
        Color.FromRgb((byte)Math.Min(255, c.R * f), (byte)Math.Min(255, c.G * f), (byte)Math.Min(255, c.B * f));

    readonly List<Game> games = new();
    readonly List<Border> items = new();
    readonly string settingsPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Baslatici", "settings.json");
    AppSettings settings = new();
    Game? current;

    Grid gamePage = new(), settingsPage = new();
    Border art = new(), playBtn = new(), verIconHolder = new(), settingsItem = new();
    TextBlock gameTitle = new(), ver1 = new(), ver2 = new(), bgLabel = new();
    StackPanel verPanel = new(), sideList = new();
    WrapPanel chips = new();
    TextBox renameBox = new(), addNameBox = new();
    TextBlock iconLabel = new();
    Border delBtn = new();
    TextBox descBox = new(), verBox = new();
    TextBlock pathLabel = new(), infoDesc = new(), infoStats = new();
    CheckBox minimizeBox = new();
    TextBox hexBox = new();
    Border playBody = new(), playTop = new(), playBottom = new();
    SolidColorBrush shadowBrush = new(Colors.Black);
    Image playImg = new();
    Border playGlow = new();
    Canvas playLabel = new();
    TextBlock playImgLabel = new();

    public MainWindow()
    {
        Title = "Oyun Başlatıcı";
        Width = 1100; Height = 680; MinWidth = 800; MinHeight = 500;
        Background = B("#262626");
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FontFamily = new FontFamily("Segoe UI");

        LoadGames();
        LoadSettings();
        ApplyOverrides();

        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(222) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Yan menü
        var side = new Grid { Background = B("#1F1F1F") };
        side.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        side.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        side.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var profile = MakeProfile();
        var listScroll = new ScrollViewer { Content = sideList, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        RebuildSidebar();
        settingsItem = MakeSettingsItem();
        Grid.SetRow(profile, 0); Grid.SetRow(listScroll, 1); Grid.SetRow(settingsItem, 2);
        side.Children.Add(profile); side.Children.Add(listScroll); side.Children.Add(settingsItem);
        root.Children.Add(side);

        // Sağ taraf: oyun sayfası ve ayarlar sayfası üst üste
        var host = new Grid();
        BuildGamePage(); BuildSettingsPage();
        host.Children.Add(gamePage); host.Children.Add(settingsPage);
        Grid.SetColumn(host, 1);
        root.Children.Add(host);

        PreviewKeyDown += (_, e) => { if (e.Key == Key.Enter && gamePage.Visibility == Visibility.Visible && current != null) Play(current); };
        Content = root;
        if (games.Count > 0) Select(games[0]);
    }

    // ---------- Veri ----------
    void LoadGames()
    {
        try
        {
            var file = FindFile("games.json") ?? "games.json";
            var list = JsonSerializer.Deserialize<List<Game>>(File.ReadAllText(file), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (list != null) games.AddRange(list);
        }
        catch (Exception ex) { MessageBox.Show("games.json okunamadı:\n" + ex.Message, "Hata"); }
    }

    void LoadSettings()
    {
        try { if (File.Exists(settingsPath)) settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(settingsPath)) ?? new(); }
        catch { settings = new(); }
    }

    void ApplyOverrides()
    {
        foreach (var g in games)
        {
            if (string.IsNullOrEmpty(g.Key)) g.Key = g.Name;
            g.BaseIcon ??= g.Icon;
            if (settings.Games.TryGetValue(g.Key, out var st))
            {
                if (!string.IsNullOrEmpty(st.Name)) g.Name = st.Name;
                if (!string.IsNullOrEmpty(st.Icon)) g.Icon = st.Icon;
                if (st.Description != null) g.Description = st.Description;
                if (st.Version != null) g.Version = st.Version;
                if (!string.IsNullOrEmpty(st.Path)) g.Path = st.Path;
            }
        }
        foreach (var u in settings.UserGames)
        {
            if (string.IsNullOrEmpty(u.Key)) u.Key = Guid.NewGuid().ToString();
            games.Add(u);
        }
        if (settings.Order.Count > 0)
        {
            var ord = settings.Order;
            var sorted = games.OrderBy(x => { var i = ord.IndexOf(x.Key); return i < 0 ? int.MaxValue : i; }).ToList();
            games.Clear(); games.AddRange(sorted);
        }
    }

    void RebuildSidebar()
    {
        sideList.Children.Clear(); items.Clear();
        foreach (var g in games) { var it = MakeItem(g); items.Add(it); sideList.Children.Add(it); }
    }

    void RefreshAll()
    {
        RebuildSidebar();
        bool onSettings = settingsPage.Visibility == Visibility.Visible;
        Highlight(onSettings ? settingsItem : items.FirstOrDefault(i => i.Tag == current));
        RefreshSettings(); RefreshGame();
    }

    void AddGame()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Programlar|*.exe|Tüm dosyalar|*.*" };
        if (dlg.ShowDialog() != true) return;
        var name = addNameBox.Text.Trim();
        if (name.Length == 0) name = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
        var g = new Game { Key = Guid.NewGuid().ToString(), Name = name, Path = dlg.FileName, Color = "#3C8527", Description = "" };
        settings.UserGames.Add(g); games.Add(g);
        addNameBox.Text = ""; current = g;
        SaveSettings(); RefreshAll();
    }

    void RenameGame()
    {
        if (current == null) return;
        var n = renameBox.Text.Trim();
        if (n.Length == 0) return;
        current.Name = n; Setting(current).Name = n;
        SaveSettings(); RefreshAll();
    }

    void PickIcon()
    {
        if (current == null) return;
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Resimler|*.png;*.jpg;*.jpeg;*.bmp;*.ico|Tüm dosyalar|*.*" };
        if (dlg.ShowDialog() != true) return;
        current.Icon = dlg.FileName; Setting(current).Icon = dlg.FileName;
        SaveSettings(); RefreshAll();
    }

    void ResetIcon()
    {
        if (current == null) return;
        current.Icon = current.BaseIcon; Setting(current).Icon = null;
        SaveSettings(); RefreshAll();
    }

    void DeleteGame()
    {
        if (current == null) return;
        var u = settings.UserGames.FirstOrDefault(x => x.Key == current.Key);
        if (u == null) return;
        if (MessageBox.Show("\"" + current.Name + "\" silinsin mi?", "Oyunu Sil", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        settings.UserGames.Remove(u); games.Remove(u); settings.Games.Remove(u.Key);
        current = games.FirstOrDefault();
        SaveSettings(); RefreshAll();
    }

    static DropShadowEffect Shadow() => new DropShadowEffect { Color = Colors.Black, BlurRadius = 8, ShadowDepth = 1, Opacity = 0.9 };

    string Stats(Game g)
    {
        var st = Setting(g);
        var parts = new List<string>();
        if (st.Seconds >= 60)
        {
            var t = TimeSpan.FromSeconds(st.Seconds);
            parts.Add("Oynama süresi: " + (t.TotalHours >= 1 ? (int)t.TotalHours + " sa " : "") + t.Minutes + " dk");
        }
        if (st.LastPlayed != null) parts.Add("Son oynama: " + st.LastPlayed.Value.ToString("dd.MM.yyyy HH:mm"));
        return string.Join("  ·  ", parts);
    }

    void SaveDescription()
    {
        if (current == null) return;
        current.Description = descBox.Text; Setting(current).Description = descBox.Text;
        SaveSettings(); RefreshAll();
    }

    void SaveVersion()
    {
        if (current == null) return;
        current.Version = verBox.Text.Trim(); Setting(current).Version = verBox.Text.Trim();
        SaveSettings(); RefreshAll();
    }

    void ChangeExe()
    {
        if (current == null) return;
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Programlar|*.exe|Tüm dosyalar|*.*" };
        if (dlg.ShowDialog() != true) return;
        current.Path = dlg.FileName; Setting(current).Path = dlg.FileName;
        SaveSettings(); RefreshAll();
    }

    void MoveGame(int d)
    {
        if (current == null) return;
        int i = games.IndexOf(current), j = i + d;
        if (i < 0 || j < 0 || j >= games.Count) return;
        (games[i], games[j]) = (games[j], games[i]);
        settings.Order = games.Select(x => x.Key).ToList();
        SaveSettings(); RefreshAll();
    }

    void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(settingsPath)!);
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) { MessageBox.Show("Ayarlar kaydedilemedi:\n" + ex.Message, "Hata"); }
    }

    GameSetting Setting(Game g)
    {
        if (!settings.Games.TryGetValue(g.Key, out var s)) { s = new GameSetting(); settings.Games[g.Key] = s; }
        return s;
    }

    static string? FindFile(string? p)
    {
        if (string.IsNullOrWhiteSpace(p)) return null;
        if (System.IO.Path.IsPathRooted(p)) return File.Exists(p) ? p : null;
        var a = System.IO.Path.Combine(Directory.GetCurrentDirectory(), p);
        if (File.Exists(a)) return a;
        var b = System.IO.Path.Combine(AppContext.BaseDirectory, p);
        return File.Exists(b) ? b : null;
    }

    static BitmapImage? LoadImage(string? p)
    {
        var f = FindFile(p);
        if (f == null) return null;
        try
        {
            var bi = new BitmapImage();
            bi.BeginInit(); bi.UriSource = new Uri(f); bi.CacheOption = BitmapCacheOption.OnLoad; bi.EndInit(); bi.Freeze();
            return bi;
        }
        catch { return null; }
    }

    Color GameColor(Game g) => C(Setting(g).Color ?? g.Color, "#3C8527");

    // ---------- Yan menü ----------
    UIElement MakeProfile()
    {
        var name = Environment.UserName;
        var avatar = new Grid { Width = 40, Height = 40, Margin = new Thickness(0, 0, 12, 0) };
        avatar.Children.Add(new Ellipse { Fill = B("#3A3A3A") });
        avatar.Children.Add(new TextBlock
        {
            Text = name.Length > 0 ? name.Substring(0, 1).ToUpper() : "?", Foreground = Brushes.White,
            FontSize = 18, FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
        });
        var labels = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        labels.Children.Add(new TextBlock { Text = name, Foreground = Brushes.White, FontSize = 16, TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 140 });
        labels.Children.Add(new TextBlock { Text = "Profil", Foreground = B("#888888"), FontSize = 12 });
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(16, 14, 16, 18) };
        row.Children.Add(avatar); row.Children.Add(labels);
        return row;
    }

    Border MakeIcon(Game g, double size)
    {
        var holder = new Border { Width = size, Height = size, Background = new SolidColorBrush(C(g.Color, "#3C8527")) };
        var img = LoadImage(g.Icon);
        if (img != null)
        {
            var im = new Image { Source = img, Stretch = Stretch.UniformToFill };
            RenderOptions.SetBitmapScalingMode(im, BitmapScalingMode.NearestNeighbor);
            holder.Child = im; holder.Background = Brushes.Transparent;
        }
        else
            holder.Child = new TextBlock
            {
                Text = g.Name.Length > 0 ? g.Name.Substring(0, 1).ToUpper() : "?", Foreground = Brushes.White,
                FontSize = size * 0.5, FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
            };
        return holder;
    }

    Border MakeItem(Game g)
    {
        var icon = MakeIcon(g, 40); icon.Margin = new Thickness(0, 0, 12, 0);
        var labels = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        labels.Children.Add(new TextBlock { Text = g.Subtitle, Foreground = Brushes.White, FontSize = 11, FontWeight = FontWeights.SemiBold });
        labels.Children.Add(new TextBlock { Text = g.Name, Foreground = Brushes.White, FontSize = 17, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 140 });
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(icon); row.Children.Add(labels);
        var b = new Border { Child = row, Padding = new Thickness(13, 10, 13, 10), Cursor = Cursors.Hand,
            BorderThickness = new Thickness(3, 0, 0, 0), BorderBrush = Brushes.Transparent, Tag = g };
        b.MouseLeftButtonUp += (_, _) => Select(g);
        Hover(b);
        return b;
    }

    Border MakeSettingsItem()
    {
        var icon = new TextBlock { Text = "\uE713", FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 26,
            Foreground = Brushes.White, Margin = new Thickness(7, 0, 19, 0), VerticalAlignment = VerticalAlignment.Center };
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(icon);
        row.Children.Add(new TextBlock { Text = "Ayarlar", Foreground = Brushes.White, FontSize = 16, VerticalAlignment = VerticalAlignment.Center });
        var b = new Border { Child = row, Padding = new Thickness(13, 14, 13, 14), Cursor = Cursors.Hand,
            BorderThickness = new Thickness(3, 0, 0, 0), BorderBrush = Brushes.Transparent, Tag = "settings" };
        b.MouseLeftButtonUp += (_, _) => ShowSettings();
        Hover(b);
        return b;
    }

    void Hover(Border b)
    {
        b.MouseEnter += (_, _) => { if (b.BorderBrush == Brushes.Transparent) b.Background = B("#292929"); };
        b.MouseLeave += (_, _) => { if (b.BorderBrush == Brushes.Transparent) b.Background = Brushes.Transparent; };
    }

    void Highlight(Border? active)
    {
        foreach (var b in items.Append(settingsItem))
        {
            bool on = b == active;
            b.Background = on ? B("#2E2E2E") : Brushes.Transparent;
            b.BorderBrush = on ? Brushes.White : Brushes.Transparent;
        }
    }

    // ---------- Sayfa parçaları ----------
    StackPanel Header(string title, string tab, out TextBlock titleBlock)
    {
        titleBlock = new TextBlock { Text = title, Foreground = Brushes.White, FontSize = 14, FontWeight = FontWeights.Bold };
        var tabBlock = new TextBlock { Text = tab, Foreground = Brushes.White, FontSize = 18, FontWeight = FontWeights.Bold };
        var p = new StackPanel { Margin = new Thickness(24, 16, 24, 0), Background = B("#262626") };
        p.Children.Add(titleBlock);
        var t = new StackPanel { Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
        t.Children.Add(tabBlock);
        t.Children.Add(new Border { Height = 3, Background = B("#3C8527"), Margin = new Thickness(0, 4, 0, 0) });
        p.Children.Add(t);
        return p;
    }

    Border MakeButton(string text, string hex, Action click)
    {
        var col = C(hex, "#3C8527");
        var b = new Border
        {
            Child = new TextBlock { Text = text, Foreground = Brushes.White, FontSize = 14, FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
            Background = new SolidColorBrush(col), BorderBrush = new SolidColorBrush(Shade(col, 0.6)),
            BorderThickness = new Thickness(0, 0, 0, 3), Padding = new Thickness(18, 9, 18, 8),
            Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 10, 0)
        };
        b.MouseEnter += (_, _) => b.Background = new SolidColorBrush(Shade(col, 1.15));
        b.MouseLeave += (_, _) => b.Background = new SolidColorBrush(col);
        b.MouseLeftButtonUp += (_, _) => click();
        return b;
    }

    static TextBox MakeTextBox(double w) => new TextBox { Width = w, Padding = new Thickness(8, 7, 8, 7), Margin = new Thickness(0, 0, 10, 0), FontSize = 14,
        Background = B("#333333"), Foreground = Brushes.White, BorderBrush = B("#555555"), CaretBrush = Brushes.White };

    static TextBlock Label(string t) => new TextBlock { Text = t, Foreground = Brushes.White, FontSize = 12, FontWeight = FontWeights.Bold };

    // ---------- Oyun sayfası ----------
    void BuildGamePage()
    {
        gamePage.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        gamePage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        gamePage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(76) });

        var header = Header("", "Oyna", out gameTitle);
        Grid.SetRow(header, 0); gamePage.Children.Add(header);

        infoDesc = new TextBlock { Foreground = Brushes.White, FontSize = 16, TextWrapping = TextWrapping.Wrap, MaxWidth = 640, Effect = Shadow() };
        infoStats = new TextBlock { Foreground = B("#DDDDDD"), FontSize = 13, Margin = new Thickness(0, 6, 0, 0), Effect = Shadow() };
        var info = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(24, 0, 24, 18) };
        info.Children.Add(infoDesc); info.Children.Add(infoStats);
        art.Child = info;
        Grid.SetRow(art, 1); gamePage.Children.Add(art);

        var bar = new Grid { Background = B("#1F1F1F") };
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        verPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        var vt = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        ver1 = new TextBlock { Text = "Son Sürüm", Foreground = Brushes.White, FontSize = 14, FontWeight = FontWeights.SemiBold };
        ver2 = new TextBlock { Foreground = B("#BBBBBB"), FontSize = 12 };
        vt.Children.Add(ver1); vt.Children.Add(ver2);
        verIconHolder = new Border { Margin = new Thickness(0, 0, 12, 0) };
        verPanel.Children.Add(verIconHolder); verPanel.Children.Add(vt);
        Grid.SetColumn(verPanel, 0); bar.Children.Add(verPanel);

        playBtn = new Border { Width = 310, Height = 64, Padding = new Thickness(2), Cursor = Cursors.Hand };
        var layers = new Grid { UseLayoutRounding = true, SnapsToDevicePixels = true };
        playBody = new Border();
        playTop = new Border { Height = 4, VerticalAlignment = VerticalAlignment.Top };
        playBottom = new Border { Height = 6, VerticalAlignment = VerticalAlignment.Bottom };
        playLabel = PixelText("OYNA", 4, Brushes.White, shadowBrush, 3);
        playLabel.HorizontalAlignment = HorizontalAlignment.Center;
        playLabel.VerticalAlignment = VerticalAlignment.Center;
        playLabel.Margin = new Thickness(0, 0, 0, 2);
        playImg = new Image { Stretch = Stretch.Fill, Visibility = Visibility.Collapsed };
        RenderOptions.SetBitmapScalingMode(playImg, BitmapScalingMode.NearestNeighbor);
        playGlow = new Border { Background = Brushes.White, Opacity = 0, IsHitTestVisible = false };
        layers.Children.Add(playBody); layers.Children.Add(playTop);
        layers.Children.Add(playBottom); layers.Children.Add(playLabel);
        layers.Children.Add(playImg); layers.Children.Add(playGlow);
        playBtn.Child = layers;
        // Fareyle üstüne gelince hafif aydınlanma ve yumuşak zoom
        var scale = new ScaleTransform(1, 1);
        playBtn.RenderTransform = scale;
        playBtn.RenderTransformOrigin = new Point(0.5, 0.5);
        void Zoom(double to)
        {
            var a = new DoubleAnimation(to, TimeSpan.FromMilliseconds(120)) { EasingFunction = new QuadraticEase() };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, a);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, a);
        }
        playBtn.MouseEnter += (_, _) => { if (current != null) ApplyPlayColor(GameColor(current), 1.12); playGlow.Opacity = playImg.Visibility == Visibility.Visible ? 0.15 : 0; Zoom(1.05); };
        playBtn.MouseLeave += (_, _) => { if (current != null) ApplyPlayColor(GameColor(current), 1.0); playGlow.Opacity = 0; Zoom(1.0); };
        playBtn.MouseLeftButtonDown += (_, _) => Zoom(0.97);
        playBtn.MouseLeftButtonUp += (_, _) => Zoom(1.05);
        playBtn.MouseLeftButtonUp += (_, _) => { if (current != null) Play(current); };
        Grid.SetColumn(playBtn, 1); bar.Children.Add(playBtn);

        Grid.SetRow(bar, 2); gamePage.Children.Add(bar);
    }

    void RefreshGame()
    {
        if (current == null) return;
        var g = current; var col = GameColor(g);
        gameTitle.Text = g.Name.ToUpperInvariant();
        var bg = LoadImage(Setting(g).Background ?? g.Background);
        art.Background = bg != null
            ? new ImageBrush(bg) { Stretch = Stretch.UniformToFill }
            : new LinearGradientBrush(Shade(col, 0.45), Color.FromRgb(0x26, 0x26, 0x26), 90);
        ApplyPlayImage(g);
        ApplyPlayColor(col, 1.0);
        bool hasVer = !string.IsNullOrWhiteSpace(g.Version);
        verPanel.Visibility = hasVer ? Visibility.Visible : Visibility.Hidden;
        ver2.Text = g.Version ?? "";
        verIconHolder.Child = MakeIcon(g, 36);
        infoDesc.Text = g.Description ?? "";
        infoDesc.Visibility = string.IsNullOrWhiteSpace(g.Description) ? Visibility.Collapsed : Visibility.Visible;
        infoStats.Text = Stats(g);
    }

    void ApplyPlayImage(Game g)
    {
        // Ayarlardan seçilen resim, yoksa exe klasöründeki oyna.png
        var img = LoadImage(Setting(g).PlayImage ?? settings.AllPlayImage ?? "oyna.png");
        bool has = img != null;
        playImg.Source = img;
        playImg.Visibility = has ? Visibility.Visible : Visibility.Collapsed;
        var v = has ? Visibility.Collapsed : Visibility.Visible;
        playBody.Visibility = v; playTop.Visibility = v; playBottom.Visibility = v; playLabel.Visibility = v;
        playBtn.Padding = has ? new Thickness(0) : new Thickness(2);
        playBtn.Height = has ? 310.0 * img!.PixelHeight / img.PixelWidth : 64;
    }

    void ApplyPlayColor(Color col, double f)
    {
        if (playImg.Visibility == Visibility.Visible) { playBtn.Background = Brushes.Transparent; return; }
        var c = Shade(col, f);
        playBtn.Background = new SolidColorBrush(Shade(c, 0.25));
        playBody.Background = new LinearGradientBrush(Shade(c, 1.12), Shade(c, 0.9), 90);
        playTop.Background = new SolidColorBrush(Shade(c, 1.35));
        playBottom.Background = new SolidColorBrush(Shade(c, 0.6));
        shadowBrush.Color = Shade(c, 0.35);
    }

    // Kendi çizdiğim 5x7 piksel harfler (Mojang yazı tipi değil)
    static Canvas PixelText(string text, double u, Brush fill, Brush shadow, double off)
    {
        var glyphs = new Dictionary<char, string[]>
        {
            ['O'] = new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['Y'] = new[] { "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.." },
            ['N'] = new[] { "#...#", "##..#", "#.#.#", "#.#.#", "#..##", "#...#", "#...#" },
            ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
        };
        var cv = new Canvas { Width = (text.Length * 6 - 1) * u + off, Height = 7 * u + off, SnapsToDevicePixels = true };
        foreach (var pass in new[] { (shadow, off), (fill, 0.0) })
            for (int i = 0; i < text.Length; i++)
            {
                if (!glyphs.TryGetValue(text[i], out var rows)) continue;
                for (int r = 0; r < 7; r++)
                    for (int c = 0; c < 5; c++)
                        if (rows[r][c] == '#')
                        {
                            var rect = new System.Windows.Shapes.Rectangle { Width = u, Height = u, Fill = pass.Item1 };
                            Canvas.SetLeft(rect, (i * 6 + c) * u + pass.Item2);
                            Canvas.SetTop(rect, r * u + pass.Item2);
                            cv.Children.Add(rect);
                        }
            }
        return cv;
    }

    void Select(Game g)
    {
        current = g;
        Highlight(items.First(i => i.Tag == g));
        settingsPage.Visibility = Visibility.Collapsed;
        gamePage.Visibility = Visibility.Visible;
        RefreshGame();
    }

    // ---------- Ayarlar sayfası ----------
    void BuildSettingsPage()
    {
        settingsPage.Background = B("#262626");
        settingsPage.Visibility = Visibility.Collapsed;
        settingsPage.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        settingsPage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var header = Header("AYARLAR", "Genel", out _);
        Grid.SetRow(header, 0); settingsPage.Children.Add(header);

        var body = new StackPanel { Margin = new Thickness(24, 28, 24, 24) };
        body.Children.Add(Label("OYUN"));
        chips = new WrapPanel { Margin = new Thickness(0, 10, 0, 18) };
        body.Children.Add(chips);

        body.Children.Add(Label("OYUN ADI"));
        renameBox = MakeTextBox(260);
        var renRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 28) };
        renRow.Children.Add(renameBox);
        renRow.Children.Add(MakeButton("Adı Değiştir", "#3C8527", RenameGame));
        body.Children.Add(renRow);

        body.Children.Add(Label("OYUN SİMGESİ"));
        iconLabel = new TextBlock { Foreground = B("#AAAAAA"), FontSize = 13, Margin = new Thickness(0, 8, 0, 10), TextTrimming = TextTrimming.CharacterEllipsis };
        body.Children.Add(iconLabel);
        var icRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 28) };
        icRow.Children.Add(MakeButton("Simge Seç", "#3C8527", PickIcon));
        icRow.Children.Add(MakeButton("Sıfırla", "#555555", ResetIcon));
        body.Children.Add(icRow);

        body.Children.Add(Label("AÇIKLAMA"));
        descBox = MakeTextBox(520);
        descBox.AcceptsReturn = true; descBox.TextWrapping = TextWrapping.Wrap; descBox.Height = 70; descBox.Margin = new Thickness(0, 10, 0, 10);
        descBox.HorizontalAlignment = HorizontalAlignment.Left; descBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        body.Children.Add(descBox);
        var dRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 28) };
        dRow.Children.Add(MakeButton("Açıklamayı Kaydet", "#3C8527", SaveDescription));
        body.Children.Add(dRow);

        body.Children.Add(Label("SÜRÜM YAZISI"));
        verBox = MakeTextBox(160);
        var vRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 28) };
        vRow.Children.Add(verBox);
        vRow.Children.Add(MakeButton("Kaydet", "#3C8527", SaveVersion));
        body.Children.Add(vRow);

        body.Children.Add(Label("OYUN DOSYASI (EXE)"));
        pathLabel = new TextBlock { Foreground = B("#AAAAAA"), FontSize = 13, Margin = new Thickness(0, 8, 0, 10), TextTrimming = TextTrimming.CharacterEllipsis };
        body.Children.Add(pathLabel);
        var pRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 28) };
        pRow.Children.Add(MakeButton("Exe Değiştir", "#3C8527", ChangeExe));
        body.Children.Add(pRow);

        body.Children.Add(Label("YAN MENÜDEKİ SIRA"));
        var oRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 28) };
        oRow.Children.Add(MakeButton("Yukarı Taşı", "#2F6DB5", () => MoveGame(-1)));
        oRow.Children.Add(MakeButton("Aşağı Taşı", "#2F6DB5", () => MoveGame(1)));
        body.Children.Add(oRow);

        body.Children.Add(Label("OYUN EKLE"));
        body.Children.Add(new TextBlock { Text = "Oyun adını yaz (boş bırakırsan dosya adı kullanılır), sonra exe dosyasını seç.",
            Foreground = B("#AAAAAA"), FontSize = 13, Margin = new Thickness(0, 8, 0, 10) });
        addNameBox = MakeTextBox(260);
        delBtn = MakeButton("Bu Oyunu Sil", "#C0392B", DeleteGame);
        var addRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 30) };
        addRow.Children.Add(addNameBox);
        addRow.Children.Add(MakeButton("Exe Seç ve Ekle", "#3C8527", AddGame));
        addRow.Children.Add(delBtn);
        body.Children.Add(addRow);

        body.Children.Add(Label("ARKA PLAN FOTOĞRAFI"));
        bgLabel = new TextBlock { Foreground = B("#AAAAAA"), FontSize = 13, Margin = new Thickness(0, 8, 0, 10), TextTrimming = TextTrimming.CharacterEllipsis };
        body.Children.Add(bgLabel);
        var bgRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 30) };
        bgRow.Children.Add(MakeButton("Fotoğraf Seç", "#3C8527", PickPhoto));
        bgRow.Children.Add(MakeButton("Sıfırla", "#555555", () => { if (current == null) return; Setting(current).Background = null; SaveSettings(); RefreshSettings(); RefreshGame(); }));
        body.Children.Add(bgRow);

        body.Children.Add(Label("OYNA BUTONU RENGİ"));
        var sw = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 14) };
        foreach (var hex in new[] { "#3C8527", "#2F6DB5", "#C0392B", "#8E44AD", "#E67E22", "#16A085" })
        {
            var s = new Border { Width = 36, Height = 36, Background = B(hex), Margin = new Thickness(0, 0, 10, 0), Cursor = Cursors.Hand };
            s.MouseLeftButtonUp += (_, _) => SetColor(hex);
            sw.Children.Add(s);
        }
        body.Children.Add(sw);
        var hexRow = new StackPanel { Orientation = Orientation.Horizontal };
        hexBox = new TextBox { Width = 110, Padding = new Thickness(8, 7, 8, 7), Margin = new Thickness(0, 0, 10, 0), FontSize = 14,
            Background = B("#333333"), Foreground = Brushes.White, BorderBrush = B("#555555"), CaretBrush = Brushes.White };
        hexRow.Children.Add(hexBox);
        hexRow.Children.Add(MakeButton("Uygula", "#3C8527", () => SetColor(hexBox.Text.Trim())));
        hexRow.Children.Add(MakeButton("Sıfırla", "#555555", () => SetColor(null)));
        body.Children.Add(hexRow);

        body.Children.Add(new Border { Height = 28 });
        body.Children.Add(Label("OYNA BUTONU RESMİ (PNG)"));
        playImgLabel = new TextBlock { Foreground = B("#AAAAAA"), FontSize = 13, Margin = new Thickness(0, 8, 0, 10), TextTrimming = TextTrimming.CharacterEllipsis };
        body.Children.Add(playImgLabel);
        var imgRow = new StackPanel { Orientation = Orientation.Horizontal };
        imgRow.Children.Add(MakeButton("Tüm Oyunlar İçin Seç", "#3C8527", () => PickPlayImage(true)));
        imgRow.Children.Add(MakeButton("Sadece Bu Oyun İçin Seç", "#2F6DB5", () => PickPlayImage(false)));
        imgRow.Children.Add(MakeButton("Bu Oyunu Sıfırla", "#555555", () => { if (current == null) return; Setting(current).PlayImage = null; SaveSettings(); RefreshSettings(); RefreshGame(); }));
        imgRow.Children.Add(MakeButton("Hepsini Sıfırla", "#555555", () => { settings.AllPlayImage = null; foreach (var gs in settings.Games.Values) gs.PlayImage = null; SaveSettings(); RefreshSettings(); RefreshGame(); }));
        body.Children.Add(imgRow);

        body.Children.Add(new Border { Height = 28 });
        body.Children.Add(Label("BAŞLATICI"));
        minimizeBox = new CheckBox { Content = "Oyun açılınca başlatıcıyı küçült, oyun kapanınca geri getir", Foreground = Brushes.White,
            FontSize = 14, Margin = new Thickness(0, 10, 0, 0), IsChecked = settings.MinimizeOnPlay };
        minimizeBox.Click += (_, _) => { settings.MinimizeOnPlay = minimizeBox.IsChecked == true; SaveSettings(); };
        body.Children.Add(minimizeBox);

        Grid.SetRow(body, 1); settingsPage.Children.Add(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        Grid.SetRow(settingsPage.Children[1], 1);
    }

    void ShowSettings()
    {
        Highlight(settingsItem);
        gamePage.Visibility = Visibility.Collapsed;
        settingsPage.Visibility = Visibility.Visible;
        RefreshSettings();
    }

    void RefreshSettings()
    {
        chips.Children.Clear();
        foreach (var g in games)
        {
            bool on = g == current;
            var c = new Border { Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 0, 10, 10), Cursor = Cursors.Hand,
                Background = B(on ? "#3C8527" : "#333333"),
                Child = new TextBlock { Text = g.Name, Foreground = Brushes.White, FontSize = 14 } };
            c.MouseLeftButtonUp += (_, _) => { current = g; RefreshSettings(); RefreshGame(); };
            chips.Children.Add(c);
        }
        if (current == null) return;
        renameBox.Text = current.Name;
        descBox.Text = current.Description ?? "";
        verBox.Text = current.Version ?? "";
        pathLabel.Text = current.Path;
        iconLabel.Text = string.IsNullOrEmpty(current.Icon) ? "Simge yok (oyunun ilk harfi gösteriliyor)" : current.Icon;
        delBtn.Visibility = settings.UserGames.Any(x => x.Key == current.Key) ? Visibility.Visible : Visibility.Collapsed;
        var bg = Setting(current).Background ?? current.Background;
        bgLabel.Text = string.IsNullOrEmpty(bg) ? "Seçili fotoğraf yok" : bg;
        var pi = Setting(current).PlayImage;
        playImgLabel.Text = !string.IsNullOrEmpty(pi) ? "Bu oyun: " + pi
            : !string.IsNullOrEmpty(settings.AllPlayImage) ? "Tüm oyunlar: " + settings.AllPlayImage
            : "Resim seçilmedi (klasörde oyna.png varsa o kullanılır)";
        var col = GameColor(current);
        hexBox.Text = $"#{col.R:X2}{col.G:X2}{col.B:X2}";
    }

    void PickPhoto()
    {
        if (current == null) return;
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Resimler|*.png;*.jpg;*.jpeg;*.bmp|Tüm dosyalar|*.*" };
        if (dlg.ShowDialog() == true)
        {
            Setting(current).Background = dlg.FileName;
            SaveSettings(); RefreshSettings(); RefreshGame();
        }
    }

    void PickPlayImage(bool all)
    {
        if (current == null) return;
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "PNG ve resimler|*.png;*.jpg;*.jpeg;*.bmp|Tüm dosyalar|*.*" };
        if (dlg.ShowDialog() == true)
        {
            if (all) { settings.AllPlayImage = dlg.FileName; foreach (var gs in settings.Games.Values) gs.PlayImage = null; }
            else Setting(current).PlayImage = dlg.FileName;
            SaveSettings(); RefreshSettings(); RefreshGame();
        }
    }

    void SetColor(string? hex)
    {
        if (current == null) return;
        if (hex != null)
        {
            try { ColorConverter.ConvertFromString(hex); }
            catch { MessageBox.Show("Geçersiz renk. Örnek: #3C8527", "Hata"); return; }
        }
        Setting(current).Color = hex;
        SaveSettings(); RefreshSettings(); RefreshGame();
    }

    // ---------- Oyunu başlat ----------
    void Play(Game g)
    {
        try
        {
            var psi = new ProcessStartInfo(g.Path) { UseShellExecute = true, Arguments = g.Args ?? "" };
            var dir = System.IO.Path.GetDirectoryName(g.Path);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir)) psi.WorkingDirectory = dir;
            var start = DateTime.Now;
            var proc = Process.Start(psi);
            if (settings.MinimizeOnPlay) WindowState = WindowState.Minimized;
            if (proc != null)
            {
                proc.EnableRaisingEvents = true;
                proc.Exited += (_, _) => Dispatcher.Invoke(() =>
                {
                    var st = Setting(g);
                    var secs = (long)(DateTime.Now - start).TotalSeconds;
                    if (secs >= 5) { st.Seconds += secs; st.LastPlayed = start; SaveSettings(); }
                    if (settings.MinimizeOnPlay) { WindowState = WindowState.Normal; Activate(); }
                    RefreshGame();
                });
            }
        }
        catch (Exception ex) { MessageBox.Show("Oyun başlatılamadı:\n" + g.Path + "\n\n" + ex.Message, "Hata"); }
    }
}