using Network.Types;

static class GameStateExtensions
{
    public static bool GameHasEnded(this GameState gameState) => gameState == GameState.Expired || gameState == GameState.Finished;
}
