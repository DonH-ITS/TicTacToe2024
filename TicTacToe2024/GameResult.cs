namespace TicTacToe2024
{
    public class GameResult
    {
        public string Player1Name { get; set; }
        public string Player2Name { get; set; }
        public int Result { get; set; }

        public GameResult(string player1, string player2, int result) {
            Player1Name = player1;
            Player2Name = player2;
            Result = result;
        }

        public GameResult() {

        }

        public override string ToString() {
            if (Result == 1) {
                return Player1Name + " beat " + Player2Name;
            }
            else if (Result == 2) {
                return Player2Name + " beat " + Player1Name;
            }
            else {
                return Player1Name + " and " + Player2Name + " drew";
            }
        }
    }
}
