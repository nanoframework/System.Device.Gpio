//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.CompilerServices;

namespace Windows.Devices.Gpio
{
    /// <summary>
    /// Counts changes of a specified polarity on a general-purpose I/O (GPIO) pin.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When the pin is an input, interrupts are used to detect pin changes unless the MCU supports a counter in hardware. 
    /// Changes of the pin are enabled for the specified polarity, and the count is incremented when a change occurs.
    /// </para>
    /// <para>
    /// When the pin is an output, the count will increment whenever the specified transition occurs on the pin. 
    /// For example, if the pin is configured as an output and counting is enabled for rising edges, writing a 0 and then a 1 will cause the count to be incremented.
    /// </para>
    /// </remarks>
    public sealed class Gpio​Change​Counter : IDisposable
    {
        // property backing fields
        private int _pinNumber;
        private bool _inputMode;
        private GpioChangePolarity _polarity = GpioChangePolarity.Falling;
        private bool _countActive = false;

        // this is used as the lock object 
        // a lock is required because multiple threads can access the Gpio​Change​Counter
        private object _syncLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="Gpio​Change​Counter"/> class associated with the specified pin.
        /// Only a single <see cref="Gpio​Change​Counter"/> may be associated with a pin at any given time.
        /// </summary>
        /// <param name="pin">The pin on which to count changes.
        /// This pin must have been opened in Exclusive mode, and cannot be associated with another GpioChangeCounter.
        /// </param>
        /// <remarks>The following exceptions can be thrown by this method:
        /// <list type="bullet">
        /// <item><term>E_INVALIDARG : TThe pin is already associated with a change counter.That change counter must be disposed before the pin can be associated with a new change counter.</term></item>
        /// <item><term>E_ACCESSDENIED : The pin is not opened in Exclusive mode.</term></item>
        /// </list>
        /// </remarks>
        public Gpio​Change​Counter(Gpio​Pin pin)
        {
            if ( pin.SharingMode != GpioSharingMode.Exclusive )
            {
                throw new  ArgumentException();
            }

            _pinNumber = pin.PinNumber;

            _inputMode = (pin.GetDriveMode() < GpioPinDriveMode.Output );

            NativeInit();
        }

        /// <summary>
        /// Gets whether pin change counting is currently active.
        /// </summary>
        /// <returns><c>TRUE</c> if this pin change counting is active and <c>FALSE</c> otherwise.</returns>
        public bool IsStarted
        {
            get
            {
                return _countActive;
            }
        }


        /// <summary>
        /// Gets or sets the polarity of transitions that will be counted. The polarity may only be changed when pin counting is not started.
        /// </summary>
        /// <remarks><para>The default polarity value is Falling. See <see cref="GpioChangePolarity"></see> for more information on polarity values. Counting a single edge can be considerably more efficient than counting both edges.</para>
        /// <para>The following exceptions can be thrown when setting the polarity:</para>
        /// <list type="bullet">
        /// <item><term>E_INVALID_OPERATION :Change counting is currently active. Polarity can only be set before calling Start() or after calling Stop().</term></item>
        /// </list>
        /// </remarks>
        public GpioChangePolarity Polarity
        {
            get
            {
                return _polarity;
            }

            set
            {
                CheckIfActive(true);

                _polarity = value;
            }
        }

        /// <summary>
        /// Reads the current count of polarity changes. Before counting has been started, this will return 0.
        /// </summary>
        /// <returns>A <see cref="GpioChangeCount" /> structure containing a count and an associated timestamp.</returns>
        /// <remarks><para>The following exception can be thrown by this method:</para>
        /// <list type="bullet">
        /// <item><term>E_OBJECT_DISPOSED : The change counter or the associated pin has been disposed.</term></item>
        /// </list>
        /// </remarks>
        public GpioChangeCount Read()
        {
            return ReadInternal(false);
        }

        internal GpioChangeCount ReadInternal(bool reset)
        {
            GpioChangeCount changeCount;

            lock (_syncLock)
            {
                if (_disposedValue) { throw new ObjectDisposedException(); }

                changeCount = NativeRead(reset);
            }
            return changeCount;
        }

        /// <summary>
        /// Resets the count to 0 and returns the previous count.
        /// </summary>
        /// <returns>A <see cref="GpioChangeCount" /> structure containing a count and an associated timestamp.</returns>
        /// <remarks><para>The following exception can be thrown by this method:</para>
        /// <list type="bullet">
        /// <item><term>E_OBJECT_DISPOSED : The change counter or the associated pin has been disposed.</term></item>
        /// </list>
        /// </remarks>
        public GpioChangeCount Reset()
        {
            return ReadInternal(true);
        }

        /// <summary>
        /// Starts counting changes in pin polarity. This method may only be called when change counting is not already active.
        /// </summary>
        /// <remarks>
        /// <para>Calling Start() may enable or reconfigure interrupts for the pin.</para>
        /// <para>The following exceptions can be thrown by this method:</para>
        /// <remarks>The following exception can be thrown by this method:
        /// <list type="bullet">
        /// <item><term>E_INVALID_OPERATION : Change counting has already been started.</term></item>
        /// <item><term>E_OBJECT_DISPOSED : The change counter or the associated pin has been disposed.</term></item>
        /// </list>
        /// </remarks>
        public void Start()
        {
            lock (_syncLock)
            {
                if (_disposedValue) { throw new ObjectDisposedException(); }

                CheckIfActive(true);

                _countActive = true;

                NativeStart();
            }
        }

        /// <summary>
        /// Stop counting changes in pin polarity. This method may only be called when change counting is currently active.
        /// </summary>
        /// <remarks>
        /// <para>Calling Stop() may enable or reconfigure interrupts for the pin.</para>
        /// <para>The following exceptions can be thrown by this method:</para>
        /// <list type="bullet">
        /// <item><term>E_INVALID_OPERATION : Change counting has not been started.</term></item>
        /// <item><term>E_OBJECT_DISPOSED : The change counter or the associated pin has been disposed.</term></item>
        /// </list>
        /// </remarks>
        public void Stop()
        {
            lock (_syncLock)
            {
                if (_disposedValue) { throw new ObjectDisposedException(); }

                CheckIfActive(false);

                _countActive = false;

                NativeStop();
            }
        }


        private void CheckIfActive(bool state)
        {
            if (_countActive == state)
            {
                throw (new InvalidOperationException());
            }
        }


        #region IDisposable Support

        private bool _disposedValue = false; 

        void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                NativeDispose();

                _disposedValue = true;
            }
        }

        #pragma warning disable 1591
        ~GpioChangeCounter()
        {
           Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            lock (_syncLock)
            {
                Dispose(true);

                GC.SuppressFinalize(this);
            }
        }
        #endregion


        #region Native
        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeInit();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern GpioChangeCount NativeRead(bool Reset);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeStart();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeStop();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeDispose();
        #endregion

    }
}
