using Microsoft.Maui.ApplicationModel;

namespace SeminarioMobile;

// Botão "Sair" fixo (canto superior direito) presente em todas as abas — fecha o app
// por completo (não apenas volta uma tela), após confirmação do usuário.
public static class SairHelper
{
    public static async Task ConfirmarESairAsync()
    {
        var confirmar = await Shell.Current.DisplayAlert("Sair", "Deseja realmente sair do aplicativo?", "Sim", "Não");
        if (!confirmar) return;

        Platform.CurrentActivity?.FinishAffinity();
    }
}
