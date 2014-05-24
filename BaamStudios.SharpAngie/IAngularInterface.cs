using System;

namespace BaamStudios.SharpAngie
{
    public interface IAngularInterface
    {
        /// <summary>
        /// Sets the entire model in javascript. The implementation should call window.setModel(modelJson).
        /// </summary>
        /// <param name="clientId">user-defined value from the Bridge.Initialize method.</param>
        /// <param name="modelJson"></param>
        void SetModel(string clientId, string modelJson);

        /// <summary>
        /// Sets a (new) property in javascript. The implementation should call window.setModelProperty(propertyPath, valueJson).
        /// </summary>
        /// <param name="clientId">user-defined value from the Bridge.Initialize method.</param>
        /// <param name="propertyPath"></param>
        /// <param name="valueJson"></param>
        void SetModelProperty(string clientId, string propertyPath, string valueJson);
    }
}
