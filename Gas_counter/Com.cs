using System;
using System.IO.Ports;
using System.Threading;
using System.Timers;
using System.Windows;

namespace Gas_count
{
	class Com
	{
		public static event EventHandler<EventArgs> Disc;

		public delegate void Reciver_packet_handler(string angle, string temp);

		public static event Reciver_packet_handler Reciver_packet;

		private static SerialPort _serialPort = new SerialPort();

		bool counter_wakeup;

		System.Timers.Timer timer;

		bool mutex = false;

		bool sleep = true;

		public Com() 
		{
			_serialPort.BaudRate = 9600;
			timer = new System.Timers.Timer(15000);
			timer.Elapsed += new ElapsedEventHandler(wakeup_timeuot);
			timer.Stop();
			Disc += Com_Disc;
			_serialPort.DataReceived += _serialPort_DataReceived;
		}

		private void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			if (!mutex)
			{
				try
				{
					byte[] data = new byte[20];

					for (int i = 0; i < data.Length; i++)
					{
						data[i] = (byte)_serialPort.ReadByte();
					}
					if (data[0] == 0xAB && data[data.Length - 1] == crc(data))
					{
						int angle = data[9];
						angle |= data[10] << 8;
						angle |= data[11] << 16;
						angle |= data[12] << 24;
						float fangle = angle / 100000.0f;

						int temp = data[13];
						temp |= (data[14] & 0x7F) << 8;
						float ftemp = temp / 10.0f;

						Reciver_packet?.Invoke(string.Format("{0:f4}", fangle), string.Format("{0:f1}", ftemp)); 
					}
				}
				catch (Exception)
				{
				}

			}
		}

		private void Com_Disc(object sender, EventArgs e)
		{
			timer.Stop();
			_serialPort.Close();
		}

		private byte crc(byte[] data) 
		{
			byte _crc = 0;

			for (int i = 0; i < data.Length - 1; i++)
			{
				_crc ^= data[i];
			}

			return _crc;
		}

		public bool set_name(string name) 
		{
			try
			{
				if (_serialPort.IsOpen)
				{
					Disc?.Invoke(this, null);
				}
				else
				{
					_serialPort.PortName = name;
					_serialPort.Open();
					wakeup_counter();
					if (counter_wakeup) 
					{
//						timer.Start();
					}
				}
			}
			catch (Exception)
			{
			}

			return _serialPort.IsOpen;
		}

		private void wakeup_counter()
		{
			mutex = true;

			if (_serialPort.IsOpen)
			{
				if (!counter_wakeup)
				{
					int i;
					byte[] send = new byte[2];
					send[0] = (byte)0xAA;
					send[1] = (byte)0xAA;

					_serialPort.ReadTimeout = 20;
					_serialPort.DiscardInBuffer();
					for (i = 0; i < 100; i++)
					{
						try
						{
							_serialPort.Write(send, 0, 2);
							if (_serialPort.ReadByte() > 0x00) { i = 1000; }
						}
						catch (Exception E)
						{
						}
					}

					_serialPort.ReadTimeout = 350;
					_serialPort.DiscardInBuffer();

					for (i = 0; i < 5; i++)
					{
						try
						{
							_serialPort.Write(send, 0, 1);
							if (_serialPort.ReadByte() == 0xDD) { i = 1000; }

						}
						catch (Exception E)
						{
						}
					}
					if (i >= 999)
					{
						counter_wakeup = true;
						wakeup_timeuot_reset();
					}
					else
					{
						counter_wakeup = false;
					}
				}
			}
			else
			{
				counter_wakeup = false;
			}

			mutex = false;
		}

		private void wakeup_timeuot(object source, ElapsedEventArgs e) 
		{
			counter_wakeup = false;
		}

		private void wakeup_timeuot_reset() 
		{
			timer.Stop();
			timer.Start();
		}

		private void counter_synchro() 
		{
			byte[] send = new byte[1];
			send[0] = (byte)0xAA;
			mutex = true;
			if (_serialPort.IsOpen)
			{
				_serialPort.ReadTimeout = 350;
				_serialPort.DiscardInBuffer();

				int i = 0;
				for (; i < 3; i++)
				{
					try
					{
						_serialPort.Write(send, 0, send.Length);
						if (_serialPort.ReadByte() == 0xDD) { i = 1000; }
					}
					catch (Exception E)
					{
					}
				}
				if (i < 999)
				{
					Disc?.Invoke(this, null);
				}
			}
			mutex = false;
		}

