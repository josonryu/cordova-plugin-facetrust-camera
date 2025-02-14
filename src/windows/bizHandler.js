var CryptoJS = require('./crypto-js');

var bizHandler = {
    MSG_MAP: {
        'IC00_0001': 'DynaEyeライブラリ起動パラメタ「PERSONAL_IDENTIFY_DOCUMENTS」が必須です。',
        'IC00_0002': 'DynaEyeライブラリ起動パラメタ「PERSONAL_IDENTIFY_DOCUMENTS」の設定値が不正です。\n「01,02,03,04,05,06,07,08,09,10,11,12,13,14,15,16」のいずれかを設定してください。',
        'IC00_0003': 'DynaEyeライブラリ起動パラメタ「CAMERA_DIV」が必須です。',
        'IC00_0004': 'DynaEyeライブラリ起動パラメタ「CAMERA_DIV」の設定値が不正です。\n「0,1」のいずれかを設定してください。',
        'IC00_0006': 'DynaEyeライブラリ起動パラメタ「CAMERA_TIMEOUT_SECONDS」の設定値が不正です。\n「1~999」を設定してください。',
        'IC00_0007': 'DynaEyeライブラリ起動パラメタの組み合わせエラーです。\n・対象パラメタ：「{0}」、「{1}」\n「{0}」が「08,09,10,11,12,13,14,15,16」のいずれかを設定された場合、「{1}」は「0」を設定してください。',
        'IC00_0008': 'ライブラリ設定ファイル（settings.xml）の作成に失敗しました。',
        'IC00_0009': 'ライブラリ設定ファイル（settings.xml）の更新に失敗しました。',
        'IC00_0010': 'ライブラリ設定ファイル（settings.xml）が存在しません。ポーリング処理が失敗しました。',
        'IC00_0011': '復号化処理が失敗しました。',
        'IC00_0012': 'DynaEyeライブラリ起動パラメタが未定義です。',
        'IC00_0013': 'DynaEyeライブラリ起動処理が失敗しました。',
        'IC00_0014': 'ライブラリ設定ファイル（settings.xml）の読み込み中にエラーが発生しました。ポーリング処理が失敗しました。',
        'IC00_0015': 'DynaEyeの撮影結果がありません。ポーリング処理が失敗しました。',
        'IC00_0016': 'ライブラリ設定ファイル（settings.xml）のフォーマットが不正です。ポーリング処理が失敗しました。',
        'IC01_0001': 'DynaEye初期化が失敗しました。\n・DynaEyeリターンコード：{0}',
        'IC01_0002': '暗号化処理が失敗しました。',
        'IC01_0003': '撮影結果出力が失敗しました。',
        'IC01_0004': 'DynaEye撮影が失敗しました。\n・DynaEyeリターンコード：{0}',
        'IC01_0005': 'その他のエラーが発生しました。',
        'IC99_0001': 'ライブラリ設定ファイル（settings.xml）が存在しません。DynaEye初期化が失敗しました。',
        'IC99_0002': 'ライブラリ設定ファイル（settings.xml）のフォーマットが不正です。DynaEye初期化が失敗しました。',
        'IC99_0003': 'ライブラリ設定ファイル（settings.xml）の必須エレメントが設定されていません。DynaEye初期化が失敗しました。',
        'IC99_0004': '削除処理が失敗しました。',
        'IC99_0005': 'ライブラリ設定ファイル（settings.xml）の読み込み中にエラーが発生しました。DynaEye初期化が失敗しました。',
        'IC99_0006': 'ライブラリ設定ファイル（settings.xml）が書き込み不可です。DynaEye初期化が失敗しました。',
    },
    CODE_MAP: {
        'DOC_TYPE_OCR': {
            '01': '運転免許証（表面）',
            '02': '運転免許証（裏面）',
            '03': 'マイナンバーカード（表面）',
            '04': 'マイナンバーカード（裏面）',
            '05': '通知カード',
            '06': '在留カード',
            '07': '特別永住者証明書',
            '08': 'パスポート（旅券）',
            '09': '各種福祉手帳',
            '10': 'その他（顔写真あり）',
            '11': '運転履歴証明書',
            '12': '健康保険書',
            '13': '住民票の写し',
            '14': '印鑑登録証明書',
            '15': 'その他１（顔写真なし）',
            '16': 'その他２（顔写真なし）'
        },
        'RESULT_CODE': {
            'SUCCESS': '0', // 成功
            'CANCEL': '1', // キャンセル
            'ERROR': '9' // 失敗
        },
        'CAMERA_DIV': {
            'SCAN': '0', // スキャンモード
            'OCR': '1' // OCRモード
        },
        'PHOTO_MODE': {
            '1': '自動モード',
            '2': '手動モード',
            '3': 'タイマーモード'
        }
    },
    ARG_KEYS: ['PERSONAL_IDENTIFY_DOCUMENTS', 'CAMERA_DIV', 'CAMERA_TIMEOUT_SECONDS'],
    PATH: {
        RESULT_FILE_NAME: 'result.xml',
        SETTINGS_FILE_NAME: 'settings.xml',
        CAMERA_FOLDER: 'Documents/facetrust/camera'
    },
    AES: {
        KEY: "8XYa/2N2Yue1DlfqyZKu7/TSKMFc+MOhe5vlWHh7ZAw=",
        IV: "CdNuaCStKx9ATFKJ4OEKAA==",
    },
    hasCode: function (map, code) {
        return map.hasOwnProperty(code);
    },
    hasValue: function (map, val) {
        return Object.values(map).includes(val);
    },
    getValue: function (map, code) {
        return map[code];
    },
    isOcrAllowed: function (type) {
        return ['01', '02', '03', '04', '05', '06', '07'].includes(type);
    },
    /**
     * フォルダ作成処理
     * フォルダが既に存在しても、新たに作成する事はない。
     * @param  {string} path - 作成対象対象ディレクトリルートパス
     * @param  {string} dirName - ディレクトリルートパス配下に作成するディレクトリパス
     * @param  {any} successCallback - 処理成功時コールバック
     * @param  {any} errorCallback - 処理成功時コールバック
     */
    createDirectory: function (path, dirName, successCallback, errorCallback) {
        // ファイルシステム取得成功時コールバック処理
        var resolveSuccessCallback = function (fs) {
            // ファイルシステム取得オプション
            var options = {
                exclusive: false,
                create: true
            };
            var createDir = function (dirEntry, folders) {
                if (folders[0] == '.' || folders[0] == '') {
                    folders = folders.slice(1);
                }
                if (folders.length <= 0) {
                    // 作成実施完了
                    successCallback();
                    return;
                }
                // 次の階層のディレクトリを取得
                dirEntry.getDirectory(folders[0], options, function (dirEntry) {
                    // 作成するディレクトリが存在する場合
                    if (0 < folders.length) {
                        // ディレクトリ作成処理を再帰的に呼び出す
                        createDir(dirEntry, folders.slice(1));
                    }
                    else {
                        // 作成実施完了
                        successCallback();
                    }
                }, errorCallback);
            };
            var dirArray = dirName.split('/');
            if (dirArray[0] == '.' || dirArray[0] == '') {
                dirArray = dirArray.slice(1);
            }
            // ディレクトリ作成処理
            createDir(fs, dirArray);
        };
        // ファイル操作処理
        window.resolveLocalFileSystemURL(path, resolveSuccessCallback, errorCallback);
        return;
    },
    /**
     * TXTデータのファイル読取り処理
     * @param  {string} filePath - TXTデータファイルパス
     * @param  {any} successCallback - 処理成功時コールバック
     * @param  {any} resolveErrorCallback - リゾルブ失敗時コールバック
     * @param  {any} readErrorCallback - 読取り失敗時コールバック
     */
    readFileTxtData: function (filePath, successCallback, resolveErrorCallback, readErrorCallback) {
        window.resolveLocalFileSystemURL(filePath, function (fileEntry) {
            fileEntry.file(function (file) {
                var reader = new FileReader();
                reader.onloadend = function (e) {
                    // 処理成功
                    successCallback(reader.result);
                };
                reader.readAsText(file);
            }, readErrorCallback);
        }, resolveErrorCallback);
    },
    /**
     * TXTデータのファイル保存処理
     * @param  {string} dirPath - 保存先ファイルパス
     * @param  {string} fileName - 保存先ファイル名
     * @param  {string} txtData - TXT保存文字列
     * @param  {any} successCallback - 処理成功時コールバック
     * @param  {any} resolveErrorCallback - リゾルブ失敗時コールバック
     * @param  {any} writeErrorCallback - 保存失敗時コールバック
     */
    saveFileTxtData: function (dirPath, fileName, txtData, successCallback, resolveErrorCallback, writeErrorCallback) {
        var strSrc = [txtData];
        // TXT形式データのBlob型変換
        var dataBlob = new Blob(strSrc, { type: 'text/plain' });
        window.resolveLocalFileSystemURL(dirPath, function (dir) {
            // ファイルシステムオプション
            var options = {
                exclusive: false,
                create: true
            };
            dir.getFile(fileName, options, function (file) {
                file.createWriter(function (fileWriter) {
                    fileWriter.write(dataBlob);
                    // 処理成功
                    fileWriter.onwrite = successCallback;
                    // 処理失敗
                    fileWriter.onerror = writeErrorCallback;
                }, writeErrorCallback);
            }, resolveErrorCallback);
        }, resolveErrorCallback);
    },
    removeFile: function (filePath, successCallback, errorCallback) {
        window.resolveLocalFileSystemURL(filePath, function (fileEntry) {
            fileEntry.remove(successCallback, errorCallback);
        }, errorCallback);
    },
    jsonToXml: function (json) {
        let xml = '<?xml version="1.0" encoding="UTF-8" ?>\n';
        function buildXml(obj) {
            let result = '';

            for (const key in obj) {
                if (obj.hasOwnProperty(key)) {
                    if (typeof obj[key] === 'object' && obj[key] !== null) {
                        result += `<${key}>\n${buildXml(obj[key])}</${key}>\n`;
                    } else {
                        result += `<${key}>${obj[key]}</${key}>\n`;
                    }
                }
            }
            return result;
        }
        xml += buildXml(json);
        return xml;
    },
    xmlToJson: function (xml) {
        var json = {};
        var rootElement = xml.documentElement;
        for (let i = 0; i < rootElement.children.length; i++) {
            const child = rootElement.children[i];
            const elementName = child.nodeName;
            const elementValue = child.textContent;
            json[elementName] = elementValue;
        }
        return json;
    },
    deserialize: function (xmlString) {
        var parser = new DOMParser();
        var xmlDoc = parser.parseFromString(xmlString, "application/xml");
        if (xmlDoc.querySelector("parsererror")) {
            return '';
        } else {
            return xmlDoc;
        }
    },
    /**
     * メッセージを取得する。
     *
     * @param {string} id - メッセージID
     * @param {array} params - 埋め込み文字列
     */
    getMsg: function (id, params) {
        if (!id) {
            // idが設定されていない場合はnullを返却する。
            return null;
        }
        var msgDef = bizHandler.MSG_MAP[id];
        if (!msgDef) {
            // idからメッセージ定義が取得できない場合はnullを返却する。
            return msgDef;
        }
        if (params && Array.isArray(params) && params.length > 0) {
            // パラメータが設定された場合は埋め込む
            return msgDef.replace(/{(\d+)}/g, function (match, number) {
                return typeof params[number] != 'undefined' ? params[number] : match;
            });
        }
        else {
            // パラメータが設定されていない場合はそのまま返却
            return msgDef;
        }
    },
    getErrorResult: function (msgId, params) {
        return {
            'RESULT': bizHandler.CODE_MAP.RESULT_CODE.ERROR,
            'ERROR': {
                'CODE': msgId,
                'MESSAGE': bizHandler.getMsg(msgId, params)
            }
        };
    },
    getCancelResult: function () {
        return {
            'RESULT': bizHandler.CODE_MAP.RESULT_CODE.CANCEL,
            'EEROR': null
        };
    },
    getSuccessResult: function (result) {
        return {
            'RESULT': bizHandler.CODE_MAP.RESULT_CODE.SUCCESS,
            'ERROR': null,
            'MODE': result['MODE'],
            'TYPE': result['TYPE'],
            'OCR': JSON.parse(result['OCR']) || {},
            'PIC': result['PIC']
        }
    },
    encrypt: function (data) {
        var keyBytes = CryptoJS.enc.Base64.parse(bizHandler.AES.KEY);
        var ivBytes = CryptoJS.enc.Base64.parse(bizHandler.AES.IV);
        var option = { iv: ivBytes, mode: CryptoJS.mode.CBC, padding: CryptoJS.pad.Pkcs7 };
        const encrypted = CryptoJS.AES.encrypt(data, keyBytes, option);
        return encrypted.toString();
    },
    decrypt: function (encryptedData) {
        var keyBytes = CryptoJS.enc.Base64.parse(bizHandler.AES.KEY);
        var ivBytes = CryptoJS.enc.Base64.parse(bizHandler.AES.IV);
        var option = { iv: ivBytes, mode: CryptoJS.mode.CBC, padding: CryptoJS.pad.Pkcs7 };
        const decrypted = CryptoJS.AES.decrypt(encryptedData, keyBytes, option);
        return decrypted.toString(CryptoJS.enc.Utf8);
    },
    trace: function (msgId) {
        // var today = new Date();
        // var date = today.getFullYear() + ('0' + (today.getMonth() + 1)).slice(-2) + ('0' + today.getDate()).slice(-2);
        // var datetime;
    }
};

module.exports = bizHandler;