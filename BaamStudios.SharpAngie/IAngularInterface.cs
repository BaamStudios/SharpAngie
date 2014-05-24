using System;

namespace BaamStudios.SharpAngie
{
    public interface IAngularInterface
    {
        /// <summary>
        /// Sets the entire model in javascript.
        /// </summary>
        /// <param name="modelJson"></param>
        void SetModel(string clientId, string modelJson);

        /// <summary>
        /// Sets a (new) property in javascript.
        /// </summary>
        /// <param name="propertyPath"></param>
        /// <param name="valueJson"></param>
        void SetModelProperty(string clientId, string propertyPath, string valueJson);
    }
}