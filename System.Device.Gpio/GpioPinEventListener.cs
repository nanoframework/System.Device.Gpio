﻿//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using nanoFramework.Runtime.Events;
using System.Collections;

namespace System.Device.Gpio
{
    internal class GpioPinEventListener : IEventProcessor, IEventListener
    {
        // Map of pin numbers to GpioPin objects.
        private readonly ArrayList _pinMap = new ArrayList();

        public GpioPinEventListener()
        {
            EventSink.AddEventProcessor(EventCategory.Gpio, this);
            EventSink.AddEventListener(EventCategory.Gpio, this);
        }

        public BaseEvent ProcessEvent(uint data1, uint data2, DateTime time)
        {
            return new GpioPinEvent
            {
                // Data1 is packed by PostManagedEvent, so we need to unpack the high word.
                PinNumber = (int)(data1 >> 16),
                EventType = (data2 == 0) ? PinEventTypes.Falling : PinEventTypes.Rising,
            };
        }

        public void InitializeForEventSource()
        {
        }

        public bool OnEvent(BaseEvent ev)
        {
            var pinEvent = (GpioPinEvent)ev;
            GpioPin pin = null;

            lock (_pinMap.SyncRoot)
            {
                pin = FindGpioPin(pinEvent.PinNumber);
            }

            // Avoid calling this under a lock to prevent a potential lock inversion.
            if (pin != null)
            {
                pin.OnPinChangedInternal(pinEvent.EventType);
            }

            return true;
        }

        public void AddPin(GpioPin pin)
        {
            lock (_pinMap.SyncRoot)
            {
                _pinMap.Add(pin);
            }
        }

        public void RemovePin(int pinNumber)
        {
            lock (_pinMap.SyncRoot)
            {
                var pin = FindGpioPin(pinNumber);

                if (pin != null)
                {
                    _pinMap.Remove(pin);
                }
            }
        }

        private GpioPin FindGpioPin(int number)
        {
            for (int i = 0; i < _pinMap.Count; i++)
            {
                if (((GpioPin)_pinMap[i]).PinNumber == number)
                {
                    return (GpioPin)_pinMap[i];
                }
            }

            return null;
        }
    }
}
