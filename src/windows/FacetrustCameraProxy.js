var BIZ = require('./bizHandler');

var { CODE_MAP, ARG_KEYS,
    PATH, hasCode,
    getValue, isOcrAllowed,
    existsFile, readFileTxtData,
    saveFileTxtData, jsonToXml,
    xmlToJson, deserialize,
    getErrorResult, getCancelResult } = BIZ;
var cameraFolderPath, imageFilePath, settingsFilePath;
var personalIdentifyDocuments, cameraDiv, cameraShutdownSeconds;

exports.startCamera = function (successCallback, errorCallback, args) {

    // SF-001:初期化
    cameraFolderPath = cordova.file.dataDirectory + PATH.CAMERA_FOLDER;
    imageFilePath = cameraFolderPath + '/' + PATH.IMAGE_FILE_NAME;
    settingsFilePath = cameraFolderPath + '/' + PATH.SETTINGS_FILE_NAME;

    // SF-001:入力パラメータチェック
    var error = checkArg(args[0]);
    if (error) {
        return errorCallback(error);
    }

    var getScanInfoSuccessCallback = function (scanInfo) {
        successCallback(scanInfo);
    };
    var launchCameraAppSuccessCallback = function () {
        // SF-004:設定ファイル監視
        getScanInfo(getScanInfoSuccessCallback, errorCallback);
    };
    var writeSettingsSuccessCallback = function () {
        // SF-001:DynaEyeライブラリ起動
        launchCameraApp(launchCameraAppSuccessCallback, errorCallback);
    };
    // SF-001:設定ファイル初期化
    writeSettings(writeSettingsSuccessCallback, errorCallback);
};

function checkArg(arg) {
    if (!(arg)) {
        return getErrorResult('IC00_0012');
    }
    if (!arg.hasOwnProperty(ARG_KEYS[0])) {
        return getErrorResult('IC00_0001');
    }
    if (!hasCode(CODE_MAP['DOC_TYPE_OCR'], arg[ARG_KEYS[0]])) {
        return getErrorResult('IC00_0002');
    }
    if (!arg.hasOwnProperty(ARG_KEYS[1])) {
        return getErrorResult('IC00_0003');
    }
    if (!hasCode(CODE_MAP['CAMERA_DIV'], arg[ARG_KEYS[1]])) {
        return getErrorResult('IC00_0004');
    }
    if (!arg.hasOwnProperty(ARG_KEYS[2])) {
        return getErrorResult('IC00_0005');
    }
    if (!(arg[ARG_KEYS[2]] >= 1 && arg[ARG_KEYS[2]] <= 999)) {
        return getErrorResult('IC00_0006');
    }
    if (arg[ARG_KEYS[1]] === '1' && !isOcrAllowed(arg[ARG_KEYS[0]])) {
        return getErrorResult('IC00_0007', [ARG_KEYS[0], ARG_KEYS[1]]);
    }
    personalIdentifyDocuments = arg[ARG_KEYS[0]];
    cameraDiv = arg[ARG_KEYS[1]];
    cameraShutdownSeconds = arg[ARG_KEYS[2]];
}

function launchCameraApp(successCallback, errorCallback) {
    if (window.Windows && Windows.ApplicationModel.FullTrustProcessLauncher) {
        try {
            Windows.ApplicationModel.FullTrustProcessLauncher.launchFullTrustProcessForCurrentAppAsync();
            setTimeout(successCallback, 500);
        } catch (e) {
            errorCallback(getErrorResult('IC00_0013'));
        }
    } else {
        errorCallback(getErrorResult('IC00_0013'));
    }
}

function getScanInfo(successCallback, errorCallback) {
    var MAX_RETRY_COUNT = cameraShutdownSeconds / 0.5;
    var count = 0;
    var tryGetScanInfo = function () {
        count++;
        readSettings(function (settings) {
            var { CAMERA_SCAN_RETURN_CODE, ERROR_CODE } = settings;
            if (CAMERA_SCAN_RETURN_CODE === '1') {
                return errorCallback(getCancelResult());
            } else if (CAMERA_SCAN_RETURN_CODE === '0') {

            } else if (CAMERA_SCAN_RETURN_CODE === '') {
                if (count <= MAX_RETRY_COUNT) {
                    setTimeout(tryGetScanInfo, 500);
                } else {
                    return errorCallback(getErrorResult('IC00_0015'));
                }
            } else {
                return errorCallback(getErrorResult(ERROR_CODE, CAMERA_SCAN_RETURN_CODE));
            }

            // if (CAMERA_SCREEN_STATUS === '0' && IMAGE_FILE_EXISTS !== '0' && CAMERA_SCAN_ERROR_CODE !== '1') {
            //     setTimeout(tryGetScanInfo, 500);
            // } else if (IMAGE_FILE_EXISTS === '0') {
            //     return readFileTxtData(imageFilePath, (image) => {
            //         return successCallback({ mode: Number(SCAN_PHOTO_MODE), image });
            //     }, errorCallback);
            // } else if (CAMERA_SCAN_ERROR_CODE === '1') {
            //     return errorCallback('cancelCallback');
            // } else if (CAMERA_SCREEN_STATUS === '1') {
            //     return errorCallback();
            // } else {
            //     return errorCallback();
            // }
        }, errorCallback);
    }
    tryGetScanInfo();
}

function writeSettings(successCallback, errorCallback) {
    var settings = {
        'SETTINGS': {
            'PERSONAL_IDENTIFY_DOCUMENTS': getValue(CODE_MAP['DOC_TYPE_OCR'], personalIdentifyDocuments),
            'CAMERA_DIV': cameraDiv,
            'CAMERA_SHUTDOWN_SECONDS': cameraShutdownSeconds,
            'CAMERA_SCAN_RETURN_CODE': '',
            'ERROR_CODE': ''
        }
    };
    var resolveErrorCallback = function () {
        errorCallback(getErrorResult('IC00_0009'));
    };
    var writeErrorCallback = function () {
        errorCallback(getErrorResult('IC00_0008'));
    };
    saveFileTxtData(cameraFolderPath, PATH.SETTINGS_FILE_NAME, jsonToXml(settings), successCallback, resolveErrorCallback, writeErrorCallback);
}

function readSettings(successCallback, errorCallback) {
    var readFileTxtDataSuccessCallback = function (xmlString) {
        var xmlDoc = deserialize(xmlString);
        if (!xmlDoc) return errorCallback(getErrorResult('IC00_0016'));
        return successCallback(xmlToJson(xmlDoc));
    };
    var resolveErrorCallback = function () {
        errorCallback(getErrorResult('IC00_0010'));
    };
    var readErrorCallback = function () {
        errorCallback(getErrorResult('IC00_0014'));
    };
    var existsFileSuccessCallback = function () {
        readFileTxtData(settingsFilePath, readFileTxtDataSuccessCallback, resolveErrorCallback, readErrorCallback);
    };
    var existsFileErrorCallback = function () {
        errorCallback(getErrorResult('IC00_0010'));
    };
    existsFile(settingsFilePath, existsFileSuccessCallback, existsFileErrorCallback);
}

cordova.commandProxy.add('FacetrustCamera', exports);