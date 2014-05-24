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

        public void SetViewModelProperty(string propertyPath, object value)
        {
            ReflectionsHelper.SetDeepProperty(_viewModel, propertyPath, value, () => _changingPropertyFromJs = propertyPath,
                () => _changingPropertyFromJs = null);
        }

        public void InvokeViewModelMethod(string methodPath, object[] args)
        {
            ReflectionsHelper.InvokeDeepMethod(_viewModel, methodPath, args);
        }
    }
}