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
	    public bool IsAuthenticated;

	    private readonly AuthenticationFormViewModel _authenticationFormViewModel;
	    private readonly Dialog _dialog;

	    public AuthenticationForm(AuthenticationFormViewModel authenticationFormViewModel, Dialog dialog)
	    {
	        InitializeComponent();

	        _authenticationFormViewModel = authenticationFormViewModel;
	        _dialog = dialog;

	        BindingContext = authenticationFormViewModel;
	    }

	    private async void Ok_OnClicked(object sender, EventArgs e)
	    {
	        if (await _authenticationFormViewModel.Validate())
	        {
	            _dialog.PostLogin(_authenticationFormViewModel.Username, _authenticationFormViewModel.Password, _authenticationFormViewModel.Store);

	            IsAuthenticated = true;
	            await PopupNavigation.Instance.PopAsync();
	        }
	    }
	}
}