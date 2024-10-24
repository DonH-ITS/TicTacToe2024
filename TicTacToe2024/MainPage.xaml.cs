using System.Text.Json;
using Microsoft.Maui.Controls.Shapes;
using Path = Microsoft.Maui.Controls.Shapes.Path;

namespace TicTacToe2024
{
    public partial class MainPage : ContentPage
    {
        private bool gridcreated = false;
        private int player = 1;
        private int numbrows;
        private int[,] positions;
        private string[] playerNames;
        private bool winner = false;
        private bool useShapes = false;
        private List<GameResult> gameResults;
        private Color bgColour = Colors.Red;
        private Color pl1Colour = Colors.Green;
        private Color pl2Colour = Colors.Blue;

        public MainPage() {
            InitializeComponent();
            InitialiseSettings();
            this.LayoutChanged += OnWindowChange;
           }

        private void OnWindowChange(object? sender, EventArgs e) {
            double maxWidth = this.Width - 20;
            /*double maxHeight = this.Height;
            maxHeight -= TopGrid.Height;
            maxHeight -= StartBtn.Height;
            maxHeight -= GridSizeStack.Height;
            maxHeight -= 10;*/
            double maxHeight = this.Height - (MainLayout.Height - GridPageContent.Height) - 5;
            if(maxWidth < maxHeight) {
                GridPageContent.HeightRequest = maxWidth;
                GridPageContent.WidthRequest = maxWidth;
            }
            else {
                GridPageContent.HeightRequest = maxHeight;
                GridPageContent.WidthRequest = maxHeight;
            }

            // If Xs or Os are drawn, they will not resize with the Grid. This code will now resize them
            double bHeight = 0;

            // First find out the size of a border
            foreach (var item in GridPageContent.Children) {
                if (item.GetType() == typeof(Border)) {
                    bHeight = ((Border)item).Height;
                    break;
                }
            }

            // Now resize the X Paths and the Ellipses
            foreach (var item in GridPageContent.Children) {
                if (item.GetType() == typeof(Path)) {
                    Path pth = (Path)item;
                    pth.Scale = bHeight / pth.Height;
                }
                else if (item.GetType() == typeof(Ellipse)) {
                    ((Ellipse)item).HeightRequest = bHeight;
                    ((Ellipse)item).WidthRequest = bHeight;
                }
            }
        }

        private void InitialiseSettings() {
            useShapes = Preferences.Default.Get("UseShapes", false);
            shapesSwitch.IsToggled = useShapes;


            // We only want to read one of json or CSV, I've commented out the CSV part
            /* if (File.Exists(System.IO.Path.Combine(FileSystem.Current.AppDataDirectory, "gameresults.csv"))){
                 gameResults = ReadResultsCSV("gameResults.csv");
             }*/
            if (File.Exists(System.IO.Path.Combine(FileSystem.Current.AppDataDirectory, "gameresults.csv"))) {
                gameResults = ReadResultsJSON("gameresults.json");
            }
            else {
                gameResults = new List<GameResult>();
            }
            playerNames = new string[2];
            playerNames[0] = Preferences.Default.Get("pl1", "");
            playerNames[1] = Preferences.Default.Get("pl2", "");
            GridSize.Text = Preferences.Default.Get("numbrows", 3).ToString();


        }

        private async void StartBtn_Clicked(object sender, EventArgs e) {
            // If Get names returns false, don't start the game
            if(!(await GetNames()))
                { return; }
            if (!gridcreated) {
                //First get the size of the grid from the box
                //Try Parse will try to Parse the entry box, if it fails numbrows will be assigned 0
                int.TryParse(GridSize.Text, out numbrows);
                //We don't want less than 3 rows in a grid and we don't want more than let's say 9
                if (numbrows <= 2) {
                    numbrows = 3;
                }
                else if (numbrows > 9)
                    numbrows = 9;
                GridSize.Text = numbrows + "";
                Preferences.Default.Set("numbrows", numbrows);
                positions = new int[numbrows, numbrows];
                //Disable the box for entering the grid size
                GridSize.IsEnabled = false;
                CreateTheGrid();
            }
            //If the grid has already been created, call the RestartGame method instead to reset the board
            else {
                RestartGame();
                //New Game might have different number of rows, will require calling CreateTheGrid again if this is the case
                if (int.TryParse(GridSize.Text, out int newrows)) {
                    if(newrows != numbrows) {
                        numbrows = newrows;
                        if (numbrows <= 2) {
                            numbrows = 3;
                        }
                        else if (numbrows > 9)
                            numbrows = 9;
                        GridSize.Text = numbrows + "";
                        Preferences.Default.Set("numbrows", numbrows);
                        positions = new int[numbrows, numbrows];
                        ResetTheGrid();
                        CreateTheGrid();
                    }
                }
                GridSize.IsEnabled = false; 
            }
        }

