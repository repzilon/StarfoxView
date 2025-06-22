using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Starfox.Editor.Settings;
using WPF.UI.Extensions.Controls;

namespace StarFoxMapVisualizer.Dialogs
{
    /// <summary>
    /// Interaction logic for SettingsDialog.xaml
    /// </summary>
    public partial class SettingsDialog : Window
    {
        Dictionary<SFEditorSettings, PropertyViewer> propViewerMap = new Dictionary<SFEditorSettings, PropertyViewer>();

        public SettingsDialog()
        {
            InitializeComponent();
            Loaded += delegate
            {
                Load();
            };
        }

        private void Load()
        {
            PropertyViewer.RegisterRangedCustomConverter(typeof(RangedUserSetting));

            SettingsTabs.Items.Clear();
            propViewerMap.Clear();

            var settings = AppResources.ImportedProject.Settings;
            foreach ( SFEditorSettings item in settings.Values )
            {
                SFCodeProjectSettingsTypes type = item.SettingsType;
                PropertyViewer propsViewerControl = new PropertyViewer((GraphicsUserSettings)item);
                TabItem newItem = new TabItem()
                {
                    Header = Enum.GetName(typeof(SFCodeProjectSettingsTypes), type),
                    Content = new ScrollViewer()
                    {
                        Padding = new Thickness(10),
                        Content = propsViewerControl
                    }
                };
                propViewerMap.Add(item, propsViewerControl);
                SettingsTabs.Items.Add(newItem);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Apply();
            Close();
        }

        void Apply()
        {
            foreach (var settingTuple in propViewerMap)
            {
                //Apply changed values
                settingTuple.Value.ApplyValues();
                //Fire event saying they're changed
                settingTuple.Key.ApplyChanges();
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            //Apply value changes
            Apply();
            //Reload the display
            Load();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
