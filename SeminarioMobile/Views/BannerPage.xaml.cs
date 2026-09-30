using SeminarioMobile.ViewModels;

namespace SeminarioMobile.Views;

public partial class BannerPage : ContentPage
{
    private readonly BannerViewModel _viewModel;

    public BannerPage(BannerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CarregarCommand.ExecuteAsync(null);
    }

    private void OnMenuClicked(object? sender, EventArgs e) =>
        Shell.Current.FlyoutIsPresented = true;

}
