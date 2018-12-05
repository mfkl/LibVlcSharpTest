using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibVlcSharpTest.ViewModels;
using LibVLCSharp.Shared;
using Rg.Plugins.Popup.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace LibVlcSharpTest.Popups
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AuthenticationForm : Rg.Plugins.Popup.Pages.PopupPage
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool Store { get; set; }
        public bool IsCompleted { get; set; }

        private readonly AuthenticationFormViewModel _authenticationFormViewModel;

        public AuthenticationForm(AuthenticationFormViewModel authenticationFormViewModel)
        {
            InitializeComponent();

            _authenticationFormViewModel = authenticationFormViewModel;

            BindingContext = authenticationFormViewModel;
        }

        private async void Ok_OnClicked(object sender, EventArgs e)
        {
            if (await _authenticationFormViewModel.Validate())
            {
                Username = FormUsername.Text;
                Password = FormPassword.Text;
                Store = FormStore.IsToggled;
                IsCompleted = true;
                await PopupNavigation.Instance.PopAsync();
            }
        }
    }
}