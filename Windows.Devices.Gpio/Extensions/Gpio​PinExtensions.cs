//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.CompilerServices;

namespace Windows.Devices.Gpio
{
    /// <summary>
    /// nanoFramework extensions for <see cref="GpioPin"/>.
    /// </summary>
    public static class Gpio​PinExtensions
    {
        /// <summary>
        /// Sets the pin to the specified alternate function.
        /// </summary>
        /// <param name="pin"></param>
        /// <param name="alternateFunction">The value of the alternate function.</param>
        /// <remarks>
        /// This extension is exclusive of nanoFramework and it may not be supported in all platforms.
        /// WARNING: Use with caution! There is no validation on the execution of this call and there is the potential for breaking things, 
        /// so be sure to know what you are doing when using it.
        /// Platforms supporting this feature: Cortex-M and ESP32.
        /// Platforms not supporting this feature: none.
        /// </remarks>
        public static void SetAlternateFunction(this Gpio​Pin pin, int alternateFunction)
        {
            pin.NativeSetAlternateFunction(alternateFunction);
        }
    }
}
