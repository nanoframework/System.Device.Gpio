﻿//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System.Runtime.CompilerServices;

namespace Windows.Devices.Gpio
{
    /// <summary>
    /// Represents the default general-purpose I/O (GPIO) controller for the system.
    /// </summary>
    /// <remarks>To get a <see cref="Gpio​Controller"/> object, use the <see cref="GetDefault"/> method.</remarks>
    public sealed class Gpio​Controller : IGpioController
    {
        // this is used as the lock object 
        // a lock is required because multiple threads can access the I2C controller
        static object _syncLock;

        // we can have only one instance of the GpioController
        // need to do a lazy initialization of this field to make sure it exists when called elsewhere.
        private static GpioController s_instance;

        /// <summary>
        /// Gets the number of pins on the general-purpose I/O (GPIO) controller.
        /// </summary>
        /// <value>
        /// The number of pins on the GPIO controller. Some pins may not be available in user mode.
        /// For information about how the pin numbers correspond to physical pins, see the documentation for your circuit board.
        /// </value>
        public extern int PinCount
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /*
         * We do not have any Provider on our boards, so this method is not implemented
        /// <summary>
        /// Gets all the controllers that are connected to the system asynchronously.
        /// </summary>
        /// <param name="provider">The GPIO provider for the controllers on the system.</param>
        /// <returns>When the method completes successfully, it returns a list of values that represent the controllers available on the system.</returns>
        public static Gpio​Controller[] GetControllers(IGpioProvider provider)
        {
            return null;
        }
        */


