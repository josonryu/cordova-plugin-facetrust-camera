var exec = require('cordova/exec');

exports.startCamera = function (arg0, successCallback, errorCallback) {
    exec(successCallback, errorCallback, 'FacetrustCamera', 'startCamera', [arg0]);
};
