//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System.Runtime.CompilerServices;

namespace System.Device.Gpio
{
    // This should be a TypedEventHandler "EventHandler<PinValueChangedEventArgs>"
#pragma warning disable 1591
    public delegate void PinValueChangedEventHandler(
        object sender,
        PinValueChangedEventArgs e);

    /// <summary>
    /// Represents a general-purpose I/O (GPIO) pin.
    /// </summary>
    public sealed class Gpio​Pin : IDisposable
    {
        private static readonly GpioPinEventListener s_gpioPinEventManager = new GpioPinEventListener();

        // this is used as the lock object 
        // a lock is required because multiple threads can access the GpioPin
        private readonly object _syncLock = new object();

        private readonly int _pinNumber;

        private readonly PinMode _pinMode = PinMode.Input;
        private TimeSpan _debounceTimeout = TimeSpan.Zero;
        private PinValueChangedEventHandler _callbacks = null;

#pragma warning disable 0414
        // this field is used in native so it must be kept here despite "not being used"
        private PinValue _lastInputValue = PinValue.Low;
#pragma warning restore 0414

        internal Gpio​Pin(int pinNumber)
        {
            _pinNumber = pinNumber;
        }

        internal bool Init()
        {
            if (NativeInit(_pinNumber))
            {
                // add the pin to the event listener in order to receive the callbacks from the native interrupts
                s_gpioPinEventManager.AddPin(this);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets or sets the debounce timeout for the general-purpose I/O (GPIO) pin, which is an interval during which changes to the value of the pin are filtered out and do not generate ValueChanged events.
        /// </summary>
        /// <value>
        /// The debounce timeout for the GPIO pin, which is an interval during which changes to the value of the pin are filtered out and do not generate ValueChanged events.
        /// If the length of this interval is 0, all changes to the value of the pin generate ValueChanged events.
        /// </value>
        public TimeSpan DebounceTimeout
        {
            get
            {
                return _debounceTimeout;
            }

            set
            {
                _debounceTimeout = value;

                NativeSetDebounceTimeout();
            }
        }

        /// <summary>
        /// Gets the pin number of the general-purpose I/O (GPIO) pin.
        /// </summary>
        /// <value>
        /// The pin number of the GPIO pin.
        /// </value>
        public int PinNumber
        {
            get
            {
                return _pinNumber;
            }
        }

        /// <summary>
        /// Gets the current pin mode for the general-purpose I/O (GPIO) pin. The pin mode specifies whether the pin is configured as an input or an output, and determines how values are driven onto the pin.
        /// </summary>
        /// <returns>An enumeration value that indicates the current pin mode for the GPIO pin.
        /// The pin mode specifies whether the pin is configured as an input or an output, and determines how values are driven onto the pin.</returns>
        public PinMode GetPinMode()
        {
            lock (_syncLock)
            {
                // check if pin has been disposed
                if (!_disposedValue) { return _pinMode; }

                throw new ObjectDisposedException();
            }
        }

        /// <summary>
        /// Gets whether the general-purpose I/O (GPIO) pin supports the specified pin mode.
        /// </summary>
        /// <param name="pinMode">The pin mode that you want to check for support.</param>
        /// <returns>
        /// <see langword="true"/> if the GPIO pin supports the pin mode that pinMode specifies; otherwise false. 
        /// If you specify a pin mode for which this method returns <see langword="false"/> when you call <see cref="SetPinMode"/>, <see cref="SetPinMode"/> generates an exception.
        /// </returns>
        public bool IsPinModeSupported(PinMode pinMode)
        {
            lock (_syncLock)
            {
                // check if pin has been disposed
                if (!_disposedValue) { return NativeIsPinModeSupported(pinMode); }

                throw new ObjectDisposedException();
            }
        }

        /// <summary>
        /// Sets the pin mode of the general-purpose I/O (GPIO) pin. 
        /// The pin mode specifies whether the pin is configured as an input or an output, and determines how values are driven onto the pin.
        /// </summary>
        /// <param name="value">An enumeration value that specifies pin mode to use for the GPIO pin.
        /// The pin mode specifies whether the pin is configured as an input or an output, and determines how values are driven onto the pin.</param>
        /// <exception cref="ArgumentException">The GPIO pin does not support the specified pin mode.</exception>
        public void SetPinMode(PinMode value)
        {
            lock (_syncLock)
            {
                // check if pin has been disposed
                if (_disposedValue) { throw new ObjectDisposedException(); }

                // the native call takes care of:
                // 1) validating if the requested pin mode is supported
                // 2) throwing ArgumentException otherwise
                // 3) store the requested pin mode in _pinMode field
                NativeSetPinMode(value);
            }
        }

        /// <summary>
        /// Reads the current value of the general-purpose I/O (GPIO) pin.
        /// </summary>
        /// <returns>The current value of the GPIO pin. If the pin is configured as an output, this value is the last value written to the pin.</returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern PinValue Read();

        /// <summary>
        /// Drives the specified value onto the general purpose I/O (GPIO) pin according to the current pin mode for the pin 
        /// if the pin is configured as an output, or updates the latched output value for the pin if the pin is configured as an input.
        /// </summary>
        /// <param name="value">The enumeration value to write to the GPIO pin.
        /// <para>If the GPIO pin is configured as an output, the method drives the specified value onto the pin according to the current pin mode for the pin.</para>
        /// <para>If the GPIO pin is configured as an input, the method updates the latched output value for the pin. The latched output value is driven onto the pin when the configuration for the pin changes to output.</para>
        /// </param>
        /// <exception cref="InvalidOperationException">This exception will be thrown on an attempt to write to a pin that hasn't been opened or is not configured as output.</exception>
        public void Write(PinValue value)
        {
            lock (_syncLock)
            {
                // check if pin has been disposed
                if (_disposedValue) { throw new ObjectDisposedException(); }

                // the native call takes care of:
                // 1) validating if the requested pin mode is supported
                // 2) throwing ArgumentException otherwise
                // 3) firing the event for pin value changed if there are any registered callbacks
                WriteNative(value);
            }
        }

        /// <summary>
        /// Occurs when the value of the general-purpose I/O (GPIO) pin changes, either because of an external stimulus when the pin is configured as an input, or when a value is written to the pin when the pin in configured as an output.
        /// </summary>
        public event PinValueChangedEventHandler ValueChanged
        {
            add
            {
                lock (_syncLock)
                {
                    if (_disposedValue)
                    {
                        throw new ObjectDisposedException();
                    }

                    var callbacksOld = _callbacks;
                    var callbacksNew = (PinValueChangedEventHandler)Delegate.Combine(callbacksOld, value);

                    try
                    {
                        _callbacks = callbacksNew;
                        NativeSetPinMode(_pinMode);
                    }
                    catch
                    {
                        _callbacks = callbacksOld;
                        throw;
                    }
                }
            }

            remove
            {
                lock (_syncLock)
                {
                    if (_disposedValue)
                    {
                        throw new ObjectDisposedException();
                    }

                    var callbacksOld = _callbacks;
                    var callbacksNew = (PinValueChangedEventHandler)Delegate.Remove(callbacksOld, value);

                    try
                    {
                        _callbacks = callbacksNew;
                        NativeSetPinMode(_pinMode);
                    }
                    catch
                    {
                        _callbacks = callbacksOld;
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Handles internal events and re-dispatches them to the publicly subscribed delegates.
        /// </summary>
        /// <param name="edge">The state transition for this event.</param>
        internal void OnPinChangedInternal(PinEventTypes edge)
        {
            PinValueChangedEventHandler callbacks = null;

            lock (_syncLock)
            {
                if (!_disposedValue)
                {
                    callbacks = _callbacks;
                }
            }

            callbacks?.Invoke(this, new PinValueChangedEventArgs(edge, _pinNumber));
        }

        /// <summary>
        /// Toggles the output of the general purpose I/O (GPIO) pin if the pin is configured as an output.
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
#pragma warning disable S4200 // OK to call native methods directly in nanoFramework
        public extern void Toggle();
#pragma warning restore S4200 // Native methods should be wrapped

        #region IDisposable Support

        private bool _disposedValue;

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // remove the pin from the event listener
                    s_gpioPinEventManager.RemovePin(_pinNumber);
                }

                DisposeNative();

                _disposedValue = true;
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void DisposeNative();

#pragma warning disable 1591
        ~Gpio​Pin()
        {
            Dispose(false);
        }

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

        #endregion

        #region external calls to native implementations

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool NativeIsPinModeSupported(PinMode pinMode);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeSetPinMode(PinMode pinMode);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool NativeInit(int pinNumber);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeSetDebounceTimeout();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void WriteNative(PinValue value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern void NativeSetAlternateFunction(int alternateFunction);

        #endregion
    }
}
