namespace Satori.Pages.ViewModels;

public class PositiveIntegerViewModel
{
    public string? TextInput { get; set; }
    public string? ValidationErrorMessage { get; set; }

    public bool TryParse(out int value)
    {
        if (int.TryParse(TextInput, out value) && value > 0)
        {
            return true;
        }
        ValidationErrorMessage = "Value must be positive integer";
        return false;
    }
}