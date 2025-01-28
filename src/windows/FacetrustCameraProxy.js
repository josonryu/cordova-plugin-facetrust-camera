var imageFileName, settingsFileName, cameraFolderPath, imageFilePath, settingsFilePath;

exports.startCamera = function (successCallback, errorCallback, args) {
    // if (!(args && args[0])) { 
    //     return errorCallback();
    // }
    
    imageFileName = 'imagebase64.txt';
    settingsFileName = 'settings.xml';
    cameraFolderPath = cordova.file.dataDirectory + 'camera';
    imageFilePath = cameraFolderPath + '/' + imageFileName;
    settingsFilePath = cameraFolderPath + '/' + settingsFileName;

    var getScanInfoSuccessCallback = function (scanInfo) {
        successCallback(scanInfo);
    };
    var launchCameraAppSuccessCallback = function () {
        getScanInfo(getScanInfoSuccessCallback, errorCallback);
    };
    var writeSettingsSuccessCallback = function () {
        launchCameraApp(launchCameraAppSuccessCallback, errorCallback);
    };
    writeSettings(args[0], writeSettingsSuccessCallback, errorCallback);
};

function launchCameraApp (successCallback, errorCallback) {
    if (window.Windows && Windows.ApplicationModel.FullTrustProcessLauncher) {
        try {
            Windows.ApplicationModel.FullTrustProcessLauncher.launchFullTrustProcessForCurrentAppAsync();
            var checkCameraStatus = function () {
                getSettings(function (settings) {
                    if (settings && settings['CAMERA_SCREEN_STATUS'] === '0') {
                        return successCallback();
                    }
                    setTimeout(checkCameraStatus, 500);
                }, errorCallback);
            };
            setTimeout(checkCameraStatus, 500);
        } catch (error) {
            errorCallback(error);
        }
    } else {
        errorCallback();
    }
}

function getScanInfo (successCallback, errorCallback) {
    var tryGetScanInfo = function () {
        getSettings(function (settings) {
            if (settings) {
                var { CAMERA_SCREEN_STATUS, IMAGE_FILE_EXISTS, SCAN_PHOTO_MODE, CAMERA_SCAN_ERROR_CODE } = settings;
                if (CAMERA_SCREEN_STATUS === '0' && IMAGE_FILE_EXISTS !== '0' && CAMERA_SCAN_ERROR_CODE !== '1') {
                    setTimeout(tryGetScanInfo, 500);
                } else if (IMAGE_FILE_EXISTS === '0') {
                    return readFileTxtData(imageFilePath, (image) => {
                        return successCallback({ mode: Number(SCAN_PHOTO_MODE), image });
                    }, errorCallback);
                } else if (CAMERA_SCAN_ERROR_CODE === '1') {
                    return errorCallback('cancelCallback');
                } else if (CAMERA_SCREEN_STATUS === '1') {
                    return errorCallback();
                } else {
                    return errorCallback();
                }
            } else {
                return errorCallback();
            }
        }, errorCallback);
    }
    tryGetScanInfo();
}

function writeSettings (params, successCallback, errorCallback) {
    var settings = {
        'SETTINGS': {
            'PERSONAL_IDENTIFY_DOCUMENTS': getDocumentName(params['PERSONAL_IDENTIFY_DOCUMENTS']),
            'CAMERA_MODE': params['CAMERA_MODE'],
            'CAMERA_SHUTDOWN_SECONDS': params['CAMERA_SHUTDOWN_SECONDS'],
            'CAMERA_SCREEN_STATUS': '',
            'IMAGE_FILE_EXISTS': '',
            'SCAN_PHOTO_MODE': '',
            'CAMERA_SCAN_ERROR_CODE': '',
        }
    };
    var xmlString = jsonToXml(settings);
    saveFileTxtData(cameraFolderPath, settingsFileName, xmlString, successCallback, errorCallback);
}

function getSettings (successCallback, errorCallback) {
    var readFileTxtDataSuccessCallback = function (xmlString) {
        if (!xmlString) return errorCallback();
        var xmlDoc = deserialize(xmlString);
        if (!xmlDoc) return errorCallback();

        var xmlObj = {};
        var rootElement = xmlDoc.documentElement;
        for (let i = 0; i < rootElement.children.length; i++) {
            const child = rootElement.children[i];
            const elementName = child.nodeName;
            const elementValue = child.textContent || null;
            xmlObj[elementName] = elementValue;
        }
        return successCallback(xmlObj);
    };
    var existsFileSuccessCallback = function () {
        readFileTxtData(settingsFilePath, readFileTxtDataSuccessCallback, errorCallback);
    };
    existsFile(settingsFilePath, existsFileSuccessCallback, errorCallback);
}

/**
 * ファイル・フォルダ存在確認
 * @param  {string} path - 存在確認対象パス(ファイル・ディレクトリ)
 * @param  {any} successCallback - 処理成功時コールバック
 * @param  {any} errorCallback - 処理成功時コールバック
 */
function existsFile (path, successCallback, errorCallback) {
    window.resolveLocalFileSystemURL(path, successCallback, errorCallback);
}

/**
 * TXTデータのファイル読取り処理
 * @param  {string} filePath - TXTデータファイルパス
 * @param  {any} successCallback - 処理成功時コールバック
 * @param  {any} errorCallback - 処理成功時コールバック
 */
function readFileTxtData (filePath, successCallback, errorCallback) {
    window.resolveLocalFileSystemURL(filePath, function (fileEntry) {
        fileEntry.file(function (file) {
            var reader = new FileReader();
            reader.onloadend = function (e) {
                // 処理成功
                successCallback(reader.result);
            };
            reader.readAsText(file);
        }, function (error) {
            // 処理失敗
            errorCallback(error);
        });
    }, function (error) {
        // 処理失敗
        errorCallback(error);
    });
}

/**
 * TXTデータのファイル保存処理
 * @param  {string} dirPath - 保存先ファイルパス
 * @param  {string} fileName - 保存先ファイル名
 * @param  {string} txtData - TXT保存文字列
 * @param  {any} successCallback - 処理成功時コールバック
 * @param  {any} errorCallback - 処理成功時コールバック
 */
function saveFileTxtData (dirPath, fileName, txtData, successCallback, errorCallback) {
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
                successCallback();
            }, function (e) {
                // ファイルシステムエラーコード情報取得
                var error = fileSystemErrorHandler(e);
                // 処理失敗
                errorCallback(error);
            });
        });
    }, function (e) {
        // ファイルシステムエラーコード情報取得
        var error = fileSystemErrorHandler(e);
        // 処理失敗
        errorCallback(error);
    });
}

function jsonToXml(json) {
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
}

function serialize (xmlDoc) {
    var serializer = new XMLSerializer();
    return serializer.serializeToString(xmlDoc);
}

function deserialize (xmlString) {
    var parser = new DOMParser();
    var xmlDoc = parser.parseFromString(xmlString, "application/xml");
    if (xmlDoc.querySelector("parsererror")) {
        return '';
    } else {
        return xmlDoc;
    }
}

function getDocumentName (kbn) {
    switch (kbn) {
        case '01':
            return 'マイナンバーカード（表面）';
        default:
            return '';
    }
}

cordova.commandProxy.add('FacetrustCamera', exports);