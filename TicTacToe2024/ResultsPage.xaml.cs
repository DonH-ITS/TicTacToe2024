namespace TicTacToe2024;

public partial class ResultsPage : ContentPage
{
	public ResultsPage(List<GameResult> results)
	{
		InitializeComponent();
		foreach (var entry in results) {
			Label label = new Label();
			label.Text = entry.ToString();
			LayoutStack.Add(label);
		}
	}
}