		private bool get_coeff(byte adr, ref uint result) 
		{
			bool status = false;
			
			wakeup_timeuot_reset();
			wakeup_counter();
			mutex = true;
			try
			{
				_serialPort.DiscardInBuffer();
				if (adr <= 16)
				{
					byte[] send = new byte[4];

					send[0] = (byte)0xAB;
					send[1] = (byte)0x32;
					send[2] = (byte)(adr * 4);
					send[3] = (byte)0x04;

					_serialPort.Write(send, 0, send.Length);
				}

				byte[] data_in = new byte[4];

				UInt32 buf = 0;

				for (int i = 0; i < data_in.Length; i++)
				{
					data_in[i] |= (byte)_serialPort.ReadByte();
					buf |= (UInt32)(data_in[i] << i * 8);
				}

				if (crc(data_in) == (byte)_serialPort.ReadByte())
				{
					status = true;
					result = buf;
				}
				else 
				{
					MessageBox.Show("Error CRC");
				}
			}
			catch (Exception E)
			{
				counter_synchro();
				status = false;
			}
			mutex = false;
			return status;
		}

		private bool set_coeff(byte adr, uint data) 
		{
			bool result = false;
			
			wakeup_timeuot_reset();
			wakeup_counter();
			mutex = true;
			try
			{
				_serialPort.DiscardInBuffer();
				if (adr <= 16)
				{
					byte[] send = new byte[7];

					send[0] = (byte)0xAB;
					send[1] = (byte)0x25;
					send[2] = (byte)(adr * 4);
					send[3] = (byte)data;
					send[4] = (byte)(data >> 8);
					send[5] = (byte)(data >> 16);
					send[6] = (byte)(data >> 24);

					_serialPort.Write(send, 0, send.Length);
				}

				byte[] data_in = new byte[2];

				UInt32 buf = 0;

				for (int i = 0; i < data_in.Length; i++)
				{
					data_in[i] |= (byte)_serialPort.ReadByte();
					buf |= (UInt32)(data_in[i] << i * 8);
				}

				if (buf == 0xABAB && get_coeff(adr, ref buf) && (buf == data - 1 || buf == data || buf == data + 1))
				{
					result = true;
				}
				else
				{
					result = false;
				}
			}
			catch (Exception)
			{
				result = false;
			}
			mutex = false;
			return result;
		}

		public UInt32 get_coef_L(byte number)
		{
			UInt32 result = 0;
			if (_serialPort.IsOpen == false || number > 8)
			{
				result = 0;
			}
			else
			{
				for (int i = 0; i < 2; i++)
				{
					if (get_coeff((byte)(number + 8), ref result)) 
					{
						break;
					}
				}
			}
			return result;
		}

		public UInt32 get_coef_A(byte number)
		{
			UInt32 result = 0;
			if (_serialPort.IsOpen == false || number > 8) 
			{
				result = 0;
			}
			else 
			{
				for (int i = 0; i < 2; i++)
				{
					if (get_coeff((byte)(number), ref result)) 
					{
						break;
					}
				}
			}
			return result;
		}

		public bool set_coef_L(byte number, UInt32 val)
		{
			if (_serialPort.IsOpen == false || counter_wakeup == false || number > 8) { return false; }

			return set_coeff((byte)(number + 8), val);
		}

		public bool set_coef_A(byte number, UInt32 val)
		{
			if (_serialPort.IsOpen == false || counter_wakeup == false || number > 8) { return false; }

			return set_coeff((byte)(number), val);
		}

		public bool apply_coef() 
		{
			bool result = false;

			wakeup_timeuot_reset();
			wakeup_counter();
			mutex = true;
			try
			{
				_serialPort.DiscardInBuffer();
				byte[] send = new byte[2];

				send[0] = (byte)0xAB;
				send[1] = (byte)0x60;

				_serialPort.Write(send, 0, send.Length);

				byte[] data_in = new byte[2];

				UInt32 buf = 0;

				for (int i = 0; i < data_in.Length; i++)
				{
					data_in[i] |= (byte)_serialPort.ReadByte();
					buf |= (UInt32)(data_in[i] << i * 8);
				}

				if (buf == 0xABAB)
				{
					MessageBox.Show("Apply OK!");
				}
				else
				{
					MessageBox.Show("Apply error!");
				}


			}
			catch (Exception E)
			{
				MessageBox.Show("Apply error!");
				counter_synchro();
			}

			mutex = false;
			return result;
		}

	}
}