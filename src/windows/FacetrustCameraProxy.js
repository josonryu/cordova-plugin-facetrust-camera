var imagePath, settingsPath;
exports.startCamera = function (successCallback, errorCallback, args) {
    // if (!(args && args[0])) { 
    //     return errorCallback();
    // }
    // var parameter = args[0];
    
    // writeSettings(parameter, successCallback, errorCallback);
    imagePath = cordova.file.dataDirectory + 'camera/imagebase64.txt';
    settingsPath = cordova.file.dataDirectory + 'camera/settings.xml';

    var getScanInfoSuccessCallback = function (scanInfo) {
        successCallback(scanInfo);
    };
    var launchCameraAppSuccessCallback = function () {
        getScanInfo(getScanInfoSuccessCallback, errorCallback);
    };
    launchCameraApp(launchCameraAppSuccessCallback, errorCallback);
};

function launchCameraApp (successCallback, errorCallback) {
    if (window.Windows && Windows.ApplicationModel.FullTrustProcessLauncher) {
        try {
            Windows.ApplicationModel.FullTrustProcessLauncher.launchFullTrustProcessForCurrentAppAsync();
            var checkCameraStatus = function () {
                getSettings(function (settings) {
                    if (settings && settings['CameraScreenStatus'] === '0') {
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
                var { CameraScreenStatus, ImageFileExists, ScanPhotoMode, CameraScanErrorCode } = settings;
                if (CameraScreenStatus === '0' && ImageFileExists !== '0' && CameraScanErrorCode !== '1') {
                    setTimeout(tryGetScanInfo, 500);
                } else if (ImageFileExists === '0') {
                    return readFileTxtData(imagePath, (image) => {
                        return successCallback({ mode: Number(ScanPhotoMode), image });
                    }, errorCallback);
                } else if (CameraScanErrorCode === '1') {
                    return errorCallback('cancelCallback');
                } else if (CameraScreenStatus === '1') {
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

function writeSettings (settings, successCallback, errorCallback) {
    var JSONtoXML = function (obj) {
        var xml = '';
        for (var prop in obj) {
          xml += obj[prop] instanceof Array ? '' : '<' + prop + '>';
          if (obj[prop] instanceof Array) {
            for (let array in obj[prop]) {
              xml += '\n<' + prop + '>\n';
              xml += JSONtoXML(new Object(obj[prop][array]));
              xml += '</' + prop + '>';
            }
          } else if (typeof obj[prop] == 'object') {
            xml += JSONtoXML(new Object(obj[prop]));
          } else {
            xml += obj[prop];
          }
          xml += obj[prop] instanceof Array ? '' : '</' + prop + '>\n';
        }
        xml = xml.replace(/<\/?[0-9]{1,}>/g, '');
        return xml;
      }
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
        readFileTxtData(settingsPath, readFileTxtDataSuccessCallback, errorCallback);
    };
    existsFile(settingsPath, existsFileSuccessCallback, () => { });
    // existsFile(settingsPath, existsFileSuccessCallback, errorCallback);
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

cordova.commandProxy.add('FacetrustCamera', exports);