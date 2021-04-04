using Gas_count;
using HelperClass;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Gas_counter
{
	class MainVM : DependencyObject
	{
		public ObservableCollection<string> Ports { get; private set; }
		public ObservableCollection<Coeff> L_coef { get; private set; } = new ObservableCollection<Coeff>();
		public ObservableCollection<Coeff> A_coef { get; private set; } = new ObservableCollection<Coeff>();

		public DelegateCommand OpenPort { get; private set; }

		public DelegateCommand Read_all { get; private set; }
		public DelegateCommand Read_L { get; private set; }
		public DelegateCommand Read_A { get; private set; }

		public DelegateCommand Write_all { get; private set; }
		public DelegateCommand Write_L { get; private set; }
		public DelegateCommand Write_A { get; private set; }

		public DelegateCommand Apply_all { get; private set; }

		public string Port
		{
			get { return (string)GetValue(PortProperty); }
			set { SetValue(PortProperty, value); }
		}
		public static readonly DependencyProperty PortProperty =
			DependencyProperty.Register("Port", typeof(string), typeof(MainVM), new PropertyMetadata(""));

		public bool PortIsOpen
		{
			get { return (bool)GetValue(PortIsOpenProperty); }
			set { SetValue(PortIsOpenProperty, value); }
		}
		public static readonly DependencyProperty PortIsOpenProperty =
			DependencyProperty.Register("PortIsOpen", typeof(bool), typeof(MainVM), new PropertyMetadata(false));

		public bool PortIsClose
		{
			get { return (bool)GetValue(PortIsCloseProperty); }
			set { SetValue(PortIsCloseProperty, value); }
		}
		public static readonly DependencyProperty PortIsCloseProperty =
			DependencyProperty.Register("PortIsClose", typeof(bool), typeof(MainVM), new PropertyMetadata(true));

		public string Temperatura
		{
			get { return (string)GetValue(TemperaturaProperty); }
			set { SetValue(TemperaturaProperty, value); }
		}
		public static readonly DependencyProperty TemperaturaProperty =
			DependencyProperty.Register("Temperatura", typeof(string), typeof(MainVM), new PropertyMetadata(""));

		public string Angle
		{
			get { return (string)GetValue(AngleProperty); }
			set { SetValue(AngleProperty, value); }
		}
		public static readonly DependencyProperty AngleProperty =
			DependencyProperty.Register("Angle", typeof(string), typeof(MainVM), new PropertyMetadata(""));




		private Com com = new Com();

		public MainVM() 
		{

			get_port();

			OpenPort = new DelegateCommand(openPort);

			Read_all = new DelegateCommand(read_all_coeff);
			Read_L = new DelegateCommand(read_l_coeff);
			Read_A = new DelegateCommand(read_a_coeff);

			Write_all = new DelegateCommand(write_all_coeff);
			Write_L = new DelegateCommand(write_l_coeff);
			Write_A = new DelegateCommand(write_a_coeff);

			Apply_all = new DelegateCommand(apply_all_coeff);

			Coeff.Write_ += Coeff_Write_;
			Com.Reciver_packet += Com_Reciver_packet;
			Com.Disc += Com_Disc;

			for (int i = 0; i < 8; i++)
			{
				L_coef.Add(new Coeff(nameof(L_coef) + " " + i.ToString()));
			}

			for (int i = 0; i < 8; i++)
			{
				A_coef.Add(new Coeff(nameof(A_coef) + " " + i.ToString()));
			}

		}

		private void Com_Reciver_packet(string angle, string temp)
		{

			Dispatcher.Invoke(() =>
			{
				Angle = angle;
				Temperatura = temp;
			}
			);


		}

		private void get_port() 
		{
			Ports = new ObservableCollection<string>(SerialPort.GetPortNames());
			Ports.Add("");
		}

		private void Com_Disc(object sender, EventArgs e)
		{
			Dispatcher.Invoke(()=>
			{
				PortIsOpen = false;
				PortIsClose = !PortIsOpen;
				read_all_coeff(null);
				MessageBox.Show("Disconnect!!!");
			}
			);
		}

		private void Coeff_Write_(object sender, EventArgs e)
		{
			Coeff coeff = sender as Coeff;

			if (coeff != null) 
			{
				byte adr =(byte)(L_coef.Count - byte.Parse(coeff.name_coeff.Substring(7)) - 1);

				byte index = byte.Parse(coeff.name_coeff.Substring(7));

				if (coeff.name_coeff.StartsWith(nameof(L_coef)))
				{
					if (com.set_coef_L((byte)adr, L_coef[index].coeff))
					{
						MessageBox.Show("Write OK!!!");
					}
					else 
					{
						MessageBox.Show("Write Error!!!");
					}
				}
				else 
				{
					if (coeff.name_coeff.StartsWith(nameof(A_coef)))
					{
						if (com.set_coef_A((byte)adr, A_coef[index].coeff))
						{
							MessageBox.Show("Write OK!!!");
						}
						else 
						{
							MessageBox.Show("Write Error!!!");
						}
					}
					else 
					{
						MessageBox.Show("Write Error!!!");
					}
				}
			}

		}

		private void openPort(object obj)
		{
			if (!string.IsNullOrEmpty(Port))
			{
				PortIsOpen = com.set_name(Port);
				PortIsClose = !PortIsOpen;
				read_all_coeff(null);
			}
			else 
			{
				get_port();
			}
		}

		private void read_l_coeff(object obj)
		{
			int j = 0;
			for (int i = A_coef.Count - 1; i >= 0; i--, j++)
			{
				L_coef[j].coeff = com.get_coef_L((byte)i);
			}
		}

		private void read_a_coeff(object obj)
		{
			int j = 0;
			for (int i = A_coef.Count - 1; i >= 0; i--, j++)
			{
				A_coef[j].coeff = com.get_coef_A((byte)i);
			}
		}

		private void read_all_coeff(object obj)
		{
			read_l_coeff(null);
			read_a_coeff(null);
		}

		private void write_all_coeff(object obj)
		{
			write_l_coeff(null);
			write_a_coeff(null);
		}

		private void write_l_coeff(object obj)
		{
			int j = 0;
			int i = L_coef.Count - 1;

			for (; i >= 0; i --, j++)
			{
				if (!com.set_coef_L((byte)i, L_coef[j].coeff))
				{
					break;
				}
			}


			if (i < 0)
			{
				MessageBox.Show("Write Ok!");
			}
			else
			{
				MessageBox.Show("Write error!");
			}
		}

		private void write_a_coeff(object obj)
		{
			int j = 0;
			int i = A_coef.Count - 1;

			for (; i >= 0; i--, j++)
			{
				if (!com.set_coef_A((byte)i, A_coef[j].coeff)) 
				{
					break;
				}
			}

			if (i < 0)
			{
				MessageBox.Show("Write Ok!");
			}
			else 
			{
				MessageBox.Show("Write error!");
			}
		}

		private void apply_all_coeff(object obj)
		{
			com.apply_coef();
		}
	}
}