        /// <summary>
        /// Gets the default general-purpose I/O (GPIO) controller for the system.
        /// </summary>
        /// <returns>The default GPIO controller for the system, or null if the system has no GPIO controller.</returns>
        public static Gpio​Controller GetDefault()
        {
            if (s_instance == null)
            {
                if (_syncLock == null)
                {
                    _syncLock = new object();
                }

                lock (_syncLock)
                {
                    if (s_instance == null)
                    {
                        s_instance = new Gpio​Controller();
                    }
                }
            }

            return s_instance;
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
        /// <returns>The opened GPIO pin.</returns>
        /// <remarks>The following exceptions can be thrown by this method:
        /// <list type="bullet">
        /// <item><term>E_INVALIDARG (0x80070057)</term>
        /// <description>An invalid parameter was specified. This error will be returned if the pin number is out of range. 
        /// Pin numbers start at 0 and increase to the maximum pin number, which is one less than the value returned by <see cref="PinCount"/>.</description></item>
        /// <item><term>HRESULT_FROM_WIN32(ERROR_NOT_FOUND) (0x80070490)</term>
        /// <description>The pin is not available to usermode applications; it is reserved by the system. See the documentation for your circuit board to find out which pins are available to usermode applications.</description></item>
        /// <item><term>HRESULT_FROM_WIN32(ERROR_SHARING_VIOLATION) (0x80070020)</term>
        /// <description>The pin is currently open in an incompatible sharing mode. For example:
        /// <list type="bullet">
        /// <item><term>The pin is already open in GpioSharingMode.Exclusive mode.</term></item>
        /// <item><term>The pin is already open in GpioSharingMode.SharedReadOnly mode when you request to open it in GpioSharingMode.Exclusive mode. </term></item>
        /// </list></description></item>
        /// <item><term>HRESULT_FROM_WIN32(ERROR_GPIO_INCOMPATIBLE_CONNECT_MODE) (0x80073bde)</term>
        /// <description>The pin is currently muxed to a different function; for example I2C, SPI, or UART. Ensure the pin is not in use by another function.</description></item>
        /// <item><term>HRESULT_FROM_WIN32(ERROR_GEN_FAILURE) (0x8007001f)</term>
        /// <description>The GPIO driver returned an error. Ensure that the GPIO driver is running and configured correctly.</description></item>
        /// </list>
        /// </remarks>
        public Gpio​Pin OpenPin(int pinNumber)
        {
            return OpenPin(pinNumber, GpioSharingMode.Exclusive);
        }

        /// <summary>
        /// Opens the specified general-purpose I/O (GPIO) pin in the specified mode.
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
        /// <returns>The opened GPIO pin.</returns>
        /// <remarks>The following exceptions can be thrown by this method:
        /// <list type="bullet">
        /// <item><term>E_INVALIDARG (0x80070057)</term>
        /// <description>An invalid parameter was specified. This error will be returned if the pin number is out of range. 
        /// Pin numbers start at 0 and increase to the maximum pin number, which is one less than the value returned by <see cref="PinCount"/>.</description></item>
        /// <item><term>HRESULT_FROM_WIN32(ERROR_NOT_FOUND) (0x80070490)</term>
        /// <description>The pin is not available to usermode applications; it is reserved by the system. See the documentation for your circuit board to find out which pins are available to usermode applications.</description></item>
        /// <item><term>HRESULT_FROM_WIN32(ERROR_SHARING_VIOLATION) (0x80070020)</term>
        /// <description>The pin is currently open in an incompatible sharing mode. For example:
        /// <list type="bullet">
        /// <item><term>The pin is already open in GpioSharingMode.Exclusive mode.</term></item>
        /// <item><term>The pin is already open in GpioSharingMode.SharedReadOnly mode when you request to open it in GpioSharingMode.Exclusive mode. </term></item>
        /// </list></description></item>
        /// <item><term>HRESULT_FROM_WIN32(ERROR_GPIO_INCOMPATIBLE_CONNECT_MODE) (0x80073bde)</term>
        /// <description>The pin is currently muxed to a different function; for example I2C, SPI, or UART. Ensure the pin is not in use by another function.</description></item>
        /// <item><term>HRESULT_FROM_WIN32(ERROR_GEN_FAILURE) (0x8007001f)</term>
        /// <description>The GPIO driver returned an error. Ensure that the GPIO driver is running and configured correctly.</description></item>
        /// </list>
        /// </remarks>
        public Gpio​Pin OpenPin(int pinNumber, GpioSharingMode sharingMode)
        {
            //Note : sharingMode ignored at the moment because we do not handle shared accessed pins

            var pin = new Gpio​Pin(pinNumber);

            if (pin.Init())
            {
                return pin;
            }

            throw new System.InvalidOperationException();
        }

        /// <summary>
        /// Opens the specified general-purpose I/O (GPIO) pin in the specified mode, and gets a status value that you can use to handle a failure to open the pin programmatically.
        /// </summary>
        /// <param name="pinNumber">The pin number of the GPIO pin that you want to open. Some pins may not be available in user mode. For information about how the pin numbers correspond to physical pins, see the documentation for your circuit board.</param>
        /// <param name="sharingMode">The mode in which you want to open the GPIO pin, which determines whether other connections to the pin can be opened while you have the pin open.</param>
        /// <param name="pin">The opened GPIO pin if the return value is true; otherwise null.</param>
        /// <param name="openStatus">An enumeration value that indicates either that the attempt to open the GPIO pin succeeded, or the reason that the attempt to open the GPIO pin failed.</param>
        /// <returns>True if the method successfully opened the pin; otherwise false.
        /// <para>If the method returns true, the pin parameter receives an instance of a GpioPin, and the openStatus parameter receives GpioOpenStatus.PinOpened.If the method returns false, the pin parameter is null and the openStatus parameter receives the reason that the operation failed.</para></returns>
        public bool TryOpenPin(int pinNumber, GpioSharingMode sharingMode, out Gpio​Pin pin, out GpioOpenStatus openStatus)
        {
            //Note : sharingMode ignored at the moment because we do not handle shared accessed pins

            var newPin = new Gpio​Pin(pinNumber);

            if (newPin.Init())
            {
                pin = newPin;
                openStatus = GpioOpenStatus.PinOpened;

                return true;
            }
            else
            {
                // failed to init the Gpio pint
                pin = null;
                openStatus = GpioOpenStatus.PinUnavailable;

                return false;
            }
        }

        #region IGpioController 
        bool IGpioController.TryOpenPin(int pinNumber, GpioSharingMode sharingMode, out IGpioPin pin, out GpioOpenStatus openStatus)
        {
            var result = TryOpenPin(pinNumber, sharingMode, out var p, out var os);
            pin = p;
            openStatus = os;

            return result;
        }

        IGpioPin IGpioController.OpenPin(int pinNumber)
        {
            return OpenPin(pinNumber);
        }

        IGpioPin IGpioController.OpenPin(int pinNumber, GpioSharingMode sharingMode)
        {
            return OpenPin(pinNumber, sharingMode);
        }
        #endregion
    }
}
