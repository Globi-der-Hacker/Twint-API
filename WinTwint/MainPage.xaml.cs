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
using System.Threading.Tasks;
using TwintApi;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinTwint
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(App.Current.TwintApi.BearerToken))
            {
                try
                {
                    await App.Current.TwintApi.GetToken(Storage.Instance.DeviceId, Storage.Instance.DevicePassword, Storage.Instance.DeviceUuid, Storage.Instance.Pin, Storage.Instance.PrivateCustomerUuid);
                }
                catch (TwintException ex) 
                {
                    await HandleError(ex);
                }                
            }

            await LoadData();
        }

        private async Task LoadData()
        {
            if (!string.IsNullOrEmpty(App.Current.TwintApi.BearerToken))
            {
                try
                {
                    var response = await App.Current.TwintApi.GetBalance();
                    lblBalance.Text = $"{response.Balance.Amount:0.00} {response.Balance.Currency}";

                    var nameResponse = await App.Current.TwintApi.GetName();
                    lblTitle.Text = $"Twint: {nameResponse.FirstName} {nameResponse.LastName}";

                    var orders = await App.Current.TwintApi.GetOrders();
                    foreach (var order in orders)
                    {
                        lvTransactions.Items.Add(new Transaction
                        {
                            Name = order.TransactionSide == "CREDIT" ? order.P2pSenderMobileNr : order.P2pRecipientMobileNr,
                            Direction = (order.TransactionSide == "CREDIT" ? "erhalten" : "gesendet") + " " + order.CtlCreTs,
                            Amount = (order.TransactionSide == "CREDIT" ? "" : "-") + order.PaidAmount.ToString("0.00"),
                            Message = order.P2pInitiateMessage
                        });
                    }
                }
                catch (TwintException ex)
                {
                    await HandleError(ex);
                }
            }
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            lvTransactions.Items.Clear();
            await LoadData();
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Navigate(typeof(SendPage));
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