        // This method is so that on a new game, if the number of rows has changed, we need to clear out the borders and rows/columns already there
        // Then we can call CreateTheGrid again with the new numbrows
        private void ResetTheGrid() {
            GridPageContent.Clear();
            GridPageContent.ColumnDefinitions.Clear();
            GridPageContent.RowDefinitions.Clear();
        }

        // I decided to put the reading names into their own method, if this method successfully reads the names it returns true, false otherwise
        // The Task<bool> is because this method is async, if you have an async method that returns say an int it would be async Task<int>
        // I also save the playerNames in my preferences setting
        private async Task<bool> GetNames() {
            for (int i = 0; i < 2; i++) {
                playerNames[i] = await DisplayPromptAsync("Starting Game", "What is Player "+ (i+1) + "'s name?", initialValue: playerNames[i]);
                if (playerNames[i] == null || playerNames[i].Length <= 0) {
                    await DisplayAlert("Starting Game", "Error Need Names", "OK");
                    return false;
                }
            }
            Preferences.Default.Set("pl1", playerNames[0]);
            Preferences.Default.Set("pl2", playerNames[1]);
            return true;
        }

        private void RestartGame() {
            //We need to reset a bunch of variables, there is no winner so set it to false
            //Reset so Player 1 goes first
            //Reset the text to give feedback
            winner = false;
            player = 1;
            whichplayerlabel.Text = playerNames[player-1]+"'s Turn";
            //All entries in positions have to be reset to 0
            for (int i = 0; i < numbrows; i++) {
                for (int j = 0; j < numbrows; j++) {
                    positions[i, j] = 0;
                }
            }

            //When starting a new game, we need to remove all the X's and O's from the board
            //We prepare a list to store the children ready for deletion
            //Search all the children of GridPageContent, if the child is of type Path or Ellipse we enter it into the list
            List<View> childrenToRemove = new();
            foreach (var item in GridPageContent.Children) {
                if (item.GetType() == typeof(Path)) {
                    childrenToRemove.Add((Path)item);
                }
                else if (item.GetType() == typeof(Ellipse)) {
                    childrenToRemove.Add((Ellipse)item);
                }
            }

            //Actually remove them from the Grid
            foreach (var item in childrenToRemove) {
                GridPageContent.Remove(item);
            }

            //This section of code is how to reset the colours of the grid if you didn't want to draw X's and O's
           
            foreach (var item in GridPageContent.Children)
            {
                if(item.GetType() == typeof(Border)) {
                    Border border = (Border)item;
                    border.BackgroundColor = bgColour;
                }
            }
            
            StartBtn.IsEnabled = false;
        }

        private void CreateTheGrid() {
            //Create numbrows rows and numbrows columns 3x3, 4x4 etc.
            for (int i = 0; i < numbrows; ++i) {
                GridPageContent.AddRowDefinition(new RowDefinition());
                GridPageContent.AddColumnDefinition(new ColumnDefinition());
            }

            //Populate the grid with Borders
            for (int i = 0; i < numbrows; ++i) {
                for (int j = 0; j < numbrows; ++j) {
                    Border styledBorder = new Border
                    {
                        BackgroundColor = bgColour, // Set the background color
                        Stroke = Colors.Black,
                        StrokeThickness = 3

                    };
                    TapGestureRecognizer tapGestureRecognizer = new TapGestureRecognizer();
                    tapGestureRecognizer.Tapped += OnBorderTapped;
                    styledBorder.GestureRecognizers.Add(tapGestureRecognizer);
                    GridPageContent.Add(styledBorder, j, i);
                }

            }
            //Make the Text say it is player 1's turn
            whichplayerlabel.Text = playerNames[player-1] + "'s Turn";
            gridcreated = true;
            //Disable the start button
            StartBtn.IsEnabled = false;
        }

        private void OnBorderTapped(object sender, TappedEventArgs e) {
            Border border = (Border)sender;
            if (border != null) {
                DoMove(border);
            }
        }


