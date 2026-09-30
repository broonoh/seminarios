namespace SeminarioMobile;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
	}

	private async void OnSairTapped(object? sender, EventArgs e) =>
		await SairHelper.ConfirmarESairAsync();
}
