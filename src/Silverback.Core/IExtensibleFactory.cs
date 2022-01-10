// // Copyright (c) 2020 Sergio Aquilini
// // This code is licensed under MIT license (see LICENSE file for details)
//
// namespace Silverback;
//
// /// <summary>
// ///     Builds a service of type <typeparamref name="TService" /> according to the provided settings.
// /// </summary>
// /// <typeparam name="TService">
// ///     The type of the service to build.
// /// </typeparam>
// /// <typeparam name="TSettingsBase">
// ///     The base type of the settings to use.
// /// </typeparam>
// public interface IExtensibleFactory<TService, TSettingsBase>
// {
//     /// <summary>
//     ///     Returns an object of type <see cref="TService" /> according to the specified settings.
//     /// </summary>
//     /// <typeparam name="TSettings">
//     ///     The type of the settings.
//     /// </typeparam>
//     /// <param name="settings">
//     ///     The settings that will be used to create the service.
//     /// </param>
//     /// <returns>
//     ///     The service of type <typeparam name="TService" />.
//     /// </returns>
//     TService GetService<TSettings>(TSettings? settings)
//         where TSettings : TSettingsBase;
// }
