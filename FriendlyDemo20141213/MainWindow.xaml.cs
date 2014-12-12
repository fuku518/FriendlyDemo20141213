using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Codeer.Friendly.Dynamic;
using Codeer.Friendly.Windows;
using System.Threading;
using Codeer.Friendly.Windows.Grasp;

namespace FriendlyDemo20141213
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private WindowsAppFriend _app;
        private Process _process; 

        private void OnButton1(object sender, RoutedEventArgs e)
        {
            _process = Process.Start("WpfApplication1.exe");
        }

        private void OnButton2(object sender, RoutedEventArgs e)
        {
            if (_process == null)
            {
                //実行されてないから起動する
                _process = Process.Start("WpfApplication1.exe");
                while (_process.MainWindowHandle == IntPtr.Zero)
                {
                    _process.Refresh();
                    Thread.Sleep(10);
                }
            }
            else
            {
                if (_process.HasExited) {
                    //終わってるので再実行
                    _process = Process.Start("WpfApplication1.exe");
                    while (_process.MainWindowHandle == IntPtr.Zero)
                    {
                        _process.Refresh();
                        Thread.Sleep(10);
                    }
                }
            }
            _app = new WindowsAppFriend(_process);
            WindowsAppExpander.LoadAssembly(_app, GetType().Assembly);

            var window = _app.Type<Application>().Current.MainWindow;
            GetStaticMethodInvoker().StaticProcedure(window);
            _app.Dispose();
        }

        private dynamic GetStaticMethodInvoker()
        {
            return _app.Type(GetType());
        }

        private static void StaticProcedure(Window window)
        {
            const int horzCount = 8 + 2;
            const int vertCount = 6 + 2;
            const int bombCount = 15;
            const int cellSize = 50;

            window.Width = cellSize * (horzCount - 2) + 20;
            window.Height = cellSize * (vertCount - 2) + 60;

            var wrap = new WrapPanel
            {
                Margin = new Thickness(1, 1, 0, 0)
            };
            var rootGrid = window.Content as Grid;
            rootGrid.Children.Clear();
            rootGrid.Children.Add(wrap);

            var opened = new bool[horzCount * vertCount];
            var bombed = new int[horzCount * vertCount];
            var counts = new int[horzCount * vertCount];

            var rnd = new Random(Environment.TickCount);
            //●～*配置
            for (var i = 0; i < bombCount; i++)
            {
                var h = rnd.Next(horzCount - 2) + 1;
                var v = rnd.Next(vertCount - 2) + 1;
                bombed[h + v * horzCount] = 1;
            }
            //●～*数カウント
            for (var y = 1; y < vertCount - 1; y++)
            {
                for (var x = 1; x < horzCount - 1; x++)
                {
                    var index = x + y * horzCount;
                    counts[index] = bombed[index - horzCount - 1] + bombed[index - horzCount - 0] + bombed[index - horzCount + 1] +
                                    bombed[index - 0 - 1] + bombed[index - 0 - 0] + bombed[index - 0 + 1] +
                                    bombed[index + horzCount - 1] + bombed[index + horzCount - 0] + bombed[index + horzCount + 1];
                }
            }
            //フィールド生成＆マウスイベント処理
            for (var y = 1; y < vertCount - 1; y++)
            {
                for (var x = 1; x < horzCount - 1; x++)
                {
                    var index = x + y*horzCount;
                    var cell = new Border
                    {
                        BorderBrush = new SolidColorBrush(Colors.Black),
                        BorderThickness = new Thickness(1),
                        Width = cellSize,
                        Height = cellSize,
                        Margin = new Thickness(-1, -1, 0, 0),
                        Tag = index
                    };
                    var grid = new Grid();
                    var text = new TextBlock
                    {
                        FontSize = 32,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    text.Text = (bombed[index] == 1) ? "*" : counts[index].ToString();
                    text.Foreground = (bombed[index] == 1)
                        ? new SolidColorBrush(Colors.Crimson)
                        : new SolidColorBrush(Colors.DodgerBlue);
                    grid.Children.Add(text);
                    var cover = new Grid
                    {
                        Background = new SolidColorBrush(Colors.LightSkyBlue),
                        Opacity = 1
                    };
                    grid.Children.Add(cover);
                    cell.Child = grid;
                    wrap.Children.Add(cell);
                    cell.MouseDown += (o, args) =>
                    {
                        //マウスイベント
                        var cell2 = o as Border;
                        var index2 = cell2.Tag as int?;
                        if (index2 == null) return;

                        var grid2 = cell2.Child as Grid;
                        var cover2 = grid2.Children[1] as Grid;

                        if (opened[(int) index2]) return;

                        if (args.ChangedButton == MouseButton.Right)
                        {
                            //右クリック
                            cover.Background = new SolidColorBrush(Colors.DarkOrange);
                        }
                        else
                        {
                            //左クリック
                            cover2.Opacity = 0;
                            opened[(int) index2] = true;
                            if (bombed[(int) index2] == 1)
                            {
                                //全部開ける
                                for (var y3 = 0; y3 < vertCount - 2; y3++)
                                {
                                    for (var x3 = 0; x3 < horzCount - 2; x3++)
                                    {
                                        var index3 = x3 + y3*(horzCount - 2);
                                        var index4 = x3 + 1 + (y3 + 1)*horzCount;
                                        var cell3 = wrap.Children[index3] as Border;
                                        var grid3 = cell3.Child as Grid;
                                        var cover3 = grid3.Children[1] as Grid;
                                        if (bombed[index4] == 1)
                                        {
                                            cover3.Opacity = 0;
                                        }
                                    }
                                }
                                MessageBox.Show(window, "残念！踏んじゃった。");
                            }
                            else
                            {
                                var restCount = 0;
                                for (var y4 = 1; y4 < vertCount - 1; y4++)
                                {
                                    for (var x4 = 1; x4 < horzCount - 1; x4++)
                                    {
                                        var index4 = x4 + y4*horzCount;
                                        if (opened[index4] == false && bombed[index4] == 0) restCount++;
                                    }
                                }
                                if (restCount == 0)
                                {
                                    for (var y3 = 0; y3 < vertCount - 2; y3++)
                                    {
                                        for (var x3 = 0; x3 < horzCount - 2; x3++)
                                        {
                                            var index3 = x3 + y3*(horzCount - 2);
                                            var cell3 = wrap.Children[index3] as Border;
                                            var grid3 = cell3.Child as Grid;
                                            var cover3 = grid3.Children[1] as Grid;
                                            cover3.Opacity = 0;
                                        }
                                    }
                                    MessageBox.Show(window, "やったね、大成功！");
                                }
                            }
                        }
                    };
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }


        
    }
}
