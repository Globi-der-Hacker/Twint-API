using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WinTwint
{
    public class Storage
    {
        // Using Lazy<T> for lazy initialization without explicit locks
        private static readonly Lazy<Storage> instance = new Lazy<Storage>(() => new Storage());

        private string baseDirectory;
        private string phoneNumber;
        private string deviceId;
        private string privateCustomerUuid;
        private string deviceUuid;
        private string devicePassword;
        private string financialAccountId;
        private string pin;

        // Private constructor to prevent instantiation from outside
        private Storage()
        {
            baseDirectory = Windows.Storage.ApplicationData.Current.LocalFolder.Path;

            phoneNumber = Read("PhoneNumber");
            deviceId = Read("DeviceId");
            privateCustomerUuid = Read("PrivateCustomerUuid");
            deviceUuid = Read("DeviceUuid");
            devicePassword = Read("DevicePassword");
            financialAccountId = Read("FinancialAccountId");
            pin = Read("Pin");

            if (string.IsNullOrEmpty(deviceId))
            {
                DeviceId = GetRandomHexString(8);
            }
        }

        private string Read(string name, string def = "")
        {
            string file = Path.Combine(baseDirectory, name);
            if (File.Exists(file))
            {
                return File.ReadAllText(Path.Combine(baseDirectory, name));
            }

            return def;
        }

        private void Write(string name, string value)
        {
            string file = Path.Combine(baseDirectory, name);
            File.WriteAllText(file, value);
        }

        private string GetRandomHexString(int length)
        {
            byte[] randomBytes = new byte[length];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return BitConverter.ToString(randomBytes).Replace("-", string.Empty).ToLower();
        }

        public static Storage Instance => instance.Value;

        public string PhoneNumber
        {
            get => phoneNumber;
            set
            {
                phoneNumber = value;
                Write("PhoneNumber", value);
            }
        }

        public string DeviceId
        {
            get => deviceId;
            set
            {
                deviceId = value;
                Write("DeviceId", value);
            }
        }

        public string PrivateCustomerUuid
        {
            get => privateCustomerUuid;
            set
            {
                privateCustomerUuid = value;
                Write("PrivateCustomerUuid", value);
            }
        }

        public string DeviceUuid
        {
            get => deviceUuid;
            set
            {
                deviceUuid = value;
                Write("DeviceUuid", value);
            }
        }

        public string DevicePassword
        {
            get => devicePassword;
            set
            {
                devicePassword = value;
                Write("DevicePassword", value);
            }
        }

        public string FinancialAccountId
        {
            get => financialAccountId;
            set
            {
                financialAccountId = value;
                Write("FinancialAccountId", value);
            }
        }

        public string Pin
        {
            get => pin;
            set
            {
                pin = value;
                Write("Pin", value);
            }
        }

        public string CaCertFile => Path.Combine(baseDirectory, "twint_ca.cer");

        public string SigningCertFile => Path.Combine(baseDirectory, "twint_signing.cer");

        public string SigningCertWithPrivateKeyFile => Path.Combine(baseDirectory, "twint_signing.pfx");
    }
}
