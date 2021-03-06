﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using Uno.AzureDevOps.Business;
using Uno.AzureDevOps.Business.Authentication;
using Uno.AzureDevOps.Business.Extensions;
using Uno.AzureDevOps.Business.VSTS;
using Uno.AzureDevOps.Client;
using Uno.AzureDevOps.Framework.AppVersion;
using Uno.AzureDevOps.Framework.Commands;
using Uno.AzureDevOps.Framework.Navigation;
using Uno.AzureDevOps.Framework.Tasks;
using Uno.AzureDevOps.Views.Content;
using Windows.UI.Popups;

namespace Uno.AzureDevOps.Presentation
{
	[Windows.UI.Xaml.Data.Bindable]
	public class SideMenuViewModel : ViewModelBase
	{
		private readonly IStackNavigationService _navigationService;
		private readonly IUserPreferencesService _userPreferencesService;
		private readonly IVSTSRepository _vstsRespository;
		private readonly IAuthenticationService _authenticationService;
		private ITaskNotifier<UserProfile> _userProfile;
		private AccountData _account;

		public SideMenuViewModel()
		{
			_navigationService = SimpleIoc.Default.GetInstance<IStackNavigationService>();
			_userPreferencesService = SimpleIoc.Default.GetInstance<IUserPreferencesService>();
			_vstsRespository = SimpleIoc.Default.GetInstance<IVSTSRepository>();
			_authenticationService = SimpleIoc.Default.GetInstance<IAuthenticationService>();

			UserProfile = new TaskNotifier<UserProfile>(_vstsRespository.GetUserProfile());
			Logout = new AsyncCommand(async () => await LogoutPrompt());
			Account = _userPreferencesService.GetPreferredAccount();

			ToProfilePage = new RelayCommand(() => _navigationService.ToProfilePage());
			ToAboutPage = new RelayCommand(() => _navigationService.ToAboutPage());
			ToOrganizationListPage = new RelayCommand(() => _navigationService.NavigateToAndClearStack(nameof(OrganizationListPage)));
			ToProjectListPage = new RelayCommand(() => _navigationService.NavigateToAndClearStack(nameof(ProjectListPage), Account));

			AppVersion = VersionHelper.GetAppVersionWithBuildNumber;
		}

		public ITaskNotifier<UserProfile> UserProfile
		{
			get => _userProfile;
			set => Set(() => UserProfile, ref _userProfile, value);
		}

		public ICommand ToProfilePage { get; }

		public ICommand ToAboutPage { get; }

		public ICommand ToOrganizationListPage { get; }

		public ICommand ToProjectListPage { get; }

		public ICommand Logout { get; }

		public string AppVersion { get; }

		public AccountData Account
		{
			get => _account;
			set => Set(() => Account, ref _account, value);
		}

		private async Task LogoutPrompt()
		{
#if __WASM__
			// MessageDialog not fully implemented yet for Uno/Wasm
			// https://github.com/nventive/Uno/issues/124

			await Task.Yield();

			const string js = "(confirm(\"Are you sure you want to logout ?\") ? \"Yes\" : \"No\")";
			var r = Uno.Foundation.WebAssemblyRuntime.InvokeJS(js);
			if (r == "Yes")
			{
				LogoutHandler(null);
			}
#else
			var messageDialog = new MessageDialog("Are you sure you want to logout ?");

			messageDialog.Commands.Add(new UICommand("Cancel", null));
			messageDialog.Commands.Add(new UICommand("Logout", new UICommandInvokedHandler(LogoutHandler)));

			messageDialog.CancelCommandIndex = 0;
			messageDialog.DefaultCommandIndex = 1;

			await messageDialog.ShowAsync();
#endif
		}

		private void LogoutHandler(IUICommand command)
		{
			_authenticationService.Logout();
		}
	}
}