        /* Taking out BtnMove_Clicked as clicking on the squares is better anyway
        private void BtnMove_Clicked(object sender, EventArgs e) {
            int row, column;
            //Try Parse is another way to convert from string to integer, it checks whether the parse can work first instead of just crashing
            //int.TryParse(string, out) is the form of it
            //If it can parse it returns true and assigns the integer to the output variable
            //If it cannot parse it returns false and assigns 0 to the output variable
            if (!int.TryParse(EntryC.Text, out column) || !int.TryParse(EntryR.Text, out row)) {
                //If either entry cannot be parsed, we exit out of the method by just using return;
                //No feedback will be given to the user
                return;
            }
            //We need to subtract one from each of column and row if it has got this far
            --column;
            --row;

            //Make sure we are within the limits of the grid
            if (column > numbrows || column < 0 || row > numbrows || row < 0)
                return;

            //We are going to do a loop over all the Children of the grid finding all the objects that are there, looking for a match
            foreach (var item in GridPageContent.Children) {
                //We only want to search Borders, so ignore all other types of items
                if (item.GetType() == typeof(Border)) {
                    //Cast the object to type Frame so we can use all the Frame attributes and methods
                    Border border = (Border)item;

                    //Search for a match, if we find one, do the move and exit out of the loop with break
                    if (column == Convert.ToInt32(border.GetValue(Grid.ColumnProperty).ToString()) && row == Convert.ToInt32(border.GetValue(Grid.RowProperty).ToString())) {
                        DoMove(border);
                        break;
                    }
                }
            }
        }
        */

        private void FinishGame(int which) {
            if (which != 3) {
                whichplayerlabel.Text = playerNames[which-1] + " wins";
            }
            else {
                whichplayerlabel.Text = "It's a Draw";
            }
            //Set winner to be true to prevent any more moves
            winner = true;
            //Enable the start game button so we can reset the board
            StartBtn.IsEnabled = true;

            // At the end of a game, add the result to an array and write the results file
            // This could easily get too big, so maybe a feature in the future is limit it to only 10/20 games or something
            gameResults.Add(new GameResult(playerNames[0], playerNames[1], which));
                       
            //WriteResultsCSV(gameResults, "gameresults.csv");
            WriteResultsJSON(gameResults, "gameresults.json");

            //Reenable the box for changing the GridSize
            GridSize.IsEnabled = true;
        }

        private static void WriteResultsCSV(List<GameResult> results, string fileName) {
            string targetFile = System.IO.Path.Combine(FileSystem.Current.AppDataDirectory, fileName);
            using FileStream outputStream = File.OpenWrite(targetFile);
            using StreamWriter streamWriter = new StreamWriter(outputStream);
            foreach (var entry in results) {
                streamWriter.WriteLine(entry.Player1Name + "," + entry.Player2Name + "," + entry.Result);
            }
        }

        private static List<GameResult> ReadResultsCSV(string fileName) {
            string targetFile = System.IO.Path.Combine(FileSystem.Current.AppDataDirectory, fileName);
            List<GameResult> results = new List<GameResult>();
            if(File.Exists(targetFile)){
                using FileStream outputStream = File.OpenRead(targetFile);
                using StreamReader streamReader = new StreamReader(outputStream);
                while (!streamReader.EndOfStream) {
                    string line = streamReader.ReadLine();
                    //Split by the delimiter ,
                    string[] splits = line.Split(',');
                    results.Add(new GameResult(splits[0], splits[1], Convert.ToInt32(splits[2])));
                }
            }
            return results;
        }

        private static void WriteResultsJSON(List<GameResult> results, string fileName) {
            string jsonarray = JsonSerializer.Serialize(results);
            string targetFile = System.IO.Path.Combine(FileSystem.Current.AppDataDirectory, fileName);
            using FileStream outputStream = File.OpenWrite(targetFile);
            using StreamWriter streamWriter = new StreamWriter(outputStream);
            streamWriter.Write(jsonarray);
        }

        private static List<GameResult> ReadResultsJSON(string fileName) {
            string targetFile = System.IO.Path.Combine(FileSystem.Current.AppDataDirectory, fileName);
            List<GameResult> results;
            if (File.Exists(targetFile)) {
                using FileStream outputStream = File.OpenRead(targetFile);
                using StreamReader streamReader = new StreamReader(outputStream);
                string jsonstring = streamReader.ReadToEnd();
                results = JsonSerializer.Deserialize<List<GameResult>>(jsonstring);
            }
            else {
                results = new List<GameResult>();
            }

            return results;
        }

