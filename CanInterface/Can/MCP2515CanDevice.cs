﻿using CanInterface.MCP2515;
using CanInterface.MCP2515.Enum;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CanInterface.Can
{
    /// <summary>
    /// Manages the Controller to recieve messages by polling the controller
    /// </summary>
    public class MCP2515CanDevice : IDisposable
    {
        /// <summary>
        /// Invoked when a message is read from the controller
        /// </summary>
        public EventHandler<CanMessageEvent> CanMessageRecieved;

        protected Task ReadTask = null;
        protected CancellationTokenSource ReadTaskCancellationToken = null;

        protected Task WriteTask = null;
        protected CancellationTokenSource WriteTaskCancellationToken = null;
        protected ConcurrentQueue<(CanMessage, int)> TransmitQueue = new ConcurrentQueue<(CanMessage, int)>();
        protected ManualResetEventSlim TransmitWait = new ManualResetEventSlim(false);

        /// <summary>
        /// The controller used to communicate with the can network
        /// </summary>
        public IController Controller { get; protected set; }
        /// <summary>
        /// The amount of time to wait betweem polling for read messages (Default: 100 ms).
        /// </summary>
        public TimeSpan ReadPollingWaitPeriod { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Creates an instance of the <see cref="MCP2515CanDevice"/>
        /// </summary>
        /// <param name="controller">The controller to communicate with</param>
        public MCP2515CanDevice(IController controller)
        {
            Controller = controller ?? throw new ArgumentNullException(nameof(controller));

            ReadTaskCancellationToken = new CancellationTokenSource();
            ReadTask = new Task(ReceiveWorker, (ReadTaskCancellationToken.Token, controller, ReadPollingWaitPeriod), ReadTaskCancellationToken.Token, TaskCreationOptions.LongRunning);

            WriteTaskCancellationToken = new CancellationTokenSource();
            WriteTask = new Task(TransmitWorker, (WriteTaskCancellationToken.Token, controller, TransmitWait, TransmitQueue), WriteTaskCancellationToken.Token, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Start polling to receive data from the controller
        /// </summary>
        public void StartReceiving() => ReadTask.Start();

        /// <summary>
        /// Starts the transmit worker
        /// </summary>
        public void StartTransmitting() => WriteTask.Start();

        /// <summary>
        /// Adds a message to be transmitted.
        /// </summary>
        /// <param name="message"></param>
        public void Transmit(CanMessage message)
        {
            if(WriteTask.Status == TaskStatus.Created)
            {
                WriteTask.Start();
            }

            TransmitQueue.Enqueue((message, 0));
            TransmitWait.Set();
        }

        protected void ReceiveWorker(object sync)
        {
            (CancellationToken token, IController controller, TimeSpan readWaitTime) = ((CancellationToken, IController, TimeSpan))sync;

            var wait = new ManualResetEventSlim(false);

            bool read = false;
            CanMessage message = null;
            while(!token.IsCancellationRequested)
            {
                var interrupts = controller.ReadInteruptFlags();
                
                if (interrupts.RX0IF && ((read, message) = Controller.Receive(ReceiveBuffer.RX0)).Item1)
                {
                    CanMessageRecieved?.Invoke(this, new CanMessageEvent(message, ReceiveBuffer.RX0));
                }

                if (interrupts.RX1IF && ((read, message) = Controller.Receive(ReceiveBuffer.RX1)).Item1)
                {
                    CanMessageRecieved?.Invoke(this, new CanMessageEvent(message, ReceiveBuffer.RX1));
                }

                try
                {
                    //use a eventwait to sleep as we do not have access to thread.sleep
                    wait.Wait(readWaitTime, token);
                }
                catch(OperationCanceledException)
                { 
                    //do nothing
                }
                
            }
        }

        protected void TransmitWorker(object sync)
        {
            (CancellationToken token, IController controller, ManualResetEventSlim waitForWork, ConcurrentQueue<(CanMessage, int)> messages) = ((CancellationToken, IController, ManualResetEventSlim, ConcurrentQueue<(CanMessage, int)>))sync;


            while (!token.IsCancellationRequested)
            {
                if(messages.TryPeek(out (CanMessage Message, int FailedCount) messageToTransmit))
                {
                    if(messageToTransmit.FailedCount > 5 || controller.Transmit(messageToTransmit.Message))
                    {
                        //just need to 
                        messages.TryDequeue(out (CanMessage,int) _);
                    }
                    else
                    {
                        messageToTransmit.FailedCount++;
                    }
                }
                else
                {
                    waitForWork.Reset();
                }


                try
                {
                    waitForWork.Wait(token);
                }
                catch (OperationCanceledException)
                {
                    //do nothing
                }

            }
        }

        public Task Transmit(CanMessage message, TimeSpan? timeout = null)
        {
            return Task.Run(() => { Transmit(message); });
        }
        
        public void Dispose()
        {
            ReadTaskCancellationToken.Cancel();
            if(!ReadTask.Wait(TimeSpan.FromSeconds(5)))
            {
                throw new TimeoutException("Timeout waiting for read thread to shutdown");
            }

            WriteTaskCancellationToken.Cancel();
            if(!WriteTask.Wait(TimeSpan.FromSeconds(5)))
            {
                throw new TimeoutException("Timeout waiting for write thread to shutdown");
            }
        }
        
    }
}
