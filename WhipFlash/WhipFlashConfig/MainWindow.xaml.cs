using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using WhipFlashServer;

namespace WhipFlashConfig
{
    public class MainWindow : Window
    {
        private LightsConfiguration currentConfig;
        private LightsConfiguration backupConfig;


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

        public void button_click_quit(object sender, RoutedEventArgs e)
        {
            var qbutton = (Button)sender;
            qbutton.Content = "you've quit";
        }

        public void LoadConfig(string loadpath)
        {
            try
            {
                currentConfig = LightsConfiguration.LoadConfigFromFile(loadpath);
                backupConfig = currentConfig;
            }
            catch
            {
                //file not found etc
            }

        }

        public void NewConfig()
        {
            //todo: confirm
            currentConfig = new LightsConfiguration();
            backupConfig = currentConfig;
        }

        public void SaveConfig(string savepath)
        {
            LightsConfiguration.SaveConfigToFile(currentConfig, savepath);
        }

        public void click_NewFile(object sender, RoutedEventArgs rea)
        {
            NewConfig();
            
        }

        public void click_SaveFile()
        {

        }

        public void click_SaveFileAs()
        {

        }

        public void click_LoadFile()
        {
            //todo: trigger a file dialog
            string inpath = "";

            //
            LoadConfig(inpath);
        }
    }
}
