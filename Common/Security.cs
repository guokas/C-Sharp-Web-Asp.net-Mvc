using System;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace Common
{
    public class Security
    {

        /// <summary>
        /// Encrypt
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Encrypt(string str)
        {
            string ret = "";
            try
            {
                string k = "50-41-53-53-34-5F-50-41-53-53-50-41-32-5F-45-4E-43-5F-4B-45-59-00-00-00-00-00-00-00-00-00-00-00";
                AES aes = new AES();
                aes.SetParams(k);
                ret = aes.Encrypt(str);
            }
            catch { }
            return ret;
        }


        /// <summary>
        /// decrypt
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Decrypt(string str)
        {
            string ret = "";
            try
            {
                string k = "50-41-53-53-34-5F-50-41-53-53-50-41-32-5F-45-4E-43-5F-4B-45-59-00-00-00-00-00-00-00-00-00-00-00";
                AES aes = new AES();
                aes.SetParams(k);
                ret = aes.Decrypt(str);
            }
            catch { }
            return ret;
        }

    }
    public interface ISymmetricEncryption
    {
        byte[] Encrypt(byte[] byteArr);
        string Encrypt(string toEncryptStr);
        byte[] Decrypt(byte[] byteArr);
        string Decrypt(string toDecryptStr);
    }
    public class SymmetricEncryptor : ISymmetricEncryption
    {
        const CipherMode defaultMode = CipherMode.ECB;
        const PaddingMode defaultPadding = PaddingMode.PKCS7;

        protected System.Security.Cryptography.SymmetricAlgorithm provider = null;
        public System.Security.Cryptography.SymmetricAlgorithm Provider { get { return provider; } }
        protected event Action paraChanged = delegate { };
        protected SymmetricEncryptor()
        {
            initProvider();
            initOther();
        }
        protected virtual void initProvider()
        {

        }
        private void initOther()
        {
            provider.Mode = defaultMode;
            provider.Padding = defaultPadding;
            provider.GenerateIV();
            provider.GenerateKey();
        }
        private byte[] hexStrToByteArr(string str)
        {
            return (from s in str.Split('-') select Convert.ToByte(s, 16)).ToArray();
        }
        private void setKey(string key)
        {
            try
            {
                provider.Key = hexStrToByteArr(key);
                paraChanged();
            }
            catch { throw new Exception("It is not a legal hex string.  An exception occurs at when decoding 'key' value."); }
        }
        private void setIv(string iv)
        {
            try
            {
                provider.IV = hexStrToByteArr(iv);
            }
            catch { throw new Exception("It is not a legal hex string.  An exception occurs at when decoding 'key' value."); }
        }
        /// <summary>
        /// set parameter
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="paddingMode"></param>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        public void SetParams(CipherMode mode, PaddingMode paddingMode, string key, string iv)
        {
            provider.Mode = mode;
            provider.Padding = paddingMode;
            setKey(key);
            if (!string.IsNullOrEmpty(iv))
            {
                setIv(iv);
            }
            paraChanged();
        }
        public void SetParams(string key, string iv)
        {
            setKey(key);
            if (!string.IsNullOrEmpty(iv))
            {
                setIv(iv);
            }
            paraChanged();
        }
        public void SetParams(string key)
        {
            setKey(key);
            paraChanged();
        }
        public void GetKeyAndIv(out string Key, out string IV)
        {
            Key = BitConverter.ToString(provider.Key);
            IV = BitConverter.ToString(provider.IV);
        }
        public virtual byte[] Encrypt(byte[] byteArr)
        {
            ICryptoTransform cTransform = provider.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(byteArr, 0, byteArr.Length);
            return resultArray;
        }
        public virtual string Encrypt(string toEncryptStr)
        {
            byte[] toEncryptArray = System.Text.Encoding.UTF8.GetBytes(toEncryptStr);
            byte[] resultArray = Encrypt(toEncryptArray);
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        public virtual byte[] Decrypt(byte[] byteArr)
        {
            ICryptoTransform cTransform = provider.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(byteArr, 0, byteArr.Length);
            return resultArray;
        }

        public virtual string Decrypt(string toDecryptStr)
        {
            byte[] byteArr = Convert.FromBase64String(toDecryptStr);
            byte[] resultArray = Decrypt(byteArr);
            return System.Text.Encoding.UTF8.GetString(resultArray);
        }


        public string getLegalSizeMsg()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("MaxSize\tMinSize\tSkipSize");
            foreach (var item in provider.LegalKeySizes)
            {
                sb.AppendLine(string.Format("{0}\t{1}\t{2}", item.MaxSize, item.MinSize, item.SkipSize));
            }
            return sb.ToString();
        }
    }
    public class AES : SymmetricEncryptor
    {
        protected override void initProvider()
        {
            base.initProvider();
            provider = System.Security.Cryptography.Aes.Create();
        }
        public AES()
            : base()
        {
            provider.Padding = System.Security.Cryptography.PaddingMode.Zeros;
        }

        public override string Decrypt(string toDecryptStr)
        {
            byte[] byteArr = Convert.FromBase64String(toDecryptStr);
            byte[] resultArray = Decrypt(byteArr);
            string str = System.Text.Encoding.GetEncoding("GB2312").GetString(resultArray);
            str = str.TrimEnd('\0');
            return str;
        }
        public override string Encrypt(string toEncryptStr)
        {
            byte[] toEncryptArray = System.Text.Encoding.GetEncoding("GB2312").GetBytes(toEncryptStr);
            byte[] resultArray = Encrypt(toEncryptArray);
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }
    }

}
