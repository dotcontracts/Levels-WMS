using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using wms_android.Interfaces;

namespace wms_android.ViewModels
{
    public class BaseViewModel : ObservableObject
    {
        private string _title;
        private bool _isBusy;
        protected ISessionManager? SessionManager { get; private set; }

        public BaseViewModel()
        {
        }

        public BaseViewModel(ISessionManager sessionManager) : this()
        {
            SessionManager = sessionManager;
        }

        protected async Task RefreshSessionActivity()
        {
            try
            {
                if (SessionManager != null)
                    await SessionManager.UpdateActivityAsync();
            }
            catch { }
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

    }
}
