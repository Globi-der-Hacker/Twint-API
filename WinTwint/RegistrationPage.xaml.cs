using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using TwintApi;
using TwintApi.Crypto;

namespace WinTwint
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RegistrationPage : Page
    {
        enum OnBoardingStep
        {
            VerifyPhone = 1,
            EnterCode = 2,
            EnterPin = 3,
            Completed = 4,
        }

        private OnBoardingStep _step;

        public RegistrationPage()
        {
            this.InitializeComponent();

            txtNumber.Text = Storage.Instance.PhoneNumber;
            txtCode.Text = string.Empty;
            txtPin.Text = Storage.Instance.Pin;

            if (string.IsNullOrEmpty(Storage.Instance.DevicePassword))
            {
                _step = OnBoardingStep.VerifyPhone;
            }
            else
            {
                _step = OnBoardingStep.Completed;
            }

            SetControls();
        }

        private void SetControls()
        {
            switch (_step)
            {
                case OnBoardingStep.VerifyPhone:
                    txtNumber.IsEnabled = true;
                    txtCode.IsEnabled = false;
                    txtPin.IsEnabled = false;
                    btnDelete.IsEnabled = false;
                    btnVerifyCode.IsEnabled = false;                    
                    txtNumber_TextChanged(txtNumber, null);
                    btnVerifyPin.IsEnabled = false;
                    btnCancel.IsEnabled = false;
                    break;
                case OnBoardingStep.EnterCode:
                    txtNumber.IsEnabled = false;
                    txtCode.IsEnabled = true;
                    txtPin.IsEnabled = false;
                    btnDelete.IsEnabled = false;
                    btnVerify.IsEnabled = false;
                    btnVerifyCode.IsEnabled = txtCode.Text.Length == 5;
                    btnVerifyPin.IsEnabled = false;
                    txtCode.Focus(FocusState.Programmatic);
                    btnCancel.IsEnabled = true;
                    break;
                case OnBoardingStep.EnterPin:
                    txtNumber.IsEnabled = false;
                    txtCode.Text = string.Empty;
                    txtCode.IsEnabled = false;
                    txtPin.IsEnabled = true;
                    btnDelete.IsEnabled = false;
                    btnVerify.IsEnabled = false;
                    btnVerifyCode.IsEnabled = false;
                    btnVerifyPin.IsEnabled = txtPin.Text.Length == 6;
                    txtPin.Focus(FocusState.Programmatic);
                    btnCancel.IsEnabled = true;
                    break;
                case OnBoardingStep.Completed:
                    txtNumber.IsEnabled = false;
                    txtCode.IsEnabled = false;
                    txtPin.IsEnabled = false;
                    btnDelete.IsEnabled = true;
                    btnVerify.IsEnabled = false;
                    btnVerifyCode.IsEnabled = false;
                    btnVerifyPin.IsEnabled = false;
                    btnCancel.IsEnabled = false;
                    break;
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog();

            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
            dialog.XamlRoot = this.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = "Registrierung löschen";
            dialog.PrimaryButtonText = "Ja";
            dialog.SecondaryButtonText = "Nein";
            dialog.DefaultButton = ContentDialogButton.Secondary;
            dialog.Content = "Die aktuelle Registrierung löschen und mit neuer Telefonnummer verknüpfen?";

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                Storage.Instance.DevicePassword = "";
                Storage.Instance.DeviceUuid = "";
                Storage.Instance.Pin = "";
                Storage.Instance.PrivateCustomerUuid = "";
                Storage.Instance.FinancialAccountId = "";

                SetControls();
                App.Current.SetTwintEnabled(false);
            }
        }

        private void txtNumber_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            // Get the current text of the TextBox
            var text = ((TextBox)sender).Text;

            // Use a regular expression to only allow numeric values
            var regex = new Regex("^\\+?[0-9 ]*$");

            // If the text does not match the regular expression, undo the change
            if (!regex.IsMatch(text))
            {
                ((TextBox)sender).Undo();
            }
            else
            {
                ((TextBox)sender).ClearUndoRedoHistory();
            }
        }

        private void txtNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_step == OnBoardingStep.VerifyPhone)
            {
                var text = ((TextBox)sender).Text.Replace(" ", "");

                // +41791112233 => +41 79 111 22 33
                if (text.Length == 12 && text.StartsWith("+41"))
                {
                    ((TextBox)sender).Text = $"{text.Substring(0, 3)} {text.Substring(3, 2)} {text.Substring(5, 3)} {text.Substring(8, 2)} {text.Substring(10, 2)}";
                    btnVerify.IsEnabled = true;
                }
                // 0791112233 => +41 79 111 22 33
                else if (text.Length == 10 && text[0] == '0')
                {
                    ((TextBox)sender).Text = $"+41 {text.Substring(1, 2)} {text.Substring(3, 3)} {text.Substring(6, 2)} {text.Substring(8, 2)}";
                    btnVerify.IsEnabled = true;
                }
                else if (text.Length >= 12)
                {
                    btnVerify.IsEnabled = true;
                }
                else
                {
                    btnVerify.IsEnabled = false;
                }
            }
        }

        private async void btnVerify_Click(object sender, RoutedEventArgs e)
        {
            string number = txtNumber.Text.Replace(" ", "");
            try
            {
                await App.Current.TwintApi.StartVerifyPhoneNumber(number);

                Storage.Instance.PhoneNumber = number;
                _step = OnBoardingStep.EnterCode;
                SetControls();
            }
            catch (TwintException tex)
            {
                await HandleError(tex);
            }
        }

        private void txtCode_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            // Get the current text of the TextBox
            var text = ((TextBox)sender).Text;

            // Use a regular expression to only allow numeric values
            var regex = new Regex("^[0-9]*$");

            // If the text does not match the regular expression, undo the change
            if (!regex.IsMatch(text))
            {
                ((TextBox)sender).Undo();
            }
            else
            {
                ((TextBox)sender).ClearUndoRedoHistory();
            }

            btnVerifyCode.IsEnabled = text.Length == 5;
        }

        private async void btnVerifyCode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await App.Current.TwintApi.CompleteVerifyPhoneNumber(txtCode.Text);

                _step = OnBoardingStep.EnterPin;
                SetControls();
            }
            catch (TwintException tex)
            {
                await HandleError(tex);
            }
        }

        private void txtPin_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            // Get the current text of the TextBox
            var text = ((TextBox)sender).Text;

            // Use a regular expression to only allow numeric values
            var regex = new Regex("^[0-9]*$");

            // If the text does not match the regular expression, undo the change
            if (!regex.IsMatch(text))
            {
                ((TextBox)sender).Undo();
            }
            else
            {
                ((TextBox)sender).ClearUndoRedoHistory();
            }

            btnVerifyPin.IsEnabled = text.Length == 6;
        }

        private async void btnVerifyPin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var response = await App.Current.TwintApi.Reboard(
                    Storage.Instance.DeviceId,
                    Storage.Instance.PhoneNumber,
                    txtPin.Text,
                    File.ReadAllText(Storage.Instance.CaCertFile),
                    CertificateHelper.GetPublicKeyFingerprint(Storage.Instance.CaCertFile),
                    File.ReadAllText(Storage.Instance.SigningCertFile),
                    CertificateHelper.GetPublicKeyFingerprint(Storage.Instance.SigningCertFile)
                );

                Storage.Instance.Pin = txtPin.Text;
                Storage.Instance.DeviceUuid = response.DeviceUuid;
                Storage.Instance.DevicePassword = response.DevicePassword;
                Storage.Instance.FinancialAccountId = response.FinancialAccountId;
                Storage.Instance.PrivateCustomerUuid = response.PrivateCustomerUuid;
                
                _step = OnBoardingStep.Completed;
                SetControls();
                App.Current.SetTwintEnabled(true);
            }
            catch (TwintException tex)
            {
                await HandleError(tex);
            }
        }

        private async Task HandleError(TwintException tex)
        {
            ContentDialog dialog = new ContentDialog();

            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
            dialog.XamlRoot = this.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = "Etwas ist schief gelaufen!";
            dialog.PrimaryButtonText = "Ok";
            dialog.DefaultButton = ContentDialogButton.Primary;
            string response = tex.Response;
            if (response.Length > 500)
            {
                response = response.Substring(0, 500) + "...";
            }    
            dialog.Content = $"Der Server meldet: {(int)tex.StatusCode} {tex.StatusDescription}\r\n\r\nDetails:\r\n{response}";

            await dialog.ShowAsync();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            _step = OnBoardingStep.VerifyPhone;
            SetControls();
        }
    }
}
