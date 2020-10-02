﻿//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace Windows.Devices.Gpio
{
    /// <summary>
    /// Provides data about the <see cref="GpioPin.ValueChanged"/> event that occurs when the value of the general-purpose I/O (GPIO) pin changes, either because of an external stimulus when the pin is configured as an input, or when a value is written to the pin when the pin in configured as an output.
    /// </summary>
    public sealed class GpioPinValueChangedEventArgs
    {
        private GpioPinEdge _edge;
     
        /// <summary>
        /// Creats a new instance of the GpioPinValueChangedEventArgs class
        /// </summary>
        /// <param name="edge">An enumeration value that indicates the type of change that occurred to the value of the GPIO pin for the GpioPin.ValueChanged event.</param>
        public GpioPinValueChangedEventArgs(GpioPinEdge edge)
        {
            _edge = edge;
        }

        /// <summary>
        /// Gets the type of change that occurred to the value of the general-purpose I/O (GPIO) pin for the <see cref="GpioPin.ValueChanged"/> event.
        /// </summary>
        /// <value>An enumeration value that indicates the type of change that occurred to the value of the GPIO pin for
        ///     the GpioPin.ValueChanged event.</value>
        public GpioPinEdge Edge
        {
            get
            {
                return _edge;
            }
        }
    }
}
