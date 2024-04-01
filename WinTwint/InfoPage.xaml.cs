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
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class InfoPage : Page
    {
        public InfoPage()
        {
            this.InitializeComponent();

            lblNumber.Text = Storage.Instance.PhoneNumber;
            lblDeviceId.Text = Storage.Instance.DeviceId;
            lblDeviceUuid.Text = Storage.Instance.DeviceUuid;
            lblDevicePassword.Text = Storage.Instance.DevicePassword;
            lblPin.Text = Storage.Instance.Pin;

            lblCustomerUuid.Text = Storage.Instance.PrivateCustomerUuid;
            lblAccountId.Text = Storage.Instance.FinancialAccountId;
            lblCaCert.Text = CertificateHelper.GetPublicKeyFingerprint(Storage.Instance.CaCertFile);
            lblSigningCert.Text = CertificateHelper.GetPublicKeyFingerprint(Storage.Instance.SigningCertFile);
            lblToken.Text = App.Current.TwintApi.BearerTokenForDisplay;
        }
    }
}
