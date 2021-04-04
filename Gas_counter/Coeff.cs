using HelperClass;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Gas_counter
{
	class Coeff : INotifyPropertyChanged
	{

		public static event EventHandler<EventArgs> Write_;
		public DelegateCommand Write_coef { get; private set; }

		private uint _coeff;
		public uint coeff
		{
			get => _coeff;
			set
			{
				if (_coeff != value)
				{
					_coeff = value;
					OnPropertyChanged();
				}
			}
		}

		private string _name_coeff;
		public string name_coeff
		{
			get => _name_coeff;
			set
			{
				if (_name_coeff != value)
				{
					_name_coeff = value;
					OnPropertyChanged();
				}
			}
		}

		public	Coeff(string name) 
		{
			_name_coeff = name;
			Write_coef = new DelegateCommand(write);
		}

		private void write(object obj) 
		{
			Write_?.Invoke(this, null);
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged([CallerMemberName] string prop = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
		}
	}
}
