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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TwintApi;
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
    public sealed partial class SendPage : Page
    {
        public SendPage()
        {
            this.InitializeComponent();

            btnSend.IsEnabled = false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            txtNumber.Focus(FocusState.Programmatic);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Navigate(typeof(MainPage));
        }

        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(txtAmount.Text, out var amount))
            {
                try
                {
                    var nameResponse = await App.Current.TwintApi.GetName();

                    await App.Current.TwintApi.Send(
                        amount,
                        txtMessage.Text,
                        "",
                        "",
                        txtNumber.Text.Replace(" ", ""),
                        nameResponse.FirstName,
                        nameResponse.LastName,
                        Storage.Instance.SigningCertWithPrivateKeyFile
                    );

                    App.Current.Navigate(typeof(MainPage));
                }
                catch (TwintException ex)
                {
                    await HandleError(ex);
                }
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
            var text = ((TextBox)sender).Text.Replace(" ", "");

            // +41791112233 => +41 79 111 22 33
            if (text.Length == 12 && text.StartsWith("+41"))
            {
                ((TextBox)sender).Text = $"{text.Substring(0, 3)} {text.Substring(3, 2)} {text.Substring(5, 3)} {text.Substring(8, 2)} {text.Substring(10, 2)}";
                txtAmount.Focus(FocusState.Programmatic);
            }
            // 0791112233 => +41 79 111 22 33
            else if (text.Length == 10 && text[0] == '0')
            {
                ((TextBox)sender).Text = $"+41 {text.Substring(1, 2)} {text.Substring(3, 3)} {text.Substring(6, 2)} {text.Substring(8, 2)}";
                txtAmount.Focus(FocusState.Programmatic);
            }

            Validate();
        }

        private void txtAmount_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            // Get the current text of the TextBox
            var text = ((TextBox)sender).Text;

            // Use a regular expression to only allow numeric values
            var regex = new Regex("^\\d*\\.?\\d{0,2}$");

            // If the text does not match the regular expression, undo the change
            if (!regex.IsMatch(text))
            {
                ((TextBox)sender).Undo();
            }
            else
            {
                ((TextBox)sender).ClearUndoRedoHistory();
            }

            Validate();
        }

        private void Validate()
        {
            btnSend.IsEnabled = txtNumber.Text.Length >= 10 && decimal.TryParse(txtAmount.Text, out var amount) && amount > 0;
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
    }
}
