﻿using Microsoft.NetConf2021.Maui.Resources.Strings;

namespace Microsoft.NetConf2021.Maui.ViewModels;

public class DiscoverViewModel : BaseViewModel
{
    private readonly ShowsService showsService;
    private readonly SubscriptionsService subscriptionsService;
    private IEnumerable<ShowViewModel> shows;
    private CategoriesViewModel categoriesVM;
    private string text;

    public ObservableRangeCollection<ShowGroup> PodcastsGroup { get; private set; } = new ObservableRangeCollection<ShowGroup>();
    public ObservableRangeCollection<ShowViewModel> TemporalPodcastsGroup { get; private set; } = new ObservableRangeCollection<ShowViewModel>();

    public ICommand SearchCommand { get; }

    public ICommand SubscribeCommand => new AsyncCommand<ShowViewModel>(SubscribeCommandExecute);

    public ICommand SeeAllCategoriesCommand => new AsyncCommand(SeeAllCategoriesCommandExecute);

    public string Text
    {
        get { return text; }
        set 
        {
            SetProperty(ref text, value);
        }
    }  

    public CategoriesViewModel CategoriesVM
    {
        get { return categoriesVM; }      
        set {  SetProperty(ref categoriesVM, value); }
    }

    public DiscoverViewModel(ShowsService shows, SubscriptionsService subs, CategoriesViewModel categories)
    {
        showsService = shows;
        subscriptionsService = subs;

        SearchCommand = new AsyncCommand(OnSearchCommandAsync);
        categoriesVM = categories;
    }

    internal async Task InitializeAsync()
    {
        await FetchAsync();
    }

    private async Task FetchAsync()
    {
        var podcastsModels = await showsService.GetShowsAsync();

        if (podcastsModels == null)
        {
            await Shell.Current.DisplayAlert(
                AppResource.Error_Title,
                AppResource.Error_Message,
                AppResource.Close);

            return;
        }

        await CategoriesVM.InitializeAsync();
        shows = await ConvertToViewModels(podcastsModels);
        UpdatePodcasts(shows);
    }

    private async Task<List<ShowViewModel>> ConvertToViewModels(IEnumerable<Show> podcasts)
    {
        var viewmodels = new List<ShowViewModel>();
        foreach (var podcast in podcasts)
        {
            var podcastViewModel = new ShowViewModel(podcast, subscriptionsService);
            await podcastViewModel.InitializeAsync();
            viewmodels.Add(podcastViewModel);
        }

        return viewmodels;
    }

    private void UpdatePodcasts(IEnumerable<ShowViewModel> listPodcasts)
    {
        var groupedShows = listPodcasts
            .GroupBy(podcasts => podcasts.Show.IsFeatured)
            .Where(group => group.Any())
            .ToDictionary(group => group.Key ? AppResource.Whats_New : AppResource.Specially_For_You, group => group.ToList())
            .Select(dictionary => new ShowGroup(dictionary.Key, dictionary.Value));

        PodcastsGroup.ReplaceRange(groupedShows);
        TemporalPodcastsGroup.ReplaceRange(listPodcasts);

    }

    private async Task OnSearchCommandAsync()
    {
        IEnumerable<Show> list;
        if (string.IsNullOrWhiteSpace(Text))
        {
            list = await showsService.GetShowsAsync();
        }
        else
        {
            list = await showsService.SearchShowsAsync(Text);
        }

        if (list != null)
        {
            UpdatePodcasts(await ConvertToViewModels(list));
        }
    }

    private async Task SubscribeCommandExecute(ShowViewModel vm)
    {
        await subscriptionsService.UnSubscribeFromShowAsync(vm.Show);
        vm.IsSubscribed = subscriptionsService.IsSubscribed(vm.Show.Id);
    }

    private Task SeeAllCategoriesCommandExecute()
    {
        return Shell.Current.GoToAsync($"{nameof(CategoriesPage)}");
    }
}