        private void DoMove(Border border) {
            //if winner is blocking DoMove from running if winner is set to true
            if (winner)
                return;

            int column = Convert.ToInt32(border.GetValue(Grid.ColumnProperty).ToString());
            int row = Convert.ToInt32(border.GetValue(Grid.RowProperty).ToString());
            if (positions[row, column] == 0) {
                positions[row, column] = player;
                double height = border.Height;
                int result = CheckWinner(player);
                bool update = true;
                if (result == player || result == 3) {
                    FinishGame(result);
                    update = false;
                }

                //Draw Cross's (X's) for player 1, remembering to change player after the cross is drawn
                if (player == 1) {

                    if (useShapes) {
                        Path cross = UsefulMethods.MakeCrossUsingPath(height, 6, Color.FromRgb(0, 0, 0));
                        GridPageContent.Add(cross, column, row);
                    }
                    else {
                        border.BackgroundColor = pl1Colour;
                    }
                    player = 2;
                }
                //Draw an ellipse for player 2
                else {
                    if (useShapes) {
                        Ellipse ell = UsefulMethods.DrawEllipse(height);
                        GridPageContent.Add(ell, column, row);
                    }
                    else {
                        border.BackgroundColor = pl2Colour;
                    }
                    player = 1;
                }
                //Only update the player label text if there has not been a winner or a draw
                if (update) whichplayerlabel.Text = playerNames[player-1] + "'s Turn";
            }

        }

        private int CheckWinner(int player) {
            //If a row, column or diagonal is complete, we return the player number to indicate they have won
            if (UsefulMethods.SearchRowsComplete(positions, numbrows, player))
                return player;
            if (UsefulMethods.SearchColsComplete(positions, numbrows, player))
                return player;
            if (UsefulMethods.SearchDiagonalComplete(positions, numbrows, player))
                return player;
            //Check if Draw and if it is a draw return 3
            if (!UsefulMethods.FindinArray(positions, numbrows, 0))
                return 3;
            //If game can continue return 0
            return 0;
        }

        private void coloursorXSwitch_Toggled(object sender, ToggledEventArgs e) {
            useShapes = ((Switch)sender).IsToggled;
            Preferences.Default.Set("UseShapes", useShapes);

            // If some things have already been drawn, change to the other display on a Toggled event
            ChangeShapesColours();
        }

        private void ChangeShapesColours() {
            if (!useShapes) {
                //First Remove all the Shapes
                List<View> childrenToRemove = new();
                foreach (var item in GridPageContent.Children) {
                    if (item.GetType() == typeof(Path)) {
                        childrenToRemove.Add((Path)item);
                    }
                    else if (item.GetType() == typeof(Ellipse)) {
                        childrenToRemove.Add((Ellipse)item);
                    }
                }

                foreach (var item in childrenToRemove) {
                    GridPageContent.Remove(item);
                }
                //Now Colour the borders in depending on positions array
                for (int i = 0; i < numbrows; i++) {
                    for (int j = 0; j < numbrows; j++) {
                        if (positions[i, j] == 1) {
                            FillIn(i, j, pl1Colour);
                        }
                        else if (positions[i, j] == 2) {
                            FillIn(i, j, pl2Colour);
                        }
                    }
                }
            }
            else {
                double height=0;
                //Set all the border backgrounds to default
                foreach (var item in GridPageContent.Children) {
                    if (item.GetType() == typeof(Border)) {
                        ((Border)item).BackgroundColor = bgColour;
                        height = ((Border)item).Height;
                    }
                }
                //Add the Ellipse or Path
                for (int i = 0; i < numbrows; i++) {
                    for (int j = 0; j < numbrows; j++) {
                        if(positions[i,j] != 0)
                            GridPageContent.Add(positions[i,j] == 1 ? UsefulMethods.MakeCrossUsingPath(height, 6, Color.FromRgb(0, 0, 0)) : UsefulMethods.DrawEllipse(height), j, i);
                    }
                }
            }

        }

        private void FillIn(int row, int column, Color colour) {
            foreach (var item in GridPageContent.Children) {
                if (item.GetType() == typeof(Border)) {
                    Border border = (Border)item;
                    if (column == Convert.ToInt32(border.GetValue(Grid.ColumnProperty).ToString()) && row == Convert.ToInt32(border.GetValue(Grid.RowProperty).ToString())) {
                        border.BackgroundColor = colour;
                        break;
                    }
                }
            }
        }

        private async void ToolbarItem_Clicked(object sender, EventArgs e) {
            /*
             * string allResults = "";
            foreach(var result in gameResults) {
                allResults += result + "\n";
            }
            await DisplayAlert("Game Results", allResults, "OK");
            */
            await Navigation.PushAsync(new ResultsPage(gameResults));
        }
    }

}
