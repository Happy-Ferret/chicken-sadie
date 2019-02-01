using System;
using Android.OS;
using Android.App;
using Android.Content;

namespace chickens
{
	public abstract class BindingActivity<S, I> : Activity where S : Service where I : class
	{
		protected string TAG { get { return this.GetType().Name; }}

		protected ServiceConnection<S,I> serviceConnection;
		public ServiceBinder<S> Binder { get; set; }

		private bool isBound;
		public bool IsBound {
			get { return isBound; } 
			set
			{
				if (!isBound && value)
				{
					OnServiceConnected();
				}
				if (isBound && !value)
				{
					OnServiceDisconnecting();
				}
				isBound = value;
			}
		}

		protected override void OnResume()
		{
			base.OnResume();
			if (!IsBound)
			{
				BindToService();
			}
		}

		protected override void OnPause()
		{
			if (IsBound)
			{
				UnbindFromService(); 
			}
			base.OnPause();
		}

		public I Service
		{
			get
			{
				if (Binder != null) { return Binder.GetService() as I; } else { return null; }
			}
		}

		protected void BindToService()
		{
			var intent = new Intent(this, typeof(S));
			serviceConnection = new ServiceConnection<S,I>(this);
			BindService(intent, serviceConnection, Bind.AutoCreate);
		}

		protected void UnbindFromService()
		{
			if (IsBound)
			{
				UnbindService(serviceConnection);
				IsBound = false;
			}
		}

		protected abstract void OnServiceConnected();
		protected abstract void OnServiceDisconnecting();
	}
}

