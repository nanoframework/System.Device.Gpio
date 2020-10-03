using System;
using System.Threading;
using System.Diagnostics;
using System.Device.Gpio;

namespace GpioTest
{
    public class Program
    {
        public static void Main()
        {
            Debug.WriteLine("Hello from nanoFramework!");

            GpioController controller = new GpioController();
            controller.OpenPin(18, PinMode.Output);
            controller.OpenPin(26, PinMode.InputPullUp);
            controller.RegisterCallbackForPinValueChangedEvent(26, PinEventTypes.Rising, PinChangedCallback);
            controller.OpenPin(32, PinMode.InputPullUp);

            while(true)
            {
                controller.Write(18, PinValue.High);
                Thread.Sleep(500);
                controller.Write(18, PinValue.Low);
                Thread.Sleep(500);
                var res = controller.WaitForEvent(32, PinEventTypes.Falling, new TimeSpan(0, 0, 5));
                if (res.TimedOut)
                {
                    Debug.WriteLine("no event, timeout");
                }
                else
                {
                    Debug.WriteLine($"Event: {res.EventTypes}");
                }
            }

            Thread.Sleep(Timeout.Infinite);

            
        }

        private static void PinChangedCallback(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            Debug.WriteLine($"Pin Number: {pinValueChangedEventArgs.PinNumber}, event: {pinValueChangedEventArgs.ChangeType}");
        }
    }
}
