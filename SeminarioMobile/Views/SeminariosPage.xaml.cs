using SeminarioMobile.ViewModels;

namespace SeminarioMobile.Views;

public partial class SeminariosPage : ContentPage
{
    private readonly SeminariosViewModel _viewModel;

    public SeminariosPage(SeminariosViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CarregarCommand.ExecuteAsync(null);
    }

    private async void OnFabClicked(object? sender, EventArgs e) =>
        await ScrollPrincipal.ScrollToAsync(0, 0, true);

    private void OnMenuClicked(object? sender, EventArgs e) =>
        Shell.Current.FlyoutIsPresented = true;

}
