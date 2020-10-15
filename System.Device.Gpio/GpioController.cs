// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Runtime.CompilerServices;

namespace System.Device.Gpio
{
    /// <summary>
    /// Represents a general-purpose I/O (GPIO) controller.
    /// </summary>
    public sealed class GpioController : IDisposable
    {
        private static readonly ArrayList s_GpioPins = new ArrayList();

        // this is used as the lock object 
        // a lock is required because multiple threads can access the GPIO controller
        static readonly object _syncLock = new object();

        private bool _disposedValue;

        /// <summary>
        /// Initializes a new instance of the System.Device.Gpio.GpioController class that
        /// will use the logical pin numbering scheme as default.
        /// </summary>
        public GpioController()
        {
        }

        /// <summary>
        /// Initializes a new instance of the System.Device.Gpio.GpioController class that
        /// will use the specified numbering scheme. The controller will default to use the
        /// driver that best applies given the platform the program is executing on.
        /// </summary>
        /// <param name="numberingScheme">The numbering scheme used to represent pins provided by the controller.</param>
        public GpioController(PinNumberingScheme numberingScheme) : this()
        {
            NumberingScheme = numberingScheme;
        }

        /// <summary>
        /// The numbering scheme used to represent pins provided by the controller.
        /// </summary>
        public PinNumberingScheme NumberingScheme { get; internal set; }

