// TODO: Delete
//using System;
//using Microsoft.Extensions.Configuration;
//using Silverback.Messaging.Configuration.Reflection;

//namespace Silverback.Messaging.Configuration
//{
//    public class ExplicitlyTypedSectionReader
//    {
//        private readonly CustomActivator _customActivator;

//        public ExplicitlyTypedSectionReader(CustomActivator customActivator)
//        {
//            _customActivator = customActivator;
//        }

//        public T GetObject<T>(IConfigurationSection configSection) =>
//            (T) GetObject(configSection);

//        public object GetObject(IConfigurationSection configSection)
//        {
//            var typeName = configSection.GetSection("Type").Value;

//            if (string.IsNullOrWhiteSpace(typeName))
//                throw new InvalidOperationException($"Missing Type in section {configSection.Path}.");

//            var obj = _customActivator.Activate<IEndpoint>(typeName, configSection);
            
//            configSection.Bind(obj);

//            return obj;
//        }
//    }
//}