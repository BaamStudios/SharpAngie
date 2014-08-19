using System.Linq;
using Awesomium.Core;
using BaamStudios.SharpAngie;

namespace BaamStudios.SharpAngieDemo
{
    public class WebViewBridge : IAngularInterface
    {
        private readonly IWebView _webView;
        private readonly Bridge _bridge;

        private JSObject _js;

        public WebViewBridge(IWebView webView, ViewModelBase viewModel)
        {
            _webView = webView;
            _bridge = new Bridge(viewModel, this);

            _webView.NativeViewInitialized += (sender, args) => _webView.DocumentReady += (sender2, args2) => Initialize();
        }

        private void Initialize()
        {
            _js = _webView.CreateGlobalJavascriptObject("sharpAngieBridge");
            _js.Bind("initialize", false, (x, y) => _bridge.Initialize(string.Empty));
            _js.Bind("setProperty", false, SetViewModelProperty);
            _js.Bind("invokeMethod", false, InvokeViewModelMethod);
        }

        #region js -> c#

        private void SetViewModelProperty(object sender, JavascriptMethodEventArgs e)
        {
            var propertyPath = e.Arguments[0];
            var value = ToObject(e.Arguments[1]);
            _bridge.SetViewModelProperty(string.Empty, propertyPath, value);
        }

        private void InvokeViewModelMethod(object sender, JavascriptMethodEventArgs e)
        {
            var methodPath = e.Arguments[0];
            var args = ((JSValue[])e.Arguments[1]).Select(ToObject).ToArray();
            _bridge.InvokeViewModelMethod(string.Empty, methodPath, args);
        }

        private static object ToObject(JSValue e)
        {
            if (e.IsArray) return null;
            if (e.IsBoolean) return (bool)e;
            if (e.IsDouble) return (double)e;
            if (e.IsInteger) return (int)e;
            if (e.IsNull) return null;
            if (e.IsNumber) return (decimal)e;
            if (e.IsObject) return null;
            if (e.IsString) return (string)e;
            if (e.IsUndefined) return null;
            return null;
        }

        #endregion

        #region c# -> js

        void IAngularInterface.SetModel(string clientId, string modelJson)
        {
            dynamic window = (JSObject)_webView.ExecuteJavascriptWithResult("window");
            window.setModel(modelJson);
        }

        void IAngularInterface.SetModelProperty(string clientId, string propertyPath, string valueJson)
        {
            dynamic window = (JSObject)_webView.ExecuteJavascriptWithResult("window");
            window.setModelProperty(propertyPath, valueJson ?? JSValue.Undefined);
        }

        #endregion
    }
}
