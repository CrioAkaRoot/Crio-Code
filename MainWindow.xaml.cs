using Microsoft.Win32;
using IOPath = System.IO.Path;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;
using System.Windows.Media;
using System.Threading.Tasks;
using NLua;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;

namespace CrioCode;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private string? currentFilePath;
    private ObservableCollection<TabItem> _tabs;
    private TabItem? _selectedTab;
    private bool _isLoading = false;

    public static readonly RoutedCommand RunCodeCommand = new RoutedCommand();

    public MainWindow()
    {
        InitializeComponent();
        
        try
        {
            
            using (var stream = new FileStream("Lua.xshd", FileMode.Open))
            using (var reader = new XmlTextReader(stream))
            {
                MainTextEditor.SyntaxHighlighting = 
                    HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }

            MainTextEditor.TextArea.TextView.ElementGenerators.Clear();
            MainTextEditor.Options.EnableVirtualSpace = false;
            MainTextEditor.Options.EnableRectangularSelection = false;
            MainTextEditor.Options.EnableTextDragDrop = false;
            MainTextEditor.Options.EnableImeSupport = false;
            
            MainTextEditor.FontSize = 14;
            MainTextEditor.FontFamily = new FontFamily("Consolas");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке файла подсветки синтаксиса: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        MainTextEditor.TextChanged += MainTextEditor_TextChanged!;
        StatusText.Text = "CrioCode - Новый файл";
        
        _tabs = new ObservableCollection<TabItem>();
        TabsPanel.ItemsSource = _tabs;
        TabsPanel.SelectionChanged += TabsPanel_SelectionChanged;
        
        CreateNewTab();
        
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, SaveFile_Click));
        CommandBindings.Add(new CommandBinding(RunCodeCommand, RunLuaCode_Click));
    }

    private void MainTextEditor_TextChanged(object sender, EventArgs e)
    {
        if (_selectedTab != null && !_isLoading)
        {
            bool hasChanged = _selectedTab.Content != MainTextEditor.Text;
            _selectedTab.Content = MainTextEditor.Text;
            _selectedTab.IsModified = hasChanged;
            CharCount.Text = $"Символов: {MainTextEditor.Text.Length}";
            
        }
    }

    private void CreateNewTab(string? filePath = null, string? content = null)
    {
        var title = filePath != null ? IOPath.GetFileName(filePath) : "Новый файл";
        var tab = new TabItem(title, content ?? string.Empty, filePath);
        _tabs.Add(tab);
        _selectedTab = tab;
        TabsPanel.SelectedItem = tab;
        
        _isLoading = true;
        MainTextEditor.Clear();
        MainTextEditor.Text = tab.Content;
        _isLoading = false;
        
        currentFilePath = tab.FilePath;
        StatusText.Text = tab.Title;
        CharCount.Text = $"Символов: {tab.Content.Length}";
        
        tab.IsModified = false;
    }

    private void NewFile_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTab?.IsModified == true)
        {
            var result = MessageBox.Show("Сохранить текущий файл?", "CrioCode - Новый файл", 
                MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SaveFile_Click(sender, e);
            }
            else if (result == MessageBoxResult.Cancel)
            {
                return;
            }
        }

        MainTextEditor.Clear();
        CreateNewTab();
    }

    private async void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Lua файлы (*.lua)|*.lua|Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
            Title = "CrioCode - Открыть файл"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {

                var tab = new TabItem(IOPath.GetFileName(openFileDialog.FileName), "Загрузка...", openFileDialog.FileName);
                _tabs.Add(tab);
                TabsPanel.SelectedItem = tab;
                _selectedTab = tab;

                string content = await Task.Run(() => File.ReadAllText(openFileDialog.FileName));

                _isLoading = true;
                tab.Content = content;
                MainTextEditor.Text = content;
                _isLoading = false;
                
                CharCount.Text = $"Символов: {content.Length}";
                StatusText.Text = $"CrioCode - Открыт файл: {tab.Title}";
                tab.IsModified = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии файла: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void SaveFile_Click(object sender, ExecutedRoutedEventArgs e)
    {
        await SaveCurrentFileAsync();
    }

    private async void SaveFile_Click(object sender, RoutedEventArgs e)
    {
        await SaveCurrentFileAsync();
    }

    private async Task SaveCurrentFileAsync()
    {
        if (_selectedTab == null) return;

        if (string.IsNullOrEmpty(_selectedTab.FilePath))
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Lua файлы (*.lua)|*.lua|Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                DefaultExt = ".lua",
                Title = "CrioCode - Сохранить файл"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                _selectedTab.FilePath = saveFileDialog.FileName;
                _selectedTab.Title = IOPath.GetFileName(saveFileDialog.FileName);
            }
            else
            {
                return;
            }
        }

        try
        {
            StatusText.Text = "Сохранение...";
            await Task.Run(() => File.WriteAllText(_selectedTab.FilePath, _selectedTab.Content));
            _selectedTab.IsModified = false;
            StatusText.Text = $"CrioCode - Файл сохранен: {_selectedTab.Title}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Ошибка при сохранении";
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Cut_Click(object sender, RoutedEventArgs e)
    {
        MainTextEditor.Cut();
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        MainTextEditor.Copy();
    }

    private void Paste_Click(object sender, RoutedEventArgs e)
    {
        MainTextEditor.Paste();
    }

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            this.DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Normal)
        {
            WindowState = WindowState.Maximized;
        }
        else
        {
            WindowState = WindowState.Normal;
        }
    }

    private void CloseTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is TabItem tab)
        {
            if (tab.IsModified)
            {
                var result = MessageBox.Show($"Сохранить изменения в файле {tab.Title}?",
                    "CrioCode - Закрытие вкладки",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _selectedTab = tab;
                    SaveFile_Click(sender, e);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            _tabs.Remove(tab);
            
            if (_tabs.Count == 0)
            {
                CreateNewTab();
            }
            else
            {
                var newSelectedTab = _tabs[_tabs.Count - 1];
                _selectedTab = newSelectedTab;
                MainTextEditor.Text = newSelectedTab.Content;
                currentFilePath = newSelectedTab.FilePath;
                StatusText.Text = newSelectedTab.Title;
            }
        }
    }

    private void TabsPanel_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TabsPanel.SelectedItem is TabItem tab)
        {
            _selectedTab = tab;
            _isLoading = true;
            MainTextEditor.Text = tab.Content;
            _isLoading = false;
            currentFilePath = tab.FilePath;
            StatusText.Text = tab.Title;
        }
    }

    private async void RunLuaCode_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(MainTextEditor.Text))
            return;

        try
        {
            StatusText.Text = "Выполнение...";
            
            var outputWindow = new Window
            {
                Title = "CrioCode - Результат выполнения",
                Width = 600,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent
            };

            var mainBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x37))
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var titleBar = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x26)),
                CornerRadius = new CornerRadius(10, 10, 0, 0)
            };

            var titleGrid = new Grid();
            
            var titleText = new TextBlock
            {
                Text = "Результат выполнения",
                Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 5, 0)
            };

            var closeButton = new Button
            {
                Width = 12,
                Height = 12,
                Margin = new Thickness(4, 0, 0, 0)
            };

            var buttonTemplate = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xFF, 0x60, 0x5C)));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));

            var pathFactory = new FrameworkElementFactory(typeof(System.Windows.Shapes.Path));
            pathFactory.Name = "PART_Path";
            pathFactory.SetValue(System.Windows.Shapes.Path.StrokeProperty, new SolidColorBrush(Color.FromArgb(0x66, 0x00, 0x00, 0x00)));
            pathFactory.SetValue(System.Windows.Shapes.Path.StrokeThicknessProperty, 1d);
            pathFactory.SetValue(System.Windows.Shapes.Path.MarginProperty, new Thickness(3));
            pathFactory.SetValue(System.Windows.Shapes.Path.VisibilityProperty, Visibility.Collapsed);
            pathFactory.SetValue(System.Windows.Shapes.Path.DataProperty, new GeometryGroup
            {
                Children = new GeometryCollection
                {
                    new LineGeometry(new Point(0, 0), new Point(6, 6)),
                    new LineGeometry(new Point(6, 0), new Point(0, 6))
                }
            });

            borderFactory.AppendChild(pathFactory);
            buttonTemplate.VisualTree = borderFactory;

            var trigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(System.Windows.Shapes.Path.VisibilityProperty, Visibility.Visible));
            buttonTemplate.Triggers.Add(trigger);

            closeButton.Template = buttonTemplate;
            closeButton.Click += (s, args) => outputWindow.Close();

            var minimizeButton = new Button
            {
                Width = 12,
                Height = 12,
                Margin = new Thickness(0, 0, 4, 0)
            };

            var minimizeTemplate = new ControlTemplate(typeof(Button));
            var minimizeBorderFactory = new FrameworkElementFactory(typeof(Border));
            minimizeBorderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xFE, 0xBC, 0x2E)));
            minimizeBorderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));

            var minimizePathFactory = new FrameworkElementFactory(typeof(System.Windows.Shapes.Path));
            minimizePathFactory.Name = "PART_Path";
            minimizePathFactory.SetValue(System.Windows.Shapes.Path.StrokeProperty, new SolidColorBrush(Color.FromArgb(0x66, 0x00, 0x00, 0x00)));
            minimizePathFactory.SetValue(System.Windows.Shapes.Path.StrokeThicknessProperty, 1d);
            minimizePathFactory.SetValue(System.Windows.Shapes.Path.MarginProperty, new Thickness(3));
            minimizePathFactory.SetValue(System.Windows.Shapes.Path.VisibilityProperty, Visibility.Collapsed);
            minimizePathFactory.SetValue(System.Windows.Shapes.Path.DataProperty, new LineGeometry(new Point(0, 3), new Point(6, 3)));

            minimizeBorderFactory.AppendChild(minimizePathFactory);
            minimizeTemplate.VisualTree = minimizeBorderFactory;

            var minimizeTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            minimizeTrigger.Setters.Add(new Setter(System.Windows.Shapes.Path.VisibilityProperty, Visibility.Visible));
            minimizeTemplate.Triggers.Add(minimizeTrigger);

            minimizeButton.Template = minimizeTemplate;
            minimizeButton.Click += (s, args) => outputWindow.WindowState = WindowState.Minimized;

            buttonPanel.Children.Add(minimizeButton);
            buttonPanel.Children.Add(closeButton);
            titleGrid.Children.Add(titleText);
            titleGrid.Children.Add(buttonPanel);
            titleBar.Child = titleGrid;

            var outputText = new TextBox
            {
                IsReadOnly = true,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                Padding = new Thickness(10),
                Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
                Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(10),
                TextWrapping = TextWrapping.NoWrap
            };

            var scrollBarStyle = new Style(typeof(ScrollBar));
            scrollBarStyle.Setters.Add(new Setter(ScrollBar.BackgroundProperty, Brushes.Transparent));
            scrollBarStyle.Setters.Add(new Setter(ScrollBar.BorderBrushProperty, Brushes.Transparent));
            scrollBarStyle.Setters.Add(new Setter(ScrollBar.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0x68, 0x68, 0x68))));
            scrollBarStyle.Setters.Add(new Setter(ScrollBar.WidthProperty, 8d));

            outputText.Resources.Add(typeof(ScrollBar), scrollBarStyle);

            Grid.SetRow(titleBar, 0);
            Grid.SetRow(outputText, 1);
            mainGrid.Children.Add(titleBar);
            mainGrid.Children.Add(outputText);
            mainBorder.Child = mainGrid;
            outputWindow.Content = mainBorder;

            titleBar.MouseLeftButtonDown += (s, args) =>
            {
                if (args.ChangedButton == MouseButton.Left)
                    outputWindow.DragMove();
            };

            string luaCode = MainTextEditor.Text;
            string result = string.Empty;

            await Task.Run(() =>
            {
                try
                {
                    using (var lua = new Lua())
                    {

                        lua.DoString(@"
                            OUTPUT_BUFFER = {}
                            local originalPrint = print
                            function print(...)
                                local args = {...}
                                local text = ''
                                for i,v in ipairs(args) do
                                    if type(v) == 'string' then
                                        local bytes = {v:byte(1, #v)}
                                        local utf8_chars = {}
                                        for _, b in ipairs(bytes) do
                                            if b < 128 then
                                                table.insert(utf8_chars, string.char(b))
                                            else
                                                local utf8_byte = b + 0xD0
                                                if b >= 0xC0 then
                                                    utf8_byte = b + 0x30
                                                elseif b >= 0x80 then
                                                    utf8_byte = b + 0x70
                                                end
                                                table.insert(utf8_chars, string.char(0xD0, utf8_byte))
                                            end
                                        end
                                        text = text .. table.concat(utf8_chars)
                                    else
                                        text = text .. tostring(v)
                                    end
                                    if i < #args then
                                        text = text .. '\t'
                                    end
                                end
                                table.insert(OUTPUT_BUFFER, text)
                            end
                        ");

                        lua.DoString(luaCode);

                        var buffer = lua["OUTPUT_BUFFER"] as NLua.LuaTable;
                        if (buffer != null)
                        {
                            var outputs = new List<string>();
                            foreach (var line in buffer.Values)
                            {
                                if (line != null)
                                {
                                    outputs.Add(line.ToString() ?? "");
                                }
                            }
                            result = string.Join(Environment.NewLine, outputs);
                        }
                    }
                }
                catch (Exception ex)
                {
                    result = $"Ошибка выполнения: {ex.Message}";
                }
            });

            outputText.Text = string.IsNullOrEmpty(result) ? 
                "Код выполнен успешно (нет вывода)" : result;
            outputWindow.Show();
            StatusText.Text = "Выполнено успешно";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при выполнении кода:\n{ex.Message}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Ошибка выполнения";
        }
    }
}