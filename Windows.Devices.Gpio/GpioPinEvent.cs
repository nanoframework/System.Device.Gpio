//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using nanoFramework.Runtime.Events;

namespace Windows.Devices.Gpio
{
    internal class GpioPinEvent : BaseEvent
    {
        public int PinNumber;
        public GpioPinEdge Edge;
    }
}
