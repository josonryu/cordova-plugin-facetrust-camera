var BIZ = require('./bizHandler');

var { CODE_MAP, ARG_KEYS, PATH,
    hasCode, hasValue,
    getValue, isOcrAllowed,
    readFileTxtData, saveFileTxtData,
    jsonToXml, xmlToJson,
    deserialize, decrypt,
    getErrorResult, getCancelResult,
    getSuccessResult, removeFile,
    createDirectory, trace } = BIZ;
var { DOC_TYPE_OCR, RESULT_CODE, CAMERA_DIV } = CODE_MAP;
var cameraFolderPath, resultFilePath, settingsFilePath;
var personalIdentifyDocuments, cameraDiv, cameraTimeoutSeconds;

exports.startCamera = function (successCallback, errorCallback, args) {

    // SF-001:初期化
    cameraFolderPath = cordova.file.dataDirectory + PATH.CAMERA_FOLDER;
    resultFilePath = cameraFolderPath + '/' + PATH.RESULT_FILE_NAME;
    settingsFilePath = cameraFolderPath + '/' + PATH.SETTINGS_FILE_NAME;

    // SF-001:入力パラメータチェック
    var error = checkArg(args[0]);
    if (error) {
        return errorCallback(error);
    }

    var getCameraResultSuccessCallback = function (cameraResult) {
        clearCameraInfo();
        successCallback(cameraResult);
    };
    var launchCameraAppSuccessCallback = function () {
        // SF-004:設定ファイル監視
        getCameraResult(getCameraResultSuccessCallback, errorCallback);
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
    if (!hasCode(DOC_TYPE_OCR, arg[ARG_KEYS[0]])) {
        return getErrorResult('IC00_0002');
    }
    if (!arg.hasOwnProperty(ARG_KEYS[1])) {
        return getErrorResult('IC00_0003');
    }
    if (!hasValue(CAMERA_DIV, arg[ARG_KEYS[1]])) {
        return getErrorResult('IC00_0004');
    }
    if (arg.hasOwnProperty(ARG_KEYS[2]) && arg[ARG_KEYS[2]] && (arg[ARG_KEYS[2]] < 1 || arg[ARG_KEYS[2]] > 999)) {
        return getErrorResult('IC00_0006');
    }
    if (arg[ARG_KEYS[1]] === CAMERA_DIV.OCR && !isOcrAllowed(arg[ARG_KEYS[0]])) {
        return getErrorResult('IC00_0007', [ARG_KEYS[0], ARG_KEYS[1]]);
    }
    personalIdentifyDocuments = arg[ARG_KEYS[0]];
    cameraDiv = arg[ARG_KEYS[1]];
    cameraTimeoutSeconds = arg[ARG_KEYS[2]] || 300;
}

async function launchCameraApp(successCallback, errorCallback) {
    if (window.Windows && Windows.ApplicationModel.FullTrustProcessLauncher) {
        try {
            await Windows.ApplicationModel.FullTrustProcessLauncher.launchFullTrustProcessForCurrentAppAsync();
            setTimeout(successCallback, 500);
        } catch (e) {
            errorCallback(getErrorResult('IC00_0013'));
        }
    } else {
        errorCallback(getErrorResult('IC00_0013'));
    }
}

function getCameraResult(successCallback, errorCallback) {
    var MAX_RETRY_COUNT = cameraTimeoutSeconds / 0.5;
    var count = 0;
    var tryGetCameraResult = function () {
        count++;
        readSettings(function (settings) {
            var { DYNAEYE_RETURN_CODE, CAMERA_ERROR_CODE } = settings;
            if (DYNAEYE_RETURN_CODE === RESULT_CODE.CANCEL) {
                return errorCallback(getCancelResult());
            } else if (DYNAEYE_RETURN_CODE === RESULT_CODE.SUCCESS) {
                if (CAMERA_ERROR_CODE === '') {
                    readResult(function (result) {
                        try {
                            if (cameraDiv === CAMERA_DIV.OCR) {
                                result['OCR'] = decrypt(result['OCR']);
                            }
                            result['PIC'] = decrypt(result['PIC']);
                            return successCallback(getSuccessResult(result));
                        } catch (error) {
                            return errorCallback(getErrorResult('IC00_0011'));
                        }
                    }, function () { });
                } else {
                    return errorCallback(getErrorResult(CAMERA_ERROR_CODE));
                }
            } else if (DYNAEYE_RETURN_CODE === '') {
                if (CAMERA_ERROR_CODE === '') {
                    if (count <= MAX_RETRY_COUNT) {
                        setTimeout(tryGetCameraResult, 500);
                    } else {
                        return errorCallback(getErrorResult('IC00_0015'));
                    }
                } else {
                    return errorCallback(getErrorResult(CAMERA_ERROR_CODE));
                }
            } else {
                return errorCallback(getErrorResult(CAMERA_ERROR_CODE, [DYNAEYE_RETURN_CODE]));
            }
        }, errorCallback);
    }
    tryGetCameraResult();
}

function writeSettings(successCallback, errorCallback) {
    var settings = {
        'SETTINGS': {
            'PERSONAL_IDENTIFY_DOCUMENTS': getValue(CODE_MAP['DOC_TYPE_OCR'], personalIdentifyDocuments),
            'CAMERA_DIV': cameraDiv,
            'CAMERA_TIMEOUT_SECONDS': cameraTimeoutSeconds,
            'DYNAEYE_RETURN_CODE': '',
            'CAMERA_ERROR_CODE': ''
        }
    };
    var resolveErrorCallback = function () {
        errorCallback(getErrorResult('IC00_0008'));
    };
    var writeErrorCallback = function () {
        errorCallback(getErrorResult('IC00_0009'));
    };
    var createDirectorySuccessCallback = function () {
        saveFileTxtData(cameraFolderPath, PATH.SETTINGS_FILE_NAME, jsonToXml(settings), successCallback, resolveErrorCallback, writeErrorCallback);
    };
    createDirectory(cordova.file.dataDirectory, PATH.CAMERA_FOLDER, createDirectorySuccessCallback, resolveErrorCallback);
}

function readSettings(successCallback, errorCallback) {
    var readFileTxtDataSuccessCallback = function (xmlString) {
        var xmlDoc = deserialize(xmlString);
        if (!xmlDoc) return errorCallback(getErrorResult('IC00_0016'));
        return successCallback(xmlToJson(xmlDoc));
    };
    var readErrorCallback = function (error) {
        if (error.code === FileError.NOT_FOUND_ERR) {
            errorCallback(getErrorResult('IC00_0010'));
        } else {
            errorCallback(getErrorResult('IC00_0014'));
        }
    };
    readFileTxtData(settingsFilePath, readFileTxtDataSuccessCallback, readErrorCallback, readErrorCallback);
}

function readResult(successCallback, errorCallback) {
    var readFileTxtDataSuccessCallback = function (xmlString) {
        var xmlDoc = deserialize(xmlString);
        return successCallback(xmlToJson(xmlDoc));
    };
    readFileTxtData(resultFilePath, readFileTxtDataSuccessCallback, function () { }, function () { });
}

function clearCameraInfo() {
    removeFileErrorCallback = function () {
        trace(cameraFolderPath, 'IC99_0004');
    }
    removeFile(settingsFilePath, function () { }, removeFileErrorCallback);
    removeFile(resultFilePath, function () { }, removeFileErrorCallback);
}

cordova.commandProxy.add('FacetrustCamera', exports);