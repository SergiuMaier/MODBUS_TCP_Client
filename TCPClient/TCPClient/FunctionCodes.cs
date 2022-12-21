﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPClient
{
    public class FunctionCodes
    {
        public const byte slaveIdLength = 0x01;
        public const byte functionCodeLength = 0x01;
        public const byte firstAddressLength = 0x02;
        public const byte numberOfRegistersLength = 0x02;
        public const byte numberBytesToFollow = 0x01;

        public static void addTwoBytesToBuffer(byte[] buffer, short number, int indexBuffer)
        {
            buffer[indexBuffer] = (byte)(number >> 8);
            buffer[indexBuffer + 1] = (byte)(number);
        }

        //fc 03
        public void readHoldingRegisters(byte[] buffer, string transactionText, short protocolId, byte slaveId, byte functionCode, string firstAddressText, string numberOfRegistersText)
        {
            short transactionId = short.Parse(transactionText, NumberStyles.HexNumber);
            addTwoBytesToBuffer(buffer, transactionId, 0);
            
            addTwoBytesToBuffer(buffer, protocolId, 2);

            short firstAddress = short.Parse(firstAddressText, NumberStyles.HexNumber);
            short numberOfRegisters = short.Parse(numberOfRegistersText, NumberStyles.HexNumber);
            
            short lengthOfMessage = (short)(slaveIdLength + functionCodeLength + firstAddressLength + numberOfRegistersLength);
            addTwoBytesToBuffer(buffer, lengthOfMessage, 4);

            buffer[6] = slaveId;
            buffer[7] = functionCode;
            addTwoBytesToBuffer(buffer, firstAddress, 8);
            addTwoBytesToBuffer(buffer, numberOfRegisters, 10);
        }

        //fc 06
        public void presetSingleRegister(byte[] buffer, string transactionText, short protocolId, byte slaveId, byte functionCode, string firstAddressText, string registerValueText)
        {
            short transactionId = short.Parse(transactionText, NumberStyles.HexNumber);
            addTwoBytesToBuffer(buffer, transactionId, 0);
            addTwoBytesToBuffer(buffer, protocolId, 2);

            short firstAddress = short.Parse(firstAddressText, NumberStyles.HexNumber);
            short registerValue = short.Parse(registerValueText, NumberStyles.HexNumber);

            short lengthOfMessage = (short)(slaveIdLength + functionCodeLength + firstAddressLength + numberOfRegistersLength);
            addTwoBytesToBuffer(buffer, lengthOfMessage, 4);
            
            buffer[6] = slaveId;
            buffer[7] = functionCode;
            addTwoBytesToBuffer(buffer, firstAddress, 8);
            addTwoBytesToBuffer(buffer, registerValue, 10);
        }

        //fc 16
        public void presetMultipleRegisters(byte[] buffer, string transactionText, short protocolId, byte slaveId, byte functionCode, string firstAddressText, string numberOfRegistersText, string registerValueText)
        {
            short firstAddress = short.Parse(firstAddressText, NumberStyles.HexNumber);
            short numberOfRegisters = short.Parse(numberOfRegistersText, NumberStyles.HexNumber);
            short[] registerValue = registerValueText.Split(' ')
                        .Select(hex => short.Parse(hex, NumberStyles.HexNumber))
                        .ToArray();
            
            short lengthOfMessage16 = (short)(slaveIdLength + functionCodeLength + firstAddressLength + numberOfRegistersLength + numberBytesToFollow + 2 * registerValue.Length);

            short transactionId = short.Parse(transactionText, NumberStyles.HexNumber);
            addTwoBytesToBuffer(buffer, transactionId, 0);
            addTwoBytesToBuffer(buffer, protocolId, 2);

            addTwoBytesToBuffer(buffer, transactionId, 0);
            addTwoBytesToBuffer(buffer, protocolId, 2);
            addTwoBytesToBuffer(buffer, lengthOfMessage16, 4);
            buffer[6] = slaveId;
            buffer[7] = functionCode;
            addTwoBytesToBuffer(buffer, firstAddress, 8);
            addTwoBytesToBuffer(buffer, numberOfRegisters, 10);

            int indexNumber = 13;
            byte nextBytes = 0;
            foreach (short element in registerValue)
            {
                addTwoBytesToBuffer(buffer, element, indexNumber);
                indexNumber += 2;
                nextBytes += 2;
            }

            buffer[12] = nextBytes;
        }
    }
}
