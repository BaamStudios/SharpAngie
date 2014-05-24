using System;
using System.Collections.Generic;
using ServiceStack.Text;

namespace BaamStudios.SharpAngie
{
    public class Bridge
    {
        private string _changingPropertyFromJs;

        private readonly ViewModelBase _viewModel;
        private readonly IAngularInterface _angularInterface;

        private Dictionary<string, ViewModelBase.DeepPropertyChangedEventHandler> _propertyChangedEventHandlersByClient = new Dictionary<string, ViewModelBase.DeepPropertyChangedEventHandler>();

        public Bridge(ViewModelBase viewModel, IAngularInterface angularInterface)
        {
            _viewModel = viewModel;
            _angularInterface = angularInterface;
        }

        /// <summary>
        /// The javascript object window.sharpAngieBridge should be set up so that any call to sharpAngieBridge.initialize() will be forwarded to this method.
        /// </summary>
        /// <param name="clientId">User defined identifier for the client. Will be forwarded to IAngularInterface. can be null if not needed.</param>
        public void Initialize(string clientId)
        {
            var modelJson = JsonSerializer.SerializeToString(_viewModel);
            _angularInterface.SetModel(clientId, modelJson);
            
            ViewModelBase.DeepPropertyChangedEventHandler handler;
            if (string.IsNullOrEmpty(clientId) || !_propertyChangedEventHandlersByClient.TryGetValue(clientId, out handler))
            {
                handler = (s, propertyPath, value) =>
                {
                    if (_changingPropertyFromJs == propertyPath) return;
                    var valueJson = value != null ? JsonSerializer.SerializeToString(value) : null;
                    _angularInterface.SetModelProperty(clientId, propertyPath, valueJson);
                };

                if (!string.IsNullOrEmpty(clientId))
                {
                    _propertyChangedEventHandlersByClient.Add(clientId, handler);
                }
            }

            _viewModel.DeepPropertyChanged += handler;
        }

        /// <summary>
        /// For a clean disposal of event handlers, call this method when a client has closed the html page.
        /// This is not necessary if the client id is null or empty.
        /// </summary>
        /// <param name="clientId">User defined identifier for the client.</param>
        public void OnClientDisconnected(string clientId)
        {
            ViewModelBase.DeepPropertyChangedEventHandler handler;
            if (!string.IsNullOrEmpty(clientId) && _propertyChangedEventHandlersByClient.TryGetValue(clientId, out handler))
            {
                _viewModel.DeepPropertyChanged -= handler;
                _propertyChangedEventHandlersByClient.Remove(clientId);
            }
        }

        /// <summary>
        /// The javascript object window.sharpAngieBridge should be set up so that any call to sharpAngieBridge.setProperty(propertyPath, value) will be forwarded to this method.
        /// </summary>
        public void SetViewModelProperty(string propertyPath, object value)
        {
            ReflectionsHelper.SetDeepProperty(_viewModel, propertyPath, value, () => _changingPropertyFromJs = propertyPath,
                () => _changingPropertyFromJs = null);
        }

        /// <summary>
        /// The javascript object window.sharpAngieBridge should be set up so that any call to sharpAngieBridge.invokeMethod(methodPath, args) will be forwarded to this method.
        /// </summary>
        public void InvokeViewModelMethod(string methodPath, object[] args)
        {
            ReflectionsHelper.InvokeDeepMethod(_viewModel, methodPath, args);
        }
    }
}
