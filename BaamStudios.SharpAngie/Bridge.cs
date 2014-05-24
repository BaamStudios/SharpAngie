using System;
using ServiceStack.Text;

namespace BaamStudios.SharpAngie
{
    public class Bridge
    {
        private string _changingPropertyFromJs;

        private readonly ViewModelBase _viewModel;
        private readonly IAngularInterface _angularInterface;

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

            _viewModel.DeepPropertyChanged += (s, propertyPath, value) =>
            {
                if (_changingPropertyFromJs == propertyPath) return;
                var valueJson = value != null ? JsonSerializer.SerializeToString(value) : null;
                _angularInterface.SetModelProperty(clientId, propertyPath, valueJson);
            };
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
