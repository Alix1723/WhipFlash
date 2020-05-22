using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace WhipFlashConfig
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void OnButtonClick(object sender, RoutedEventArgs e)
        {
            var qbutton = (Button)sender;
            qbutton.Content = "you've quit";
        }
    }
}
