// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Device.Gpio
{
    /// <summary>
    /// Pin modes supported by the GPIO controllers and drivers.
    /// </summary>
    public enum PinMode
    {
        /// <summary>
        /// Configures the GPIO pin in floating mode, with high impedance.
        /// </summary>
        Input,

        /// <summary>
        /// Configures the GPIO pin as high impedance with a pull-down resistor to ground.
        /// </summary>
        InputPullDown,

        /// <summary>
        /// Configures the GPIO pin as high impedance with a pull-up resistor to the voltage charge connection (VCC).
        /// </summary>
        InputPullUp,

        /// <summary>
        /// Configures the GPIO pin in strong drive mode, with low impedance.
        /// </summary>
        Output,

        /// <summary>
        /// Configures the GPIO in open drain mode.
        /// </summary>
        OutputOpenDrain,

        /// <summary>
        /// Configures the GPIO pin in open drain mode with resistive pull-up mode.
        /// </summary>
        OutputOpenDrainPullUp,

        /// <summary>
        /// Configures the GPIO pin in open collector mode.
        /// </summary>
        OutputOpenSource,

        /// <summary>
        /// Configures the GPIO pin in open collector mode with resistive pull-down mode.
        /// </summary>
        OutputOpenSourcePullDown
    }
}
