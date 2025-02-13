using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.IO;
using System.Xml.Serialization;
using System.Security.Cryptography;
using System.Text;
using PFU.DLCameraScan;
using PFU.DLCameraOcr;
using System.Timers;
using System.Collections.Generic;

namespace ScanAPP
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitCamera();
        }
        
        string cameraFolderPath;
        SettingsManager sm;
        ResultManager rm;

        string personalIdentifyDocuments;
        string cameraDiv;
        double cameraTimeoutSeconds;

        private void InitCamera()
        {
            cameraFolderPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path + @"\Documents\facetrust\camera";
            if (!Directory.Exists(cameraFolderPath))
            {
                Environment.Exit(9);
            }
            sm = new SettingsManager(cameraFolderPath);
            rm = new ResultManager(cameraFolderPath);
            personalIdentifyDocuments = sm.Get("PERSONAL_IDENTIFY_DOCUMENTS");
            cameraDiv = sm.Get("CAMERA_DIV");
            cameraTimeoutSeconds = double.Parse(sm.Get("CAMERA_TIMEOUT_SECONDS"));

            if (cameraDiv == "0")
            {
                StartCameraScan();
            }
            if (cameraDiv == "1")
            {
                StartCameraOcr();
            }
        }

        private void StartCameraScan()
        {
            try
            {
                var scanReturn = PDLCameraScan.Instance.PrepareResource();
                if (scanReturn.Code != PDLCameraScanErrorCode.OK)
                {
                    UpdateStatus(Convert.ToString((int)scanReturn.Code), "IC01_0001");
                    Environment.Exit(9);
                }
                // キャンセルするタイマーをスタート
                var timer = new CameraTimer(cameraTimeoutSeconds, CancelCameraScan);
                timer.Start();
                // カメラプレビュー開始
                PDLDocInfo docInfo;
                scanReturn = PDLCameraScan.Instance.CaptureOnce2(this, new CustomizeForm(personalIdentifyDocuments, cameraDiv), out docInfo);
                // 上でスタートしたタイマーをストップします。
                timer.Stop();
                if (scanReturn.Code == PDLCameraScanErrorCode.OK)
                {
                    OutputScanResult(docInfo);
                }
                else if (scanReturn.Code == PDLCameraScanErrorCode.Cancel)
                {
                    UpdateStatus("1", "");
                    PDLCameraScan.Instance.DeinitResource();
                    Environment.Exit(0);
                }
                else
                {
                    UpdateStatus(Convert.ToString((int)scanReturn.Code), "IC01_0004");
                    PDLCameraScan.Instance.DeinitResource();
                    Environment.Exit(9);
                }
            }
            catch
            {
                UpdateStatus("", "IC01_0005");
                Environment.Exit(9);
            }
        }

        private void StartCameraOcr()
        {
            try
            {
                // ライブラリ初期化
                SetOcrConfig();
                var ocrReturn = PDLCameraOcr.Instance.PrepareResource();
                if (ocrReturn.Code != PDLCameraOcrErrorCode.OK)
                {
                    UpdateStatus(Convert.ToString((int)ocrReturn.Code), "IC01_0001");
                    Environment.Exit(9);
                }
                // キャンセルするタイマーをスタート
                var timer = new CameraTimer(cameraTimeoutSeconds, CancelCameraOcr);
                timer.Start();
                // カメラプレビュー開始
                PDLCardInfo cardInfo;
                ocrReturn = PDLCameraOcr.Instance.CaptureOnce2(this, new CustomizeForm(personalIdentifyDocuments, cameraDiv), out cardInfo);
                // 上でスタートしたタイマーをストップします。
                timer.Stop();
                if (ocrReturn.Code == PDLCameraOcrErrorCode.OK)
                {
                    OutputOcrResult(cardInfo);
                }
                else if (ocrReturn.Code == PDLCameraOcrErrorCode.Cancel)
                {
                    UpdateStatus("1", "");
                    PDLCameraOcr.Instance.DeinitResource();
                    Environment.Exit(0);
                }
                else
                {
                    UpdateStatus(Convert.ToString((int)ocrReturn.Code), "IC01_0004");
                    PDLCameraOcr.Instance.DeinitResource();
                    Environment.Exit(9);
                }
            }
            catch
            {
                UpdateStatus("", "IC01_0005");
                Environment.Exit(9);
            }
        }

        private void OutputScanResult(PDLDocInfo docInfo)
        {
            // 画像イメージを出力する
            using (Bitmap bImage = docInfo.Image)
            {
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

                // 品質レベル：35　を設定
                EncoderParameter encParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 50L);
                EncoderParameters encParams = new EncoderParameters(1);
                encParams.Param[0] = encParam;

                MemoryStream ms = new MemoryStream();

                // 撮影画像をJPEG(品質35)にてMemoryStreamに格納
                bImage.Save(ms, jpgEncoder, encParams);

                // BASE64化して格納
                byte[] byteImage = ms.ToArray();
                var sigBase64 = Convert.ToBase64String(byteImage);
                try
                {
                    string sigBase64Encrypted = AesUtil.Encrypt(sigBase64);
                    rm.Set("MODE", ((int)docInfo.Mode).ToString(), false);
                    rm.Set("TYPE", "", false);
                    rm.Set("PIC", sigBase64Encrypted, false);
                    rm.Set("OCR", "", false);
                }
                catch (Exception)
                {
                    UpdateStatus("0", "IC01_0002");
                    PDLCameraScan.Instance.DeinitResource();
                    Environment.Exit(9);
                }

                try {
                    rm.Write();
                    UpdateStatus("0", "");
                    PDLCameraScan.Instance.DeinitResource();
                    Environment.Exit(0);
                }
                catch (Exception)
                {
                    UpdateStatus("0", "IC01_0003");
                    PDLCameraScan.Instance.DeinitResource();
                    Environment.Exit(9);
                }
            }
        }

        private void OutputOcrResult(PDLCardInfo cardInfo)
        {
            // 画像イメージを出力する
            using (Bitmap bImage = cardInfo.Image)
            {
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

                // 品質レベル：35　を設定
                EncoderParameter encParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 50L);
                EncoderParameters encParams = new EncoderParameters(1);
                encParams.Param[0] = encParam;

                MemoryStream ms = new MemoryStream();

                // 撮影画像をJPEG(品質35)にてMemoryStreamに格納
                bImage.Save(ms, jpgEncoder, encParams);

                // BASE64化して格納
                byte[] byteImage = ms.ToArray();
                var sigBase64 = Convert.ToBase64String(byteImage);
                try
                {
                    string sigBase64Encrypted = AesUtil.Encrypt(sigBase64);
                    string ocrText = $@"
                        {{
                            ""NUMBER"": ""{cardInfo.Number.Text}"",
                            ""NAME"": ""{cardInfo.Name.Text}"",
                            ""KANA"": ""{cardInfo.Kana.Text}"",
                            ""ADDRESS"": ""{cardInfo.Address.Text}"",
                            ""BIRTHDAY"": ""{cardInfo.Birthday.Text}"",
                            ""GENDER"": ""{cardInfo.Gender.Text}""
                        }}";
                    string ocrTextEncrypted = AesUtil.Encrypt(ocrText);
                    rm.Set("MODE", ((int)cardInfo.Mode).ToString(), false);
                    rm.Set("TYPE", ((int)cardInfo.CardType).ToString(), false);
                    rm.Set("PIC", sigBase64Encrypted, false);
                    rm.Set("OCR", ocrTextEncrypted, false);
                }
                catch (Exception)
                {
                    UpdateStatus("0", "IC01_0002");
                    PDLCameraOcr.Instance.DeinitResource();
                    Environment.Exit(9);
                }

                try {
                    rm.Write();
                    UpdateStatus("0", "");
                    PDLCameraOcr.Instance.DeinitResource();
                    Environment.Exit(0);
                }
                catch (Exception)
                {
                    UpdateStatus("0", "IC01_0003");
                    PDLCameraOcr.Instance.DeinitResource();
                    Environment.Exit(9);
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
            UpdateStatus("1", "");
            PDLCameraScan.Instance.CancelCaptureOnce();
            Environment.Exit(0);
        }

        public void CancelCameraOcr(object source, ElapsedEventArgs e)
        {
            UpdateStatus("1", "");
            PDLCameraOcr.Instance.CancelCaptureOnce();
            Environment.Exit(0);
        }

        private void UpdateStatus(string dynaeyeReturnCode, string cameraErrorCode)
        {
            sm.Set("DYNAEYE_RETURN_CODE", dynaeyeReturnCode, false);
            sm.Set("CAMERA_ERROR_CODE", cameraErrorCode, false);
            sm.Write();
        }
    }

    public class CameraTimer
    {
        System.Timers.Timer timer;

        public CameraTimer(double timeout, Action<object, ElapsedEventArgs> action)
        {
            timer = new System.Timers.Timer(timeout * 1000);
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

    public class MessageManager
    {
        private static Dictionary<string, string> MSG_MAP = new Dictionary<string, string>()
        {
            { "IC99_0001", "ライブラリ設定ファイル（settings.xml）が存在しません。DynaEye初期化が失敗しました。" },
            { "IC99_0002", "ライブラリ設定ファイル（settings.xml）のフォーマットが不正です。DynaEye初期化が失敗しました。" },
            { "IC99_0003", "ライブラリ設定ファイル（settings.xml）の必須エレメントが設定されていません。DynaEye初期化が失敗しました。" },
            { "IC99_0004", "削除処理が失敗しました。" },
            { "IC99_0005", "ライブラリ設定ファイル（settings.xml）の読み込み中にエラーが発生しました。DynaEye初期化が失敗しました。" },
            { "IC99_0006", "ライブラリ設定ファイル（settings.xml）が書き込み不可です。DynaEye初期化が失敗しました。" },
        };

        public static string GetMsg(string id)
        {
            if (!MSG_MAP.ContainsKey(id))
            {
                return "";
            }
            return MSG_MAP[id];
        }
    }

    public class SettingsManager
    {
        string filePath;
        Settings settings;
        LogManager lm;

        public SettingsManager(string parentPath)
        {
            filePath = parentPath + @"\settings.xml";
            lm = new LogManager(parentPath);
            try
            {
                settings = XmlSerializerUtil.Deserialize<Settings>(filePath, "SETTINGS");
                if (settings.PERSONAL_IDENTIFY_DOCUMENTS == null || 
                    settings.CAMERA_DIV == null || 
                    settings.CAMERA_TIMEOUT_SECONDS == null)
                {
                    lm.Trace("IC99_0003");
                    Environment.Exit(9);
                }
                if (!FileUtil.IsWritable(filePath))
                {
                    lm.Trace("IC99_0006");
                    Environment.Exit(9);
                }
            }
            catch (FileNotFoundException)
            {
                lm.Trace("IC99_0001");
                Environment.Exit(9);
            }
            catch (DirectoryNotFoundException)
            {
                lm.Trace("IC99_0001");
                Environment.Exit(9);
            }
            catch (InvalidOperationException)
            {
                lm.Trace("IC99_0002");
                Environment.Exit(9);
            }
            catch (Exception)
            {
                lm.Trace("IC99_0005");
                Environment.Exit(9);
            }
        }

        public string Get(string key)
        {
            switch (key)
            {
                case "PERSONAL_IDENTIFY_DOCUMENTS":
                    return settings.PERSONAL_IDENTIFY_DOCUMENTS;
                case "CAMERA_DIV":
                    return settings.CAMERA_DIV;
                case "CAMERA_TIMEOUT_SECONDS":
                    return settings.CAMERA_TIMEOUT_SECONDS;
                case "DYNAEYE_RETURN_CODE":
                    return settings.DYNAEYE_RETURN_CODE;
                case "CAMERA_ERROR_CODE":
                    return settings.CAMERA_ERROR_CODE;
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
                case "CAMERA_DIV":
                    settings.CAMERA_DIV = value;
                    break;
                case "CAMERA_TIMEOUT_SECONDS":
                    settings.CAMERA_TIMEOUT_SECONDS = value;
                    break;
                case "DYNAEYE_RETURN_CODE":
                    settings.DYNAEYE_RETURN_CODE = value;
                    break;
                case "CAMERA_ERROR_CODE":
                    settings.CAMERA_ERROR_CODE = value;
                    break;
                default:
                    break;
            }
            if (write)
            {
                Write();
            }
        }

        public void Write()
        {
            try
            {
                XmlSerializerUtil.Serialize(filePath, settings, "SETTINGS");
            }
            catch
            {
                lm.Trace("IC99_0006");
                PDLCameraOcr.Instance.DeinitResource();
                Environment.Exit(9);
            }
        }

        [Serializable]
        public class Settings
        {
            public string PERSONAL_IDENTIFY_DOCUMENTS { get; set; }
            public string CAMERA_DIV { get; set; }
            public string CAMERA_TIMEOUT_SECONDS { get; set; }
            public string DYNAEYE_RETURN_CODE { get; set; }
            public string CAMERA_ERROR_CODE { get; set; }
        }
    }

    public class ResultManager
    {
        string filePath;
        Result result;

        public ResultManager(string parentPath)
        {
            filePath = parentPath + @"\result.xml";
            result = new Result();
        }

        public string Get(string key)
        {
            switch (key)
            {
                case "MODE":
                    return result.MODE;
                case "TYPE":
                    return result.TYPE;
                case "OCR":
                    return result.OCR;
                case "PIC":
                    return result.PIC;
                default:
                    return "";
            }
        }

        public void Set(string key, string value, bool write = true)
        {
            switch (key)
            {
                case "MODE":
                    result.MODE = value;
                    break;
                case "TYPE":
                    result.TYPE = value;
                    break;
                case "OCR":
                    result.OCR = value;
                    break;
                case "PIC":
                    result.PIC = value;
                    break;
                default:
                    break;
            }
            if (write)
            {
                Write();
            }
        }

        public void Write()
        {
            try
            {
                XmlSerializerUtil.Serialize(filePath, result, "RESULT");
            }
            catch
            {
                throw;
            }
        }

        [Serializable]
        public class Result
        {
            public string MODE { get; set; }
            public string TYPE { get; set; }
            public string OCR { get; set; }
            public string PIC { get; set; }
        }
    }

    public class LogManager
    {
        string filePath;

        public LogManager(string parentPath)
        {
            filePath = $@"{parentPath}\facetrust{DateTime.Now.ToString("yyyyMMdd")}.trace";
        }

        public void Trace(string msgId)
        {
            try
            {
                using (StreamWriter sw = File.AppendText(filePath))
                {
                    sw.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] [{msgId}] - {MessageManager.GetMsg(msgId)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occur during Log Tracing :{ex.Message}");
            }
        }
    }

    public class XmlSerializerUtil
    {
        public static void Serialize<T>(string filePath, T data, string rootName)
        {
            try
            {
                XmlRootAttribute root = new XmlRootAttribute { ElementName = rootName };
                XmlSerializer serializer = new XmlSerializer(typeof(T), root);
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, data);
                }
            }
            catch
            {
                throw;
            }
        }

        public static T Deserialize<T>(string filePath, string rootName)
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
            catch
            {
                throw;
            }
        }
    }

    public class AesUtil
    {
        private static string AES_KEY = @"8XYa/2N2Yue1DlfqyZKu7/TSKMFc+MOhe5vlWHh7ZAw=";
        private static string AES_IV = @"CdNuaCStKx9ATFKJ4OEKAA==";

        public static string Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Convert.FromBase64String(AES_KEY);
                aes.IV = Convert.FromBase64String(AES_IV);
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
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string cipherText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Convert.FromBase64String(AES_KEY);
                aes.IV = Convert.FromBase64String(AES_IV);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(cipherText)))
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
    }

    public class FileUtil
    {
        public static bool IsWritable(string filePath)
        {
            try
            {
                FileAttributes attributes = File.GetAttributes(filePath);
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    return false;
                }
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
