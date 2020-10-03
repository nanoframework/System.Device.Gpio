// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.IO;

namespace System.Device.Gpio
{
    /// <summary>
    /// Represents a general-purpose I/O (GPIO) controller.
    /// </summary>
    public sealed class GpioController : IDisposable
    {
        private Windows.Devices.Gpio.GpioController _controller;
        private static Windows.Devices.Gpio.GpioPin[] _gpioPins;
        private static PinEventTypes[] _gpioEvents;
        private static PinChangeEventHandler[] _gpioPinChange;
        private static PinEventTypes[] _gpioEventsHappening;

        // this is used as the lock object 
        // a lock is required because multiple threads can access the GPIO controller
        static object _syncLock;

        /// <summary>
        /// Initializes a new instance of the System.Device.Gpio.GpioController class that
        /// will use the logical pin numbering scheme as default.
        /// </summary>
        public GpioController()
        {
            GetController();
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

        private void GetController()
        {
            if (_syncLock == null)
            {
                _syncLock = new object();
            }

            lock (_syncLock)
            {
                _controller = Windows.Devices.Gpio.GpioController.GetDefault();
                if (_gpioPins == null)
                {
                    _gpioPins = new Windows.Devices.Gpio.GpioPin[_controller.PinCount];
                    _gpioEvents = new PinEventTypes[_controller.PinCount];
                    _gpioEventsHappening = new PinEventTypes[_controller.PinCount];
                    _gpioPinChange = new PinChangeEventHandler[_controller.PinCount];
                }
            }
        }

        /// <summary>
        /// The numbering scheme used to represent pins provided by the controller.
        /// </summary>
        public PinNumberingScheme NumberingScheme { get; internal set; }

        /// <summary>
        /// The number of pins provided by the controller.
        /// </summary>
        public int PinCount => _controller.PinCount;

        /// <summary>
        /// Closes an open pin.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        public void ClosePin(int pinNumber)
        {

            if (_gpioPins[pinNumber] != null)
            {
                _gpioPins[pinNumber].Dispose();
                _gpioPins[pinNumber] = null;
            }
            else
            {
                throw new IOException($"Port {pinNumber} is not open");
            }
        }

        /// <summary>
        /// Dispose the controller
        /// </summary>
        public void Dispose()
        {
            for (int i = 0; i < _gpioPins.Length; i++)
            {
                ClosePin(i);
            }
        }

        /// <summary>
        /// Gets the mode of a pin.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <returns>The mode of the pin.</returns>
        public PinMode GetPinMode(int pinNumber)
        {
            if (_gpioPins[pinNumber] == null)
            {
                throw new IOException($"Port {pinNumber} is not open");
            }

            // It is safe to cast, enums are the same
            return (PinMode)_gpioPins[pinNumber].GetDriveMode();
        }

        /// <summary>
        /// Checks if a pin supports a specific mode.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="mode">The mode to check.</param>
        /// <returns>The status if the pin supports the mode.</returns>
        public bool IsPinModeSupported(int pinNumber, PinMode mode) => _gpioPins[pinNumber].IsDriveModeSupported((Windows.Devices.Gpio.GpioPinDriveMode)mode);

        /// <summary>
        ///  Checks if a specific pin is open.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <returns>The status if the pin is open or closed.</returns>
        public bool IsPinOpen(int pinNumber) => _gpioPins[pinNumber] != null;

        /// <summary>
        /// Opens a pin in order for it to be ready to use.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        public void OpenPin(int pinNumber)
        {
            if (IsPinOpen(pinNumber))
            {
                throw new IOException($"Pin {pinNumber} already open");
            }

            _gpioPins[pinNumber] = _controller.OpenPin(pinNumber);
        }

        /// <summary>
        /// Opens a pin and sets it to a specific mode.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="mode">The mode to be set.</param>
        public void OpenPin(int pinNumber, PinMode mode)
        {
            OpenPin(pinNumber);
            SetPinMode(pinNumber, mode);
        }

        /// <summary>
        /// Reads the current value of a pin.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <returns>The value of the pin.</returns>
        public PinValue Read(int pinNumber) => _gpioPins[pinNumber].Read() == Windows.Devices.Gpio.GpioPinValue.High ? PinValue.High : PinValue.Low;

        /// <summary>
        /// Adds a callback that will be invoked when pinNumber has an event of type eventType.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="eventTypes">The event types to wait for.</param>
        /// <param name="callback">The callback method that will be invoked.</param>
        public void RegisterCallbackForPinValueChangedEvent(int pinNumber, PinEventTypes eventTypes, PinChangeEventHandler callback)
        {
            _gpioEvents[pinNumber] = eventTypes;
            _gpioPinChange[pinNumber] = callback;
            _gpioPins[pinNumber].ValueChanged += GpioControllerValueChanged;
        }

        private void GpioControllerValueChanged(object sender, Windows.Devices.Gpio.GpioPinValueChangedEventArgs e)
        {
            var gpioPinNumber = ((Windows.Devices.Gpio.GpioPin)sender).PinNumber;
            if ((e.Edge == Windows.Devices.Gpio.GpioPinEdge.FallingEdge) && (_gpioEvents[gpioPinNumber] == PinEventTypes.Falling))
            {
                _gpioPinChange[gpioPinNumber].Invoke(this, new PinValueChangedEventArgs(PinEventTypes.Falling, gpioPinNumber));
            }

            if ((e.Edge == Windows.Devices.Gpio.GpioPinEdge.RisingEdge) && (_gpioEvents[gpioPinNumber] == PinEventTypes.Rising))
            {
                _gpioPinChange[gpioPinNumber].Invoke(this, new PinValueChangedEventArgs(PinEventTypes.Rising, gpioPinNumber));
            }
        }

        /// <summary>
        /// Sets the mode to a pin.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme</param>
        /// <param name="mode">The mode to be set.</param>
        public void SetPinMode(int pinNumber, PinMode mode)
        {
            if (!IsPinOpen(pinNumber))
            {
                throw new IOException($"Pin {pinNumber} needs to be open");
            }

            // Safe cast, same enum on nanoFramework
            _gpioPins[pinNumber].SetDriveMode((Windows.Devices.Gpio.GpioPinDriveMode)mode);
        }

        /// <summary>
        /// Removes a callback that was being invoked for pin at pinNumber.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="callback">The callback method that will be invoked.</param>
        public void UnregisterCallbackForPinValueChangedEvent(int pinNumber, PinChangeEventHandler callback)
        {
            _gpioEvents[pinNumber] = PinEventTypes.None;
            _gpioPins[pinNumber].ValueChanged -= GpioControllerValueChanged;
        }

        /// <summary>
        /// Blocks execution until an event of type eventType is received or a period of
        /// time has expired.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="eventTypes">The event types to wait for.</param>
        /// <param name="timeout">The time to wait for the event.</param>
        /// <returns>A structure that contains the result of the waiting operation.</returns>
        public WaitForEventResult WaitForEvent(int pinNumber, PinEventTypes eventTypes, TimeSpan timeout)
        {
            _gpioEvents[pinNumber] = eventTypes;
            _gpioEventsHappening[pinNumber] = PinEventTypes.None;
            _gpioPins[pinNumber].ValueChanged += GpioControllerWaitForEvents;
            DateTime dtTimeout = DateTime.UtcNow.Add(timeout);
            while (DateTime.UtcNow < dtTimeout)
            {
                if (_gpioEventsHappening[pinNumber] != PinEventTypes.None)
                {
                    break;
                }
            }
            _gpioPins[pinNumber].ValueChanged -= GpioControllerWaitForEvents;

            if (_gpioEventsHappening[pinNumber] != PinEventTypes.None)
            {
                return new WaitForEventResult() { EventTypes = _gpioEventsHappening[pinNumber], TimedOut = false };
            }

            return new WaitForEventResult() { EventTypes = PinEventTypes.None, TimedOut = true };
        }

        private void GpioControllerWaitForEvents(object sender, Windows.Devices.Gpio.GpioPinValueChangedEventArgs e)
        {
            var gpioPinNumber = ((Windows.Devices.Gpio.GpioPin)sender).PinNumber;
            _gpioEventsHappening[gpioPinNumber] = e.Edge == Windows.Devices.Gpio.GpioPinEdge.FallingEdge ? PinEventTypes.Rising : PinEventTypes.Falling;
        }

        /// <summary>
        /// Writes a value to a pin.
        /// </summary>
        /// <param name="pinNumber">The pin number in the controller's numbering scheme.</param>
        /// <param name="value">The value to be written to the pin.</param>
        public void Write(int pinNumber, PinValue value)
        {
            _gpioPins[pinNumber].Write(value == PinValue.High ? Windows.Devices.Gpio.GpioPinValue.High : Windows.Devices.Gpio.GpioPinValue.Low);
        }
    }
}
