namespace FiresideEditorial.Services;

public class SearchState
{
    public bool IsOpen { get; private set; }
    public event Action? OnChange;

    public void Open() { IsOpen = true; OnChange?.Invoke(); }
    public void Close() { IsOpen = false; OnChange?.Invoke(); }
    public void Toggle() { IsOpen = !IsOpen; OnChange?.Invoke(); }
}
