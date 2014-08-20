var sharpAngieApp = angular.module('sharpAngieApp', typeof (angularModules) !== 'undefined' ? angularModules : []);

function log(message) {
    var element = document.getElementById("log");
    if(element)
        element.value = message + "\n" + element.value;
    if (console && console.log)
        console.log(message);
}

window.onerror = function(errorMsg, url, lineNumber, column, errorObj) {
    log('Error: ' + errorMsg + ' Script: ' + url + ' Line: ' + lineNumber
        + ' Column: ' + column + ' StackTrace: ' + errorObj);
};

function trycatch(fn) {
    try {
        return fn();
    } catch (e) {
        log(e);
    }
}

String.prototype.endsWith = function (suffix) {
    return this.indexOf(suffix, this.length - suffix.length) !== -1;
};

function isBackingField(propertyName) {
    return propertyName.endsWith("_backingfield");
}

function getBackingFieldName(propertyName) {
    return propertyName + "_backingfield";
}

/* converts a field to a property and converts subobjects if the property value is an object */
function wrapProperty(model, propertyName, parentPropertyPath) {

    var propertyPath = parentPropertyPath && parentPropertyPath.length > 0 ? parentPropertyPath + "." + propertyName : propertyName;

    // create property
    (function (p, pp) {
        var backingFieldName = getBackingFieldName(p);
        model[backingFieldName] = model[p];
        Object.defineProperty(model, p, {
            get: function () { return model[backingFieldName]; },
            set: function (value) {
                log("js: " + pp + "=" + value);
                model[backingFieldName] = value;
                sharpAngieBridge.setProperty(pp, value, model, propertyName);
            }
        });
    })(propertyName, propertyPath);

    // call recursively for subobjects
    var val = model[propertyName];
    if (typeof (val) === 'object') {
        hookModel(val, propertyPath);
    }
}

/* converts fields to properties and adds helper functions to the model */
function hookModel(model, parentPropertyPath) {

    if (model == null || typeof (model) === 'undefined')
        return model;
    
    if (typeof (model.propertyNames) === 'undefined') {
        var propertyNames = [];
        for (var key in model) {
            propertyNames.push(key);
        }
        model.propertyNames = propertyNames;
    }
    
    model.toJSON = function () {
        var result = {};
        for (var p in model) {
            if (!isBackingField(p) && p != "propertyNames") {
                // evaluate getter
                result[p] = model[p];
            }
        }
        return result;
    };

    model.invokeMethod = function (methodName) {
        var methodPath = parentPropertyPath && parentPropertyPath.length > 0 ? parentPropertyPath + "." + methodName : methodName;
        var args = Array.prototype.slice.call(arguments).slice(1);
        log("js: " + methodPath + "(" + JSON.stringify(args) + ")");
        sharpAngieBridge.invokeMethod(methodPath, args);
    };

    for (var propertyName in model) {
        if (!isBackingField(propertyName) && propertyName != "propertyNames") {
            wrapProperty(model, propertyName, parentPropertyPath);
        }
    }

    return model;
}

function parsePropertyPath(obj, propertyPath) {
    var propertyPathDotted = propertyPath.replace('[', '.').replace('].', '.').replace(']', '');
    var paths = propertyPathDotted.split('.');
    var current = obj;
    var parentPropertyPath = "";
    for (var i = 0; i < paths.length - 1; ++i) {
        if (current[paths[i]] == undefined) {
            current = undefined;
            break;
        } else {
            current = current[paths[i]];
            if (parentPropertyPath.length > 0)
                parentPropertyPath += ".";
            parentPropertyPath += paths[i];
        }
    }
    return {
        object: current,
        propertyName: paths[paths.length - 1],
        parentPropertyPath: parentPropertyPath
    };
}

sharpAngieApp.controller('SharpAngieController', function SharpAngieController($scope) {
    $scope.model = {};

    /* wrapper that tells angularjs about updates in the model data */
    $scope.safeApply = function (fn) {
        var phase = this.$root.$$phase;
        if (phase == '$apply' || phase == '$digest') {
            if (fn && (typeof (fn) === 'function')) {
                trycatch(fn);
            }
        } else {
            this.$apply(trycatch(fn));
        }
    };

    /* initialize bridge */
    var waitForBridge = setInterval(function () {
        $scope.safeApply(function () {
            //log("waiting for bridge");
            var canInitialize;
            try {
                canInitialize = sharpAngieBridge.initialize;
            } catch (e) {
            }
            if (canInitialize) {
                clearInterval(waitForBridge);
                log("initializing bridge");
                sharpAngieBridge.initialize();
                log("initialized bridge");
            }
        });
    }, 100);

    $scope.invokeMethod = function (methodPath) {
        var args = Array.prototype.slice.call(arguments).slice(1);
        log("js: " + methodPath + "(" + JSON.stringify(args) + ")");
        sharpAngieBridge.invokeMethod(methodPath, args);
    };

    /* entry point for c# */
    window.setModel = function (modelJson) {
        log("c#: model=" + modelJson);
        $scope.safeApply(function () {
            $scope.model = hookModel(JSON.parse(modelJson));
        });
    };

    /* entry point for c# */
    window.setModelProperty = function (propertyPath, valueJson) {
        var parsedPropertyPath = parsePropertyPath($scope.model, propertyPath);
        var targetObject = parsedPropertyPath.object;
        var targetProperty = parsedPropertyPath.propertyName;
        var parentPropertyPath = parsedPropertyPath.parentPropertyPath;

        // set value
        if (typeof (targetObject) !== 'undefined' && targetProperty) {
            var value = typeof (valueJson !== 'undefined') ? JSON.parse(valueJson) : undefined;
            if (value == null)
                value = undefined;
            $scope.safeApply(function () {
                log("c#: " + propertyPath + "=" + valueJson);
                var backingFieldName = getBackingFieldName(targetProperty);
                var isNew = typeof (targetObject[backingFieldName]) === 'undefined';
                if (isNew) {
                    targetObject[targetProperty] = value;
                    wrapProperty(targetObject, targetProperty, parentPropertyPath);
                } else {
                    targetObject[backingFieldName] = value;
                    if (typeof (value) === 'object') {
                        hookModel(value, propertyPath);
                    }
                }
            });
        }
    };
});