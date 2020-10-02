//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace Windows.Devices.Gpio
{
    /// <summary>
    /// 
    /// </summary>
    public interface IGpioController
    {
        /// <summary>
        /// Gets the number of pins on the general-purpose I/O (GPIO) controller.
        /// </summary>
        /// <value>
        /// The number of pins on the GPIO controller. Some pins may not be available in user mode.
        /// For information about how the pin numbers correspond to physical pins, see the documentation for your circuit board.
        /// </value>
        int PinCount
        {
            get;
        }

        /// <summary>
        /// Opens a connection to the specified general-purpose I/O (GPIO) pin in exclusive mode.
        /// </summary>
        /// <param name="pinNumber">The pin number of the GPIO pin that you want to open. The pin number must be
        /// <list type="bullet">
        /// <item><term>in range</term></item>
        /// <item><term>available to usermode applications</term></item>
        /// </list>
        /// <para>Pin numbers start at 0, and increase to the maximum pin number, which is one less than the value returned by GpioController.PinCount.</para>
        /// <para>Which pins are available to usermode depends on the circuit board on which the code is running.For information about how pin numbers correspond to physical pins, see the documentation for your circuit board.</para>
        /// </param>
        /// <returns>An interface to the opened  GPIO pin.</returns>
        IGpioPin OpenPin(int pinNumber);

        /// <summary>
        /// Opens a connection to the specified general-purpose I/O (GPIO) pin in exclusive mode.
        /// </summary>
        /// <param name="pinNumber">The pin number of the GPIO pin that you want to open. The pin number must be
        /// <list type="bullet">
        /// <item><term>in range</term></item>
        /// <item><term>available to usermode applications</term></item>
        /// </list>
        /// <para>Pin numbers start at 0, and increase to the maximum pin number, which is one less than the value returned by GpioController.PinCount.</para>
        /// <para>Which pins are available to usermode depends on the circuit board on which the code is running.For information about how pin numbers correspond to physical pins, see the documentation for your circuit board.</para>
        /// </param>
        /// <param name="sharingMode">The mode in which you want to open the GPIO pin, which determines whether other connections to the pin can be opened while you have the pin open.</param>
        /// <returns>An interface to the opened  GPIO pin.</returns>
        IGpioPin OpenPin(int pinNumber, GpioSharingMode sharingMode);

        /// <summary>
        /// Opens the specified general-purpose I/O (GPIO) pin in the specified mode, and gets a status value that you can use to handle a failure to open the pin programmatically.
        /// </summary>
        /// <param name="pinNumber">The pin number of the GPIO pin that you want to open. Some pins may not be available in user mode. For information about how the pin numbers correspond to physical pins, see the documentation for your circuit board.</param>
        /// <param name="sharingMode">The mode in which you want to open the GPIO pin, which determines whether other connections to the pin can be opened while you have the pin open.</param>
        /// <param name="pin">The opened GPIO pin if the return value is true; otherwise null.</param>
        /// <param name="openStatus">An enumeration value that indicates either that the attempt to open the GPIO pin succeeded, or the reason that the attempt to open the GPIO pin failed.</param>
        /// <returns>True if the method successfully opened the pin; otherwise false.
        /// <para>If the method returns true, the pin parameter receives an instance of a GpioPin, and the openStatus parameter receives GpioOpenStatus.PinOpened.If the method returns false, the pin parameter is null and the openStatus parameter receives the reason that the operation failed.</para></returns>
        bool TryOpenPin(int pinNumber, GpioSharingMode sharingMode, out IGpioPin pin, out GpioOpenStatus openStatus);
    }
}
