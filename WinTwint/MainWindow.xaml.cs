using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TwintApi.Crypto;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinTwint
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            
            if (!File.Exists(Storage.Instance.SigningCertFile))
            {
                CertificateHelper.CreateCertificates(Storage.Instance.CaCertFile, Storage.Instance.SigningCertFile, Storage.Instance.SigningCertWithPrivateKeyFile);
            }

            if (string.IsNullOrEmpty(Storage.Instance.DevicePassword))
            {
                nvMain.SelectedItem = nvMain.MenuItems[0];
                SetTwintEnabled(false);
            }
            else
            {
                nvMain.SelectedItem = nvMain.MenuItems[1];
            }
        }

        public Frame ContentFrame => contentFrame;

        public void SetTwintEnabled(bool enabled)
        {
            nviTwint.IsEnabled = enabled;
            nviInfo.IsEnabled = enabled;
        }

        private void nvMain_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var selectedItem = (Microsoft.UI.Xaml.Controls.NavigationViewItem)args.SelectedItem;
            if (selectedItem != null)
            {
                switch ((string)selectedItem.Tag)
                {
                    case "Registration":
                        contentFrame.Navigate(typeof(RegistrationPage));
                        break;
                    case "Twint":
                        contentFrame.Navigate(typeof(MainPage));
                        break;
                    case "Info":
                        contentFrame.Navigate(typeof(InfoPage));
                        break;
                }
            }
        }
    }
}