        /// <summary>
        /// The number of pins provided by the controller.
        /// </summary>
        public extern int PinCount
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /// <summary>
        /// Opens a pin in order for it to be ready to use.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <exception cref="InvalidOperationException">This exception will be thrown if the pin is already open.</exception>
        public void OpenPin(int pinNumber)
        {
            var pin = new Gpio​Pin(pinNumber);

            if (pin.Init())
            {
                // add to array
                s_GpioPins.Add(new GpioPinBundle() { PinNumber = pinNumber, GpioPin = pin });

                // done here
                return;
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Opens a pin and sets it to a specific mode.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="mode">The mode to be set.</param>
        public void OpenPin(
            int pinNumber,
            PinMode mode)
        {
            OpenPin(pinNumber);
            SetPinMode(pinNumber, mode);
        }

        /// <summary>
        /// Closes an open pin.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <exception cref="InvalidOperationException">This exception will be thrown on an attempt to close a pin that hasn't been opened.</exception>
        public void ClosePin(int pinNumber)
        {
            // need to find the pin 1st
            for (int i = 0; i < s_GpioPins.Count; i++)
            {
                if (((GpioPinBundle)s_GpioPins[i]).PinNumber == pinNumber)
                {
                    ((GpioPinBundle)s_GpioPins[i]).GpioPin.Dispose();

                    // done here
                    return;
                }
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Dispose the controller
        /// </summary>
        public void Dispose()
        {
            lock (_syncLock)
            {
                if (!_disposedValue)
                {
                    Dispose(true);

                    GC.SuppressFinalize(this);
                }
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // 1st dispose the pins that where opened through the controller
                    for (int i = 0; i < s_GpioPins.Count; i++)
                    {
                        ((GpioPinBundle)s_GpioPins[i]).GpioPin.Dispose();
                    }
                }

                DisposeNative();

                _disposedValue = true;
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void DisposeNative();

        /// <summary>
        /// Gets the mode of a pin.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <returns>The mode of the pin.</returns>
        public PinMode GetPinMode(int pinNumber)
        {
            // need to find the pin 1st
            for (int i = 0; i < s_GpioPins.Count; i++)
            {
                if (((GpioPinBundle)s_GpioPins[i]).PinNumber == pinNumber)
                {
                    return ((GpioPinBundle)s_GpioPins[i]).GpioPin.GetDriveMode();
                }
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Checks if a pin supports a specific mode.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="mode">The mode to check.</param>
        /// <returns>The status if the pin supports the mode.</returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern bool IsPinModeSupported(
            int pinNumber,
            PinMode mode);

        /// <summary>
        ///  Checks if a specific pin is open.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <returns>The status if the pin is open or closed.</returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern bool IsPinOpen(int pinNumber);

        /// <summary>
        /// Reads the current value of a pin.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <returns>The value of the pin.</returns>
        public PinValue Read(int pinNumber)
        {
            return NativeRead(pinNumber) == 1 ? PinValue.High : PinValue.Low;
        }

        /// <summary>
        /// Adds a callback that will be invoked when pinNumber has an event of type eventType.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="eventTypes">The event types to wait for.</param>
        /// <param name="callback">The callback method that will be invoked.</param>
        /// <exception cref="InvalidOperationException">This exception will be thrown on an attempt to register a callback to a pin that hasn't been opened.</exception>
        public void RegisterCallbackForPinValueChangedEvent(
            int pinNumber,
            PinEventTypes eventTypes,
            PinChangeEventHandler callback)
        {
            // need to find the pin 1st
            for (int i = 0; i < s_GpioPins.Count; i++)
            {
                if (((GpioPinBundle)s_GpioPins[i]).PinNumber == pinNumber)
                {
                    // get GpioPin
                    var gpioPin = (GpioPinBundle)s_GpioPins[i];

                    // set everything event related
                    gpioPin.GpioEvents = eventTypes;
                    gpioPin.GpioPinChange = callback;
                    gpioPin.GpioPin.ValueChanged += GpioControllerValueChanged;

                    // done here
                    return;
                }
            }

            throw new InvalidOperationException();
        }

        private void GpioControllerValueChanged(
            object sender,
            PinValueChangedEventArgs e)
        {
            // need to find the pin 1st
            GpioPinBundle gpioPin = null;

            for (int i = 0; i < s_GpioPins.Count; i++)
            {
                if (((GpioPinBundle)s_GpioPins[i]).PinNumber == e.PinNumber)
                {
                    gpioPin = (GpioPinBundle)s_GpioPins[i];

                    // done here
                    break;
                }
            }

            // sanity check
            if(gpioPin != null)
            {
                if (((gpioPin.GpioEvents & PinEventTypes.Falling) == PinEventTypes.Falling)
                    && ((e.ChangeType & PinEventTypes.Falling) == PinEventTypes.Falling))
                {
                    gpioPin.GpioPinChange.Invoke(
                        this,
                        new PinValueChangedEventArgs(PinEventTypes.Falling, gpioPin.PinNumber));
                }

                if (((gpioPin.GpioEvents & PinEventTypes.Rising) == PinEventTypes.Rising)
                    && ((e.ChangeType & PinEventTypes.Rising) == PinEventTypes.Rising))
                {
                    gpioPin.GpioPinChange.Invoke(
                        this,
                        new PinValueChangedEventArgs(PinEventTypes.Rising, gpioPin.PinNumber));
                }
            }
        }

        /// <summary>
        /// Sets the mode to a pin.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme</param>
        /// <param name="mode">The mode to be set.</param>
        [MethodImpl(MethodImplOptions.InternalCall)] 
        public extern void SetPinMode(
            int pinNumber,
            PinMode mode);

        /// <summary>
        /// Removes a callback that was being invoked for pin at pinNumber.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="callback">The callback method that will be invoked.</param>
        public void UnregisterCallbackForPinValueChangedEvent(
            int pinNumber,
            PinChangeEventHandler callback)
        {
            for (int i = 0; i < s_GpioPins.Count; i++)
            {
                if (((GpioPinBundle)s_GpioPins[i]).PinNumber == pinNumber)
                {
                    // get GpioPin
                    var gpioPin = (GpioPinBundle)s_GpioPins[i];

                    // clear everything event related
                    gpioPin.GpioEvents = PinEventTypes.None;
                    gpioPin.GpioPin.ValueChanged -= GpioControllerValueChanged;

                    // done here
                    return;
                }
            }
        }

        /// <summary>
        /// Writes a value to a pin.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="value">The value to be written to the pin.</param>
        public void Write(int pinNumber, PinValue value)
        {
            NativeWrite(pinNumber, (byte)value);
        }

        #region native calls

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern byte NativeRead(int pinNumber);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeWrite(int pinNumber, byte value);

        #endregion
    }

    internal class GpioPinBundle
    {
        public int PinNumber;
        public GpioPin GpioPin;
        public PinEventTypes GpioEvents;
        public PinChangeEventHandler GpioPinChange;
        public PinEventTypes GpioEventsHappening;
    }
}
