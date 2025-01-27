using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Security.Cryptography;
using System.Text;
using Windows.Storage;
using PFU.DLCameraScan;
using PFU.DLCameraOcr;
using System.Timers;

namespace ScanAPP
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitCamera();

            //string plainText = "This is a secret message!";
            //string password = "your-strong-password";

            //// Generate random IV (Initialization Vector)
            //using (Aes aes = Aes.Create())
            //{
            //    aes.KeySize = 256; // Use AES-256
            //    aes.GenerateIV();
            //    byte[] iv = aes.IV;

            //    // Derive key from password using a key derivation function (PBKDF2)
            //    byte[] key = AesUtil.DeriveKeyFromPassword(password, aes.KeySize / 8);

            //    // Encrypt
            //    byte[] encrypted = AesUtil.Encrypt(plainText, key, iv);
            //    Console.WriteLine("Encrypted (Base64): " + Convert.ToBase64String(encrypted));

            //    // Decrypt
            //    string decryptedText = AesUtil.Decrypt(encrypted, key, iv);
            //    Console.WriteLine("Decrypted Text: " + decryptedText);
            //}
        }
        string cameraFolderPath;
        SettingsOperation operation;

        string personalIdentifyDocuments;
        string cameraMode;
        double cameraTimeout;

        private void InitCamera()
        {
            cameraFolderPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path + @"\camera";
            if (!Directory.Exists(cameraFolderPath))
            {
                Environment.Exit(0);
            }
            operation = new SettingsOperation(cameraFolderPath);
            personalIdentifyDocuments = operation.Get("PERSONAL_IDENTIFY_DOCUMENTS");
            cameraMode = operation.Get("CAMERA_MODE");
            string cameraShutdownSeconds = operation.Get("CAMERA_SHUTDOWN_SECONDS");
            if (cameraShutdownSeconds == "" || cameraShutdownSeconds == "0")
            {
                cameraTimeout = double.Parse(operation.Get("CAMERA_SHUTDOWN_SECONDS"));
            }
            else
            {
                cameraTimeout = 300;
            }

            if (cameraMode == "1")
            {
                StartCameraOcr();
            }
            else
            {
                StartCameraScan();
            }
        }

        private void StartCameraScan()
        {
            var scanError = PDLCameraScan.Instance.PrepareResource();
            var errorCode = (int)scanError.Code;
            operation.Set("CAMERA_SCAN_ERROR_CODE", Convert.ToString(errorCode));
            if (scanError.Code != PDLCameraScanErrorCode.OK)
            {
                Environment.Exit(0);
            }
            // CancelCaptureOnce メソッドの呼び出し例のため、
            // 300秒後にキャンセルするタイマーをスタートします。
            var timer = new CameraTimer(cameraTimeout, CancelCameraScan);
            timer.Start();
            // カメラプレビュー開始
            // CaptureOnceはUIカスタマイズ機能を使用しない場合に用いるメソッドです。
            // UIカスタマイズ機能を使用する場合、CaptureOnce2メソッドをご利用ください。
            PDLDocInfo docInfo;
            operation.Set("CAMERA_SCREEN_STATUS", "0");
            scanError = PDLCameraScan.Instance.CaptureOnce2(this, new CustomizeForm(personalIdentifyDocuments, "1"), out docInfo);
            // 上でスタートしたタイマーをストップします。
            timer.Stop();
            operation.Set("CAMERA_SCAN_ERROR_CODE", Convert.ToString((int)scanError.Code));
            switch (scanError.Code)
            {
                case PDLCameraScanErrorCode.OK:
                    OutputScanResult(docInfo);
                    break;
                case PDLCameraScanErrorCode.CropFailed:
                    OutputScanResult(docInfo);
                    break;
                case PDLCameraScanErrorCode.Cancel:
                    break;
                default:
                    break;
            }
            operation.Set("CAMERA_SCREEN_STATUS", "1");
            PDLCameraScan.Instance.DeinitResource();
            Environment.Exit(0);
        }

        private void StartCameraOcr()
        {
            // ライブラリ初期化
            SetOcrConfig();
            var scanError = PDLCameraOcr.Instance.PrepareResource();
            var errorCode = (int)scanError.Code;
            operation.Set("CAMERA_SCAN_ERROR_CODE", Convert.ToString(errorCode));
            if (scanError.Code != PDLCameraOcrErrorCode.OK)
            {
                Environment.Exit(0);
            }
            // CancelCaptureOnce メソッドの呼び出し例のため、
            // 300秒後にキャンセルするタイマーをスタートします。
            var timer = new CameraTimer(cameraTimeout, CancelCameraOcr);
            timer.Start();
            // カメラプレビュー開始
            // CaptureOnceはUIカスタマイズ機能を使用しない場合に用いるメソッドです。
            // UIカスタマイズ機能を使用する場合、CaptureOnce2メソッドをご利用ください。
            PDLCardInfo cardInfo;
            operation.Set("CAMERA_SCREEN_STATUS", "0");
            scanError = PDLCameraOcr.Instance.CaptureOnce2(this, new CustomizeForm(personalIdentifyDocuments, "1"), out cardInfo);
            // 上でスタートしたタイマーをストップします。
            timer.Stop();
            operation.Set("CAMERA_SCAN_ERROR_CODE", Convert.ToString((int)scanError.Code));
            switch (scanError.Code)
            {
                case PDLCameraOcrErrorCode.OK:
                    OutputOcrResult(cardInfo);
                    break;
                case PDLCameraOcrErrorCode.CamOcrInternal:
                    OutputOcrResult(cardInfo);
                    break;
                case PDLCameraOcrErrorCode.Cancel:
                    break;
                default:
                    break;
            }
            operation.Set("CAMERA_SCREEN_STATUS", "1");
            PDLCameraOcr.Instance.DeinitResource();
            Environment.Exit(0);
        }

        private void OutputScanResult(PDLDocInfo docInfo)
        {
            // 画像イメージを出力する
            using (Bitmap bImage = docInfo.Image)
            {
                if (bImage != null)
                {
                    // イメージを設定
                    // this.pictureBox.Image = docInfo.Image;
                    // JPEG用エンコーダの取得
                    ImageCodecInfo jpgEncoder = null;
                    foreach (ImageCodecInfo ici in ImageCodecInfo.GetImageEncoders())
                    {
                        if (ici.FormatID == ImageFormat.Jpeg.Guid)
                        {
                            jpgEncoder = ici;
                            break;
                        }
                    }

                    // 品質レベル：35　を設定(Settingsより取得)
                    EncoderParameter encParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 50L);
                    EncoderParameters encParams = new EncoderParameters(1);
                    encParams.Param[0] = encParam;

                    MemoryStream ms = new MemoryStream();

                    // 撮影画像をJPEG(品質35)にてMemoryStreamに格納
                    bImage.Save(ms, jpgEncoder, encParams);

                    // BASE64化して格納
                    byte[] byteImage = ms.ToArray();
                    if (byteImage.Length > 0)
                    {
                        var SigBase64 = Convert.ToBase64String(byteImage);
                        if (SigBase64.Length > 0)
                        {
                            File.WriteAllText(cameraFolderPath + @"\imagebase64.txt", SigBase64);
                            operation.Set("SCAN_PHOTO_MODE", ((int)docInfo.Mode).ToString(), false);
                            operation.Set("IMAGE_FILE_EXISTS", "0", false);
                            operation.write();
                        }
                        else
                        {
                            operation.Set("CAMERA_SCAN_ERROR_CODE", "base64 transform failed");
                        }
                    }
                }
                else
                {
                    operation.Set("CAMERA_SCAN_ERROR_CODE", "image not found");
                }
            }
        }

        private void OutputOcrResult(PDLCardInfo cardInfo)
        {
            // 画像イメージを出力する
            using (Bitmap bImage = cardInfo.Image)
            {
                if (bImage != null)
                {
                    // イメージを設定
                    // this.pictureBox.Image = docInfo.Image;
                    // JPEG用エンコーダの取得
                    ImageCodecInfo jpgEncoder = null;
                    foreach (ImageCodecInfo ici in ImageCodecInfo.GetImageEncoders())
                    {
                        if (ici.FormatID == ImageFormat.Jpeg.Guid)
                        {
                            jpgEncoder = ici;
                            break;
                        }
                    }

                    // 品質レベル：35　を設定(Settingsより取得)
                    EncoderParameter encParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 50L);
                    EncoderParameters encParams = new EncoderParameters(1);
                    encParams.Param[0] = encParam;

                    MemoryStream ms = new MemoryStream();

                    // 撮影画像をJPEG(品質35)にてMemoryStreamに格納
                    bImage.Save(ms, jpgEncoder, encParams);

                    // BASE64化して格納
                    byte[] byteImage = ms.ToArray();
                    if (byteImage.Length > 0)
                    {
                        var SigBase64 = Convert.ToBase64String(byteImage);
                        if (SigBase64.Length > 0)
                        {
                            File.WriteAllText(cameraFolderPath + @"\imagebase64.txt", SigBase64);
                            operation.Set("SCAN_PHOTO_MODE", ((int)cardInfo.Mode).ToString(), false);
                            operation.Set("IMAGE_FILE_EXISTS", "0", false);
                            operation.write();
                        }
                        else
                        {
                            operation.Set("CAMERA_SCAN_ERROR_CODE", "base64 transform failed");
                        }
                    }
                }
                else
                {
                    operation.Set("CAMERA_SCAN_ERROR_CODE", "image not found");
                }
            }
        }

        /// <summary>
        ///　カメラOCRライブラリ設定更新（OCRモード）
        /// </summary>
        private void SetOcrConfig()
        {
            var config = PDLCameraOcr.Instance.GetConfig();

            // 認識処理に関する設定情報 ---------------------------------------

            // 運転免許証の住所が都道府県を省略している場合に、都道府県を補完する機能の有効状態
            // 郵便番号検索を行う場合は true　が必須条件
            // config.RecognitionParam.NeedsPrefectures = Properties.Settings.Default.NeedsPrefectures;
            // 撮影非対象の本人確認書類が撮影されたときにエラーコードを返すかどうか
            // config.RecognitionParam.IsCardTypeErrorEnabled = Properties.Settings.Default.IsCardTypeErrorEnabled;
            // 運転免許証表面に光が反射していても認識を開始するかどうか
            // config.RecognitionParam.CheckLight = Properties.Settings.Default.CheckLight;

            // 運転免許証（表面）
            // config.RecognitionParam.IsOcrDriversLicenseEnabled = Properties.Settings.Default.IsOcrDriversLicenseEnabled;
            // 運転免許証（表面）
            // config.RecognitionParam.IsOcrDriversLicenseBackEnabled = Properties.Settings.Default.IsOcrDriversLicenseBackEnabled;
            // 通知カード
            // config.RecognitionParam.IsOcrNotificationEnabled = Properties.Settings.Default.IsOcrNotificationEnabled;
            // マイナンバーカード（表面）
            // config.RecognitionParam.IsOcrMyNumberFrontEnabled = Properties.Settings.Default.IsOcrMyNumberFrontEnabled;
            // マイナンバーカード（裏面）
            // config.RecognitionParam.IsOcrMyNumberBackEnabled = Properties.Settings.Default.IsOcrMyNumberBackEnabled;
            // 在留カード
            // config.RecognitionParam.IsOcrResidenceCardEnabled = Properties.Settings.Default.IsOcrResidenceCardEnabled;
            // 特別永住者証明書
            // config.RecognitionParam.IsOcrSpecialPermanentEnabled = Properties.Settings.Default.IsOcrSpecialPermanentEnabled;

            // 撮影ガイドの設定情報 ------------------------------------------------------------

            // 撮影ガイドの可視状態
            config.GuideParam.Visible = true;

            // 認識領域の設定情報 ---------------------------------------------------------------

            // 認識領域の可視状態
            config.CroppingRegionParam.Visible = true;
            // ----------------------------------------------------------------------------------

            // ライブラリの再設定
            PDLCameraOcr.Instance.SetConfig(config);

        }

        public void CancelCameraScan(object source, ElapsedEventArgs e)
        {
            PDLCameraScan.Instance.CancelCaptureOnce();
        }

        public void CancelCameraOcr(object source, ElapsedEventArgs e)
        {
            PDLCameraOcr.Instance.CancelCaptureOnce();
        }
    }

    public class CameraTimer
    {
        System.Timers.Timer timer;

        public CameraTimer(double timeout, Action<object, ElapsedEventArgs> action)
        {
            timer = new System.Timers.Timer(timeout);
            timer.Elapsed += new ElapsedEventHandler(action);
            timer.AutoReset = false;

        }

        public void Start()
        {
            timer.Enabled = true;
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
            timer.Dispose();
        }
    }

    public class SettingsOperation
    {
        string filePath;
        Settings settings;

        public SettingsOperation(string cameraFolderPath)
        {
            filePath = cameraFolderPath + @"\settings.xml";
            settings = XmlSerializerUtil.Deserilaze<Settings>(filePath, "SETTINGS");
        }

        public string Get(string key)
        {
            switch (key)
            {
                case "PERSONAL_IDENTIFY_DOCUMENTS":
                    return settings.PERSONAL_IDENTIFY_DOCUMENTS;
                case "CAMERA_MODE":
                    return settings.CAMERA_MODE;
                case "CAMERA_SHUTDOWN_SECONDS":
                    return settings.CAMERA_SHUTDOWN_SECONDS;
                case "CAMERA_SCREEN_STATUS":
                    return settings.CAMERA_SCREEN_STATUS;
                case "SCAN_PHOTO_MODE":
                    return settings.SCAN_PHOTO_MODE;
                case "IMAGE_FILE_EXISTS":
                    return settings.IMAGE_FILE_EXISTS;
                case "CAMERA_SCAN_ERROR_CODE":
                    return settings.CAMERA_SCAN_ERROR_CODE;
                default:
                    return "";
            }
        }

        public void Set(string key, string value, bool write = true)
        {
            switch (key)
            {
                case "PERSONAL_IDENTIFY_DOCUMENTS":
                    settings.PERSONAL_IDENTIFY_DOCUMENTS = value;
                    break;
                case "CAMERA_MODE":
                    settings.CAMERA_MODE = value;
                    break;
                case "CAMERA_SHUTDOWN_SECONDS":
                    settings.CAMERA_SHUTDOWN_SECONDS = value;
                    break;
                case "CAMERA_SCREEN_STATUS":
                    settings.CAMERA_SCREEN_STATUS = value;
                    break;
                case "SCAN_PHOTO_MODE":
                    settings.SCAN_PHOTO_MODE = value;
                    break;
                case "IMAGE_FILE_EXISTS":
                    settings.IMAGE_FILE_EXISTS = value;
                    break;
                case "CAMERA_SCAN_ERROR_CODE":
                    settings.CAMERA_SCAN_ERROR_CODE = value;
                    break;
                default:
                    break;
            }
            if (write)
            {
                XmlSerializerUtil.Serialize(filePath, settings);
            }
        }

        public void write()
        {
            XmlSerializerUtil.Serialize(filePath, settings);
        }

        [Serializable]
        public class Settings
        {
            public string PERSONAL_IDENTIFY_DOCUMENTS { get; set; }
            public string CAMERA_MODE { get; set; }
            public string CAMERA_SHUTDOWN_SECONDS { get; set; }
            // 0 ：visible, 1 ：invisible
            public string CAMERA_SCREEN_STATUS { get; set; }
            // 1 ：自動モード, 2 ：手動モード, 3 ：タイマーモード
            public string SCAN_PHOTO_MODE { get; set; }
            // 0 ：exists, 1 ：not exists
            public string IMAGE_FILE_EXISTS { get; set; }
            // OK 0 正常終了
            // Cancel 1 ユーザー操作によるキャンセル
            // CropFailed 2 正常終了（撮影には成功したが、書類の切り出しに失敗）
            // Memory -2001 メモリ不足
            // DlScanUninitialized -6001 未初期化
            // CamScanUnknown 9000 内部エラー
            // CamScanInvalidArgument 9001 引数エラー
            // CamScanInternal 9002 内部エラー
            // CamScanBadSequence 9003 呼び出しシーケンスエラー
            // CamScanHardwareFailed 9100 ハードウェア処理エラー
            // CamScanNotReleased 9200 リソース解放エラー
            public string CAMERA_SCAN_ERROR_CODE { get; set; }
        }
    }

    public class XmlSerializerUtil
    {
        public static void Serialize<T>(string filePath, T data)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, data);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occur during XML Serialization :{ex.Message}");
            }
        }

        public static T Deserilaze<T>(string filePath, string rootName)
        {
            try
            {
                XmlRootAttribute root = new XmlRootAttribute { ElementName = rootName };
                XmlSerializer serializer = new XmlSerializer(typeof(T), root);
                using (StreamReader reader = new StreamReader(filePath, Encoding.UTF8))
                {
                    return (T)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occur during XML Deserialization :{ex.Message}");
            }
            return default(T);
        }
    }

    public class AesUtil
    {
        public static byte[] Encrypt(string plainText, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                    }
                    return ms.ToArray();
                }
            }
        }

        public static string Decrypt(byte[] cipherText, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream(cipherText))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }

        public static byte[] DeriveKeyFromPassword(string password, int keySize)
        {
            // Generate salt
            byte[] salt = Encoding.UTF8.GetBytes("your-random-salt");

            // Derive key from password using PBKDF2 (Rfc2898DeriveBytes)
            using (var keyDerivationFunction = new Rfc2898DeriveBytes(password, salt, 10000))
            {
                return keyDerivationFunction.GetBytes(keySize);
            }
        }
    }
}
