﻿using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using Votings.Common.Interfaces;
using Votings.Common.Models;
using Votings.Common.Services;

namespace Votings.Common.ViewModels
{
    public class VotingDetailCrossViewModel : MvxViewModel<NavigationArgs>
    {
        private readonly IApiService apiService;
        private readonly IDialogService dialogService;
        private readonly IMvxNavigationService navigationService;
        private VotingEvent votingEvent;

        public VotingDetailCrossViewModel(
            IApiService apiService,
            IDialogService dialogService,
            IMvxNavigationService navigationService)
        {
            this.apiService = apiService;
            this.dialogService = dialogService;
            this.navigationService = navigationService;
        }

        public override void Prepare(NavigationArgs parameter)
        {
            this.votingEvent = parameter.VotingEvent;

            //TODO: Search if user already voted, otherwise navigate to candidates
        }
    }
}