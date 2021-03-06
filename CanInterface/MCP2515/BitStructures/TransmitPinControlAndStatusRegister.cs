﻿using CanInterface.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanInterface.MCP2515.BitStructures
{
    public struct TransmitPinControlAndStatusRegister
    {
        /// <summary>
        /// TX2RTS Pin State bit
        /// - Reads state of TX2RTS pin when in Digital Input mode
        /// - Reads as ‘0’ when pin is in ‘Request-to-Send’ mode
        /// </summary>
        public bool B2RTS;
        /// <summary>
        ///  TX1RTX Pin State bit
        /// - Reads state of TX1RTS pin when in Digital Input mode
        /// - Reads as ‘0’ when pin is in ‘Request-to-Send’ mode
        /// </summary>
        public bool B1RTS;
        /// <summary>
        /// TX0RTS Pin State bit
        /// - Reads state of TX0RTS pin when in Digital Input mode
        /// - Reads as ‘0’ when pin is in ‘Request-to-Send’ mode
        /// </summary>
        public bool B0RTS;
        /// <summary>
        /// TX2RTS Pin mode bit
        /// 1 = Pin is used to request message transmission of TXB2 buffer(on falling edge)
        /// 0 = Digital input
        /// </summary>
        public bool B2RTSM;
        /// <summary>
        ///  TX1RTS Pin mode bit
        /// 1 = Pin is used to request message transmission of TXB1 buffer(on falling edge)
        /// 0 = Digital input
        /// </summary>
        public bool B1RTSM;
        /// <summary>
        /// TX0RTS Pin mode bit
        /// 1 = Pin is used to request message transmission of TXB0 buffer(on falling edge)
        /// 0 = Digital input
        /// </summary>
        public bool B0RTSM;


        public TransmitPinControlAndStatusRegister(byte value)
        {
            (_,_, B2RTS, B1RTS, B0RTS, B2RTSM, B1RTSM, B0RTSM) = value.GetBits();
        }

        public TransmitPinControlAndStatusRegister(bool b2rtsm, bool b1rtsm, bool b0rtsm)
        {
            B2RTS = false;
            B1RTS = false;
            B0RTS = false;
            B2RTSM = b2rtsm;
            B1RTSM = b1rtsm;
            B0RTSM = b0rtsm;
        }

        public byte ToByte()
        {
            return ((byte)(0b0000_0000)).SetBits(false, false, false, false, false, B2RTSM, B1RTSM, B0RTSM);
        }

        public static implicit operator byte(TransmitPinControlAndStatusRegister register)
        {
            return register.ToByte();
        }

        public static implicit operator TransmitPinControlAndStatusRegister(byte register)
        {
            return new TransmitPinControlAndStatusRegister(register);
        }
    }
}
